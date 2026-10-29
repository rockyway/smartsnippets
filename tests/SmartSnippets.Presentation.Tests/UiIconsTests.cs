using SmartSnippets.Presentation.Text;

namespace SmartSnippets.Presentation.Tests;

public sealed class UiIconsTests
{
    [Fact]
    public void ImportExportGlyphsMatchPlan010D7()
    {
        Assert.Equal("\uE896", UiIcons.Import);
        Assert.Equal("\uE898", UiIcons.Export);
        Assert.NotEqual(UiIcons.Import, UiIcons.Export);
    }
}
