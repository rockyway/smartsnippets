using System.Runtime.InteropServices;

namespace SmartSnippets.Uno;

internal static partial class WindowDiagnosticNative
{
    internal static WindowDiagnosticFacts Sample(nint window, nint a, nint b) => WindowDiagnosticFacts.Capture(
        window, a, b, ClassName, h => h != 0 && IsWindowVisible(h) != 0,
        h => GetWindowLongPtrW(h, -20), h => GetWindow(h, 4), IsMessageOnly,
        h => GetWindowThreadProcessId(h, out _), h => DwmGetWindowAttribute(h, 14, out var value, 4) >= 0 ? value : null);
    private static unsafe string ClassName(nint window)
    {
        var buffer = stackalloc char[256];
        var count = GetClassNameW(window, buffer, 256);
        return new string(buffer, 0, Math.Max(0, count));
    }
    [LibraryImport("user32.dll")] private static partial int IsWindowVisible(nint window);
    [LibraryImport("user32.dll")] private static partial nint GetWindowLongPtrW(nint window, int index);
    [LibraryImport("user32.dll")] private static partial nint GetWindow(nint window, uint command);
    // HWND_MESSAGE is a lookup sentinel, not assumed to be an actual parent HWND.
    private static bool IsMessageOnly(nint window)
    {
        if (window == 0 || GetAncestor(window, 1) == 0) return false;
        nint child = 0;
        for (var i = 0; i < 256; i++)
        {
            child = FindWindowExW(-3, child, 0, 0);
            if (child == 0) break;
            if (child == window) return true;
        }
        return false; // Bounded diagnostic only; never admission.
    }
    [LibraryImport("user32.dll")] private static partial nint FindWindowExW(nint parent, nint after, nint className, nint title);
    [LibraryImport("user32.dll")] private static partial nint GetAncestor(nint window, uint flags);
    [LibraryImport("user32.dll")] private static partial uint GetWindowThreadProcessId(nint window, out uint pid);
    [LibraryImport("user32.dll")] private static unsafe partial int GetClassNameW(nint window, char* text, int capacity);
    [LibraryImport("dwmapi.dll")] private static partial int DwmGetWindowAttribute(nint window, uint attribute, out int value, uint size);
}
