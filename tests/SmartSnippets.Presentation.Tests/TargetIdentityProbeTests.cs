using SmartSnippets.Presentation.Services;

namespace SmartSnippets.Presentation.Tests;

public sealed class TargetIdentityProbeTests
{
    [Fact]
    public void ReusedHwnd_DifferentPid_IsClosedAndNotForeground()
    {
        var target = new InsertionTarget(123, 10, 7, "original");
        var elevationCalled = false;
        var probe = new TargetIdentityProbe(_ => true, _ => 11, () => 123, () => 99, _ => { elevationCalled = true; return false; });
        Assert.Equal(TargetCheck.Closed, probe.Check(target));
        Assert.False(probe.IsTargetForeground(target));
        Assert.False(elevationCalled);
    }

    [Fact]
    public void MatchingPidButDifferentForeground_IsNotForeground()
    {
        var probe = new TargetIdentityProbe(_ => true, _ => 10, () => 456, () => 99, _ => false);
        Assert.False(probe.IsTargetForeground(new(123, 10, 0, "app")));
    }
}
