using Microsoft.Data.Sqlite;
using SmartSnippets.Core.Data;
using SmartSnippets.Core.Domain;
using SmartSnippets.Core.Store;

namespace SmartSnippets.Core.Tests.Data;

public class PasswordSnippetPersistenceTests
{
    private static SnippetStore Open(TempDatabase db, TestSecretProtector? protector = null)
    {
        new Migrator(db.Factory).MigrateToLatest();
        var store = new SnippetStore(db.Factory, TimeProvider.System, protector ?? new TestSecretProtector());
        store.Load();
        return store;
    }

    private static SnippetDraft Password(string text = "PRIVATE-BODY") => new(text, "manual", true, null, "", true);

    [Fact]
    public void CiphertextIsBoundInsteadOfPlaintext()
    {
        using var db = new TempDatabase();
        var store = Open(db);
        // A trigger inspects the bound value, rather than just the final stored row.
        db.Exec("CREATE TRIGGER require_cipher BEFORE INSERT ON snippets WHEN NEW.is_password=1 AND NEW.text NOT LIKE 'dpapi:v1:%' BEGIN SELECT RAISE(ABORT, 'not protected'); END;");
        db.Exec("CREATE TRIGGER require_cipher_update BEFORE UPDATE OF text ON snippets WHEN NEW.is_password=1 AND NEW.text NOT LIKE 'dpapi:v1:%' BEGIN SELECT RAISE(ABORT, 'not protected'); END;");
        var row = store.CreateSnippet(Password()).Value!;
        Assert.Equal("PRIVATE-BODY", row.Text);
        var cipher = Assert.IsType<string>(db.Scalar("SELECT text FROM snippets"));
        Assert.StartsWith("dpapi:v1:", cipher);
        Assert.DoesNotContain(row.Text, cipher);
        store.UpdateSnippet(row.Id, Password("OTHER-PRIVATE-BODY"));
        Assert.NotEqual(cipher, db.Scalar("SELECT text FROM snippets"));
        store.Load();
        Assert.Equal("OTHER-PRIVATE-BODY", Assert.Single(store.Snippets).Snippet.Text);
        Assert.Equal(1L, db.Scalar("PRAGMA secure_delete"));
    }

    [Fact]
    public void ProtectorFailureDoesNotCommitOrPublish()
    {
        using var db = new TempDatabase();
        var protector = new TestSecretProtector();
        var store = Open(db, protector);
        var row = store.CreateSnippet(Password()).Value!;
        var cipher = db.Scalar("SELECT text FROM snippets");
        var snapshot = store.Snippets;
        var events = 0;
        store.Changed += (_, _) => events++;
        protector.Fail = true;
        foreach (var write in new Action[] { () => store.CreateSnippet(Password()), () => store.UpdateSnippet(row.Id, Password("new secret")) })
        {
            var error = Assert.Throws<StoreException>(write);
            Assert.Equal(StoreFailure.SecretProtection, error.Failure);
            Assert.Null(error.InnerException);
        }
        Assert.Equal(1L, db.Scalar("SELECT count(*) FROM snippets"));
        Assert.Equal(cipher, db.Scalar("SELECT text FROM snippets"));
        Assert.Same(snapshot, store.Snippets);
        Assert.Equal(0, events);
    }

