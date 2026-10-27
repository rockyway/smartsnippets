using Microsoft.Data.Sqlite;
using SmartSnippets.Core.Data;
using SmartSnippets.Core.Domain;
using SmartSnippets.Core.Query;
using SmartSnippets.Core.Store;
using SmartSnippets.Core.Tests.Data;

namespace SmartSnippets.Core.Tests.Store;

public class SnippetStoreTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private sealed class FakeTime : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = T0;

        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class Env : IDisposable
    {
        public Env(TimeSpan? busyTimeout = null)
        {
            Db = new TempDatabase(busyTimeout);
            new Migrator(Db.Factory).MigrateToLatest();
            Store = new SnippetStore(Db.Factory, Time);
            Store.Changed += (_, e) => Events.Add(e);
            Store.Load();
            Events.Clear();
        }

        public TempDatabase Db { get; }

        public FakeTime Time { get; } = new();

        public SnippetStore Store { get; }

        public List<StoreChangedEventArgs> Events { get; } = [];

        public SnippetStore NewStore()
        {
            var store = new SnippetStore(Db.Factory, Time);
            store.Load();
            return store;
        }

        public void Dispose() => Db.Dispose();
    }

    private static SnippetDraft Draft(string text, string tags = "", string label = "", bool custom = false, long? folder = null) =>
        new(text, label, custom, folder, tags);

    [Fact]
    public void Create_RaisesChangedOnce_SnapshotContainsNew()
    {
        using var env = new Env();
        env.Time.Now = T0.AddMinutes(5);

        var result = env.Store.CreateSnippet(Draft("hello world", "a, b"));

        Assert.True(result.Success);
        Assert.Equal("", result.Message);
        var e = Assert.Single(env.Events);
        Assert.Equal(StoreChangeKind.SnippetCreated, e.Kind);
        Assert.Equal(result.Value!.Id, e.Id);
        Assert.Equal([result.Value.Id], e.SnippetIds);
        var view = Assert.Single(env.Store.Snippets);
        Assert.Equal("hello world", view.Snippet.Text);
        Assert.Equal(["a", "b"], view.Snippet.Tags);
        Assert.Null(view.FolderName);
        Assert.Equal(T0.AddMinutes(5), view.Snippet.CreatedAt);
        Assert.Equal(T0.AddMinutes(5), view.Snippet.UpdatedAt);
        Assert.Equal(0, view.Snippet.UsageCount);
    }

    [Fact]
    public void Create_AutoLabel_FromTagThenText()
    {
        using var env = new Env();

        var withTag = env.Store.CreateSnippet(Draft("body line\nsecond", "  greet , hi")).Value!;
        var noTag = env.Store.CreateSnippet(Draft("\n  first real line \nsecond")).Value!;

        Assert.Equal("greet", withTag.Label);
        Assert.False(withTag.IsLabelCustom);
        Assert.Equal("first real line", noTag.Label);
    }

    [Fact]
    public void CustomLabel_NotOverwrittenByTextOrTagEdits()
    {
        using var env = new Env();
        var id = env.Store.CreateSnippet(Draft("text", "t1", "My label", custom: true)).Value!.Id;

        var updated = env.Store.UpdateSnippet(id, Draft("other text", "t2, t3", "My label", custom: true)).Value!;

        Assert.Equal("My label", updated.Label);
        Assert.True(updated.IsLabelCustom);
        Assert.Equal("My label", env.Store.Snippets[0].Snippet.Label);
    }

    [Fact]
    public void ClearedCustomLabel_ReturnsToAuto()
    {
        using var env = new Env();
        var id = env.Store.CreateSnippet(Draft("text", "", "Custom", custom: true)).Value!.Id;

        var updated = env.Store.UpdateSnippet(id, Draft("some text", "", "   ", custom: true)).Value!;

        Assert.Equal("some text", updated.Label);
        Assert.False(updated.IsLabelCustom);
    }

    [Fact]
    public void ValidationFailure_NoEvent_CacheUnchanged()
    {
        using var env = new Env();
        var keep = env.Store.CreateSnippet(Draft("keep")).Value!;
        env.Events.Clear();
        var before = env.Store.Snippets;

        var blank = env.Store.CreateSnippet(Draft("   \n "));
        var badFolder = env.Store.CreateSnippet(Draft("x", folder: 999));
        var badUpdate = env.Store.UpdateSnippet(keep.Id, Draft(" "));
        var missing = env.Store.UpdateSnippet(12345, Draft("x"));

        Assert.Equal(ValidationError.TextRequired, blank.Error);
        Assert.Equal(ValidationError.TextRequired.ToMessage(), blank.Message);
        Assert.Null(blank.Value);
        Assert.Equal(ValidationError.FolderNotFound, badFolder.Error);
        Assert.Equal(ValidationError.TextRequired, badUpdate.Error);
        Assert.Equal(ValidationError.SnippetNotFound, missing.Error);
        Assert.Empty(env.Events);
        Assert.Same(before, env.Store.Snippets);
        Assert.Equal("keep", env.Store.Snippets[0].Snippet.Text);
        Assert.Equal(1, Convert.ToInt32(env.Db.Scalar("SELECT COUNT(*) FROM snippets")));
    }

    [Fact]
    public void Update_KeepsCreatedAndUsage_BumpsUpdatedAt()
    {
        using var env = new Env();
        var id = env.Store.CreateSnippet(Draft("a")).Value!.Id;
        env.Time.Now = T0.AddHours(1);
        env.Store.RecordUsage(id);
        env.Time.Now = T0.AddHours(2);
        env.Events.Clear();

        var updated = env.Store.UpdateSnippet(id, Draft("b")).Value!;

        Assert.Equal(T0, updated.CreatedAt);
        Assert.Equal(T0.AddHours(2), updated.UpdatedAt);
        Assert.Equal(1, updated.UsageCount);
        Assert.Equal(T0.AddHours(1), updated.LastUsedAt);
        var e = Assert.Single(env.Events);
        Assert.Equal(StoreChangeKind.SnippetUpdated, e.Kind);
    }

    [Fact]
    public void Text_StoredExactly_NoTrimming()
    {
        using var env = new Env();
        const string text = "  line1\r\nline2\rline3\n  ";

        env.Store.CreateSnippet(Draft(text));

        Assert.Equal(text, env.NewStore().Snippets[0].Snippet.Text);
    }

    [Fact]
    public void DeleteSnippet_RemovesAndRaises_UnknownIdFails()
    {
        using var env = new Env();
        var id = env.Store.CreateSnippet(Draft("x")).Value!.Id;
        env.Events.Clear();

        Assert.Equal(ValidationError.SnippetNotFound, env.Store.DeleteSnippet(id + 100).Error);
        Assert.Empty(env.Events);

        Assert.True(env.Store.DeleteSnippet(id).Success);
        Assert.Empty(env.Store.Snippets);
        var e = Assert.Single(env.Events);
        Assert.Equal(StoreChangeKind.SnippetDeleted, e.Kind);
        Assert.Equal(id, e.Id);
        Assert.Empty(env.NewStore().Snippets);
    }

    [Fact]
    public void CreateFolder_TrimsAndSorts_RaisesOnce()
    {
        using var env = new Env();

        var b = env.Store.CreateFolder("  beta ").Value!;
        env.Store.CreateFolder("Alpha");
        env.Store.CreateFolder("charlie");

        Assert.Equal("beta", b.Name);
        Assert.Equal(["Alpha", "beta", "charlie"], env.Store.Folders.Select(f => f.Name));
        Assert.Equal(3, env.Events.Count);
        Assert.All(env.Events, e => Assert.Equal(StoreChangeKind.FolderCreated, e.Kind));
    }

    [Fact]
    public void CreateFolder_EmptyOrDuplicate_Fails()
    {
        using var env = new Env();
        env.Store.CreateFolder("Work");
        env.Events.Clear();

        Assert.Equal(ValidationError.FolderNameRequired, env.Store.CreateFolder("   ").Error);
        Assert.Equal(ValidationError.FolderNameDuplicate, env.Store.CreateFolder(" WORK ").Error);
        Assert.Equal(ValidationError.FolderNameDuplicate, env.Store.CreateFolder("work").Error);
        Assert.Empty(env.Events);
        Assert.Single(env.Store.Folders);
    }

    [Fact]
    public void RenameFolder_ReflectedInSnippetViews()
    {
        using var env = new Env();
        var folder = env.Store.CreateFolder("Old").Value!;
        var snippet = env.Store.CreateSnippet(Draft("x", folder: folder.Id)).Value!;
        env.Time.Now = T0.AddHours(1);
        env.Events.Clear();

        var renamed = env.Store.RenameFolder(folder.Id, " New ");

        Assert.True(renamed.Success);
        Assert.Equal("New", renamed.Value!.Name);
        Assert.Equal("New", env.Store.Snippets[0].FolderName);
        Assert.Equal(snippet.UpdatedAt, env.Store.Snippets[0].Snippet.UpdatedAt);
        var e = Assert.Single(env.Events);
        Assert.Equal(StoreChangeKind.FolderRenamed, e.Kind);
        Assert.Equal(folder.Id, e.Id);
        Assert.Equal("New", env.NewStore().Snippets[0].FolderName);
    }

    [Fact]
    public void RenameFolder_CaseOnlyChangeAllowed_OtherFoldersNameRejected()
    {
        using var env = new Env();
        var work = env.Store.CreateFolder("work").Value!;
        env.Store.CreateFolder("Home");
        env.Events.Clear();

        Assert.Equal("WORK", env.Store.RenameFolder(work.Id, "WORK").Value!.Name);
        Assert.Equal(ValidationError.FolderNameDuplicate, env.Store.RenameFolder(work.Id, "home").Error);
        Assert.Equal(ValidationError.FolderNameRequired, env.Store.RenameFolder(work.Id, " ").Error);
        Assert.Equal(ValidationError.FolderNotFound, env.Store.RenameFolder(999, "Zed").Error);
        Assert.Single(env.Events);
        Assert.Equal("WORK", env.Store.Folders.Single(f => f.Id == work.Id).Name);
    }

    [Fact]
    public void DeleteFolder_SnippetsBecomeUnfiled_EventCarriesIds()
    {
        using var env = new Env();
        var folder = env.Store.CreateFolder("Doomed").Value!;
        var other = env.Store.CreateFolder("Other").Value!;
        var a = env.Store.CreateSnippet(Draft("a", folder: folder.Id)).Value!;
        var b = env.Store.CreateSnippet(Draft("b", folder: folder.Id)).Value!;
        var c = env.Store.CreateSnippet(Draft("c", folder: other.Id)).Value!;
        env.Time.Now = T0.AddHours(3);
        env.Events.Clear();

        Assert.True(env.Store.DeleteFolder(folder.Id).Success);

        var e = Assert.Single(env.Events);
        Assert.Equal(StoreChangeKind.FolderDeleted, e.Kind);
        Assert.Equal(folder.Id, e.Id);
        Assert.Equal([a.Id, b.Id], e.SnippetIds);
        Assert.Equal(["Other"], env.Store.Folders.Select(f => f.Name));
        var views = env.Store.Snippets.ToDictionary(v => v.Snippet.Id);
        Assert.Null(views[a.Id].Snippet.FolderId);
        Assert.Null(views[a.Id].FolderName);
        Assert.Equal(T0.AddHours(3), views[b.Id].Snippet.UpdatedAt);
        Assert.Equal(other.Id, views[c.Id].Snippet.FolderId);
        Assert.Equal(c.UpdatedAt, views[c.Id].Snippet.UpdatedAt);
    }

    [Fact]
    public void DeleteFolder_EmptyAndUnknown()
    {
        using var env = new Env();
        var folder = env.Store.CreateFolder("Empty").Value!;
        env.Events.Clear();

        Assert.Equal(ValidationError.FolderNotFound, env.Store.DeleteFolder(999).Error);
        Assert.Empty(env.Events);

        Assert.True(env.Store.DeleteFolder(folder.Id).Success);
        var e = Assert.Single(env.Events);
        Assert.Empty(e.SnippetIds);
        Assert.Empty(env.Store.Folders);
    }

    [Fact]
    public void RecordUsage_CountsUse_LeavesUpdatedAt()
    {
        using var env = new Env();
        var id = env.Store.CreateSnippet(Draft("x")).Value!.Id;
        env.Time.Now = T0.AddDays(1);
        env.Events.Clear();

        Assert.True(env.Store.RecordUsage(id).Success);
        Assert.True(env.Store.RecordUsage(id).Success);

        var s = env.Store.Snippets[0].Snippet;
        Assert.Equal(2, s.UsageCount);
        Assert.Equal(T0.AddDays(1), s.LastUsedAt);
        Assert.Equal(T0, s.UpdatedAt);
        Assert.Equal(2, env.Events.Count);
        Assert.All(env.Events, e => Assert.Equal(StoreChangeKind.UsageRecorded, e.Kind));
        Assert.Equal(2, env.NewStore().Snippets[0].Snippet.UsageCount);
    }

    [Fact]
    public void RecordUsage_UnknownId_FailsWithoutEvent()
    {
        using var env = new Env();

        var result = env.Store.RecordUsage(42);

        Assert.Equal(ValidationError.SnippetNotFound, result.Error);
        Assert.Empty(env.Events);
    }

    [Fact]
    public void Load_RaisesReloaded_AndReadsPersistedData()
    {
        using var env = new Env();
        var folder = env.Store.CreateFolder("F").Value!;
        env.Store.CreateSnippet(Draft("x", folder: folder.Id));
        var fresh = new SnippetStore(env.Db.Factory, env.Time);
        var events = new List<StoreChangedEventArgs>();
        fresh.Changed += (_, e) => events.Add(e);

        fresh.Load();

        var e = Assert.Single(events);
        Assert.Equal(StoreChangeKind.Reloaded, e.Kind);
        Assert.Null(e.Id);
        Assert.Equal("F", Assert.Single(fresh.Snippets).FolderName);
    }

    [Fact]
    public void Snapshots_AreReplaced_OldSnapshotUnaffected()
    {
        using var env = new Env();
        ISnippetStore store = env.Store;
        store.CreateSnippet(Draft("one"));
        var old = store.Snippets;

        store.CreateSnippet(Draft("two"));

        Assert.Single(old);
        Assert.Equal(2, store.Snippets.Count);
    }

    [Fact]
    public void DbBusy_ThrowsStoreException_CacheUnchanged()
    {
        using var env = new Env(busyTimeout: TimeSpan.FromSeconds(1));
        var folder = env.Store.CreateFolder("F").Value!;
        var id = env.Store.CreateSnippet(Draft("keep", folder: folder.Id)).Value!.Id;
        env.Events.Clear();
        var foldersBefore = env.Store.Folders;
        var snippetsBefore = env.Store.Snippets;

        using var blocker = env.Db.Factory.Open();
        using (var begin = blocker.CreateCommand())
        {
            begin.CommandText = "BEGIN EXCLUSIVE;";
            begin.ExecuteNonQuery();
        }

        var failures = new Action[]
        {
            () => env.Store.CreateSnippet(Draft("new")),
            () => env.Store.UpdateSnippet(id, Draft("changed")),
            () => env.Store.RenameFolder(folder.Id, "H"),
            () => env.Store.DeleteFolder(folder.Id),
            () => env.Store.RecordUsage(id),
        };
        foreach (var call in failures)
        {
            var ex = Assert.Throws<StoreException>(call);
            Assert.Equal(StoreFailure.DatabaseBusy, ex.Failure);
            Assert.IsType<SqliteException>(ex.InnerException);
            Assert.DoesNotContain("keep", ex.Message);
        }

        Assert.Empty(env.Events);
        Assert.Same(foldersBefore, env.Store.Folders);
        Assert.Same(snippetsBefore, env.Store.Snippets);

        using (var rollback = blocker.CreateCommand())
        {
            rollback.CommandText = "ROLLBACK;";
            rollback.ExecuteNonQuery();
        }

        Assert.True(env.Store.CreateSnippet(Draft("after")).Success);
        Assert.Equal(2, env.NewStore().Snippets.Count);
    }

    [Fact]
    public void DbMissingTables_ThrowsUnavailable()
    {
        using var db = new TempDatabase();
        var store = new SnippetStore(db.Factory, TimeProvider.System);

        var ex = Assert.Throws<StoreException>(store.Load);

        Assert.Equal(StoreFailure.DatabaseUnavailable, ex.Failure);
    }

    [Fact]
    public void Reload_EqualsCache()
    {
        using var env = new Env();
        var work = env.Store.CreateFolder("Work").Value!;
        var home = env.Store.CreateFolder("home").Value!;
        env.Time.Now = T0.AddMinutes(1);
        var a = env.Store.CreateSnippet(Draft("alpha 👋\r\nx", "t1, T1, t2", folder: work.Id)).Value!;
        env.Time.Now = T0.AddMinutes(2);
        var b = env.Store.CreateSnippet(Draft("beta", "", "Custom", true, home.Id)).Value!;
        var c = env.Store.CreateSnippet(Draft("gamma")).Value!;
        env.Time.Now = T0.AddMinutes(3).AddTicks(1234);
        env.Store.UpdateSnippet(a.Id, Draft("alpha2", "z", folder: home.Id));
        env.Store.RecordUsage(b.Id);
        env.Store.RecordUsage(b.Id);
        env.Store.RenameFolder(home.Id, "Home2");
        env.Store.DeleteSnippet(c.Id);
        env.Store.CreateSnippet(Draft("delta", folder: work.Id));
        env.Time.Now = T0.AddMinutes(4);
        env.Store.DeleteFolder(work.Id);

        var fresh = env.NewStore();

        Assert.Equal(env.Store.Folders, fresh.Folders);
        Assert.Equal(env.Store.Snippets.Count, fresh.Snippets.Count);
        for (var i = 0; i < fresh.Snippets.Count; i++)
        {
            AssertSameView(env.Store.Snippets[i], fresh.Snippets[i]);
        }
    }

    private static void AssertSameView(SnippetView expected, SnippetView actual)
    {
        Assert.Equal(expected.FolderName, actual.FolderName);
        Assert.Equal(expected.Snippet with { Tags = [] }, actual.Snippet with { Tags = [] });
        Assert.Equal(expected.Snippet.Tags, actual.Snippet.Tags);
    }
}
