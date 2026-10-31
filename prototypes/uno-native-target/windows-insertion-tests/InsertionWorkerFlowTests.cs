using Microsoft.Data.Sqlite;
using SmartSnippets.Core.Data;
using SmartSnippets.Core.Domain;
using SmartSnippets.Core.Store;
using SmartSnippets.Presentation.Services;
using SmartSnippets.TargetIdentity018;
using SmartSnippets.WindowsInsertion018;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace SmartSnippets.WindowsInsertion018.Tests;

// Actual worker flow + actual InsertionService + actual SQLite. Only OS
// boundaries are substituted. These tests are NOT native T4/T5 evidence.
public sealed class InsertionWorkerFlowTests
{
    [Fact]
    public void DispatchedFlowUsesCrLfAndRecordsUsageExactlyOnceDespiteDuplicate() => Run(async fixture =>
    {
        var dispatcher = new ControlledDispatcher();
        var attempt = fixture.Execute(dispatcher);
        Assert.False(attempt.IsCompleted);
        dispatcher.Focus.SetResult(FocusPath.Direct);
        var result = await attempt;
        Assert.Equal(InsertionOutcome.Inserted, result.First.Outcome);
        Assert.Equal(InsertionOutcome.Busy, result.Duplicate?.Outcome);
        Assert.Equal(OwnClipboardPublication.Multiline, dispatcher.Text);
        Assert.Equal(1, dispatcher.SendCalls);
        Assert.Equal(1, dispatcher.RestoreCalls);
        fixture.AssertUsage(1);
    });

    [Theory]
    [InlineData(SwapStatus.NotPreserved, InsertionOutcome.ClipboardNotPreserved)]
    [InlineData(SwapStatus.Busy, InsertionOutcome.ClipboardBusy)]
    [InlineData(SwapStatus.Failed, InsertionOutcome.PasteFailed)]
    public void RefusalContentionAndFailureCannotDispatchOrCreditUsage(SwapStatus swap, InsertionOutcome expected) => Run(async fixture =>
    {
        var dispatcher = new ControlledDispatcher { Swap = swap };
        var attempt = fixture.Execute(dispatcher);
        dispatcher.Focus.SetResult(FocusPath.Direct);
        var result = await attempt;
        Assert.Equal(expected, result.First.Outcome);
        Assert.Equal(InsertionOutcome.Busy, result.Duplicate?.Outcome);
        Assert.Equal(0, dispatcher.SendCalls);
        Assert.Equal(0, dispatcher.RestoreCalls);
        fixture.AssertUsage(0);
    });

    [Fact]
    public void UncertainBoundaryOutcomeDoesNotCountAsUsageOrNativeDelivery() => Run(async fixture =>
    {
        var dispatcher = new ControlledDispatcher { Dispatch = DispatchState.Uncertain };
        var attempt = fixture.Execute(dispatcher);
        dispatcher.Focus.SetResult(FocusPath.Direct);
        var result = await attempt;
        Assert.Equal(InsertionOutcome.MaybePasted, result.First.Outcome);
        Assert.Equal(1, dispatcher.CleanupCalls);
        Assert.Equal(1, dispatcher.RestoreCalls);
        fixture.AssertUsage(0);
    });

    [Fact]
    public void NotDispatchedHasNoUsage() => Run(async fixture =>
    {
        var dispatcher = new ControlledDispatcher { Dispatch = DispatchState.NotDispatched };
        var attempt = fixture.Execute(dispatcher);
        dispatcher.Focus.SetResult(FocusPath.Direct);
        Assert.Equal(InsertionOutcome.PasteFailed, (await attempt).First.Outcome);
        fixture.AssertUsage(0);
    });

    [Fact]
    public void FailedFocusNeverSnapshotsOrDispatches() => Run(async fixture =>
    {
        var dispatcher = new ControlledDispatcher();
        var attempt = fixture.Execute(dispatcher);
        dispatcher.Focus.SetResult(null);
        Assert.Equal(InsertionOutcome.FocusFailed, (await attempt).First.Outcome);
        Assert.Equal(0, dispatcher.SwapCalls);
        Assert.Equal(0, dispatcher.SendCalls);
        fixture.AssertUsage(0);
    });

    [Fact]
    public void ClosedOriginalNeverReachesDispatcher() => Run(async fixture =>
    {
        fixture.Probe.Status = TargetCheck.Closed;
        var dispatcher = new ControlledDispatcher();
        var result = await fixture.Execute(dispatcher);
        Assert.Equal(InsertionOutcome.TargetClosed, result.First.Outcome);
        Assert.Null(result.Duplicate);
        Assert.Equal(0, dispatcher.FocusCalls);
        Assert.Equal(0, dispatcher.SendCalls);
        fixture.AssertUsage(0);
    });

