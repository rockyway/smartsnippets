using SmartSnippets.Presentation.Services;
using SmartSnippets.TargetIdentity018;
using Xunit;

public sealed class OwnedTargetProbeTests
{
    [Fact]
    public void SamePidOtherWindowIsNotTargetForeground()
    {
        var lease = new Lease();
        using var native = new Identity();
        using var probe = new OwnedTargetProbe(lease, native);
        var a = probe.Capture("a");
        var b = probe.Capture("b");
        Assert.Equal(TargetCheck.Ok, probe.Check(a));
        native.Foreground = b.WindowHandle;
        Assert.False(probe.IsTargetForeground(a));
        Assert.True(probe.IsTargetForeground(b));
        native.Foreground = a.WindowHandle;
        Assert.True(probe.IsTargetForeground(a));
    }

    [Fact]
    public void ClosedForgedForeignAndUnpinnedTargetsFailClosed()
    {
        var lease = new Lease();
        using var native = new Identity();
        using var probe = new OwnedTargetProbe(lease, native);
        var a = probe.Capture("a");
        Assert.Equal(TargetCheck.Closed, probe.Check(a with { WindowHandle = 999 }));
        Assert.Equal(TargetCheck.Closed, probe.Check(a with { ProcessId = 43 }));
        native.Closed = true;
        Assert.Equal(TargetCheck.Closed, probe.Check(a));
        Assert.False(probe.IsTargetForeground(a));
        native.Closed = false;
        lease.Pinned = false;
        Assert.Equal(TargetCheck.Closed, probe.Check(a));
        Assert.False(probe.IsTargetForeground(a));
        Assert.Throws<InvalidOperationException>(() => probe.Capture("a"));
    }

    [Fact]
    public void OwnProcessAndDisposedProbeAreNeverForegroundEligible()
    {
        var lease = new Lease();
        var native = new Identity();
        var probe = new OwnedTargetProbe(lease, native);
        var a = probe.Capture("a");
        native.Pid = (uint)Environment.ProcessId;
        a = a with { ProcessId = native.Pid };
        Assert.Equal(TargetCheck.OwnProcess, probe.Check(a));
        Assert.False(probe.IsTargetForeground(a));
        probe.Dispose();
        probe.Dispose();
        Assert.Equal(1, native.DisposeCount);
        Assert.Equal(TargetCheck.Closed, probe.Check(a));
        Assert.Throws<ObjectDisposedException>(() => probe.Capture("a"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("a title")]
    [InlineData("windowNumber=1")]
    public void OnlyNamedFixtureWindowsCanBeCaptured(string name)
    {
        using var probe = new OwnedTargetProbe(new Lease(), new Identity());
        Assert.Throws<InvalidOperationException>(() => probe.Capture(name));
    }

    [Fact]
    public void ProvenanceIsRecheckedAfterForegroundQuery()
    {
        var lease = new Lease();
        var native = new Identity { OnForeground = () => lease.Pinned = false };
        using var probe = new OwnedTargetProbe(lease, native);
        Assert.False(probe.IsTargetForeground(probe.Capture("a")));
    }

    private sealed class Lease : IOwnedFixtureLease
    {
        public uint ExpectedPid => 42;
        internal bool Pinned = true;
        public bool IsAliveAndPinned() => Pinned;
    }

    private sealed class Identity : IExactWindowIdentity
    {
        internal uint Pid = 42;
        internal nint Foreground = 1;
        internal bool Closed;
        internal int DisposeCount;
        internal Action? OnForeground;
        public nint Capture(string fixtureWindow) => fixtureWindow == "a" ? 1 : 2;
        public bool Exists(nint identity) => !Closed && identity is 1 or 2;
        public uint ProcessId(nint identity) => Pid;
        public bool IsForeground(nint identity) { OnForeground?.Invoke(); return Foreground == identity; }
        public void Dispose() => DisposeCount++;
    }
}
