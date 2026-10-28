using SmartSnippets.Core.Domain;
using SmartSnippets.Core.Logging;
using SmartSnippets.Core.Store;
using SmartSnippets.Core.Tests.Porting;

namespace SmartSnippets.Core.Tests.Store;

[Collection("AppLog")]
public class SnippetStoreObserverTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "ss-observer-" + Guid.NewGuid().ToString("N"));
    public SnippetStoreObserverTests() { AppLog.Shutdown(); AppLog.Initialize(_directory); }
    public void Dispose() { AppLog.Shutdown(); Directory.Delete(_directory, true); }
    private string Log => File.ReadAllText(Path.Combine(_directory, "app.log"));
    [Fact] public void Import_SubscriberThrows_CommitReturned_RowsPersisted_LaterSubscribersRan()
    {
        using var e = new PortingStoreEnv(); var first = new List<StoreChangedEventArgs>(); var last = new List<StoreChangedEventArgs>();
        e.Store.Changed += (_, args) => first.Add(args);
        e.Store.Changed += (_, _) => throw new InvalidOperationException("PRIVATE-MESSAGE");
        e.Store.Changed += (_, args) => last.Add(args);
        var c = e.Store.ImportSnippets(SnippetStoreImportTests.Batch()); Assert.Equal(2, c.SnippetIds.Count); Assert.Equal(2, e.NewStore().Snippets.Count);
        Assert.Equal(StoreChangeKind.Imported, Assert.Single(first).Kind); Assert.Equal(StoreChangeKind.Imported, Assert.Single(last).Kind);
        Assert.Contains("store_subscriber_failed kind=Imported index=2 of=4 error=InvalidOperationException", Log); Assert.DoesNotContain("PRIVATE-MESSAGE", Log);
    }
    [Fact] public void Create_SubscriberThrows_ResultReturned() { using var e = new PortingStoreEnv(); e.Store.Changed += (_, _) => throw new InvalidOperationException("PRIVATE-MESSAGE"); int later = 0; e.Store.Changed += (_, _) => later++; Assert.NotNull(e.Store.CreateSnippet(new("body", "label", true, null, "")).Value); Assert.Equal(1, later); Assert.Single(e.NewStore().Snippets); Assert.Contains("store_subscriber_failed kind=SnippetCreated", Log); Assert.DoesNotContain("PRIVATE-MESSAGE", Log); }
    [Fact] public void Import_InjectedFailure_LogHasEventButNoMessage()
    {
        using var e = new PortingStoreEnv(); Trigger(e); var b = SnippetStoreImportTests.Batch(); b = b with { Candidates = [b.Candidates[0] with { Text = "SENTINEL-BODY-91c2" }] };
        Assert.Throws<StoreException>(() => e.Store.ImportSnippets(b)); Assert.Contains("store_write_failed op=import_snippets error=SqliteException sqlite=19", Log); Privacy();
    }
    [Fact] public void CreateSnippet_InjectedFailure_LogHasNoMessage() { using var e = new PortingStoreEnv(); Trigger(e); Assert.Throws<StoreException>(() => e.Store.CreateSnippet(new("SENTINEL-BODY-91c2", "Label", true, null, ""))); Assert.Contains("store_write_failed op=create_snippet error=SqliteException sqlite=19", Log); Privacy(); }
    private static void Trigger(PortingStoreEnv e) => e.Db.Exec("CREATE TRIGGER fail_import BEFORE INSERT ON snippets BEGIN SELECT RAISE(ABORT,'SENTINEL-TEXT-7f3a'); END;");
    private void Privacy() { Assert.DoesNotContain("SENTINEL-TEXT-7f3a", Log); Assert.DoesNotContain("SENTINEL-BODY-91c2", Log); }
}
