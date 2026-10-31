using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using SmartSnippets.TargetIdentity018;

namespace SmartSnippets.WindowsInsertion018;

/// <summary>Only a coordinator-duplicated retained Process handle; never PID attach.</summary>
internal sealed partial class GuardianPin : IDisposable
{
    private readonly SafeProcessHandle process;
    private readonly string className;
    internal nint Window { get; }
    private GuardianPin(SafeProcessHandle process, nint window, Guid run) =>
        (this.process, Window, className) = (process, window, "SS_ClipboardGuardian018_" + run.ToString("N"));

    internal static GuardianPin FromCoordinatorHandle(SafeProcessHandle process, nint window, Guid run)
    {
        var pin = new GuardianPin(process, window, run);
        if (!pin.IsPinned()) { pin.Dispose(); throw new ProbeFailure("guardian_identity"); }
        return pin;
    }

    internal unsafe bool IsPinned()
    {
        if (process.IsInvalid || process.IsClosed || WaitForSingleObject(process, 0) != 258 || Window == 0 || IsWindow(Window) == 0 ||
            GetWindowThreadProcessId(Window, out var pid) == 0 || pid == (uint)Environment.ProcessId || GetProcessId(process) != pid ||
            !WindowsToken.IsSameUnelevatedToken(process)) return false;
        var image = stackalloc char[32768];
        uint size = 32768;
        var name = stackalloc char[128];
        return QueryFullProcessImageNameW(process, 0, image, ref size) != 0 &&
            new ReadOnlySpan<char>(image, (int)size).Equals(Environment.ProcessPath, StringComparison.OrdinalIgnoreCase) &&
            GetClassNameW(Window, name, 128) == className.Length && new ReadOnlySpan<char>(name, className.Length).SequenceEqual(className);
    }
    public void Dispose() => process.Dispose();
    [LibraryImport("kernel32.dll")] private static partial uint WaitForSingleObject(SafeProcessHandle process, uint time);
    [LibraryImport("kernel32.dll")] private static partial uint GetProcessId(SafeProcessHandle process);
    [LibraryImport("kernel32.dll")] private static unsafe partial int QueryFullProcessImageNameW(SafeProcessHandle process, uint flags, char* path, ref uint size);
    [LibraryImport("user32.dll")] private static partial int IsWindow(nint window);
    [LibraryImport("user32.dll")] private static partial uint GetWindowThreadProcessId(nint window, out uint pid);
    [LibraryImport("user32.dll")] private static unsafe partial int GetClassNameW(nint window, char* name, int size);
}
