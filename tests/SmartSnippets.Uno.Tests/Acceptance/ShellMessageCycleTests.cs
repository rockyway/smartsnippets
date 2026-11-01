namespace SmartSnippets.Uno.Tests.Acceptance;

public sealed class ShellMessageCycleTests
{
    [Fact]
    public void ExactlyTwentyNativeLifetimesRetireBeforeCountingResources()
    {
        var created = 0; var disposed = 0; var samples = 0;
        ShellMessageCycles.Run(() => { created++; return new Window(() => disposed++); },
            () => { Assert.Equal(created, disposed); samples++; return (10, 2); },
            handle => { Assert.Equal(created, disposed); Assert.Equal((nint)123, handle); return false; });
        Assert.Equal(20, created); Assert.Equal(20, disposed); Assert.Equal(21, samples);
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    public void SurvivingWindowOrHandleOrUserObjectCannotPass(bool alive, bool handles, bool user)
    {
        var disposed = 0; var sample = 0;
        Assert.Throws<InvalidOperationException>(() => ShellMessageCycles.Run(
            () => new Window(() => disposed++),
            () => sample++ == 0 ? (10u, 2u) : (handles ? 11u : 10u, user ? 3u : 2u), _ => alive));
        Assert.Equal(1, disposed);
    }

    [Fact]
    public void FailedHandleObservationStillDisposesWindow()
    {
        var disposed = false;
        Assert.Throws<InvalidOperationException>(() => ShellMessageCycles.Run(
            () => new Window(() => disposed = true, true), () => (10u, 2u), _ => false));
        Assert.True(disposed);
    }
    private sealed class Window(Action dispose, bool fail = false) : IShellMessageWindow
    {
        public nint Handle => fail ? throw new InvalidOperationException("fixed") : 123;
        public void Dispose() => dispose();
    }
}
