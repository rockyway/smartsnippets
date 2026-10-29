using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;

namespace SmartSnippets.App.Services;

/// <summary>Set before showing, including hidden prewarm; never toggles a material backdrop.</summary>
internal static partial class WindowSurface
{
    [LibraryImport("dwmapi.dll")]
    private static partial int DwmSetWindowAttribute(nint hwnd, uint attribute, in int value, uint size);

    [LibraryImport("user32.dll")]
    private static partial uint GetSysColor(int index);

    /// <summary>COLOR_WINDOWTEXT as RGB via plain user32; replaces UISettings.UIElementColor, the suspected source of the 0x80070490 startup failure.</summary>
    internal static (byte R, byte G, byte B) WindowTextColor()
    {
        var c = GetSysColor(8); // COLOR_WINDOWTEXT, 0x00BBGGRR
        return ((byte)(c & 0xFF), (byte)((c >> 8) & 0xFF), (byte)((c >> 16) & 0xFF));
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HighContrastInfo { public uint Size; public uint Flags; public nint Scheme; }

    [LibraryImport("user32.dll", EntryPoint = "SystemParametersInfoW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SystemParametersInfo(uint action, uint param, ref HighContrastInfo value, uint winIni);

    /// <summary>High-contrast state via SPI_GETHIGHCONTRAST; avoids the WinRT AccessibilitySettings object.</summary>
    internal static bool IsHighContrast()
    {
        var info = new HighContrastInfo { Size = (uint)Marshal.SizeOf<HighContrastInfo>() };
        return SystemParametersInfo(0x42, info.Size, ref info, 0) && (info.Flags & 1) != 0;
    }

    internal static void SetDark(Window window, bool dark)
    {
        var value = dark ? 1 : 0;
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        _ = DwmSetWindowAttribute(hwnd, 20, in value, sizeof(int));
        // No show/hide transition over an uninitialized native surface.
        var disableTransitions = 1;
        _ = DwmSetWindowAttribute(hwnd, 3, in disableTransitions, sizeof(int));
    }

    /// <summary>DWM owns both the popup's single physical-pixel border and its corner clip.</summary>
    internal static (int Corner, int Border) RoundPopup(Window window, int borderColor)
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        var round = 2; // DWMWCP_ROUND / DWMWA_WINDOW_CORNER_PREFERENCE
        var cornerResult = DwmSetWindowAttribute(hwnd, 33, in round, sizeof(int));
        // Explicit COLORREF prevents active/inactive accent/default-white frames.
        var borderResult = DwmSetWindowAttribute(hwnd, 34, in borderColor, sizeof(int));
        return (cornerResult, borderResult);
    }

    internal static void Cloak(Window window, bool cloak)
    {
        var value = cloak ? 1 : 0;
        _ = DwmSetWindowAttribute(WinRT.Interop.WindowNative.GetWindowHandle(window), 13, in value, sizeof(int));
    }
}
