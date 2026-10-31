using SmartSnippets.Presentation.Navigation;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Uno.Tests.Composition;

namespace SmartSnippets.Uno.Tests.Flows;

public sealed class ActivationFlowTests
{
    [Fact]
    public async Task RedirectsBeforeComposition_UseOneCandidateOwnerAndDropStaleShutdownCallbacks()
    {
        using var f = new HostFixture();
        var queue = new CandidateActivationQueue();
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
        var queue = new CandidateActivationQueue(); var dispatcher = new Dispatcher();
        var guard = new TaskCompletionSource(); var done = new TaskCompletionSource(); var applied = new List<PageKind?>();
        queue.Attach(dispatcher, async options => { applied.Add(options.Page); if (options.Page == PageKind.Folders) await guard.Task; else done.SetResult(); });
        queue.Enqueue(new(PageKind.Folders, false, null)); queue.Enqueue(new(PageKind.Settings, false, null));
        dispatcher.Actions.Dequeue()();
        Assert.Equal(new PageKind?[] { PageKind.Folders }, applied);
        guard.SetResult();
        await done.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(new PageKind?[] { PageKind.Folders, PageKind.Settings }, applied);
    }
    private sealed class Dispatcher : IUiDispatcher
    { internal Queue<Action> Actions { get; } = new(); public void Post(Action action) => Actions.Enqueue(action); }
}
