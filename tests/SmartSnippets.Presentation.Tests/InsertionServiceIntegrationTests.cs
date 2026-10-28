using Microsoft.Data.Sqlite;
using SmartSnippets.Core.Data;
using SmartSnippets.Core.Domain;
using SmartSnippets.Core.Store;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Presentation.Tests.Fakes;

namespace SmartSnippets.Presentation.Tests;

public sealed class InsertionServiceIntegrationTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "ss-insert-" + Guid.NewGuid().ToString("N"));
    private readonly SqliteConnectionFactory _factory;
    public InsertionServiceIntegrationTests() { _factory = new(Path.Combine(_directory, "test.db")); new Migrator(_factory).MigrateToLatest(); }
    public void Dispose() { SqliteConnection.ClearAllPools(); try { Directory.Delete(_directory, true); } catch (IOException) { } }
    private SnippetStore Open() { var store = new SnippetStore(_factory, TimeProvider.System); store.Load(); return store; }

    [Theory]
    [InlineData(InsertionOutcome.Inserted)]
    [InlineData(InsertionOutcome.PasteFailed)]
    [InlineData(InsertionOutcome.ClipboardNotPreserved)]
    [InlineData(InsertionOutcome.MaybePasted)]
    [InlineData(InsertionOutcome.CopiedForManualPaste)]
    public async Task UsagePersistence_OnlyOnInserted(InsertionOutcome expected)
    {
        var store = Open();
        var snippet = store.CreateSnippet(new SnippetDraft("hello", "", false, null, "")).Value!;
        var calls = new List<string>();
        var probe = new FakeTargetProbe(calls);
        var dispatcher = new FakePasteDispatcher(calls);
        if (expected == InsertionOutcome.PasteFailed) dispatcher.Dispatch = DispatchState.NotDispatched;
        if (expected == InsertionOutcome.MaybePasted) dispatcher.Dispatch = DispatchState.Uncertain;
        if (expected == InsertionOutcome.ClipboardNotPreserved) dispatcher.Unsaved = 1;
        if (expected == InsertionOutcome.CopiedForManualPaste) probe.Result = TargetCheck.Closed;
        var service = new InsertionService(store, probe, dispatcher, delay: (_, _) => Task.CompletedTask);
        var result = await service.InsertAsync(new(1, 2, 0, "app"), snippet.Id, new(expected == InsertionOutcome.CopiedForManualPaste));
        Assert.Equal(expected, result.Outcome);
        var reloaded = Assert.Single(Open().Snippets).Snippet;
        Assert.Equal(expected == InsertionOutcome.Inserted ? 1 : 0, reloaded.UsageCount);
        Assert.Equal(snippet.UpdatedAt, reloaded.UpdatedAt);
    }
}
