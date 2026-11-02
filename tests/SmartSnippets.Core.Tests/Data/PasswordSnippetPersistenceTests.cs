using Microsoft.Data.Sqlite;
using SmartSnippets.Core.Data;
using SmartSnippets.Core.Domain;
using SmartSnippets.Core.Store;

namespace SmartSnippets.Core.Tests.Data;

public class PasswordSnippetPersistenceTests
{
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
        var store = new SnippetStore(db.Factory, TimeProvider.System);
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
            var reloaded = new SnippetStore(db.Factory, TimeProvider.System);
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
