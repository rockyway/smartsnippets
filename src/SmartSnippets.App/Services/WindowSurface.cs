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

    internal static void Cloak(Window window, bool cloak)
    {
        var value = cloak ? 1 : 0;
        _ = DwmSetWindowAttribute(WinRT.Interop.WindowNative.GetWindowHandle(window), 13, in value, sizeof(int));
    }
}
