using SmartSnippets.Presentation.Services;
using SmartSnippets.Presentation.Tests.Fakes;

namespace SmartSnippets.Presentation.Tests;

public sealed class InsertionServiceTests
{
    private readonly List<string> _calls = [];
    private readonly FakeSnippetStore _store = new();
    private readonly FakeTargetProbe _probe;
    private readonly FakePasteDispatcher _dispatcher;
    private readonly InsertionService _service;
    private readonly long _id;
    private static readonly InsertionTarget Target = new(1, 42, 0, "Notepad");

    public InsertionServiceTests()
    {
        _id = _store.SeedSnippet("a\rb").Id;
        _probe = new(_calls);
        _dispatcher = new(_calls);
        _service = new(_store, _probe, _dispatcher, delay: (duration, _) => { _calls.Add($"delay:{duration.TotalMilliseconds}"); return Task.CompletedTask; });
    }

    private Task<InsertionResult> Insert(bool loss = false, CancellationToken ct = default) => _service.InsertAsync(Target, _id, new(loss), ct);
    private int Usage => _store.Snippets.Single().Snippet.UsageCount;

    [Fact]
    public async Task Inserted_CallsStepsInOrder_AndNormalizesWithoutChangingStore()
    {
        var result = await Insert();
        Assert.Equal(InsertionOutcome.Inserted, result.Outcome);
        Assert.Equal(new[] { "check", "focus", "modifiers", "swap", "gate", "send", "delay:250", "restore" }, _calls);
        Assert.Equal("a\r\nb", _dispatcher.Text);
        Assert.Equal("a\rb", _store.Snippets.Single().Snippet.Text);
        Assert.Equal(1, Usage);
        Assert.Same(_dispatcher.LastSnapshot, _dispatcher.RestoredSnapshot);
        Assert.Null(_dispatcher.Pending);
        Assert.Equal(new byte[] { 1, 2 }, _dispatcher.Clipboard["text"]);
        Assert.Equal(new byte[] { 3, 4, 5 }, _dispatcher.Clipboard["html"]);
        Assert.Equal(new byte[] { 6, 7, 8, 9 }, _dispatcher.Clipboard["image"]);
    }

    [Theory]
    [InlineData(101, 400, 7, "first")]
    [InlineData(999, 800, 17, "second")]
    public async Task CapturedTarget_IsPassedUnchangedToEveryHop(int hwnd, uint pid, int monitor, string name)
    {
        var target = new InsertionTarget(hwnd, pid, monitor, name);
        Assert.Equal(InsertionOutcome.Inserted, (await _service.InsertAsync(target, _id)).Outcome);
        Assert.Equal(target, Assert.Single(_probe.CheckedTargets));
        Assert.Equal(target, Assert.Single(_dispatcher.FocusTargets));
        Assert.Equal(target, Assert.Single(_probe.ForegroundTargets));
    }

    [Fact]
    public async Task SnippetMissing_NoWin32Calls() { Assert.Equal(InsertionOutcome.SnippetMissing, (await _service.InsertAsync(Target, -1)).Outcome); Assert.Empty(_calls); Assert.Equal(0, Usage); }

    [Theory]
    [InlineData(TargetCheck.Closed, InsertionOutcome.TargetClosed)]
    [InlineData(TargetCheck.Elevated, InsertionOutcome.TargetElevated)]
    [InlineData(TargetCheck.OwnProcess, InsertionOutcome.PasteFailed)]
    public async Task TargetRejected_NoOtherCalls(TargetCheck check, InsertionOutcome expected)
    { _probe.Result = check; Assert.Equal(expected, (await Insert()).Outcome); Assert.Equal(new[] { "check" }, _calls); Assert.Equal(0, Usage); }

    [Fact]
    public async Task ModifierReleaseFailure_StopsBeforeClipboardAndPaste()
    {
        _dispatcher.ModifiersReleased = false;
        Assert.Equal(InsertionOutcome.PasteFailed, (await Insert()).Outcome);
        Assert.DoesNotContain("swap", _calls);
        Assert.DoesNotContain("send", _calls);
        Assert.Equal(0, Usage);
    }

