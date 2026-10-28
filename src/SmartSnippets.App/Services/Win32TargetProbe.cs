using SmartSnippets.Presentation.Services;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace SmartSnippets.App.Services;

internal sealed class Win32TargetProbe : ITargetProbe
{
    private readonly TargetIdentityProbe _probe = new(
        handle => PInvoke.IsWindow(new HWND(handle)),
        GetProcessId,
        GetForeground,
        PInvoke.GetCurrentProcessId,
        ElevationProbe.IsBlocked);

    public TargetCheck Check(InsertionTarget target) => _probe.Check(target);
    public bool IsTargetForeground(InsertionTarget target) => _probe.IsTargetForeground(target);

    private static uint GetProcessId(nint handle) =>
        PInvoke.GetWindowThreadProcessId(new HWND(handle), out var pid) == 0 ? 0 : pid;

    private static unsafe nint GetForeground() => (nint)PInvoke.GetForegroundWindow().Value;
}
