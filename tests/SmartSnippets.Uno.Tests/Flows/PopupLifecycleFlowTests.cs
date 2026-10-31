using SmartSnippets.Presentation.Navigation;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Uno.Tests.Composition;

namespace SmartSnippets.Uno.Tests.Flows;

public sealed class PopupLifecycleFlowTests
{
    [Fact]
    public async Task WarmupHideReopen_RejectsStaleReadyAndFlushesCache()
    {
        using var f = new HostFixture();
        await f.Start(background: true);
        var surface = f.Boundary.Popups.Single();
        surface.Dispose();
        f.Host.ShowPreview("first");
        surface = f.Boundary.Popups.Last();
        Assert.Equal(1, surface.Prewarms);
        surface.AutoReady = false;
        // Cancel a cold generation and prove its completion cannot reveal a newer request.
        surface.Lifecycle.Shows.Close();
        var reveals = 0;
        var lifecycle = new CandidatePopupLifecycle(a => a(), _ => { }, () => { }, () => { });
        lifecycle.Preview(f.Host.Popup, "first", () => { reveals++; return new(true, null); });
        var stale = lifecycle.Shows.WarmupGeneration;
        lifecycle.Shows.Hide();
        lifecycle.Preview(f.Host.Popup, "second", () => { reveals++; return new(true, null); });
        lifecycle.Shows.CompleteWarmup(stale);
        Assert.Equal(0, reveals);
        var rebuilds = f.Host.Popup.CacheRebuildCount;
        f.Host.Snippets.NewCommand.Execute(null);
        f.Host.Snippets.Editor!.Text = "synthetic lifecycle row";
        Assert.True(await f.Host.Snippets.Editor.SaveAsync());
        lifecycle.Shows.CompleteWarmup(lifecycle.Shows.WarmupGeneration);
        Assert.Equal(1, reveals);
        Assert.True(f.Host.Popup.CacheRebuildCount > rebuilds);
        Assert.Equal("second", f.Host.Popup.Query);
        lifecycle.Shows.Close();
        lifecycle.Preview(f.Host.Popup, null, () => { reveals++; return new(true, null); });
        Assert.Equal(1, reveals);
    }

    [Fact]
    public async Task PreviewEditor_HideClosedAndMainExitCancel_ResumeWithoutResettingQuery()
    {
        using var f = new HostFixture();
        await f.Start();
        f.Host.ShowPreview("preserved query");
        f.Host.Popup.AddSnippetCommand.Execute(null);
        var editor = f.Host.Popup.Editor;
        f.Boundary.Popups.Last().Hide();
        f.Host.ShowPreview("must not replace");
        Assert.Same(editor, f.Host.Popup.Editor);
        Assert.Equal("preserved query", f.Host.Popup.Query);
        f.Boundary.Popups.Last().Dispose();
        f.Host.ShowPreview("also ignored");
        var replacement = f.Boundary.Popups.Last();
        Assert.Equal(1, replacement.Prewarms);
        Assert.Equal(1, replacement.Shows);
        f.Host.Snippets.NewCommand.Execute(null);
        f.Host.Snippets.Editor!.Text = "dirty main editor";
        f.Boundary.Dialogs.Choices.Enqueue(SaveChoice.Cancel);
        await f.Host.ExitAsync();
        Assert.Equal(0, f.Boundary.Exits);
        Assert.Equal(2, replacement.Shows);
        Assert.Same(editor, f.Host.Popup.Editor);
        Assert.Equal("preserved query", f.Host.Popup.Query);
    }

    [Fact]
    public async Task HiddenNavigationCancel_DoesNotRevealUnlessDialogNeedsHost()
    {
        using var f = new HostFixture();
        await f.Start(background: true);
        f.Host.Snippets.NewCommand.Execute(null);
        f.Host.Snippets.Editor!.Text = "dirty";
        await f.Host.ApplyLaunchOptionsAsync(new(PageKind.Settings, false, null));
        Assert.False(f.Boundary.Main.Visible);
        Assert.Equal(PageKind.Snippets, f.Boundary.Main.Current);
        object? root = new();
        var visible = false;
        var shows = 0;
        Assert.Same(root, CandidateUiPolicy.DialogRoot(() => root, () => visible, () => { visible = true; shows++; }));
        Assert.Equal(1, shows);
        CandidateUiPolicy.DialogRoot(() => root, () => visible, () => shows++);
        Assert.Equal(1, shows);
        root = null;
        CandidateUiPolicy.DialogRoot(() => root, () => visible, () => { root = new(); shows++; });
        Assert.Equal(2, shows);
    }
}
