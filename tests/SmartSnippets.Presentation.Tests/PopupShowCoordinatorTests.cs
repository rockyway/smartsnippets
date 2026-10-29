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
        private PopupShowCoordinator Create() => new(action => Work.Enqueue(action), target => Applied.Add(target));
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
        surface.Shows.CompleteWarmup(); surface.Drain();
        Assert.Equal(1, surface.ShowCount);
        Assert.Equal(new[] { PopupFocusTarget.Footer }, surface.Applied);
    }

    [Fact]
    public void LaterFocusSupersedesQueuedSearch_AndHideCancelsQueuedFocus()
    {
        var host = new AsyncSurfaceHost(); host.Shows.CompleteWarmup();
        host.ShowAgain(); host.Focus(PopupFocusTarget.Footer); host.Drain();
        Assert.Equal(new[] { PopupFocusTarget.Footer }, host.Applied);
        host.ShowAgain(); host.Hide(); host.Drain();
        Assert.Single(host.Applied);
    }

    [Fact]
    public void NewerShowReplacesOldFocus_AndClosedSurfaceCannotStealFocus()
    {
        var host = new AsyncSurfaceHost(); host.Shows.BeginWarmup();
        host.ShowAgain(); host.Focus(PopupFocusTarget.Footer);
        host.ShowAgain();
        host.Shows.CompleteWarmup();
        host.Replace(); host.Drain();
        Assert.Empty(host.Applied);
        host.ShowAgain(); host.Shows.CompleteWarmup(); host.Drain();
        Assert.Equal(new[] { PopupFocusTarget.Search }, host.Applied);
    }
}
