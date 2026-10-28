using Microsoft.UI.Windowing;
using Windows.Graphics;
using Windows.Win32;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.HiDpi;
using SmartSnippets.Core.Layout;

namespace SmartSnippets.App.Services;

internal static class WindowPlacement
{
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
}
