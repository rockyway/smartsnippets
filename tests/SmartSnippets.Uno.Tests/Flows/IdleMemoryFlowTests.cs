using SmartSnippets.Uno.Tests.Composition;

namespace SmartSnippets.Uno.Tests.Flows;

public sealed class IdleMemoryFlowTests
{
    [Fact]
    public async Task PendingOrVisiblePopupPreventsTrimmingAndHidingRestoresAdmission()
    {
        using var f = new HostFixture();
        f.Boundary.PopupAutoReady = false;
        await f.Start(background: true);
        Assert.True(f.Boundary.MemoryTrimAdmission!()); // Native boundary separately requires a hidden, ready popup.
        f.Host.ShowPreview();
        Assert.False(f.Host.CanTrimIdleMemory);
        f.Boundary.Popups[0].Ready();
        Assert.False(f.Host.CanTrimIdleMemory);
        f.Boundary.Popups[0].Hide();
        Assert.True(f.Host.CanTrimIdleMemory);
        await f.Host.ExitAsync();
        Assert.False(f.Host.CanTrimIdleMemory);
        Assert.Equal(1, f.Boundary.MemoryTrimmerDisposals);
    }

    [Fact]
    public async Task ManagementAndDirtyEditorPreventTrimmingWithoutDestroyingState()
    {
        using var f = new HostFixture();
        await f.Start(background: true);
        var done = new TaskCompletionSource();
        var operation = f.Host.RunOperationAsync(() => done.Task);
        Assert.False(f.Host.CanTrimIdleMemory);
        done.SetResult(); await operation;
        Assert.True(f.Host.CanTrimIdleMemory);
        await f.Host.Snippets.NewAsync();
        var editor = f.Host.Snippets.Editor!;
        editor.Text = "synthetic dirty draft";
        Assert.False(f.Host.CanTrimIdleMemory);
        Assert.Same(editor, f.Host.Snippets.Editor);
        f.Host.Dispose();
        Assert.False(f.Host.CanTrimIdleMemory);
        Assert.Equal(1, f.Boundary.MemoryTrimmerDisposals);
    }
}
