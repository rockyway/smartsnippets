using SmartSnippets.Presentation.Services;

namespace SmartSnippets.Presentation.Tests;

public sealed class OverlayHoverPlacementTests
{
    [Fact]
    public void PrefersRightAndAlignsWithWindowTop()
    {
        var p = HoverDetailsPlacement.PlaceBesideWindow(-200, -100, 1600, 900, 0, 640, 120, 480, 200)!.Value;
        Assert.Equal((644d, 120d, 480d, 200d), p);
    }

    [Fact]
    public void CompanionMatchesOverlayHeightRatherThanContentTooltipLimit()
    {
        var p = HoverDetailsPlacement.PlaceBesideWindow(-200, -100, 1600, 900, 0, 640, -1, 480, 480)!.Value;
        Assert.Equal(644, p.X);
        Assert.Equal(-1, p.Y);
        Assert.Equal(480, p.Height); // Not the ordinary tooltip's 320-pixel maximum.
    }

    [Fact]
    public void UsesVisibleFrameHeightAndFourPixelGap()
    {
        // DWM visible bounds may be smaller than the native window's resize bounds.
        var p = HoverDetailsPlacement.PlaceBesideWindow(-200, -100, 1600, 900, -1, 625, -1, 480, 472)!.Value;
        Assert.Equal(629, p.X);
        Assert.Equal(-1, p.Y);
        Assert.Equal(472, p.Height);
    }

    [Fact]
    public void FallsBackLeftWhenRightCannotFit()
    {
        var p = HoverDetailsPlacement.PlaceBesideWindow(-1000, -100, 700, 900, 0, 640, 120, 480, 200)!.Value;
        Assert.Equal(-484, p.X);
        Assert.True(p.X + p.Width < 0);
    }

    [Fact]
    public void ReducesWidthOnLargerSideWhenNeitherSideFits()
    {
        var p = HoverDetailsPlacement.PlaceBesideWindow(-200, 0, 940, 700, 0, 640, 100, 480, 200)!.Value;
        Assert.Equal(644, p.X);
        Assert.Equal(296, p.Width);
        Assert.Equal(200, p.Height);
    }

    [Fact]
    public void ReducedLeftSideDoesNotOverlapWindow()
    {
        var p = HoverDetailsPlacement.PlaceBesideWindow(-300, 0, 700, 700, 0, 640, 100, 480, 200)!.Value;
        Assert.Equal(-300, p.X);
        Assert.Equal(296, p.Width);
        Assert.Equal(-4, p.X + p.Width);
    }

    [Theory]
    [InlineData(-900, -300)]
    [InlineData(900, 0)]
    public void ClampsVerticallyWithNegativeMonitorOrigins(double targetTop, double expectedY)
    {
        var p = HoverDetailsPlacement.PlaceBesideWindow(-1000, -300, 1000, 200, 0, 640, targetTop, 240, 200)!.Value;
        Assert.Equal(expectedY, p.Y);
    }

    [Fact]
    public void CapsSizeToContentLimitsAndWorkArea()
    {
        var p = HoverDetailsPlacement.PlaceBesideWindow(-1000, -50, 2000, 150, 0, 640, 100, 2000, 2000)!.Value;
        Assert.Equal(480, p.Width);
        Assert.Equal(200, p.Height);
        Assert.Equal(-50, p.Y);
    }

    [Fact]
    public void NoSideSpaceSuppressesSurfaceRatherThanCoveringOverlay()
    {
        Assert.Null(HoverDetailsPlacement.PlaceBesideWindow(0, 0, 640, 480, 0, 640, 100, 480, 200));
        Assert.Null(HoverDetailsPlacement.PlaceBesideWindow(-40, 0, 680, 480, 0, 640, 100, 480, 200));
        Assert.Null(HoverDetailsPlacement.PlaceBesideWindow(-1000, 0, 2000, 20, 0, 640, 100, 480, 200));
    }
}
