using SmartSnippets.Core.Settings;

namespace SmartSnippets.Presentation.Services;

public enum PaletteRole { Background, Surface, Layer, Hover, Pressed, Accent, AccentHover, AccentPressed, OnAccent, AccentTint, AccentText, TextSelection }

/// <summary>Explicit opaque colors for surfaces and states; no runtime color blending.</summary>
public readonly record struct PaletteTones(uint Background, uint Surface, uint Layer, uint Hover, uint Pressed,
    uint Accent, uint AccentHover, uint AccentPressed, uint OnAccent, uint TextSelection)
{
    public uint Color(PaletteRole role) => role switch
    {
        PaletteRole.Background => Background,
        PaletteRole.Surface => Surface,
        PaletteRole.Layer => Layer,
        PaletteRole.Hover => Hover,
        PaletteRole.Pressed => Pressed,
        PaletteRole.AccentHover => AccentHover,
        PaletteRole.AccentPressed => AccentPressed,
        PaletteRole.OnAccent => OnAccent,
        PaletteRole.TextSelection => TextSelection,
        _ => Accent,
    };

    public static PaletteTones? Resolve(ColorPalette palette, bool dark, bool highContrast)
    {
        if (PaletteColors.Resolve(palette, dark, highContrast) is not { } anchors) return null;
        var (surface, layer, hover, pressed, accentHover, accentPressed) = (palette, dark) switch
        {
            (ColorPalette.Ocean, false) => (0xFFF8FBFEu, 0xFFEAF3FAu, 0xFFDFECF6u, 0xFFD5E5F1u, 0xFF00518Eu, 0xFF00467Cu),
            (ColorPalette.Ocean, true) => (0xFF192D3Cu, 0xFF152735u, 0xFF233D50u, 0xFF2C485Du, 0xFFA4D7FCu, 0xFF78BAEAu),
            (ColorPalette.Forest, false) => (0xFFF9FCF9u, 0xFFEAF3ECu, 0xFFDFECE2u, 0xFFD3E4D7u, 0xFF225532u, 0xFF1B4829u),
            (ColorPalette.Forest, true) => (0xFF1D3225u, 0xFF192C20u, 0xFF294333u, 0xFF334E3Du, 0xFFB0E5BFu, 0xFF88C99Bu),
            (ColorPalette.Plum, false) => (0xFFFCF9FDu, 0xFFF2EAF5u, 0xFFEADDEDu, 0xFFE1D2E6u, 0xFF683474u, 0xFF592B65u),
            (ColorPalette.Plum, true) => (0xFF342438u, 0xFF2C1E30u, 0xFF45304Au, 0xFF513B57u, 0xFFE9C9F0u, 0xFFCEA1D8u),
            _ => throw new ArgumentOutOfRangeException(nameof(palette)),
        };
        return new(anchors.Background, surface, layer, hover, pressed, anchors.Accent, accentHover, accentPressed,
            dark ? 0xFF101010u : 0xFFFFFFFFu,
            // Native text inputs use white selected text in both themes; keep a saturated
            // selection background rather than the pale dark-mode button accent.
            PaletteColors.Resolve(palette, dark: false, highContrast: false)!.Value.Accent);
    }
}