    [Fact]
    public async Task PasteKeyCleanupFailure_StillUncertainAndRestores()
    {
        _dispatcher.Dispatch = DispatchState.Uncertain;
        _dispatcher.PasteKeysReleased = false;
        Assert.Equal(InsertionOutcome.MaybePasted, (await Insert()).Outcome);
        Assert.Contains("restore", _calls);
        Assert.Equal(0, Usage);
    }

    [Fact]
    public async Task FocusFailed_NoClipboard() { _dispatcher.Focus = null; Assert.Equal(InsertionOutcome.FocusFailed, (await Insert()).Outcome); Assert.DoesNotContain("swap", _calls); }

    [Theory]
    [InlineData(false, InsertionOutcome.FocusFailed, true)]
    [InlineData(true, InsertionOutcome.CopiedForManualPaste, false)]
    public async Task ForegroundLost_RestoresUnlessLossAllowed(bool loss, InsertionOutcome expected, bool restored)
    {
        _probe.Foreground = false; Assert.Equal(expected, (await Insert(loss)).Outcome);
        Assert.Equal(restored, _calls.Contains("restore")); Assert.DoesNotContain("send", _calls); Assert.Equal(0, Usage);
        Assert.Null(_dispatcher.Pending);
        if (loss)
        {
            Assert.Same(_dispatcher.LastSnapshot, _dispatcher.DiscardedSnapshot);
            Assert.Equal("a\r\nb", System.Text.Encoding.UTF8.GetString(_dispatcher.Clipboard["snippet"]));
        }
        else
        {
            Assert.Same(_dispatcher.LastSnapshot, _dispatcher.RestoredSnapshot);
            Assert.Equal(new byte[] { 6, 7, 8, 9 }, _dispatcher.Clipboard["image"]);
        }
    }

    [Fact]
    public async Task IncompleteSnapshot_DoesNotEmpty() { _dispatcher.Unsaved = 1; Assert.Equal(InsertionOutcome.ClipboardNotPreserved, (await Insert()).Outcome); Assert.False(_dispatcher.Emptied); Assert.False(_dispatcher.AllowIncomplete); Assert.DoesNotContain("send", _calls); }

    [Fact]
    public async Task OverCap_NotPreserved_NoEmptyClipboard()
    {
        _dispatcher.Unsaved = 1; _dispatcher.CapExceeded = true;
        Assert.Equal(InsertionOutcome.ClipboardNotPreserved, (await Insert()).Outcome);
        Assert.False(_dispatcher.Emptied);
        Assert.DoesNotContain("send", _calls);
    }

