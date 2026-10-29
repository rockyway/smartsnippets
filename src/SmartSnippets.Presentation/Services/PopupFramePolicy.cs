namespace SmartSnippets.Presentation.Services;

/// <summary>Single native popup border; COLORREF is 0x00BBGGRR, not ARGB.</summary>
public static class PopupFramePolicy
{
    public static int BorderColor(bool dark, bool highContrast, byte windowTextRed, byte windowTextGreen, byte windowTextBlue)
    {
        if (highContrast)
            return windowTextRed | (windowTextGreen << 8) | (windowTextBlue << 16);

        return dark ? 0x00606060 : 0x00909090;
    }
}
