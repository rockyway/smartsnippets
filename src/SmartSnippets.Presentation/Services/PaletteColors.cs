using SmartSnippets.Core.Settings;

namespace SmartSnippets.Presentation.Services;

/// <summary>Opaque palette anchor colors. Default and high contrast use system resources instead.</summary>
public readonly record struct PaletteColors(uint Background, uint Accent)
{
    public static PaletteColors? Resolve(ColorPalette palette, bool dark, bool highContrast)
    {
        if (highContrast) return null;
        return (palette, dark) switch
        {
            (ColorPalette.Ocean, false) => new(0xFFF2F8FC, 0xFF005A9E),
            (ColorPalette.Ocean, true) => new(0xFF101F2B, 0xFF8CCBFA),
            (ColorPalette.Forest, false) => new(0xFFF3F8F3, 0xFF28613B),
            (ColorPalette.Forest, true) => new(0xFF14241B, 0xFF9BD9AD),
            (ColorPalette.Plum, false) => new(0xFFF9F4FA, 0xFF753C83),
            (ColorPalette.Plum, true) => new(0xFF241925, 0xFFDFB5E8),
            _ => null,
        };
    }
}
