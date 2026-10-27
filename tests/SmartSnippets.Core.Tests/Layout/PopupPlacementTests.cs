using SmartSnippets.Core.Layout;

namespace SmartSnippets.Core.Tests.Layout;

public class WindowPlacementTests
{
    [Fact]
    public void CentresInWorkArea_NegativeOrigin()
    {
        var work = new PixelRect(-1920, -100, 0, 980);

        var p = PopupPlacement.CenterIn(work, 640, 480);

        Assert.Equal(new PixelPoint(-1280, 200), p);
    }

    [Fact]
    public void CentresInWorkArea_PositiveOffset()
    {
        var work = new PixelRect(1920, 0, 3840, 1040);

        var p = PopupPlacement.CenterIn(work, 600, 400);

        Assert.Equal(new PixelPoint(2580, 320), p);
    }

    [Fact]
    public void ClampsWhenPopupLargerThanWorkArea()
    {
        var work = new PixelRect(100, 50, 500, 350);

        var p = PopupPlacement.CenterIn(work, 640, 480);

        Assert.Equal(new PixelPoint(100, 50), p);
    }

    [Fact]
    public void ExactFit_PlacesAtOrigin()
    {
        var work = new PixelRect(0, 0, 640, 480);

        Assert.Equal(new PixelPoint(0, 0), PopupPlacement.CenterIn(work, 640, 480));
    }
}
