using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.UI.Windowing;
using Windows.Graphics;
using Windows.Win32;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.UI.HiDpi;
using SmartSnippets.Core.Layout;
using SmartSnippets.Uno.Platforms.Windows;

namespace SmartSnippets.Uno.Services;

internal static unsafe partial class WindowPlacement
{
    private struct Selection
    {
        internal nint Handle;
        internal Windows.Win32.Foundation.RECT Bounds;
        internal int Found, Failed;
    }
    internal static bool ParkBottomLeft(AppWindow window, int logicalWidth, int logicalHeight)
    {
        var selected = new Selection();
        if (EnumDisplays(0, 0, &VisitMonitor, (nint)(&selected)) == 0 || selected.Failed != 0 || selected.Found == 0)
            return false;
        return ParkOn(window, new HMONITOR(selected.Handle), logicalWidth, logicalHeight);
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int VisitMonitor(nint monitor, nint dc, Windows.Win32.Foundation.RECT* clip, nint context)
    {
        var info = new MONITORINFO { cbSize = (uint)sizeof(MONITORINFO) };
        var selected = (Selection*)context;
        if (!PInvoke.GetMonitorInfo(new HMONITOR(monitor), ref info)
            || info.rcMonitor.right <= info.rcMonitor.left || info.rcMonitor.bottom <= info.rcMonitor.top)
        { selected->Failed = 1; return 0; }
        var bounds = info.rcMonitor;
        var previous = selected->Bounds;
        if (selected->Found == 0 || CandidateUiPolicy.IsBetterBottomLeft(
                new(bounds.left, bounds.top, bounds.right, bounds.bottom),
                new(previous.left, previous.top, previous.right, previous.bottom)))
        { selected->Handle = monitor; selected->Bounds = bounds; selected->Found = 1; }
        return 1;
    }
    [LibraryImport("user32.dll", EntryPoint = "EnumDisplayMonitors")]
    private static partial int EnumDisplays(nint dc, nint clip,
        delegate* unmanaged[Stdcall]<nint, nint, Windows.Win32.Foundation.RECT*, nint, int> callback, nint context);

    // Return monitor/window bounds in owner-client logical coordinates, including negative origins.
    public static unsafe (Windows.Foundation.Rect Window, Windows.Foundation.Rect WorkArea)? HoverBounds(
        Windows.Win32.Foundation.HWND hwnd, double scale)
    {
        if (scale <= 0) return null;
        var info = new MONITORINFO { cbSize = (uint)sizeof(MONITORINFO) };
        var origin = new System.Drawing.Point();
        if (!PInvoke.GetMonitorInfo(PInvoke.MonitorFromWindow(hwnd, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST), ref info)
            || !PInvoke.ClientToScreen(hwnd, ref origin) || !PInvoke.GetWindowRect(hwnd, out var window)) return null;
        // GetWindowRect includes invisible resize borders. Match the visible DWM frame instead.
        var visible = default(Windows.Win32.Foundation.RECT);
        if (PInvoke.DwmGetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_EXTENDED_FRAME_BOUNDS,
            &visible, (uint)sizeof(Windows.Win32.Foundation.RECT)).Succeeded
            && visible.right > visible.left && visible.bottom > visible.top)
            window = visible;
        Windows.Foundation.Rect Convert(Windows.Win32.Foundation.RECT r) => new(
            (r.left - origin.X) / scale, (r.top - origin.Y) / scale,
            (r.right - r.left) / scale, (r.bottom - r.top) / scale);
        return (Convert(window), Convert(info.rcWork));
    }

    public static unsafe bool CenterOn(AppWindow window, HMONITOR monitor, int logicalWidth, int logicalHeight)
    {
        var info = default(MONITORINFO);
        info.cbSize = (uint)sizeof(MONITORINFO);
        if (!PInvoke.GetMonitorInfo(monitor, ref info))
        {
            return false;
        }

        var scale = 1.0;
        if (PInvoke.GetDpiForMonitor(monitor, MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out var dpiX, out _).Succeeded && dpiX > 0)
        {
            scale = dpiX / 96.0;
        }

        var width = (int)Math.Round(logicalWidth * scale);
        var height = (int)Math.Round(logicalHeight * scale);
        var work = new PixelRect(info.rcWork.left, info.rcWork.top, info.rcWork.right, info.rcWork.bottom);
        var origin = PopupPlacement.CenterIn(work, width, height);

        var bounds = new RectInt32(origin.X, origin.Y, Math.Min(width, work.Width), Math.Min(height, work.Height));
        window.MoveAndResize(bounds);
        // A mixed-DPI transition can apply the suggested WM_DPICHANGED size after the move.
        if (window.Size.Width != bounds.Width || window.Size.Height != bounds.Height)
        {
            window.MoveAndResize(bounds);
        }
        return true;
    }

    internal static bool CenterAtCursor(AppWindow window, nint capturedMonitor, int logicalWidth, int logicalHeight) =>
        PopupMonitorPlacement.Center(
            () =>
            {
                var cursor = new System.Drawing.Point();
                return PInvoke.GetCursorPos(&cursor)
                    ? (nint)PInvoke.MonitorFromPoint(cursor, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST).Value : 0;
            },
            capturedMonitor,
            () => (nint)PInvoke.MonitorFromWindow(default, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTOPRIMARY).Value,
            monitor => CenterOn(window, new HMONITOR(monitor), logicalWidth, logicalHeight));

    private static unsafe bool ParkOn(AppWindow window, HMONITOR monitor, int logicalWidth, int logicalHeight)
    {
        var info = default(MONITORINFO);
        info.cbSize = (uint)sizeof(MONITORINFO);
        if (!PInvoke.GetMonitorInfo(monitor, ref info))
        {
            return false;
        }

        var scale = 1.0;
        if (PInvoke.GetDpiForMonitor(monitor, MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out var dpiX, out _).Succeeded && dpiX > 0)
        {
            scale = dpiX / 96.0;
        }

        var width = (int)Math.Round(logicalWidth * scale);
        var height = (int)Math.Round(logicalHeight * scale);
        var work = new PixelRect(info.rcWork.left, info.rcWork.top, info.rcWork.right, info.rcWork.bottom);
        var origin = new { X = work.Left, Y = Math.Max(work.Top, work.Bottom - height) };

        var bounds = new RectInt32(origin.X, origin.Y, Math.Min(width, work.Width), Math.Min(height, work.Height));
        window.MoveAndResize(bounds);
        // A mixed-DPI transition can apply the suggested WM_DPICHANGED size after the move.
        if (window.Position.X != bounds.X || window.Position.Y != bounds.Y
            || window.Size.Width != bounds.Width || window.Size.Height != bounds.Height)
        {
            window.MoveAndResize(bounds);
        }
        return window.Position.X >= work.Left && window.Position.Y >= work.Top
            && window.Position.X + window.Size.Width <= work.Right
            && window.Position.Y + window.Size.Height <= work.Bottom;
    }
}
