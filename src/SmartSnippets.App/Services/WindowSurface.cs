using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;

namespace SmartSnippets.App.Services;

/// <summary>Set before showing, including hidden prewarm; never toggles a material backdrop.</summary>
internal static partial class WindowSurface
{
    [LibraryImport("dwmapi.dll")]
    private static partial int DwmSetWindowAttribute(nint hwnd, uint attribute, in int value, uint size);

    internal static void SetDark(Window window, bool dark)
    {
        var value = dark ? 1 : 0;
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        _ = DwmSetWindowAttribute(hwnd, 20, in value, sizeof(int));
        // No show/hide transition over an uninitialized native surface.
        var disableTransitions = 1;
        _ = DwmSetWindowAttribute(hwnd, 3, in disableTransitions, sizeof(int));
    }

    /// <summary>Windows 11 rounded borderless popup; XAML owns the single themed border.</summary>
    internal static void RoundPopup(Window window)
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        var round = 2; // DWMWCP_ROUND / DWMWA_WINDOW_CORNER_PREFERENCE
        _ = DwmSetWindowAttribute(hwnd, 33, in round, sizeof(int));
        var noBorder = -2; // DWMWA_COLOR_NONE / DWMWA_BORDER_COLOR
        _ = DwmSetWindowAttribute(hwnd, 34, in noBorder, sizeof(int));
    }

    internal static void Cloak(Window window, bool cloak)
    {
        var value = cloak ? 1 : 0;
        _ = DwmSetWindowAttribute(WinRT.Interop.WindowNative.GetWindowHandle(window), 13, in value, sizeof(int));
    }
}