    [Fact]
    public void SamePidSiblingForegroundCannotPassFinalGateOrEarnUsage() => Run(async fixture =>
    {
        fixture.Probe.Foreground = false;
        var dispatcher = new ControlledDispatcher { Restore = RestoreStatus.ClipboardChanged };
        var attempt = fixture.Execute(dispatcher);
        dispatcher.Focus.SetResult(FocusPath.Direct);
        Assert.Equal(InsertionOutcome.FocusFailed, (await attempt).First.Outcome);
        Assert.Equal(0, dispatcher.SendCalls);
        Assert.Equal(1, dispatcher.RestoreCalls);
        fixture.AssertUsage(0);
    });

    private static void Run(Func<StoreFixture, Task> body) => Assert.Equal(0, StaTaskLoop.Run(async () =>
    {
        using var fixture = new StoreFixture();
        await body(fixture);
        return 0;
    }));

    private sealed class StoreFixture : IDisposable
    {
        private readonly PrivateDiagnostics diagnostics = new();
        private readonly SnippetStore store;
        private readonly long id;
        internal readonly ProbeBoundary Probe = new();
        private readonly InsertionTarget target = new(11, 424242, 0, "");
        internal StoreFixture()
        {
            var factory = new SqliteConnectionFactory(Path.Combine(diagnostics.DirectoryPath, "synthetic.db"));
            new Migrator(factory).MigrateToLatest();
            store = new SnippetStore(factory, TimeProvider.System);
            store.Load();
            id = store.CreateSnippet(new SnippetDraft(OwnClipboardPublication.Multiline.Replace("\r\n", "\n"), "Synthetic018", true, null, "")).Value!.Id;
        }
        internal Task<(InsertionResult First, InsertionResult? Duplicate)> Execute(ControlledDispatcher dispatcher) =>
            InsertionWorkerFlow.ExecuteAsync(store, Probe, dispatcher, target, id);
        internal void AssertUsage(long expected)
        {
            store.Load();
            Assert.Equal(expected, store.Snippets.Single().Snippet.UsageCount);
        }
        public void Dispose()
        {
            diagnostics.Dispose();
            SqliteConnection.ClearAllPools();
            Directory.Delete(diagnostics.DirectoryPath, true);
        }
    }
    private sealed class ProbeBoundary : ITargetProbe
    {
        internal TargetCheck Status = TargetCheck.Ok;
        internal bool Foreground = true;
        public TargetCheck Check(InsertionTarget target) => Status;
        public bool IsTargetForeground(InsertionTarget target) => Foreground;
    }
    private sealed class Snapshot : IClipboardSnapshot
    {
        public int SavedCount => 1;
        public int SkippedByDesignCount => 0;
        public int UnsavedCount => 0;
        public bool CapExceeded => false;
        public long ByteCount => 8;
    }
    private sealed class ControlledDispatcher : IPasteDispatcher
    {
        internal readonly TaskCompletionSource<FocusPath?> Focus = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal SwapStatus Swap = SwapStatus.Swapped;
        internal DispatchState Dispatch = DispatchState.Dispatched;
        internal RestoreStatus Restore = RestoreStatus.Restored;
        internal string? Text;
        internal int SendCalls, RestoreCalls, SwapCalls, CleanupCalls, FocusCalls;
        public Task<FocusPath?> RestoreFocusAsync(InsertionTarget target, CancellationToken ct) { FocusCalls++; return Focus.Task; }
        public bool ReleaseHeldModifiers() => true;
        public ClipboardSwap TrySwapClipboard(string text, bool allowIncomplete)
        {
            Assert.False(allowIncomplete);
            SwapCalls++; Text = text;
            return new(Swap, Swap == SwapStatus.Swapped ? new Snapshot() : null);
        }
        public SwapStatus TryPlaceText(string text) => throw new InvalidOperationException("Unexpected loss fallback.");
        public DispatchState SendPaste() { SendCalls++; return Dispatch; }
        public bool ReleasePasteKeys() { CleanupCalls++; return true; }
        public RestoreStatus TryRestoreClipboard(IClipboardSnapshot snapshot) { RestoreCalls++; return Restore; }
        public void DiscardClipboardSnapshot(IClipboardSnapshot snapshot) => throw new InvalidOperationException("Unexpected snapshot discard.");
    }
}
