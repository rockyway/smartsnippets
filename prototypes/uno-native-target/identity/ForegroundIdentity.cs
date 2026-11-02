using System;
using System.Runtime.InteropServices;

namespace SmartSnippets.TargetIdentity018;

// Acceptance-only identity. Never follow owners (GA_ROOTOWNER), nor accept by PID alone.
internal static partial class ForegroundIdentity
{
    internal static nint Normalize(nint raw, Func<nint, long> style, Func<nint, nint> root,
        Func<nint, (uint Thread, uint Pid)> identity, out bool foregroundWasChild)
    {
        foregroundWasChild = raw != 0 && (style(raw) & 0x40000000) != 0;
        if (!foregroundWasChild) return raw;
        var top = root(raw);
        var childPin = identity(raw);
        var rootPin = identity(top);
        return top != 0 && childPin.Thread != 0 && childPin.Pid != 0 && childPin == rootPin ? top : 0;
    }
    internal static nint Root(nint raw) => Root(raw, out _);
    internal static nint Root(nint raw, out bool foregroundWasChild) => Normalize(raw,
        h => GetWindowLongPtrW(h, -16), h => GetAncestor(h, 2), Pin, out foregroundWasChild);
    private static (uint Thread, uint Pid) Pin(nint h)
    {
        var thread = GetWindowThreadProcessId(h, out var pid);
        return (thread, pid);
    }
    [LibraryImport("user32.dll")] private static partial nint GetWindowLongPtrW(nint h, int index);
    [LibraryImport("user32.dll")] private static partial nint GetAncestor(nint h, uint flags);
    [LibraryImport("user32.dll")] private static partial uint GetWindowThreadProcessId(nint h, out uint pid);
}
