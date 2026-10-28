using SmartSnippets.Presentation.Services;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace SmartSnippets.App.Services;

internal sealed class Win32TargetProbe : ITargetProbe
{
    public TargetCheck Check(InsertionTarget target)
    {
        var hwnd = new HWND(target.WindowHandle);
        if (!PInvoke.IsWindow(hwnd) || PInvoke.GetWindowThreadProcessId(hwnd, out var pid) == 0 || pid != target.ProcessId)
            return TargetCheck.Closed;
        if (pid == PInvoke.GetCurrentProcessId()) return TargetCheck.OwnProcess;
        return ElevationProbe.IsBlocked(pid) ? TargetCheck.Elevated : TargetCheck.Ok;
    }

    public bool IsTargetForeground(InsertionTarget target)
    {
        var hwnd = PInvoke.GetForegroundWindow();
        return hwnd == new HWND(target.WindowHandle) && PInvoke.GetWindowThreadProcessId(hwnd, out var pid) != 0 && pid == target.ProcessId;
    }
}
