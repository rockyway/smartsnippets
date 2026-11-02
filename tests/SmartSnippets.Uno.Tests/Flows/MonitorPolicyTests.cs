using SmartSnippets.Core.Layout;

namespace SmartSnippets.Uno.Tests.Flows;

public sealed class MonitorPolicyTests
{
    [Theory]
    [InlineData(false, false, 0, 1)]
    [InlineData(false, true, 0, 1)]
    [InlineData(true, false, 1, 1)]
    [InlineData(true, true, 1, 0)]
    public void Management_OrdinaryAlwaysCentersPrimary_QaParksOnlyWhenAvailable(bool qa, bool monitorPresent, int parks, int centers)
    {
        int parked = 0, centered = 0;
        Assert.True(UiPolicy.PlaceManagement(qa, () => { parked++; return monitorPresent; },
            () => { centered++; return true; }));
        Assert.Equal(parks, parked); Assert.Equal(centers, centered);
    }

    [Fact]
    public void Management_NoPlacementSuccessIsNotReportedAsSuccess()
        => Assert.False(UiPolicy.PlaceManagement(true, () => false, () => false));

    [Theory]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    public void StartupFailure_OnlyExplicitInteractiveLaunchShowsModal(bool background, bool qa, bool interactive)
    {
        int dialogs = 0, quiet = 0;
        UiPolicy.ReportStartupFailure(background, qa, () => dialogs++, () => quiet++);
        Assert.Equal(interactive ? 1 : 0, dialogs);
        Assert.Equal(interactive ? 0 : 1, quiet);
    }

    [Fact]
    public void BottomLeft_ChoosesBottomOfLeftmostColumn_NotLowestOverallOrPrimary()
    {
        PixelRect[] monitors = [new(0, 0, 1920, 1080), new(-1920, 0, 0, 1080),
            new(-1920, 1080, 0, 2160), new(0, 2160, 1920, 3240)];
        Assert.Equal(2, UiPolicy.BottomLeftMonitorIndex(monitors));
        Array.Reverse(monitors);
        Assert.Equal(1, UiPolicy.BottomLeftMonitorIndex(monitors));
    }
    [Fact]
    public void BottomLeft_SingleAndNegativeOriginAreSupported()
    {
        Assert.Equal(0, UiPolicy.BottomLeftMonitorIndex([new(-1200, -900, 0, 0)]));
        Assert.Equal(1, UiPolicy.BottomLeftMonitorIndex([new(-1200, -1800, 0, -900), new(-1200, -900, 0, 0)]));
    }
    [Fact]
    public void BottomLeft_RefusesMissingOrInvalidMonitorGeometry()
    {
        Assert.Throws<InvalidOperationException>(() => UiPolicy.BottomLeftMonitorIndex([]));
        Assert.Throws<InvalidOperationException>(() => UiPolicy.BottomLeftMonitorIndex([new(0, 0, 0, 1080)]));
    }
}
