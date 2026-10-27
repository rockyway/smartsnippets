using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

namespace SmartSnippets.App.Services;

internal readonly record struct ForegroundTargetInfo(HWND Hwnd, uint ProcessId, HMONITOR Monitor);

internal static class ForegroundTarget
{
    public static ForegroundTargetInfo? Capture()
    {
        var hwnd = PInvoke.GetForegroundWindow();
        if (hwnd.IsNull)
        {
            return null;
        }

        var threadId = PInvoke.GetWindowThreadProcessId(hwnd, out var pid);
        if (threadId == 0 || pid == 0 || pid == PInvoke.GetCurrentProcessId())
        {
            return null;
        }

        var monitor = PInvoke.MonitorFromWindow(hwnd, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
        return new ForegroundTargetInfo(hwnd, pid, monitor);
    }
}
