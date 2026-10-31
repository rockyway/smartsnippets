using SmartSnippets.Presentation.Services;
using SmartSnippets.Presentation.ViewModels;
using SmartSnippets.UnoProbe;

namespace SmartSnippets.UnoFlow.Tests;

public sealed class ProbeFlowCompositionTests
{
    private static (ProbeFlowComposition Flow, FakeOS OS) Create()
    {
        var os = new FakeOS();
        var flow = ProbeFlowComposition.Create(os, action => action());
        os.Flow = flow;
        return (flow, os);
    }
    private static void Ready(ProbeFlowComposition flow) => flow.OnLayoutReady(flow.WarmupGeneration);
    private static int Usage(ProbeFlowComposition flow) => flow.Store.Snippets.Single().Snippet.UsageCount;

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ShortcutAndMenuToEnterAndRowDispatch(bool menu, bool row)
    {
        var (flow, os) = Create(); using var lifetime = flow;
        if (menu) flow.OnMenu(); else flow.OnShortcut();
        Assert.Equal(new[] { "capture", "warmup" }, os.Events);
        Assert.Equal(os.Captured, flow.Popup.Target);
        await flow.OnEnterAsync(); // unfinished surface cannot dispatch
        Assert.Equal(0, os.Dispatches);
        Ready(flow);
        Assert.Equal(new[] { "capture", "warmup", "stop", "show", "focus" }, os.Events);
        if (row) await flow.OnRowAsync(flow.Popup.Items[flow.Popup.SelectedIndex]);
        else await flow.OnEnterAsync();
        Assert.Equal(1, os.Dispatches);
        Assert.Equal(1, os.Restores);
        Assert.Equal(1, Usage(flow));
        Assert.Equal("Synthetic flow λ 日本語 😀\r\nsecond line", os.SwappedText);
        Assert.Equal(os.Captured, os.RestoredTarget);
        Assert.True(os.Events.IndexOf("hide") < os.Events.IndexOf("restore-focus"));
    }

