using SmartSnippets.Core.Layout;

namespace SmartSnippets.Uno.Tests.Flows;

public sealed class CandidateMonitorPolicyTests
{
    [Fact]
    public void BottomLeft_ChoosesBottomOfLeftmostColumn_NotLowestOverallOrPrimary()
    {
        PixelRect[] monitors = [new(0, 0, 1920, 1080), new(-1920, 0, 0, 1080),
            new(-1920, 1080, 0, 2160), new(0, 2160, 1920, 3240)];
        Assert.Equal(2, CandidateUiPolicy.BottomLeftMonitorIndex(monitors));
        Array.Reverse(monitors);
        Assert.Equal(1, CandidateUiPolicy.BottomLeftMonitorIndex(monitors));
    }
    [Fact]
    public void BottomLeft_SingleAndNegativeOriginAreSupported()
    {
        Assert.Equal(0, CandidateUiPolicy.BottomLeftMonitorIndex([new(-1200, -900, 0, 0)]));
        Assert.Equal(1, CandidateUiPolicy.BottomLeftMonitorIndex([new(-1200, -1800, 0, -900), new(-1200, -900, 0, 0)]));
    }
    [Fact]
    public void BottomLeft_RefusesMissingOrInvalidMonitorGeometry()
    {
        Assert.Throws<InvalidOperationException>(() => CandidateUiPolicy.BottomLeftMonitorIndex([]));
        Assert.Throws<InvalidOperationException>(() => CandidateUiPolicy.BottomLeftMonitorIndex([new(0, 0, 0, 1080)]));
    }
}