    [Theory]
    [InlineData("ordinary plaintext")]
    [InlineData("dpapi:v2:YWJj")]
    [InlineData("dpapi:v1:")]
    [InlineData("dpapi:v1:   ")]
    [InlineData("dpapi:v1:!!")]
    [InlineData("dpapi:v1:YWJj")]
    public void BadEnvelopeNeverFallsBackToText(string envelope)
    {
        using var db = new TempDatabase();
        var store = Open(db);
        using (var connection = db.Factory.Open())
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "INSERT INTO snippets (text, label, created_at, updated_at, is_password) VALUES ($text, 'manual', 1, 1, 1)";
            command.Parameters.AddWithValue("$text", envelope);
            command.ExecuteNonQuery();
        }
        Assert.Equal(StoreFailure.SecretProtection, Assert.Throws<StoreException>(store.Load).Failure);
        Assert.Equal(envelope, db.Scalar("SELECT text FROM snippets"));
        Assert.Empty(store.Snippets);
    }

    [Fact]
    public void OddDecryptedByteLengthFailsClosed()
    {
        using var db = new TempDatabase();
        var protector = new TestSecretProtector();
        var store = Open(db, protector);
        store.CreateSnippet(Password());
        var snapshot = store.Snippets;
        protector.OddPlaintext = true;
        Assert.Equal(StoreFailure.SecretProtection, Assert.Throws<StoreException>(store.Load).Failure);
        Assert.Same(snapshot, store.Snippets);
    }

    [Fact]
    public void MissingProtectorFailsClosed()
    {
        using var db = new TempDatabase();
        var protectedStore = Open(db);
        var legacy = new SnippetStore(db.Factory, TimeProvider.System);
        legacy.Load();
        Assert.Equal(StoreFailure.SecretProtection, Assert.Throws<StoreException>(() => legacy.CreateSnippet(Password())).Failure);
        protectedStore.CreateSnippet(Password());
        Assert.Equal(StoreFailure.SecretProtection, Assert.Throws<StoreException>(legacy.Load).Failure);
        Assert.Throws<SmartSnippets.Core.Security.SecretProtectionException>(() => new SnippetRepository(db.Factory).LoadAll());
        Assert.Empty(legacy.Snippets);
    }

    [Fact]
    public void FailedReloadDoesNotPublishPartialState()
    {
        using var db = new TempDatabase();
        var store = Open(db);
        store.CreateSnippet(new("ordinary", "manual", true, null, ""));
        store.CreateSnippet(Password());
        var snapshot = store.Snippets;
        var folders = store.Folders;
        var events = 0;
        store.Changed += (_, _) => events++;
        db.Exec("UPDATE snippets SET text='broken' WHERE is_password=1");
        db.Exec("INSERT INTO folders (name, name_key, created_at, updated_at) VALUES ('new', 'new', 1, 1)");
        Assert.Throws<StoreException>(store.Load);
        Assert.Same(snapshot, store.Snippets);
        Assert.Same(folders, store.Folders);
        Assert.Equal(0, events);
        Assert.Equal("broken", db.Scalar("SELECT text FROM snippets WHERE is_password=1"));
    }

    [Fact]
    public void RawCodeUnitsRoundTripAndOrdinaryPrefixIsNotDecoded()
    {
        using var db = new TempDatabase();
        var store = Open(db);
        var text = " \t\r\nUnicode \u03a9 \ud83d\ude03 \ud800 \udfff\0 ";
        store.CreateSnippet(Password(text));
        store.CreateSnippet(new("dpapi:v1:not an envelope", "literal", true, null, ""));
        store.Load();
        Assert.Equal(text, store.Snippets.Single(s => s.Snippet.IsPassword).Snippet.Text);
        Assert.Equal("dpapi:v1:not an envelope", store.Snippets.Single(s => !s.Snippet.IsPassword).Snippet.Text);
    }

    [Fact]
    public void MetadataUsageAndImportDoNotReencodeOrDowngrade()
    {
        using var db = new TempDatabase();
        var protector = new TestSecretProtector();
        var store = Open(db, protector);
        var folder = store.CreateFolder("folder").Value!;
        var row = store.CreateSnippet(Password() with { FolderId = folder.Id }).Value!;
        var cipher = db.Scalar("SELECT text FROM snippets");
        protector.Fail = true;
        store.RecordUsage(row.Id);
        store.RenameFolder(folder.Id, "renamed");
        store.DeleteFolder(folder.Id);
        var imported = store.ImportSnippets(new([new(0, row.Text, "import label", null, [], null)], [], SmartSnippets.Core.Porting.DedupeMode.TextOnly));
        Assert.Equal(1, imported.SkippedDuplicates);
        Assert.Empty(imported.SnippetIds);
        Assert.Equal(cipher, db.Scalar("SELECT text FROM snippets"));
        Assert.Equal(1L, db.Scalar("SELECT is_password FROM snippets"));
        protector.Fail = false;
        store.Load();
        Assert.Equal(row.Text, Assert.Single(store.Snippets).Snippet.Text);
        Assert.Equal(1, Assert.Single(store.Snippets).Snippet.UsageCount);
    }

    [Fact]
    public void ExistingRowsRemainOrdinary()
    {
        using var db = new TempDatabase();
        new Migrator(db.Factory, Migrations.All.Take(2).ToList()).MigrateToLatest();
        db.Exec("INSERT INTO snippets (text, label, label_is_custom, tags, created_at, updated_at, last_used_at, usage_count) VALUES (' exact body ', 'manual', 1, 'tag', 100, 200, 300, 4)");
        Assert.Equal(3, new Migrator(db.Factory).MigrateToLatest());
        Assert.Equal(3L, db.Scalar("SELECT version FROM schema_version"));
        Assert.Equal(0L, db.Scalar("SELECT is_password FROM snippets"));
        var row = Assert.Single(new SnippetRepository(db.Factory).LoadAll());
        Assert.False(row.IsPassword);
        Assert.Equal(" exact body ", row.Text);
        Assert.Equal("manual", row.Label);
        Assert.True(row.IsLabelCustom);
        Assert.Null(row.FolderId);
        Assert.Equal("tag", Assert.Single(row.Tags));
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(100), row.CreatedAt);
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(200), row.UpdatedAt);
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(300), row.LastUsedAt);
        Assert.Equal(4, row.UsageCount);
        Assert.True(File.Exists(db.Factory.DatabasePath + ".v2.bak"));
    }

    [Fact]
    public void FlagRoundTripsThroughCreateUpdateAndReload()
    {
        using var db = new TempDatabase();
        new Migrator(db.Factory).MigrateToLatest();
        var store = new SnippetStore(db.Factory, TimeProvider.System, new TestSecretProtector());
        store.Load();
        var draft = new SnippetDraft("synthetic body", "manual", true, null, "tag", true);
        var created = store.CreateSnippet(draft).Value!;
        Assert.True(created.IsPassword);
        Assert.True(Assert.Single(store.Snippets).Snippet.IsPassword);
        Assert.NotEqual(created, created with { IsPassword = false });
        store.Load();
        Assert.True(Assert.Single(store.Snippets).Snippet.IsPassword);
        Assert.Equal(draft.Text, Assert.Single(store.Snippets).Snippet.Text);
        foreach (var flag in new[] { false, true })
        {
            Assert.Equal(flag, store.UpdateSnippet(created.Id, draft with { IsPassword = flag }).Value!.IsPassword);
            var reloaded = new SnippetStore(db.Factory, TimeProvider.System, new TestSecretProtector());
            reloaded.Load();
            Assert.Equal(flag, Assert.Single(reloaded.Snippets).Snippet.IsPassword);
            Assert.Equal(flag ? 1L : 0L, db.Scalar("SELECT is_password FROM snippets"));
        }
    }

    [Theory]
    [InlineData("", true)]
    [InlineData(" \t", true)]
    [InlineData("manual", false)]
    [InlineData("", false)]
    public void DirectStoreCallsRejectMissingOrAutomaticLabels(string label, bool custom)
    {
        using var db = new TempDatabase();
        new Migrator(db.Factory).MigrateToLatest();
        var store = new SnippetStore(db.Factory, TimeProvider.System);
        store.Load();
        var existing = store.CreateSnippet(new("original", "manual", true, null, "")).Value!;
        var snapshot = store.Snippets;
        var events = 0;
        store.Changed += (_, _) => events++;
        var draft = new SnippetDraft("synthetic body", label, custom, null, "tag", true);
        Assert.Equal(ValidationError.LabelRequired, store.CreateSnippet(draft).Error);
        Assert.Equal(ValidationError.LabelRequired, store.UpdateSnippet(existing.Id, draft).Error);
        Assert.Same(snapshot, store.Snippets);
        Assert.Equal(0, events);
        store.Load();
        Assert.Equal("original", Assert.Single(store.Snippets).Snippet.Text);
        Assert.False(Assert.Single(store.Snippets).Snippet.IsPassword);
    }

    [Theory]
    [InlineData("NULL")]
    [InlineData("2")]
    [InlineData("-1")]
    public void FlagConstraintRejectsInvalidValues(string value)
    {
        using var db = new TempDatabase();
        new Migrator(db.Factory).MigrateToLatest();
        Assert.Throws<SqliteException>(() => db.Exec(
            $"INSERT INTO snippets (text, label, created_at, updated_at, is_password) VALUES ('body', 'label', 1, 1, {value})"));
    }
}
