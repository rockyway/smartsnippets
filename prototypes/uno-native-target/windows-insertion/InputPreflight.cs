using System.Runtime.InteropServices;
using SmartSnippets.Presentation.Services;

namespace SmartSnippets.WindowsInsertion018;

internal static partial class InputPreflight
{
    internal static unsafe bool QuietOwnedTarget(ITargetProbe probe, InsertionTarget target)
    {
        var flags = new ObjectFlags();
        if (GetUserObjectFlags(GetProcessWindowStation(), 1, ref flags, (uint)sizeof(ObjectFlags), out _) == 0 || (flags.Flags & 1) == 0) return false;
        var desktop = OpenInputDesktop(0, 0, 1);
        if (desktop == 0) return false;
        try
        {
            var current = GetThreadDesktop(GetCurrentThreadId());
            var a = stackalloc char[256];
            var b = stackalloc char[256];
            if (current == 0 || GetUserObjectInformationW(desktop, 2, a, 512, out _) == 0 ||
                GetUserObjectInformationW(current, 2, b, 512, out _) == 0) return false;
            var lengthA = NullLength(a);
            var lengthB = NullLength(b);
            if (lengthA <= 0 || lengthB != lengthA || !new ReadOnlySpan<char>(a, lengthA).SequenceEqual(new ReadOnlySpan<char>(b, lengthB))) return false;
            var last = new LastInput { Size = (uint)sizeof(LastInput) };
            if (GetLastInputInfo(ref last) == 0 || unchecked((uint)GetTickCount64() - last.Tick) < 1500) return false;
            for (var key = 1; key < 256; key++) if ((GetAsyncKeyState(key) & 0x8000) != 0) return false;
        }
        finally { CloseDesktop(desktop); }
        // Close our desktop handle BEFORE the final exact owned-window gate.
        return probe.Check(target) == TargetCheck.Ok && probe.IsTargetForeground(target);
    }
    private static unsafe int NullLength(char* data)
    {
        for (var index = 0; index < 256; index++) if (data[index] == 0) return index;
        return -1;
    }
    [StructLayout(LayoutKind.Sequential)] private struct ObjectFlags { public int Inherit, Reserved; public uint Flags; }
    [LibraryImport("user32.dll")] private static partial nint GetProcessWindowStation();
    [LibraryImport("user32.dll", EntryPoint = "GetUserObjectInformationW")] private static partial int GetUserObjectFlags(nint window, int index, ref ObjectFlags flags, uint length, out uint needed);
    [StructLayout(LayoutKind.Sequential)] private struct LastInput { public uint Size, Tick; }
    [LibraryImport("user32.dll")] private static partial nint OpenInputDesktop(uint flags, int inherit, uint access);
    [LibraryImport("user32.dll")] private static partial int CloseDesktop(nint desktop);
    [LibraryImport("user32.dll")] private static partial nint GetThreadDesktop(uint thread);
    [LibraryImport("kernel32.dll")] private static partial uint GetCurrentThreadId();
    [LibraryImport("kernel32.dll")] private static partial ulong GetTickCount64();
    [LibraryImport("user32.dll")] private static partial int GetLastInputInfo(ref LastInput info);
    [LibraryImport("user32.dll")] private static partial short GetAsyncKeyState(int key);
    [LibraryImport("user32.dll")] private static unsafe partial int GetUserObjectInformationW(nint handle, int index, char* data, uint length, out uint needed);
}
