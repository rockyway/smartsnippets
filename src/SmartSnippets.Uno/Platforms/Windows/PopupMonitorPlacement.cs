namespace SmartSnippets.Uno.Platforms.Windows;

/// <summary>Ordinary popup placement samples the cursor for every ready show, including re-shows.</summary>
internal static class PopupMonitorPlacement
{
    internal static bool Center(Func<nint> cursorMonitor, nint capturedMonitor,
        Func<nint> primaryMonitor, Func<nint, bool> center)
    {
        var cursor = cursorMonitor();
        if (cursor != 0 && center(cursor)) return true;
        if (capturedMonitor != 0 && center(capturedMonitor)) return true;
        var primary = primaryMonitor();
        return primary != 0 && center(primary);
    }
}
