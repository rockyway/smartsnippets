using Windows.Win32;
using Windows.Win32.Foundation;

namespace SmartSnippets.App.Services;

internal static class ForegroundActivation
{
    public static bool TryActivate(HWND hwnd, out string path)
    {
        PInvoke.SetForegroundWindow(hwnd);
        if (PInvoke.GetForegroundWindow() == hwnd) { path = "direct"; return true; }
        var current = PInvoke.GetCurrentThreadId();
        var foreground = PInvoke.GetForegroundWindow();
        var thread = foreground.IsNull ? 0 : PInvoke.GetWindowThreadProcessId(foreground, out _);
        var attached = thread != 0 && thread != current && PInvoke.AttachThreadInput(current, thread, true);
        try { PInvoke.SetForegroundWindow(hwnd); }
        finally { if (attached) PInvoke.AttachThreadInput(current, thread, false); }
        var success = PInvoke.GetForegroundWindow() == hwnd;
        path = success ? "attach" : "failed";
        return success;
    }
}