    [Fact]
    public async Task AllowLoss_IncompleteSnapshot_PastesAndRestores()
    {
        _dispatcher.Unsaved = 1; Assert.Equal(InsertionOutcome.Inserted, (await Insert(true)).Outcome);
        Assert.True(_dispatcher.AllowIncomplete); Assert.Same(_dispatcher.LastSnapshot, _dispatcher.RestoredSnapshot);
        Assert.Equal(new byte[] { 1, 2 }, _dispatcher.Clipboard["text"]);
        Assert.Equal(new byte[] { 3, 4, 5 }, _dispatcher.Clipboard["html"]);
        Assert.False(_dispatcher.Clipboard.ContainsKey("image")); Assert.Equal(1, Usage);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AllowLoss_ClosedOrFocusFailed_PlacesText(bool closed)
    { if (closed) _probe.Result = TargetCheck.Closed; else _dispatcher.Focus = null; Assert.Equal(InsertionOutcome.CopiedForManualPaste, (await Insert(true)).Outcome); Assert.Contains("place", _calls); Assert.DoesNotContain("restore", _calls); Assert.Equal(0, Usage); }

    [Fact]
    public async Task AllowLoss_Elevated_NoClipboard() { _probe.Result = TargetCheck.Elevated; Assert.Equal(InsertionOutcome.TargetElevated, (await Insert(true)).Outcome); Assert.DoesNotContain("place", _calls); }
    [Fact]
    public async Task AllowLoss_PlaceBusy() { _probe.Result = TargetCheck.Closed; _dispatcher.Place = SwapStatus.Busy; Assert.Equal(InsertionOutcome.ClipboardBusy, (await Insert(true)).Outcome); Assert.Equal(10, _calls.Count(x => x == "place")); }
    [Fact]
    public async Task ClipboardBusy_RetriesTenTimes() { for (var i = 0; i < 10; i++) _dispatcher.Swaps.Enqueue(SwapStatus.Busy); Assert.Equal(InsertionOutcome.ClipboardBusy, (await Insert()).Outcome); Assert.Equal(10, _calls.Count(x => x == "swap")); Assert.Equal(9, _calls.Count(x => x == "delay:20")); }
    [Fact]
    public async Task ClipboardBusy_ThenSuccess() { _dispatcher.Swaps.Enqueue(SwapStatus.Busy); _dispatcher.Swaps.Enqueue(SwapStatus.Busy); Assert.Equal(InsertionOutcome.Inserted, (await Insert()).Outcome); Assert.Equal(3, _calls.Count(x => x == "swap")); }
    [Fact]
    public async Task SwapFailedAfterEmpty_Restores() { _dispatcher.FailAfterEmpty = true; Assert.Equal(InsertionOutcome.PasteFailed, (await Insert()).Outcome); Assert.Contains("restore", _calls); Assert.Equal(0, Usage); }

    [Theory]
    [InlineData(DispatchState.NotDispatched, InsertionOutcome.PasteFailed, false)]
    [InlineData(DispatchState.Uncertain, InsertionOutcome.MaybePasted, true)]
    public async Task FailedDispatch_OnlyUncertainReleasesAndWaits(DispatchState state, InsertionOutcome expected, bool uncertain)
    { _dispatcher.Dispatch = state; Assert.Equal(expected, (await Insert()).Outcome); Assert.Equal(uncertain, _calls.Contains("release")); Assert.Equal(uncertain, _calls.Contains("delay:250")); Assert.Contains("restore", _calls); Assert.Equal(0, Usage); }
    [Fact]
    public async Task SendThrows_IsUncertain() { _dispatcher.ThrowSend = true; Assert.Equal(InsertionOutcome.MaybePasted, (await Insert()).Outcome); Assert.Contains("release", _calls); Assert.Contains("delay:250", _calls); Assert.Equal(0, Usage); }
    [Fact]
    public async Task ExceptionAtGate_RestoresAndClearsFlag() { _probe.OnGate = () => throw new InvalidOperationException(); Assert.Equal(InsertionOutcome.PasteFailed, (await Insert()).Outcome); Assert.Contains("restore", _calls); _probe.OnGate = null; Assert.Equal(InsertionOutcome.Inserted, (await Insert()).Outcome); }
    [Fact]
    public async Task RestoreChanged_NoNotice() { _dispatcher.Restores.Enqueue(RestoreStatus.ClipboardChanged); Assert.Equal(InsertionOutcome.Inserted, (await Insert()).Outcome); }
    [Theory]
    [InlineData(RestoreStatus.Partial)]
    [InlineData(RestoreStatus.Failed)]
    [InlineData(RestoreStatus.Busy)]
    public async Task RestoreFailed_AfterPaste_InsertedWithNotice(RestoreStatus status)
    {
        for (var i = 0; i < 10; i++) _dispatcher.Restores.Enqueue(status);
        var result = await Insert();
        Assert.Equal(InsertionOutcome.Inserted, result.Outcome);
        Assert.Equal(SmartSnippets.Presentation.Text.UiText.InsertedRestoreNotice, result.Message);
        Assert.Equal(1, Usage);
    }
    [Fact]
    public async Task RestoreFailed_AfterNotDispatchedFailure_MessageHasNotice()
    {
        _dispatcher.Dispatch = DispatchState.NotDispatched; _dispatcher.Restores.Enqueue(RestoreStatus.Failed);
        Assert.Contains(SmartSnippets.Presentation.Text.UiText.RestoreNotice, (await Insert()).Message);
        Assert.Equal(0, Usage);
    }
    [Fact]
    public async Task RestoreFailed_AfterMaybePasted_MessageUnchanged()
    {
        _dispatcher.Dispatch = DispatchState.Uncertain; _dispatcher.Restores.Enqueue(RestoreStatus.Failed);
        var result = await Insert();
        Assert.Contains("Check it before trying again", result.Message);
        Assert.DoesNotContain(SmartSnippets.Presentation.Text.UiText.RestoreNotice, result.Message);
    }
    [Fact]
    public async Task RecordUsage_Exception_StillInserted()
    {
        _store.Failure = new SmartSnippets.Core.Store.StoreException(SmartSnippets.Core.Store.StoreFailure.DatabaseBusy, "busy");
        Assert.Equal(InsertionOutcome.Inserted, (await Insert()).Outcome);
        Assert.Equal(0, Usage);
    }
    [Fact]
    public async Task RecordUsage_RunsOnCallerSynchronizationContext()
    {
        var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            using var queue = new System.Collections.Concurrent.BlockingCollection<(SendOrPostCallback Callback, object? State)>();
            var context = new QueueContext(queue);
            SynchronizationContext.SetSynchronizationContext(context);
            var owner = Environment.CurrentManagedThreadId;
            var recordedThread = 0;
            _store.OnRecordUsage = _ => recordedThread = Environment.CurrentManagedThreadId;
            _dispatcher.FocusOverride = async () => { await Task.Delay(10).ConfigureAwait(false); return FocusPath.Direct; };
            var task = Insert();
            task.ContinueWith(_ => queue.CompleteAdding(), TaskScheduler.Default);
            foreach (var work in queue.GetConsumingEnumerable()) work.Callback(work.State);
            try { Assert.Equal(InsertionOutcome.Inserted, task.GetAwaiter().GetResult().Outcome); Assert.Equal(owner, recordedThread); finished.SetResult(); }
            catch (Exception ex) { finished.SetException(ex); }
        });
        thread.Start();
        await finished.Task.WaitAsync(TimeSpan.FromSeconds(5));
        thread.Join();
    }

    private sealed class QueueContext(System.Collections.Concurrent.BlockingCollection<(SendOrPostCallback Callback, object? State)> queue) : SynchronizationContext
    {
        public override void Post(SendOrPostCallback callback, object? state) => queue.Add((callback, state));
    }

    [Fact]
    public async Task RecordUsage_DeletedDuringSend_StillInserted()
    {
        _dispatcher.OnSend = () => _store.DeleteSnippet(_id);
        Assert.Equal(InsertionOutcome.Inserted, (await Insert()).Outcome);
        Assert.Empty(_store.Snippets);
    }
    [Fact]
    public async Task CancelledBeforeCheck_NoEffects() { using var cts = new CancellationTokenSource(); cts.Cancel(); await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Insert(ct: cts.Token)); Assert.Empty(_calls); }
    [Fact]
    public async Task CancelledAfterSwap_Restores() { using var cts = new CancellationTokenSource(); _dispatcher.OnSwap = cts.Cancel; await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Insert(ct: cts.Token)); Assert.Contains("restore", _calls); Assert.Equal(0, Usage); }
    [Fact]
    public async Task CancelledAfterPaste_Ignored() { using var cts = new CancellationTokenSource(); _dispatcher.OnSend = cts.Cancel; Assert.Equal(InsertionOutcome.Inserted, (await Insert(ct: cts.Token)).Outcome); Assert.Contains("restore", _calls); Assert.Equal(1, Usage); }
    [Fact]
    public async Task Logs_ContainIdAndOutcomeButNotTextOrDisplayName()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ss-insert-log-" + Guid.NewGuid().ToString("N"));
        try
        {
            SmartSnippets.Core.Logging.AppLog.Initialize(directory);
            var secretId = _store.SeedSnippet("secret snippet sentinel").Id;
            var result = await _service.InsertAsync(new(1, 42, 0, "secret window title"), secretId);
            Assert.Equal(InsertionOutcome.Inserted, result.Outcome);
            var log = File.ReadAllText(Path.Combine(directory, "app.log"));
            Assert.Contains($"outcome=Inserted id={secretId}", log);
            Assert.DoesNotContain("secret snippet sentinel", log);
            Assert.DoesNotContain("secret window title", log);
        }
        finally { SmartSnippets.Core.Logging.AppLog.Shutdown(); Directory.Delete(directory, true); }
    }
    [Fact]
    public async Task SingleFlight_BusyThenNextRuns()
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var service = new InsertionService(_store, _probe, _dispatcher, delay: (t, _) => t.TotalMilliseconds == 250 ? gate.Task : Task.CompletedTask);
        var first = service.InsertAsync(Target, _id);
        Assert.Equal(InsertionOutcome.Busy, (await service.InsertAsync(Target, _id)).Outcome);
        gate.SetResult(); Assert.Equal(InsertionOutcome.Inserted, (await first).Outcome);
        Assert.Equal(InsertionOutcome.Inserted, (await service.InsertAsync(Target, _id)).Outcome);
    }
}
