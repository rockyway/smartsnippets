using System;
using System.Runtime.InteropServices;

namespace SmartSnippets.TargetIdentity018;

// Acceptance-only identity. Never follow owners (GA_ROOTOWNER), nor accept by PID alone.
internal static partial class ForegroundIdentity
{
    internal static nint Normalize(nint raw, Func<nint, long> style, Func<nint, uint, nint> root,
        Func<nint, (uint Thread, uint Pid)> identity, out bool foregroundWasChild)
    {
        foregroundWasChild = raw != 0 && (style(raw) & 0x40000000) != 0;
        if (!foregroundWasChild) return raw;
        var top = root(raw, 2);
        var childPin = identity(raw);
        var rootPin = identity(top);
        // A failed pin must remain nonzero/foreign, never a recoverable no-foreground sample.
        return top != 0 && childPin.Thread != 0 && childPin.Pid != 0 && childPin == rootPin ? top : raw;
    }
    internal static nint Root(nint raw) => Root(raw, out _);
    internal static nint Root(nint raw, out bool foregroundWasChild) => Normalize(raw,
        h => GetWindowLongPtrW(h, -16), (h, flags) => GetAncestor(h, flags), Pin, out foregroundWasChild);
    private static (uint Thread, uint Pid) Pin(nint h)
    {
        var thread = GetWindowThreadProcessId(h, out var pid);
        return (thread, pid);
    }
    [LibraryImport("user32.dll")] private static partial nint GetWindowLongPtrW(nint h, int index);
    [LibraryImport("user32.dll")] private static partial nint GetAncestor(nint h, uint flags);
    [LibraryImport("user32.dll")] private static partial uint GetWindowThreadProcessId(nint h, out uint pid);
}
