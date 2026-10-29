using SmartSnippets.Core.Settings;
using SmartSnippets.Presentation.Services;

namespace SmartSnippets.Presentation.Tests;

public sealed class PaletteColorsTests
{
    [Theory]
    [InlineData(ColorPalette.Ocean, false, 0xFFF2F8FCu, 0xFF005A9Eu)]
    [InlineData(ColorPalette.Ocean, true, 0xFF101F2Bu, 0xFF8CCBFAu)]
    [InlineData(ColorPalette.Forest, false, 0xFFF3F8F3u, 0xFF28613Bu)]
    [InlineData(ColorPalette.Forest, true, 0xFF14241Bu, 0xFF9BD9ADu)]
    [InlineData(ColorPalette.Plum, false, 0xFFF9F4FAu, 0xFF753C83u)]
    [InlineData(ColorPalette.Plum, true, 0xFF241925u, 0xFFDFB5E8u)]
    public void Resolve_UsesApprovedAnchors(ColorPalette palette, bool dark, uint background, uint accent)
    {
        Assert.Equal(new PaletteColors(background, accent), PaletteColors.Resolve(palette, dark, false));
        Assert.Null(PaletteColors.Resolve(palette, dark, true));
    }

    [Theory]
    [InlineData(ColorPalette.Ocean, false)]
    [InlineData(ColorPalette.Ocean, true)]
    [InlineData(ColorPalette.Forest, false)]
    [InlineData(ColorPalette.Forest, true)]
    [InlineData(ColorPalette.Plum, false)]
    [InlineData(ColorPalette.Plum, true)]
    public void Tones_StatesHaveReadableForegroundsAndDistinctSelection(ColorPalette palette, bool dark)
    {
        var tones = PaletteTones.Resolve(palette, dark, false)!.Value;
        Assert.Null(PaletteTones.Resolve(palette, dark, true));
        Assert.Equal(PaletteColors.Resolve(palette, dark, false)!.Value.Background, tones.Background);
        foreach (var background in new[] { tones.Background, tones.Surface, tones.Layer, tones.Hover, tones.Pressed })
        {
            Assert.True(Contrast(background, dark ? 0xFFFFFFFF : 0xFF000000) >= 4.5);
            foreach (var accent in new[] { tones.Accent, tones.AccentHover, tones.AccentPressed })
                Assert.True(Contrast(background, accent) >= 3);
        }
        foreach (var accent in new[] { tones.Accent, tones.AccentHover, tones.AccentPressed })
            Assert.True(Contrast(accent, tones.OnAccent) >= 4.5);
        Assert.True(Contrast(tones.TextSelection, 0xFFFFFFFF) >= 4.5);
        Assert.NotEqual(tones.Accent, tones.AccentHover);
        Assert.NotEqual(tones.AccentHover, tones.AccentPressed);
        // High contrast overrides any palette, and leaving it restores the exact same variant.
        Assert.Equal(tones, PaletteTones.Resolve(palette, dark, false));
    }

    private static double Contrast(uint a, uint b)
    {
        static double Linear(uint channel)
        {
            var c = channel / 255d;
            return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }
        static double Luminance(uint color) => 0.2126 * Linear((color >> 16) & 255)
            + 0.7152 * Linear((color >> 8) & 255) + 0.0722 * Linear(color & 255);
        var l1 = Luminance(a);
        var l2 = Luminance(b);
        return (Math.Max(l1, l2) + 0.05) / (Math.Min(l1, l2) + 0.05);
    }

    [Fact]
    public void DefaultAndUnknown_UseSystemResources()
    {
        Assert.Null(PaletteColors.Resolve(ColorPalette.Default, false, false));
        Assert.Null(PaletteColors.Resolve((ColorPalette)99, true, false));
        Assert.Null(PaletteTones.Resolve(ColorPalette.Default, false, false));
        Assert.Null(PaletteTones.Resolve((ColorPalette)99, true, false));
    }
}
