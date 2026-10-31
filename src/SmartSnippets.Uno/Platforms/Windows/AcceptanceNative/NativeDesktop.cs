using System;
using System.Runtime.InteropServices;

namespace SmartSnippets.Uno.Platforms.Windows.AcceptanceNative;

/// <summary>Read-only admission only: visible/current desktop, NOT idle/all-up or permission to focus/mutate.</summary>
internal static partial class NativeDesktop
{
    internal static unsafe bool IsVisibleCurrent()
    {
        var flags = new ObjectFlags();
        if (GetFlags(GetProcessWindowStation(), 1, ref flags, (uint)sizeof(ObjectFlags), out _) == 0 || (flags.Flags & 1) == 0) return false;
        var input = OpenInputDesktop(0, 0, 1);
        if (input == 0) return false;
        try
        {
            var a = stackalloc char[256]; var b = stackalloc char[256];
            new Span<char>(a, 256).Clear(); new Span<char>(b, 256).Clear();
            if (GetUserObjectInformationW(input, 2, a, 512, out _) == 0 ||
                GetUserObjectInformationW(GetThreadDesktop(GetCurrentThreadId()), 2, b, 512, out _) == 0) return false;
            var count = 0;
            while (count < 256 && a[count] != 0) count++;
            if (count is 0 or 256 || b[count] != 0) return false;
            return new ReadOnlySpan<char>(a, count).SequenceEqual(new ReadOnlySpan<char>(b, count));
        }
        finally { _ = CloseDesktop(input); }
    }
    [StructLayout(LayoutKind.Sequential)] private struct ObjectFlags { public int Inherit, Reserved; public uint Flags; }
    [LibraryImport("user32.dll")] private static partial nint GetProcessWindowStation();
    [LibraryImport("user32.dll", EntryPoint = "GetUserObjectInformationW")] private static partial int GetFlags(nint handle, int kind, ref ObjectFlags flags, uint size, out uint returned);
    [LibraryImport("user32.dll")] private static partial nint OpenInputDesktop(uint flags, int inherit, uint access);
    [LibraryImport("user32.dll")] private static partial int CloseDesktop(nint desktop);
    [LibraryImport("user32.dll")] private static partial nint GetThreadDesktop(uint thread);
    [LibraryImport("kernel32.dll")] private static partial uint GetCurrentThreadId();
    [LibraryImport("user32.dll")] private static unsafe partial int GetUserObjectInformationW(nint handle, int kind, char* data, uint size, out uint returned);
}
