# Targeted WinUI input. Import before UIA so PerMonitorV2 awareness is selected before
# any HWND or DPI-dependent API is used. No global input or focus changes are performed.
if (-not ('SmartSnippetsPostedInput' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

public static class SmartSnippetsPostedInput
{
    private delegate bool EnumProc(IntPtr hwnd, IntPtr data);
    [StructLayout(LayoutKind.Sequential)] public struct Point { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] public struct Rect { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll", SetLastError=true)] private static extern bool SetProcessDpiAwarenessContext(IntPtr context);
    [DllImport("user32.dll")] private static extern IntPtr GetThreadDpiAwarenessContext();
    [DllImport("user32.dll")] private static extern bool AreDpiAwarenessContextsEqual(IntPtr first, IntPtr second);
    [DllImport("user32.dll")] private static extern bool EnumChildWindows(IntPtr parent, EnumProc callback, IntPtr data);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern bool IsChild(IntPtr parent, IntPtr child);
    [DllImport("user32.dll")] private static extern IntPtr GetParent(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] private static extern int GetClassName(IntPtr hwnd, StringBuilder name, int length);
    [DllImport("user32.dll")] private static extern bool GetClientRect(IntPtr hwnd, out Rect rect);
    [DllImport("user32.dll")] private static extern bool ScreenToClient(IntPtr hwnd, ref Point point);
    [DllImport("user32.dll", SetLastError=true)] private static extern bool PostMessage(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] private static extern uint MapVirtualKey(uint code, uint mapType);

    public static void RequirePerMonitorV2()
    {
        IntPtr pmv2 = new IntPtr(-4);
        SetProcessDpiAwarenessContext(pmv2);
        if (!AreDpiAwarenessContextsEqual(GetThreadDpiAwarenessContext(), pmv2))
            throw new InvalidOperationException("Per-monitor-v2 DPI awareness unavailable; input refused");
    }
    private static string ClassName(IntPtr hwnd)
    {
        var name = new StringBuilder(256);
        GetClassName(hwnd, name, name.Capacity);
        return name.ToString();
    }
    private static bool Contains(IntPtr hwnd, int x, int y)
    {
        var point = new Point { X=x, Y=y };
        Rect rect;
        return IsWindowVisible(hwnd) && ScreenToClient(hwnd, ref point) && GetClientRect(hwnd, out rect)
            && point.X >= rect.Left && point.X < rect.Right && point.Y >= rect.Top && point.Y < rect.Bottom;
    }
    private static int Depth(IntPtr hwnd, IntPtr root)
    {
        int depth = 0;
        for (var p = hwnd; p != root && p != IntPtr.Zero; p = GetParent(p)) depth++;
        return depth;
    }
    public static IntPtr FindInputWindow(IntPtr root, uint expectedPid, int screenX, int screenY)
    {
        uint rootPid;
        GetWindowThreadProcessId(root, out rootPid);
        if (root == IntPtr.Zero || rootPid != expectedPid) throw new InvalidOperationException("Unowned root window");
        var windows = new List<IntPtr>();
        EnumChildWindows(root, (h, _) => { windows.Add(h); return true; }, IntPtr.Zero);
        IntPtr bridge = IntPtr.Zero;
        foreach (var h in windows)
            if (ClassName(h) == "Microsoft.UI.Content.DesktopChildSiteBridge" && Contains(h, screenX, screenY)) { bridge = h; break; }
        if (bridge == IntPtr.Zero) throw new InvalidOperationException("WinUI input bridge not found at target point");
        var result = bridge;
        foreach (var h in windows)
        {
            uint pid;
            GetWindowThreadProcessId(h, out pid);
            if (pid == expectedPid && IsChild(bridge, h) && Contains(h, screenX, screenY) && Depth(h, bridge) > Depth(result, bridge)) result = h;
        }
        return result;
    }
    public static int PackClientPoint(int x, int y)
    {
        if (x < 0 || y < 0 || x > 32767 || y > 32767) throw new ArgumentOutOfRangeException("point");
        return (y << 16) | x;
    }
    private static void Post(IntPtr hwnd, uint message, IntPtr w, IntPtr l)
    {
        if (!PostMessage(hwnd, message, w, l)) throw new InvalidOperationException("Could not post input message");
    }
    public static IntPtr Click(IntPtr root, uint pid, int screenX, int screenY)
    {
        var hwnd = FindInputWindow(root, pid, screenX, screenY);
        var point = new Point { X=screenX, Y=screenY };
        if (!ScreenToClient(hwnd, ref point)) throw new InvalidOperationException("ScreenToClient failed");
        var position = new IntPtr(PackClientPoint(point.X, point.Y));
        Post(hwnd, 0x200, IntPtr.Zero, position);
        Post(hwnd, 0x201, new IntPtr(1), position);
        Post(hwnd, 0x202, IntPtr.Zero, position);
        return hwnd;
    }
    public static void Key(IntPtr root, uint pid, int screenX, int screenY, uint virtualKey)
    {
        var hwnd = FindInputWindow(root, pid, screenX, screenY);
        uint scan = MapVirtualKey(virtualKey, 0);
        Post(hwnd, 0x100, new IntPtr(virtualKey), new IntPtr((long)(1 | (scan << 16))));
        Post(hwnd, 0x101, new IntPtr(virtualKey), new IntPtr((long)(1u | (scan << 16) | 0xC0000000u)));
    }
    public static void Character(IntPtr root, uint pid, int screenX, int screenY, char character)
    {
        var hwnd = FindInputWindow(root, pid, screenX, screenY);
        Post(hwnd, 0x102, new IntPtr(character), new IntPtr(1));
    }
}
'@
}
[SmartSnippetsPostedInput]::RequirePerMonitorV2()
