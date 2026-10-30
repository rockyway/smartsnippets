#if WINDOWS
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;

namespace SmartSnippets.UnoProbe;

/// <summary>Park only owned probe HWNDs on the bottom monitor of the leftmost column.</summary>
internal static unsafe partial class ProbeWindowPlacement
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Rect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo { public uint Size; public Rect Monitor, Work; public uint Flags; }
    private struct Selection { public int Found, Failed; public Rect Monitor, Work; }

    internal static void Park(Window window, int width, int height, bool secondary = false)
    {
        var selected = new Selection();
        if (EnumDisplayMonitors(0, 0, &Visit, (nint)(&selected)) == 0 || selected.Failed != 0 || selected.Found == 0)
            throw new InvalidOperationException("Probe monitor selection failed.");
        var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
        if (GetWindowThreadProcessId(handle, out var owner) == 0 || owner != (uint)Environment.ProcessId)
            throw new InvalidOperationException("Probe window ownership failed.");
        var area = selected.Work;
        width = Math.Min(width, area.Right - area.Left - 32);
        height = Math.Min(height, area.Bottom - area.Top - 32);
        if (width < 240 || height < 200) throw new InvalidOperationException("Probe work area too small.");
        var x = area.Left + 16;
        if (secondary) x = Math.Min(area.Left + 680, area.Right - width - 16);
        var y = area.Bottom - height - 16;
        // SWP_NOZORDER | SWP_NOACTIVATE: positioning must not itself take focus.
        if (SetWindowPos(handle, 0, x, y, width, height, 0x14) == 0 ||
            GetWindowRect(handle, out var bounds) == 0)
            throw new InvalidOperationException("Probe window placement failed.");
        // A cross-monitor WM_DPICHANGED can apply a scaled suggested rectangle.
        // Reapply once after transition; never accept a partly-offscreen result.
        if (bounds.Left != x || bounds.Top != y || bounds.Right != x + width || bounds.Bottom != y + height)
        {
            if (SetWindowPos(handle, 0, x, y, width, height, 0x14) == 0 || GetWindowRect(handle, out bounds) == 0)
                throw new InvalidOperationException("Probe DPI placement failed.");
        }
        if (bounds.Left < area.Left ||
            bounds.Top < area.Top || bounds.Right > area.Right || bounds.Bottom > area.Bottom)
            throw new InvalidOperationException("Probe window placement failed.");
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int Visit(nint monitor, nint dc, Rect* clip, nint context)
    {
        var info = new MonitorInfo { Size = (uint)sizeof(MonitorInfo) };
        var best = (Selection*)context;
        if (GetMonitorInfoW(monitor, ref info) == 0) { best->Failed = 1; return 0; }
        if (best->Found == 0 || info.Monitor.Left < best->Monitor.Left ||
            (info.Monitor.Left == best->Monitor.Left && info.Monitor.Top > best->Monitor.Top))
        {
            best->Found = 1;
            best->Monitor = info.Monitor;
            best->Work = info.Work;
        }
        return 1;
    }

    [LibraryImport("user32.dll")]
    private static partial int EnumDisplayMonitors(nint dc, nint clip,
        delegate* unmanaged[Stdcall]<nint, nint, Rect*, nint, int> callback, nint context);
    [LibraryImport("user32.dll")]
    private static partial int GetMonitorInfoW(nint monitor, ref MonitorInfo info);
    [LibraryImport("user32.dll")]
    private static partial uint GetWindowThreadProcessId(nint window, out uint processId);
    [LibraryImport("user32.dll")]
    private static partial int GetWindowRect(nint window, out Rect bounds);
    [LibraryImport("user32.dll")]
    private static partial int SetWindowPos(nint window, nint after, int x, int y, int width, int height, uint flags);
}
#endif
