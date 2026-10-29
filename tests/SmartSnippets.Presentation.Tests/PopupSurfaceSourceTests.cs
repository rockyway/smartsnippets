using System.Xml.Linq;

namespace SmartSnippets.Presentation.Tests;

/// <summary>Source contracts, not a substitute for XAML compilation or physical-pixel UI QA.</summary>
public sealed class PopupSurfaceSourceTests
{
    private static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

    [Fact]
    public void NativePerimeterHasNoCompetingXamlBorderOrCornerClip()
    {
        var document = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "UiContracts", "Popup", "PopupWindow.xaml"));
        var root = Assert.Single(document.Descendants().Where(e => (string?)e.Attribute(Xaml + "Name") == "RootGrid"));
        Assert.Null(root.Attribute("BorderThickness"));
        Assert.Null(root.Attribute("BorderBrush"));
        Assert.Null(root.Attribute("CornerRadius"));
        Assert.Equal("{ThemeResource SolidBackgroundFillColorBaseBrush}", (string?)root.Attribute("Background"));
        Assert.DoesNotContain(document.Descendants(), e => e.Attribute("BorderThickness") is not null);
    }
}
