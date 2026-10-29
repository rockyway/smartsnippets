using SmartSnippets.Presentation.Services;
using SmartSnippets.Presentation.Tests.Fakes;
using SmartSnippets.Presentation.ViewModels;

namespace SmartSnippets.Presentation.Tests;

public sealed class PopupShowCoordinatorTests
{
    private sealed class AsyncSurfaceHost : IPopupHost
    {
        public readonly Queue<Action> Work = new();
        public readonly List<PopupFocusTarget> Applied = [];
        public PopupShowCoordinator Shows { get; private set; }
        public int ShowCount { get; private set; }
        public AsyncSurfaceHost() => Shows = Create();
        private PopupShowCoordinator Create()
        {
            var coordinator = new PopupShowCoordinator(action => Work.Enqueue(action), target => Applied.Add(target));
            coordinator.BeginWarmup();
            return coordinator;
        }
        public void Ready() => Shows.CompleteWarmup(Shows.WarmupGeneration);
        public void Replace() { Shows.Close(); Shows = Create(); Shows.BeginWarmup(); }
        public void Hide() => Shows.Hide();
        public void ShowAgain() => Shows.RequestShow(() => { ShowCount++; return new(true, PopupFocusTarget.Search); });
        public void Focus(PopupFocusTarget target) => Shows.Focus(target);
        public void OpenMainWindow() { }
        public void EditSnippet(long id) { }
        public void Drain() { while (Work.TryDequeue(out var action)) action(); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReplacementClipboardLoss_PreservesFooterFocusThroughWarmupAndResume(bool suspended)
    {
        var store = new FakeSnippetStore(); store.SeedSnippet("synthetic");
        var surface = new AsyncSurfaceHost();
        var host = new DeferredPopupHost(surface);
        var insertion = new FakeInsertionService();
        insertion.Results.Enqueue(new(InsertionOutcome.ClipboardNotPreserved, ""));
        insertion.OnCall = () => { surface.Replace(); if (suspended) host.Suspend(); };
        using var vm = new PopupViewModel(store, new MemorySettingsStore(), new(), insertion,
            new FakeClipboardService(), new FakeElevation(), host, new ScriptedDialogService(), new RecordingNotifier());
        vm.Open(new(1, 2, 3, "synthetic target"));
        await vm.InsertSelectedAsync();
        Assert.True(vm.IsConfirmingClipboardLoss);
        if (suspended) host.Resume();
        surface.Drain();
        Assert.Empty(surface.Applied);
        Assert.Equal(0, surface.ShowCount);
        surface.Ready(); surface.Drain();
        Assert.Equal(1, surface.ShowCount);
        Assert.Equal(new[] { PopupFocusTarget.Footer }, surface.Applied);
    }

    [Fact]
    public void HideBeforeFirstLayout_CancelledExitThenHotkeyRestartsInitialization()
    {
        var surface = new AsyncSurfaceHost();
        var host = new DeferredPopupHost(surface);
        var abandoned = surface.Shows.WarmupGeneration;
        host.Suspend(); host.Hide();
        Assert.False(surface.Shows.IsWarming);
        host.Resume(); // Exit was cancelled, without an insertion recovery request.
        surface.ShowAgain(); // The retained surface receives the next hotkey/show.
        Assert.True(surface.Shows.IsWarming);
        Assert.NotEqual(abandoned, surface.Shows.WarmupGeneration);
        surface.Shows.CompleteWarmup(abandoned); surface.Drain();
        Assert.Equal(0, surface.ShowCount);
        surface.Ready(); surface.Drain();
        Assert.Equal(1, surface.ShowCount);
        Assert.Equal(new[] { PopupFocusTarget.Search }, surface.Applied);
    }

    [Fact]
    public void TimeoutRestartsOnce_PreservesShowAndFocus_RejectsStaleCompletion()
    {
        var work = new Queue<Action>(); var applied = new List<PopupFocusTarget>();
        var starts = 0; var stops = 0; var shown = 0;
        var gate = new PopupShowCoordinator(work.Enqueue, applied.Add, () => starts++, () => stops++);
        gate.RequestShow(() => { shown++; return new(true, PopupFocusTarget.Search); });
        gate.Focus(PopupFocusTarget.Footer);
        var old = gate.WarmupGeneration;
        gate.WarmupTimedOut(old);
        Assert.Equal(2, starts); Assert.Equal(1, stops);
        gate.CompleteWarmup(old); Assert.Equal(0, shown);
        gate.CompleteWarmup(gate.WarmupGeneration);
        while (work.TryDequeue(out var action)) action();
        Assert.Equal(1, shown); Assert.Equal(2, stops);
        Assert.Equal(new[] { PopupFocusTarget.Footer }, applied);
    }

    [Fact]
    public void RepeatedTimeoutStaysHidden_ReportsFailure_NextRequestCanRestart()
    {
        var failures = 0; var shown = 0;
        var gate = new PopupShowCoordinator(action => action(), _ => { }, warmupFailed: () => failures++);
        gate.RequestShow(() => { shown++; return new(true, null); });
        gate.WarmupTimedOut(gate.WarmupGeneration);
        gate.WarmupTimedOut(gate.WarmupGeneration);
        Assert.False(gate.IsWarming); Assert.Equal(0, shown); Assert.Equal(1, failures);
        gate.RequestShow(() => { shown++; return new(true, null); });
        Assert.True(gate.IsWarming);
        gate.CompleteWarmup(gate.WarmupGeneration);
        Assert.Equal(1, shown);
    }

    [Fact]
    public void HiddenOrRejectedShowCannotApplyFocus_AndLatestShowWins()
    {
        var surface = new AsyncSurfaceHost(); var obsolete = 0;
        surface.Shows.RequestShow(() => { obsolete++; return new(true, PopupFocusTarget.Editor); });
        surface.Shows.Focus(PopupFocusTarget.Footer);
        surface.Shows.RequestShow(() => default); // Ignored/blocked show supersedes the old one.
        surface.Ready(); surface.Drain();
        Assert.Equal(0, obsolete); Assert.Empty(surface.Applied);
        surface.Focus(PopupFocusTarget.Search); surface.Drain();
        Assert.Empty(surface.Applied);
    }

    [Fact]
    public void LaterFocusSupersedesQueuedSearch_AndHideCancelsQueuedFocus()
    {
        var host = new AsyncSurfaceHost(); host.Ready();
        host.ShowAgain(); host.Focus(PopupFocusTarget.Footer); host.Drain();
        Assert.Equal(new[] { PopupFocusTarget.Footer }, host.Applied);
        host.ShowAgain(); host.Hide(); host.Drain();
        Assert.Single(host.Applied);
    }

    [Fact]
    public void NonActivatingPreviewSupersedesQueuedFocusFromPreviousShow()
    {
        var surface = new AsyncSurfaceHost(); surface.Ready();
        surface.ShowAgain();
        surface.Shows.RequestShow(() => new(true, null));
        surface.Drain();
        Assert.Empty(surface.Applied);
    }

    [Fact]
    public void NewerShowReplacesOldFocus_AndClosedSurfaceCannotStealFocus()
    {
        var host = new AsyncSurfaceHost(); host.Shows.BeginWarmup();
        host.ShowAgain(); host.Focus(PopupFocusTarget.Footer);
        host.ShowAgain();
        host.Ready();
        host.Replace(); host.Drain();
        Assert.Empty(host.Applied);
        host.ShowAgain(); host.Ready(); host.Drain();
        Assert.Equal(new[] { PopupFocusTarget.Search }, host.Applied);
    }
}
