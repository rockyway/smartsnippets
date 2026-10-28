using System.Diagnostics;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Presentation.Text;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

namespace SmartSnippets.App.Services;

internal static class TargetCapture
{
    private static readonly Dictionary<string, string> Names = new(StringComparer.OrdinalIgnoreCase);

    public static InsertionTarget? CaptureForeground()
    {
        var target = ForegroundTarget.Capture();
        return target is null ? null : Create(target.Value);
    }

    public static InsertionTarget? CaptureFrom(HWND hwnd)
    {
        if (hwnd.IsNull || !PInvoke.IsWindow(hwnd)) return null;
        var thread = PInvoke.GetWindowThreadProcessId(hwnd, out var pid);
        if (thread == 0 || pid == 0 || pid == PInvoke.GetCurrentProcessId()) return null;
        var monitor = PInvoke.MonitorFromWindow(hwnd, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
        return Create(new ForegroundTargetInfo(hwnd, pid, monitor));
    }

    private static unsafe InsertionTarget Create(ForegroundTargetInfo info)
    {
        var chars = stackalloc char[512];
        var length = PInvoke.GetWindowText(info.Hwnd, new Span<char>(chars, 512));
        var title = new string(chars, 0, Math.Max(0, length));
        var exe = string.Empty;
        var friendly = string.Empty;
        try
        {
            using var process = Process.GetProcessById((int)info.ProcessId);
            exe = process.ProcessName;
            var path = process.MainModule?.FileName;
            if (!string.IsNullOrEmpty(path))
            {
                if (!Names.TryGetValue(path, out friendly!))
                {
                    friendly = FileVersionInfo.GetVersionInfo(path).FileDescription ?? string.Empty;
                    Names[path] = friendly;
                }
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or ArgumentException) { }
        return new InsertionTarget((nint)info.Hwnd.Value, info.ProcessId, (nint)info.Monitor.Value, TargetDisplayName.Format(friendly, exe, title));
    }
}
