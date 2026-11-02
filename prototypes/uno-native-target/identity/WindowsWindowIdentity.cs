using System.Runtime.InteropServices;

namespace SmartSnippets.TargetIdentity018;

internal sealed partial class WindowsWindowIdentity(IOwnedFixtureLease lease, Guid run, TargetReadiness readiness) : IExactWindowIdentity
{
    private bool disposed;
    private readonly nint windowA = readiness.WindowA, windowB = readiness.WindowB;
    private readonly uint expectedPid = lease.ExpectedPid;
    private readonly uint threadA = GetWindowThreadProcessId(readiness.WindowA, out _);
    private readonly uint threadB = GetWindowThreadProcessId(readiness.WindowB, out _);
    private readonly string expectedClass = "SS_Target018_" + run.ToString("N");

    public nint Capture(string fixtureWindow)
    {
        if (disposed || windowA == 0 || windowB == 0 || windowA == windowB) return 0;
        var window = fixtureWindow switch { "a" => windowA, "b" => windowB, _ => 0 };
        return Exists(window) ? window : 0;
    }

    public unsafe bool Exists(nint identity)
    {
        if (disposed || !lease.IsAliveAndPinned() || expectedPid == (uint)Environment.ProcessId || !OperatingSystem.IsWindows() || identity == 0 ||
            (identity != windowA && identity != windowB) || IsWindow(identity) == 0 ||
            ProcessId(identity) != expectedPid ||
            GetWindowThreadProcessId(identity, out _) != (identity == windowA ? threadA : threadB) ||
            (identity == windowA ? threadA : threadB) == 0) return false;
        var editor = identity == windowA ? readiness.EditorA : readiness.EditorB;
        if (editor == 0 || GetDlgItem(identity, 101) != editor || IsChild(identity, editor) == 0 || ProcessId(editor) != expectedPid) return false;
        var name = stackalloc char[128];
        var count = GetClassNameW(identity, name, 128);
        return count == expectedClass.Length && new ReadOnlySpan<char>(name, count).SequenceEqual(expectedClass);
    }

    public uint ProcessId(nint identity) => !disposed && OperatingSystem.IsWindows() &&
        GetWindowThreadProcessId(identity, out var pid) != 0 ? pid : 0;

    public bool IsForeground(nint identity) => Exists(identity) && ForegroundIdentity.Root(GetForegroundWindow()) == identity &&
        ProcessId(identity) == expectedPid;

    public void Dispose() => disposed = true; // HWNDs belong to the fixture, never destroyed here.

    [LibraryImport("user32.dll")] private static partial nint GetDlgItem(nint window, int id);
    [LibraryImport("user32.dll")] private static partial int IsChild(nint parent, nint child);
    [LibraryImport("user32.dll")] private static partial int IsWindow(nint window);
    [LibraryImport("user32.dll")] private static partial uint GetWindowThreadProcessId(nint window, out uint pid);
    [LibraryImport("user32.dll")] private static partial nint GetForegroundWindow();
    [LibraryImport("user32.dll")] private static unsafe partial int GetClassNameW(nint window, char* name, int capacity);
}
