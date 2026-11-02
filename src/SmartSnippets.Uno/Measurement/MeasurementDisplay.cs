using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.UI.Windowing;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

namespace SmartSnippets.Uno.Measurement;

// Only measurement builds can select a display. Coordinates are physical desktop pixels,
// matching the app manifest's PerMonitorV2 context and the script's thread DPI context.
internal static unsafe partial class MeasurementDisplay
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MonitorInfo
    {
        internal uint Size;
        internal RECT Bounds, Work;
        internal uint Flags;
        internal fixed char Device[32];
    }
    private static readonly string Device = Environment.GetEnvironmentVariable("SMARTSNIPPETS_MEASURE_DISPLAY")
        ?? throw new InvalidOperationException("Measurement display required.");
    private static readonly int[] Bounds = ParseBounds();
    private static int[] ParseBounds()
    {
        var parts = Environment.GetEnvironmentVariable("SMARTSNIPPETS_MEASURE_BOUNDS")?.Split(',');
        if (parts is not { Length: 4 }) throw new InvalidOperationException("Measurement bounds required.");
        var result = parts.Select(value => int.Parse(value, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        if (result[2] <= 0 || result[3] <= 0) throw new InvalidOperationException("Invalid measurement bounds.");
        return result;
    }
    internal static bool Place(AppWindow window, int width, int height)
    {
        nint monitor = 0;
        if (EnumDisplays(0, 0, &Visit, (nint)(&monitor)) == 0 || monitor == 0) return false;
        if (!Services.WindowPlacement.CenterOn(window, new HMONITOR(monitor), width, height)) return false;
        return window.Position.X >= Bounds[0] && window.Position.Y >= Bounds[1]
            && window.Position.X + window.Size.Width <= Bounds[0] + Bounds[2]
            && window.Position.Y + window.Size.Height <= Bounds[1] + Bounds[3];
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int Visit(nint monitor, nint dc, RECT* clip, nint context)
    {
        var info = new MonitorInfo { Size = (uint)sizeof(MonitorInfo) };
        if (GetMonitorInfo(monitor, &info) == 0) return 0;
        if ((info.Flags & 1) == 0 && string.Equals(new string(info.Device), Device, StringComparison.OrdinalIgnoreCase)
            && info.Bounds.left == Bounds[0] && info.Bounds.top == Bounds[1]
            && info.Bounds.right == Bounds[0] + Bounds[2] && info.Bounds.bottom == Bounds[1] + Bounds[3])
            *(nint*)context = monitor;
        return 1;
    }
    [LibraryImport("user32.dll", EntryPoint = "GetMonitorInfoW")]
    private static partial int GetMonitorInfo(nint monitor, MonitorInfo* info);
    [LibraryImport("user32.dll", EntryPoint = "EnumDisplayMonitors")]
    private static partial int EnumDisplays(nint dc, nint clip,
        delegate* unmanaged[Stdcall]<nint, nint, Windows.Win32.Foundation.RECT*, nint, int> callback, nint context);
}
