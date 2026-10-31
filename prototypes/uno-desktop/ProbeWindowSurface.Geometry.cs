using System;
using System.Runtime.InteropServices;

namespace SmartSnippets.UnoProbe;

internal readonly record struct ProbeScreenRect(double X, double Y, double Width, double Height)
{
    internal double Right => X + Width;
    internal double Bottom => Y + Height;
    internal bool Contains(ProbeScreenRect other) => other.X >= X - 1 && other.Y >= Y - 1 &&
        other.Right <= Right + 1 && other.Bottom <= Bottom + 1;
}

internal sealed partial class ProbeWindowSurface
{
    internal ProbeScreenRect Frame
    {
        get
        {
#if WINDOWS
            RequireLiveOwner();
            if (ReadWindowRect(_handle, out var rect) == 0) throw new InvalidOperationException();
            return new(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
#else
            return FromNative(ReadNativeRect(_handle, Selector("frame")));
#endif
        }
    }

    internal ProbeScreenRect WorkArea
    {
        get
        {
#if WINDOWS
            RequireLiveOwner();
            var monitor = MonitorFromWindow(_handle, 2);
            var info = new ScreenMonitorInfo { Size = (uint)Marshal.SizeOf<ScreenMonitorInfo>() };
            if (monitor == 0 || ReadMonitorInfo(monitor, ref info) == 0) throw new InvalidOperationException();
            return new(info.Work.Left, info.Work.Top, info.Work.Right - info.Work.Left, info.Work.Bottom - info.Work.Top);
#else
            var screen = Send(_handle, Selector("screen"));
            if (screen == 0) throw new InvalidOperationException();
            return FromNative(ReadNativeRect(screen, Selector("visibleFrame")));
#endif
        }
    }

    internal void SetFrame(ProbeScreenRect frame)
    {
        if (!double.IsFinite(frame.X) || !double.IsFinite(frame.Y) || !double.IsFinite(frame.Width) ||
            !double.IsFinite(frame.Height) || frame.Width < 100 || frame.Height < 100)
            throw new InvalidOperationException();
#if WINDOWS
        RequireLiveOwner();
        for (var pass = 0; pass < 2; pass++)
            if (PositionWindow(_handle, 0, checked((int)frame.X), checked((int)frame.Y),
                checked((int)frame.Width), checked((int)frame.Height), 0x14) == 0)
                throw new InvalidOperationException(); // NOZORDER | NOACTIVATE, including cross-DPI reapply.
#else
        SetNativeRect(_handle, Selector("setFrame:display:"),
            new NativeScreenRect { X = frame.X, Y = -frame.Bottom, Width = frame.Width, Height = frame.Height }, 1);
#endif
        var actual = Frame;
        if (Math.Abs(actual.X - frame.X) > 1 || Math.Abs(actual.Y - frame.Y) > 1 ||
            Math.Abs(actual.Width - frame.Width) > 1 || Math.Abs(actual.Height - frame.Height) > 1)
            throw new InvalidOperationException("Owned frame mismatch.");
    }

#if WINDOWS
    private void RequireLiveOwner()
    {
        if (GetWindowThreadProcessId(_handle, out var pid) == 0 || pid != (uint)Environment.ProcessId)
            throw new InvalidOperationException("Owned geometry unavailable.");
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct ScreenRectNative { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)]
    private struct ScreenMonitorInfo { public uint Size; public ScreenRectNative Monitor, Work; public uint Flags; }
    [LibraryImport("user32.dll", EntryPoint = "GetWindowRect")]
    private static partial int ReadWindowRect(nint window, out ScreenRectNative rect);
    [LibraryImport("user32.dll")]
    private static partial nint MonitorFromWindow(nint window, uint flags);
    [LibraryImport("user32.dll", EntryPoint = "GetMonitorInfoW")]
    private static partial int ReadMonitorInfo(nint monitor, ref ScreenMonitorInfo info);
    [LibraryImport("user32.dll", EntryPoint = "SetWindowPos")]
    private static partial int PositionWindow(nint window, nint after, int x, int y, int width, int height, uint flags);
#else
    // NSRect is four doubles (an HFA) on the sole target, Darwin arm64. No
    // objc_msgSend_stret: the arm64 ABI handles this blittable struct directly.
    [StructLayout(LayoutKind.Sequential)]
    private struct NativeScreenRect { public double X, Y, Width, Height; }
    private static ProbeScreenRect FromNative(NativeScreenRect rect) =>
        new(rect.X, -rect.Y - rect.Height, rect.Width, rect.Height); // Same y-down space for frame and work area.
    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    private static partial NativeScreenRect ReadNativeRect(nint receiver, nint selector);
    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    private static partial void SetNativeRect(nint receiver, nint selector, NativeScreenRect rect, byte display);
#endif
}
