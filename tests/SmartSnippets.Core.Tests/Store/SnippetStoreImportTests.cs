using SmartSnippets.Core.Domain;
using SmartSnippets.Core.Porting;
using SmartSnippets.Core.Query;
using SmartSnippets.Core.Settings;
using SmartSnippets.Core.Store;
using SmartSnippets.Core.Tests.Porting;

namespace SmartSnippets.Core.Tests.Store;

public class SnippetStoreImportTests
{
    internal static ImportBatch Batch(string? folder = "Work", int usage = 7) => new([
        new(0, "first", "First", folder, ["one", "two"], new(Fixture.Now.AddDays(-3), Fixture.Now.AddDays(-2), Fixture.Now.AddDays(-1), usage)),
        new(1, "second", null, folder, [], null)], [], DedupeMode.TextOnly);
    [Fact] public void Import_OneTransaction_OneEvent_KindImported_IdsAreNew()
    {
        using var e = new PortingStoreEnv(); var c = e.Store.ImportSnippets(Batch());
        Assert.Equal(2, c.SnippetIds.Count); Assert.Equal(2, c.SnippetIds.Distinct().Count()); Assert.All(c.SnippetIds, id => Assert.True(id > 0));
        var ev = Assert.Single(e.Events); Assert.Equal(StoreChangeKind.Imported, ev.Kind); Assert.Null(ev.Id); Assert.Equal(c.SnippetIds, ev.SnippetIds);
    }
    [Fact] public void Import_SnapshotsReplacedOnce_ContainNewRows() { using var e = new PortingStoreEnv(); var s = e.Store.Snippets; var f = e.Store.Folders; int calls = 0; e.Store.Changed += (_, _) => { calls++; Assert.Equal(2, e.Store.Snippets.Count); }; e.Store.ImportSnippets(Batch()); Assert.NotSame(s, e.Store.Snippets); Assert.NotSame(f, e.Store.Folders); Assert.Equal(1, calls); }
    [Fact] public void Import_PersistsStats_ReloadEqualsCache() { using var e = new PortingStoreEnv(); e.Store.ImportSnippets(Batch()); var fresh = e.NewStore(); Assert.Equal(e.Store.Snippets.Select(s => s.Snippet), fresh.Snippets.Select(s => s.Snippet), new SnippetComparer()); Assert.Equal(7, fresh.Snippets[0].Snippet.UsageCount); Assert.Equal(Fixture.Now.AddDays(-3), fresh.Snippets[0].Snippet.CreatedAt); }
    [Fact] public void Import_FoldersCreatedInSameTransaction() { using var e = new PortingStoreEnv(); var c = e.Store.ImportSnippets(Batch()); Assert.Equal(1, c.FoldersCreated); Assert.All(e.NewStore().Snippets, s => Assert.Equal("Work", s.FolderName)); }
    [Fact] public void Import_AllDuplicates_NoWrite_NoEvent()
    {
        using var e = new PortingStoreEnv(); e.Store.ImportSnippets(Batch()); e.Events.Clear(); var s = e.Store.Snippets; var f = e.Store.Folders;
        e.Db.Exec("CREATE TRIGGER no_snippets BEFORE INSERT ON snippets BEGIN SELECT RAISE(ABORT,'no'); END; CREATE TRIGGER no_folders BEFORE INSERT ON folders BEGIN SELECT RAISE(ABORT,'no'); END;");
        var c = e.Store.ImportSnippets(Batch()); Assert.Empty(c.SnippetIds); Assert.Equal(2, c.SkippedDuplicates); Assert.Empty(e.Events); Assert.Same(s, e.Store.Snippets); Assert.Same(f, e.Store.Folders);
    }
    [Fact] public void Import_OnlyEmptyNativeFolder_WritesFolder_OneEvent() { using var e = new PortingStoreEnv(); var c = e.Store.ImportSnippets(new([], ["Empty"], DedupeMode.FullRow)); Assert.Empty(c.SnippetIds); Assert.Equal(1, c.FoldersCreated); Assert.Equal("Empty", Assert.Single(e.NewStore().Folders).Name); Assert.Single(e.Events); }
    [Fact] public void Import_EditorRules_SameAsCreate() { using var e = new PortingStoreEnv(); var created = e.Store.CreateSnippet(new("same", "", false, null, " Beta, Gamma,beta")).Value!; var c = e.Store.ImportSnippets(new([new(0, "same", null, null, [" Beta, Gamma", "beta"], null)], [], DedupeMode.FullRow)); Assert.Equal(1, c.SkippedDuplicates); Assert.Empty(c.SnippetIds); Assert.Equal("Beta", created.Label); Assert.False(created.IsLabelCustom); Assert.Equal(new[] { "Beta", "Gamma" }, created.Tags); }
    [Fact] public void Import_FullRowBatch_KeepsSameTextRowsWithDifferentMetadata() { using var e = new PortingStoreEnv(); var c = e.Store.ImportSnippets(Fixture.Read("native-v1.json").Batch); Assert.Equal(6, c.SnippetIds.Count); Assert.Equal(1, c.SkippedDuplicates); Assert.Equal(3, c.FoldersCreated); }
    [Theory] [InlineData(false)] [InlineData(true)] public void Import_InjectedFailure_RollsBack_NoRows_NoFolders_NoEvent_SnapshotsSame(bool folderTrigger)
    {
        using var e = new PortingStoreEnv(); var s = e.Store.Snippets; var f = e.Store.Folders;
        e.Db.Exec(folderTrigger ? "CREATE TRIGGER fail_import BEFORE INSERT ON folders WHEN NEW.name='Fail' BEGIN SELECT RAISE(ABORT,'injected'); END;" : "CREATE TRIGGER fail_import BEFORE INSERT ON snippets WHEN NEW.label='boom' BEGIN SELECT RAISE(ABORT,'injected'); END;");
        var b = Batch(); b = b with { Candidates = [.. b.Candidates, new(2, "third", "boom", "Fail", [], null)] };
        Assert.Throws<StoreException>(() => e.Store.ImportSnippets(b)); Assert.Same(s, e.Store.Snippets); Assert.Same(f, e.Store.Folders); Assert.Empty(e.Events); var fresh = e.NewStore(); Assert.Empty(fresh.Snippets); Assert.Empty(fresh.Folders);
    }
    [Fact] public void Import_Busy_RollsBack_NoEvent()
    {
        using var e = new PortingStoreEnv(); var s = e.Store.Snippets; var f = e.Store.Folders;
        using (var connection = e.Db.Factory.Open()) { using var tx = connection.BeginTransaction(); using var cmd = connection.CreateCommand(); cmd.Transaction = tx; cmd.CommandText = "INSERT INTO folders(name,name_key,created_at,updated_at) VALUES('held','HELD',0,0);"; cmd.ExecuteNonQuery(); var ex = Assert.Throws<StoreException>(() => e.Store.ImportSnippets(Batch())); Assert.Equal(StoreFailure.DatabaseBusy, ex.Failure); }
        Assert.Same(s, e.Store.Snippets); Assert.Same(f, e.Store.Folders); Assert.Empty(e.Events); Assert.Empty(e.NewStore().Snippets); Assert.Empty(e.NewStore().Folders);
    }
    [Fact] public void UsageImported_Paste_Sort_Reopen() => UsageChain(1_000_000_000, 3, 1_000_000_003);
    [Fact] public void UsageAtIntMax_Paste_Saturates_Reopen() => UsageChain(int.MaxValue - 1, 2, int.MaxValue);
    private static void UsageChain(int initial, int times, int expected) { using var e = new PortingStoreEnv(); var id = e.Store.ImportSnippets(Batch(usage: initial)).SnippetIds[0]; for (int i = 0; i < times; i++) e.Store.RecordUsage(id); Assert.Equal(expected, e.Store.Snippets[0].Snippet.UsageCount); Assert.Equal(id, SnippetSort.Apply(e.Store.Snippets, SortField.UsageCount)[0].Snippet.Id); Assert.Equal(expected, e.NewStore().Snippets[0].Snippet.UsageCount); }
    private sealed class SnippetComparer : IEqualityComparer<Snippet> { public bool Equals(Snippet? a, Snippet? b) => a is not null && b is not null && a with { Tags = b.Tags } == b && a.Tags.SequenceEqual(b.Tags); public int GetHashCode(Snippet s) => s.Id.GetHashCode(); }
}
