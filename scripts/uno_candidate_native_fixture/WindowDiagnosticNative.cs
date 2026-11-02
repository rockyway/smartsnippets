using System.Runtime.InteropServices;

namespace SmartSnippets.Uno;

internal static partial class WindowDiagnosticNative
{
    internal static WindowDiagnosticFacts Sample(nint window, nint a, nint b)
    {
        try
        {
            var facts = WindowDiagnosticFacts.Capture(
                window, a, b, ClassName, h => h != 0 && IsWindowVisible(h) != 0,
                h => GetWindowLongPtrW(h, -20), h => GetWindow(h, 4), IsMessageOnly,
                h => GetWindowThreadProcessId(h, out _), h => DwmGetWindowAttribute(h, 14, out var value, 4) >= 0 ? value : null);
            if (GetWindowRect(window, out var rect) == 0 || GetWindowRect(a, out var ar) == 0)
                return default;
            var any = false;
            bool? selected = null;
            MonitorCallback callback = (h, dc, r, data) =>
            {
                var info = new MonitorInfo { Size = (uint)Marshal.SizeOf<MonitorInfo>() };
                if (GetMonitorInfoW(h, ref info) == 0) return 0;
                any |= Intersects(rect, info.Monitor);
                if (!selected.HasValue && Intersects(ar, info.Monitor)) selected = Intersects(rect, info.Work); // A's (Target018-selected) work area.
                return 1;
            };
            var ok = EnumDisplayMonitors(0, 0, Marshal.GetFunctionPointerForDelegate(callback), 0);
            GC.KeepAlive(callback);
            if (ok == 0 || !selected.HasValue) return default;
            var style = (long)GetWindowLongPtrW(window, -16);
            var ex = (long)GetWindowLongPtrW(window, -20);
            return facts with {
                WsPopup = (style & 0x80000000) != 0, WsCaption = (style & 0x00c00000) != 0,
                WsChild = (style & 0x40000000) != 0, WsDisabled = (style & 0x08000000) != 0,
                WsExTopmost = (ex & 8) != 0, WsExLayered = (ex & 0x80000) != 0,
                WsExTransparent = (ex & 0x20) != 0, WsExAppwindow = (ex & 0x40000) != 0,
                ZeroArea = rect.Right <= rect.Left || rect.Bottom <= rect.Top, Offscreen = !any,
                EqualsARect = rect.Equals(ar), OnBottomLeftWork = selected.Value,
                TitleNonempty = GetWindowTextLengthW(window) > 0 };
        }
        catch { return default; }
    }

    internal static void WriteReadyInventory(nint a, nint b, string root)
    {
        var windows = new List<string>();
        WindowCallback callback = (h, data) =>
        {
            if (windows.Count == 32) return 0;
            var facts = Sample(h, a, b);
            if (!facts.Sampled) return 0;
            windows.Add("{\"window_id\":\"" + WindowDiagnosticFacts.ClassSha8(h.ToString(System.Globalization.CultureInfo.InvariantCulture)) +
                "\",\"is_a\":" + (h == a).ToString().ToLowerInvariant() +
                ",\"is_b\":" + (h == b).ToString().ToLowerInvariant() +
                ",\"new_at_refusal\":false,\"facts\":" + facts.Json() + "}");
            return 1;
        };
        var tid = GetWindowThreadProcessId(a, out _);
        var complete = tid != 0 && EnumThreadWindows(tid, Marshal.GetFunctionPointerForDelegate(callback), 0) != 0;
        GC.KeepAlive(callback);
        var json = "{\"phase\":\"ready\",\"complete\":" + complete.ToString().ToLowerInvariant() +
            ",\"windows\":[" + (complete ? string.Join(",", windows) : "") + "]}";
        var path = Path.Combine(root, "target-thread-windows-ready.json");
        File.WriteAllText(path + ".tmp", json, System.Text.Encoding.ASCII);
        File.Move(path + ".tmp", path);
    }
    private static bool Intersects(Rect a, Rect b) => Math.Max(a.Left, b.Left) < Math.Min(a.Right, b.Right) &&
        Math.Max(a.Top, b.Top) < Math.Min(a.Bottom, b.Bottom);
    [StructLayout(LayoutKind.Sequential)] private struct Rect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct MonitorInfo { public uint Size; public Rect Monitor, Work; public uint Flags; }
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate int MonitorCallback(nint h, nint dc, nint rect, nint data);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate int WindowCallback(nint h, nint data);
    [LibraryImport("user32.dll")] private static partial int GetWindowRect(nint h, out Rect rect);
    [LibraryImport("user32.dll")] private static partial int GetWindowTextLengthW(nint h);
    [LibraryImport("user32.dll")] private static partial int GetMonitorInfoW(nint h, ref MonitorInfo info);
    [LibraryImport("user32.dll")] private static partial int EnumDisplayMonitors(nint dc, nint rect, nint callback, nint data);
    [LibraryImport("user32.dll")] private static partial int EnumThreadWindows(uint thread, nint callback, nint data);
    private static unsafe string ClassName(nint window)
    {
        var buffer = stackalloc char[256];
        var count = GetClassNameW(window, buffer, 256);
        if (count <= 0) throw new InvalidOperationException("diagnostic_class");
        return new string(buffer, 0, count);
    }
    [LibraryImport("user32.dll")] private static partial int IsWindowVisible(nint window);
    [LibraryImport("user32.dll")] private static partial nint GetWindowLongPtrW(nint window, int index);
    [LibraryImport("user32.dll")] private static partial nint GetWindow(nint window, uint command);
    // HWND_MESSAGE is a lookup sentinel, not assumed to be an actual parent HWND.
    private static bool IsMessageOnly(nint window)
    {
        if (window == 0 || GetAncestor(window, 1) == 0) return false;
        nint child = 0;
        for (var i = 0; i < 256; i++)
        {
            child = FindWindowExW(-3, child, 0, 0);
            if (child == 0) break;
            if (child == window) return true;
        }
        return false; // Bounded diagnostic only; never admission.
    }
    [LibraryImport("user32.dll")] private static partial nint FindWindowExW(nint parent, nint after, nint className, nint title);
    [LibraryImport("user32.dll")] private static partial nint GetAncestor(nint window, uint flags);
    [LibraryImport("user32.dll")] private static partial uint GetWindowThreadProcessId(nint window, out uint pid);
    [LibraryImport("user32.dll")] private static unsafe partial int GetClassNameW(nint window, char* text, int capacity);
    [LibraryImport("dwmapi.dll")] private static partial int DwmGetWindowAttribute(nint window, uint attribute, out int value, uint size);
}
