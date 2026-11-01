namespace SmartSnippets.Uno.Tests.Acceptance;

public sealed class ShellOwnedPostTests
{
    [Fact]
    public void OnlyRetainedLiveIdentityReachesPostAfterAuthentication()
    {
        var events = new List<string>();
        ShellOwnedPost.Run(12, () => { events.Add("expected"); return 12; }, 123,
            hwnd => { Assert.Equal((nint)12, hwnd); events.Add("identity"); return (456, 123); },
            hwnd => { Assert.Equal((nint)12, hwnd); events.Add("alive"); return true; },
            () => { events.Add("post"); return true; });
        Assert.Equal(["expected", "identity", "alive", "expected", "post"], events);
    }

    [Theory]
    [InlineData(0, 12, 456, 123, true)] // Zero destination.
    [InlineData(12, 0, 456, 123, true)] // Disposed retained source.
    [InlineData(13, 12, 456, 123, true)] // Another HWND, even in the same PID.
    [InlineData(12, 12, 0, 123, true)] // GetWindowThreadProcessId failure with stale PID output.
    [InlineData(12, 12, 456, 0, true)]
    [InlineData(12, 12, 456, 999, true)] // Reused/foreign-PID HWND.
    [InlineData(12, 12, 456, 123, false)] // Stale HWND.
    public void ZeroStaleForeignOrWrongRetainedWindowNeverPosts(int hwnd, int expected, uint thread, uint pid, bool alive)
    {
        var posts = 0;
        var error = Assert.Throws<InvalidOperationException>(() => ShellOwnedPost.Run(hwnd, () => expected, 123,
            _ => (thread, pid), _ => alive, () => { posts++; return true; }));
        Assert.Equal("shell_post", error.Message);
        Assert.Equal(0, posts);
    }

    [Fact]
    public void RetainedHandleChangeDuringAuthenticationNeverPosts()
    {
        nint retained = 12; var posts = 0;
        Assert.Throws<InvalidOperationException>(() => ShellOwnedPost.Run(12, () => retained, 123,
            _ => { retained = 13; return (456, 123); }, _ => true, () => { posts++; return true; }));
        Assert.Equal(0, posts);
    }

    [Fact]
    public void NativePostFailureCannotBecomeSuccess()
    {
        Assert.Throws<InvalidOperationException>(() => ShellOwnedPost.Run(12, () => 12, 123,
            _ => (456, 123), _ => true, () => false));
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(12, false)]
    public void HiddenMainCannotClaimDeadMessageWindowIsLive(int handle, bool alive)
    {
        Assert.Throws<InvalidOperationException>(() => ShellMessageState.RequireLive(handle, _ => alive));
    }

    [Theory]
    [InlineData(12, 12, true)]
    [InlineData(0, 12, true)] // Handle cleared but native destruction leaked.
    [InlineData(0, 0, false)]
    public void ExitCannotClaimUndisposedOrLeakedMessageWindowIsClean(int current, int retired, bool alive)
    {
        Assert.Throws<InvalidOperationException>(() => ShellMessageState.RequireDisposed(current, retired, _ => alive));
    }

    [Fact]
    public void LiveAndDisposedChecksUseTheExactMessageWindowHandle()
    {
        ShellMessageState.RequireLive(12, hwnd => { Assert.Equal((nint)12, hwnd); return true; });
        ShellMessageState.RequireDisposed(0, 12, hwnd => { Assert.Equal((nint)12, hwnd); return false; });
    }
}
