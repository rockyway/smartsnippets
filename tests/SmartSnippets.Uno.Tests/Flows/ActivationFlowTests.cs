using SmartSnippets.Presentation.Navigation;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Uno.Tests.Composition;

namespace SmartSnippets.Uno.Tests.Flows;

public sealed class ActivationFlowTests
{
    [Fact]
    public async Task RedirectsBeforeComposition_UseOneAppOwnerAndDropStaleShutdownCallbacks()
    {
        using var f = new HostFixture();
        var queue = new ActivationQueue();
        queue.Enqueue(new(PageKind.Folders, false, null));
        queue.Enqueue(new(PageKind.Settings, false, null));
        await f.Start();
        var store = f.Host.Store;
        queue.Attach(f.Boundary.Dispatcher, options => f.Host.ApplyLaunchOptionsAsync(options));
        f.Boundary.Drain();
        Assert.Equal(PageKind.Settings, f.Boundary.Main.Current);
        Assert.Same(store, f.Host.Store);
        Assert.Equal(1, f.Boundary.Messages);
        queue.Enqueue(new(PageKind.Folders, false, null));
        queue.Close(); f.Boundary.Drain();
        Assert.Equal(PageKind.Settings, f.Boundary.Main.Current);
        queue.Enqueue(new(PageKind.Snippets, false, null)); f.Boundary.Drain();
        Assert.Equal(PageKind.Settings, f.Boundary.Main.Current);
    }
    [Fact]
    public async Task Redirects_AwaitUnsavedGuardBeforeStartingNextNavigation()
    {
        var queue = new ActivationQueue(); var dispatcher = new Dispatcher();
        var guard = new TaskCompletionSource(); var done = new TaskCompletionSource(); var applied = new List<PageKind?>();
        queue.Attach(dispatcher, async options => { applied.Add(options.Page); if (options.Page == PageKind.Folders) await guard.Task; else done.SetResult(); });
        queue.Enqueue(new(PageKind.Folders, false, null)); queue.Enqueue(new(PageKind.Settings, false, null));
        dispatcher.Actions.Dequeue()();
        Assert.Equal(new PageKind?[] { PageKind.Folders }, applied);
        guard.SetResult();
        await done.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(new PageKind?[] { PageKind.Folders, PageKind.Settings }, applied);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QaPreview_InitialAndRedirectedForwardQueryAndSortWithoutShowingMain(bool redirected)
    {
        using var f = new HostFixture();
        var options = new LaunchOptions(null, true, "synthetic query", true);
        if (!redirected) await f.Start(options: options);
        else
        {
            await f.Start(background: true);
            var queue = new ActivationQueue();
            queue.Enqueue(options);
            queue.Attach(f.Boundary.Dispatcher, o => f.Host.ApplyLaunchOptionsAsync(o));
            f.Boundary.Drain();
        }
        var popup = Assert.Single(f.Boundary.Popups);
        Assert.Equal(1, popup.Shows);
        Assert.Equal("synthetic query", popup.PreviewQuery);
        Assert.True(popup.PreviewSortMenu);
        Assert.True(f.Host.Popup.IsPreview);
        Assert.Null(f.Host.Popup.Target);
        Assert.False(f.Boundary.Main.Visible);
    }

    [Fact]
    public async Task QaPageNavigation_AwaitsDirtyGuardBeforePreview()
    {
        using var f = new HostFixture(); await f.Start(background: true);
        var guard = new TaskCompletionSource<bool>();
        f.Boundary.Main.ExitGuard = () => guard.Task;
        var apply = f.Host.ApplyLaunchOptionsAsync(new(PageKind.Folders, true, "query", true), false);
        Assert.False(apply.IsCompleted);
        Assert.Equal(0, f.Boundary.Popups[0].Shows);
        guard.SetResult(true);
        await apply;
        Assert.Equal(PageKind.Folders, f.Boundary.Main.Current);
        Assert.Equal(1, f.Boundary.Popups[0].Shows);
        Assert.False(f.Boundary.Main.Visible);
    }

    private sealed class Dispatcher : IUiDispatcher
    { internal Queue<Action> Actions { get; } = new(); public void Post(Action action) => Actions.Enqueue(action); }
}