    [Fact]
    public async Task MainRowAfterPopupOpenRetainsOriginalWindow()
    {
        var (flow, os) = Create(); using var lifetime = flow;
        flow.OnShortcut(); Ready(flow);
        flow.OnOpenMain();
        Assert.Equal("main", os.Events.Last());
        await flow.OnMainInsertAsync(flow.Main.List.Items.Single().Id);
        Assert.Equal(1, os.Captures);
        Assert.Equal(os.Captured, os.RestoredTarget);
        Assert.Equal(1, Usage(flow));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PreviewEnterAndRowNeverDispatchOrCopy(bool row)
    {
        var (flow, os) = Create(); using var lifetime = flow;
        flow.OnPreview(); Ready(flow);
        Assert.True(flow.Popup.IsPreview); Assert.Null(flow.Popup.Target);
        flow.Popup.CopyRow(flow.Popup.Items[flow.Popup.SelectedIndex]);
        flow.OnPopupCopy(flow.Popup.Items[flow.Popup.SelectedIndex]);
        if (row) await flow.OnRowAsync(flow.Popup.Items[flow.Popup.SelectedIndex]);
        else await flow.OnEnterAsync();
        await flow.OnMainInsertAsync(flow.SnippetId);
        Assert.Contains("hide", os.Events);
        Assert.Equal(0, os.Captures + os.Dispatches + os.Copies + os.Swaps);
        Assert.Equal(0, Usage(flow));
        Assert.DoesNotContain("focus", os.Events);
    }

    [Fact]
    public async Task CopyDoesNotDispatchOrCreditUsage()
    {
        var (flow, os) = Create(); using var lifetime = flow;
        flow.OnMenu(); Ready(flow); flow.OnMainCopy(flow.SnippetId);
        flow.Popup.CopyRow(flow.Popup.Items[flow.Popup.SelectedIndex]);
        Assert.Equal(2, os.Copies);
        Assert.Equal(0, os.Dispatches + os.Swaps + Usage(flow));
        await Task.CompletedTask;
    }

    [Theory]
    [InlineData(DispatchState.Uncertain, PopupFooterState.MaybePasted)]
    [InlineData(DispatchState.NotDispatched, PopupFooterState.Error)]
    public async Task FailureOrUncertainDispatchNeverCreditsUsage(DispatchState state, PopupFooterState footer)
    {
        var (flow, os) = Create(); using var lifetime = flow;
        os.Dispatch = state;
        flow.OnShortcut(); Ready(flow); await flow.OnEnterAsync();
        Assert.Equal(footer, flow.Popup.FooterState);
        Assert.Equal(2, os.Events.Count(e => e == "show"));
        Assert.Equal(0, Usage(flow)); Assert.Equal(1, os.Restores);
        if (state == DispatchState.Uncertain)
        {
            await flow.OnEnterAsync(); // caution dismiss, not a retry
            Assert.Equal(1, os.Dispatches);
            Assert.Equal(PopupFooterState.None, flow.Popup.FooterState);
        }
    }

    [Fact]
    public async Task SamePidOtherWindowRejectedAtFinalGate()
    {
        var (flow, os) = Create(); using var lifetime = flow;
        os.Foreground = os.Captured with { WindowHandle = 202 };
        flow.OnShortcut(); Ready(flow); await flow.OnEnterAsync();
        Assert.Equal(0, os.Dispatches + Usage(flow));
        Assert.Equal(1, os.Swaps); Assert.Equal(1, os.Restores);
        Assert.Equal(PopupFooterState.Error, flow.Popup.FooterState);
    }

    [Theory]
    [InlineData(SwapStatus.NotPreserved, PopupFooterState.ConfirmClipboardLoss)]
    [InlineData(SwapStatus.Failed, PopupFooterState.Error)]
    public async Task UnsafeClipboardNeverPastes(SwapStatus status, PopupFooterState footer)
    {
        var (flow, os) = Create(); using var lifetime = flow;
        os.SwapStatus = status;
        flow.OnMenu(); Ready(flow); await flow.OnEnterAsync();
        Assert.Equal(footer, flow.Popup.FooterState);
        Assert.Equal(2, os.Events.Count(e => e == "show"));
        if (status == SwapStatus.NotPreserved) Assert.Equal(PopupFocusTarget.Footer, os.Focuses.Last());
        Assert.Equal(0, os.Dispatches + os.Restores + Usage(flow));
    }

    [Fact]
    public async Task ClosedTargetNeverTouchesClipboard()
    {
        var (flow, os) = Create(); using var lifetime = flow;
        os.TargetCheck = TargetCheck.Closed;
        flow.OnShortcut(); Ready(flow); await flow.OnEnterAsync();
        Assert.Equal(0, os.Swaps + os.Dispatches + Usage(flow));
        Assert.Equal(PopupFooterState.Error, flow.Popup.FooterState);
    }

    [Fact]
    public async Task RapidDuplicatesAreSuppressedAndServiceReturnsBusy()
    {
        var (flow, os) = Create(); using var lifetime = flow;
        os.PendingFocus = new TaskCompletionSource<FocusPath?>();
        flow.OnShortcut(); Ready(flow);
        var first = flow.OnEnterAsync();
        Assert.True(flow.Popup.IsInserting);
        Assert.Equal(0, Usage(flow));
        await flow.OnEnterAsync();
        await flow.OnRowAsync(flow.Popup.Items[flow.Popup.SelectedIndex]);
        await flow.OnMainInsertAsync(flow.SnippetId);
        var busy = await flow.Insertion.InsertAsync(os.Captured, flow.SnippetId);
        Assert.Equal(InsertionOutcome.Busy, busy.Outcome);
        Assert.Equal(0, os.Dispatches + Usage(flow));
        os.PendingFocus.SetResult(FocusPath.Direct); await first;
        Assert.Equal(1, os.Dispatches); Assert.Equal(1, Usage(flow));
    }

    [Fact]
    public async Task HideCancelsWarmupAndStaleReadinessCannotReveal()
    {
        var (flow, os) = Create(); using var lifetime = flow;
        flow.OnPreview(); var stale = flow.WarmupGeneration;
        flow.OnHide(); flow.OnPreview();
        flow.OnLayoutReady(stale);
        Assert.DoesNotContain("show", os.Events);
        await flow.OnEnterAsync(); Assert.Equal(0, os.Dispatches);
        Ready(flow); Assert.Contains("show", os.Events);
    }

    [Fact]
    public async Task DefaultBoundaryRefusesNativeCaptureAndDispatch()
    {
        var os = new RefusingOS();
        using var flow = ProbeFlowComposition.Create(os, action => action());
        flow.OnShortcut(); flow.OnMenu();
        Assert.False(flow.IsWarming); Assert.Null(flow.Popup.Target);
        var result = await flow.Insertion.InsertAsync(new(101, 77, 0, ""), flow.SnippetId);
        Assert.Equal(InsertionOutcome.TargetClosed, result.Outcome);
        Assert.Equal(0, Usage(flow));
    }

    private class RefusingOS : ProbeFlowBoundary
    {
        public override void StartWarmup() { }
        public override void StopWarmup() { }
        public override void ShowPopup() { }
        public override void HidePopup() { }
        public override void ShowMain() { }
        public override void Focus(PopupFocusTarget target) { }
    }
    private sealed class FakeOS : RefusingOS
    {
        public ProbeFlowComposition Flow = null!;
        public List<string> Events { get; } = [];
        public List<PopupFocusTarget> Focuses { get; } = [];
        public InsertionTarget Captured { get; } = new(101, 77, 0, "SYNTHETIC_TITLE_018");
        public InsertionTarget Foreground { get; set; } = new(101, 77, 0, "SYNTHETIC_TITLE_018");
        public InsertionTarget? RestoredTarget;
        public int Captures, Dispatches, Copies, Swaps, Restores;
        public DispatchState Dispatch = DispatchState.Dispatched;
        public SwapStatus SwapStatus = SwapStatus.Swapped;
        public TargetCheck TargetCheck = TargetCheck.Ok;
        public string? SwappedText;
        public TaskCompletionSource<FocusPath?>? PendingFocus;
        public override InsertionTarget CaptureTarget() { Captures++; Events.Add("capture"); return Captured; }
        public override void StartWarmup() { if (Flow.Popup.IsPreview) Assert.Null(Flow.Popup.Target); else Assert.Equal(Captured, Flow.Popup.Target); Events.Add("warmup"); }
        public override void StopWarmup() => Events.Add("stop");
        public override void ShowPopup() => Events.Add("show");
        public override void HidePopup() => Events.Add("hide");
        public override void ShowMain() => Events.Add("main");
        public override void Focus(PopupFocusTarget target) { Events.Add("focus"); Focuses.Add(target); }
        public override TargetCheck Check(InsertionTarget target) { Assert.Equal(Captured, target); return TargetCheck; }
        public override bool IsTargetForeground(InsertionTarget target) => target.WindowHandle == Foreground.WindowHandle && target.ProcessId == Foreground.ProcessId;
        public override Task<FocusPath?> RestoreFocusAsync(InsertionTarget target, CancellationToken ct)
        {
            RestoredTarget = target; Events.Add("restore-focus");
            return PendingFocus?.Task ?? Task.FromResult<FocusPath?>(FocusPath.Direct);
        }
        public override bool ReleaseHeldModifiers() => true;
        public override ClipboardSwap TrySwapClipboard(string text, bool allowIncomplete)
        {
            Assert.False(allowIncomplete); Swaps++; SwappedText = text;
            return new(SwapStatus, SwapStatus == SwapStatus.Swapped ? new Snapshot() : null);
        }
        public override DispatchState SendPaste()
        {
            Assert.Equal(0, Usage(Flow)); // dies if usage is credited before confirmed dispatch
            Assert.Equal(Captured.WindowHandle, Foreground.WindowHandle);
            Assert.Contains("hide", Events);
            Dispatches++; return Dispatch;
        }
        public override bool ReleasePasteKeys() => true;
        public override RestoreStatus TryRestoreClipboard(IClipboardSnapshot snapshot) { Restores++; return RestoreStatus.Restored; }
        public override void SetText(string text) => Copies++;
    }
    private sealed class Snapshot : IClipboardSnapshot
    {
        public int SavedCount => 1;
        public int SkippedByDesignCount => 0;
        public int UnsavedCount => 0;
        public bool CapExceeded => false;
        public long ByteCount => 0;
    }
}
