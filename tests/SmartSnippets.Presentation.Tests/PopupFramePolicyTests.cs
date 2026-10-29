using SmartSnippets.Presentation.Services;

namespace SmartSnippets.Presentation.Tests;

public sealed class PopupFramePolicyTests
{
    [Theory]
    [InlineData(true, 0x00606060)]
    [InlineData(false, 0x00909090)]
    public void NormalThemeUsesExplicitNeutralGreyNotSystemAccent(bool dark, int expected)
    {
        var color = PopupFramePolicy.BorderColor(dark, false, 255, 255, 255);
        Assert.Equal(expected, color);
        Assert.Equal(color & 255, (color >> 8) & 255);
        Assert.Equal(color & 255, (color >> 16) & 255);
        Assert.NotEqual(0x00FFFFFF, color);
        Assert.NotEqual(unchecked((int)0xFFFFFFFE), color); // Never suppress the only border.
    }

    [Theory]
    [InlineData(true, 0, 0, 0, 0x00000000)]
    [InlineData(false, 255, 255, 255, 0x00FFFFFF)]
    [InlineData(true, 0x12, 0x34, 0x56, 0x00563412)]
    [InlineData(false, 0x12, 0x34, 0x56, 0x00563412)]
    public void HighContrastOverridesThemeAndUsesWindowTextInColorrefOrder(
        bool dark, byte red, byte green, byte blue, int expected)
        => Assert.Equal(expected, PopupFramePolicy.BorderColor(dark, true, red, green, blue));
}
