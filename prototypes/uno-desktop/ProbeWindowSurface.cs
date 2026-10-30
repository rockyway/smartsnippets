using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;

namespace SmartSnippets.UnoProbe;

/// <summary>Probe-owned native window only; never enumerates another app's titles.</summary>
internal sealed partial class ProbeWindowSurface
{
    private readonly nint _handle;
    private ProbeWindowSurface(nint handle) => _handle = handle;

    internal static ProbeWindowSurface Bind(Window window, string syntheticTitle)
    {
#if WINDOWS
        var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
        if (handle == 0 || GetWindowThreadProcessId(handle, out var pid) == 0 || pid != (uint)Environment.ProcessId)
            throw new InvalidOperationException("Probe window ownership failed.");
        return new(handle);
#else
        if (!OperatingSystem.IsMacOS()) throw new PlatformNotSupportedException();
        var application = Send(GetClass("NSApplication"), Selector("sharedApplication"));
        var windows = Send(application, Selector("windows"));
        var count = (nuint)Send(windows, Selector("count"));
        nint match = 0;
        for (nuint index = 0; index < count; index++)
        {
            var candidate = SendIndex(windows, Selector("objectAtIndex:"), index);
            var title = Send(candidate, Selector("title"));
            var utf8 = Send(title, Selector("UTF8String"));
            if (Marshal.PtrToStringUTF8(utf8) != syntheticTitle) continue;
            if (match != 0) throw new InvalidOperationException("Ambiguous probe window.");
            match = candidate;
        }
        if (match == 0) throw new InvalidOperationException("Probe window unavailable.");
        return new(match);
#endif
    }

    // Windows token is the exact foreground HWND; macOS token is only the
    // frontmost PID. Exact external AX-window identity is a separate T4 gate.
    internal static long ForegroundOwnerToken
    {
        get
        {
#if WINDOWS
            return GetForegroundWindow().ToInt64();
#else
            var workspace = Send(GetClass("NSWorkspace"), Selector("sharedWorkspace"));
            return GetInt(Send(workspace, Selector("frontmostApplication")), Selector("processIdentifier"));
#endif
        }
    }

    internal bool IsForeground
    {
        get
        {
#if WINDOWS
            return GetForegroundWindow() == _handle;
#else
            return SendBool(_handle, Selector("isKeyWindow")) != 0 &&
                SendBool(Send(GetClass("NSApplication"), Selector("sharedApplication")), Selector("isActive")) != 0;
#endif
        }
    }

    internal void SetWarmupCloak(bool enabled)
    {
#if WINDOWS
        var cloak = enabled ? 1 : 0;
        if (DwmSetWindowAttribute(_handle, 13, ref cloak, sizeof(int)) != 0)
            throw new InvalidOperationException("Native warmup cloak failed.");
#else
        SendDouble(_handle, Selector("setAlphaValue:"), enabled ? 0.0 : 1.0);
#endif
    }

    internal bool IsWarmupCloaked
    {
        get
        {
#if WINDOWS
            if (DwmGetWindowAttribute(_handle, 14, out var state, sizeof(int)) != 0)
                throw new InvalidOperationException("Native warmup state unavailable.");
            return (state & 1) != 0;
#else
            return GetDouble(_handle, Selector("alphaValue")) == 0.0;
#endif
        }
    }

    internal bool IsVisible
    {
        get
        {
#if WINDOWS
            return IsWindowVisible(_handle) != 0;
#else
            return SendBool(_handle, Selector("isVisible")) != 0;
#endif
        }
    }

    internal void ShowWarmup(Window window)
    {
#if WINDOWS
        window.AppWindow.Show(false);
#else
        SendObject(_handle, Selector("orderFront:"), 0);
#endif
    }

    internal void Show(Window window)
    {
#if WINDOWS
        _ = ShowWindow(_handle, 5);
#else
        var app = Send(GetClass("NSApplication"), Selector("sharedApplication"));
        SendByte(app, Selector("activateIgnoringOtherApps:"), 1);
#endif
        window.Activate();
#if WINDOWS
        _ = SetForegroundWindow(_handle);
#else
        // Pinned Uno native activation orders front but does not make key.
        // Explicitly key only this owned, ready surface inside the coordinator's show callback.
        SendObject(_handle, Selector("makeKeyAndOrderFront:"), 0);
#endif
    }

    // The macOS predicate captures only the title, never a freed NSWindow*.
    internal Func<bool> CreateAbsenceCheck(string syntheticTitle)
    {
#if WINDOWS
        var handle = _handle;
        return () => IsWindow(handle) == 0;
#else
        return () => !HasVisibleOwnWindow(syntheticTitle);
#endif
    }

#if !WINDOWS
    private static bool HasVisibleOwnWindow(string syntheticTitle)
    {
        var application = Send(GetClass("NSApplication"), Selector("sharedApplication"));
        var windows = Send(application, Selector("windows"));
        var count = (nuint)Send(windows, Selector("count"));
        for (nuint index = 0; index < count; index++)
        {
            var candidate = SendIndex(windows, Selector("objectAtIndex:"), index);
            var title = Marshal.PtrToStringUTF8(Send(Send(candidate, Selector("title")), Selector("UTF8String")));
            if (title == syntheticTitle && SendBool(candidate, Selector("isVisible")) != 0) return true;
        }
        return false;
    }
#endif

    internal void Hide()
    {
#if WINDOWS
        _ = ShowWindow(_handle, 0);
#else
        SendObject(_handle, Selector("orderOut:"), 0);
#endif
        if (IsVisible) throw new InvalidOperationException("Probe native hide failed.");
    }

#if WINDOWS
    [LibraryImport("user32.dll")]
    private static partial uint GetWindowThreadProcessId(nint window, out uint processId);
    [LibraryImport("user32.dll")]
    private static partial int IsWindowVisible(nint window);
    [LibraryImport("user32.dll")]
    private static partial int IsWindow(nint window);
    [LibraryImport("user32.dll")]
    private static partial int ShowWindow(nint window, int command);
    [LibraryImport("user32.dll")]
    private static partial nint GetForegroundWindow();
    [LibraryImport("user32.dll")]
    private static partial int SetForegroundWindow(nint window);
    [LibraryImport("dwmapi.dll")]
    private static partial int DwmSetWindowAttribute(nint window, uint attribute, ref int value, int size);
    [LibraryImport("dwmapi.dll")]
    private static partial int DwmGetWindowAttribute(nint window, uint attribute, out int value, int size);
#else
    private const string ObjC = "/usr/lib/libobjc.A.dylib";
    [LibraryImport(ObjC, EntryPoint = "objc_getClass", StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint GetClass(string name);
    [LibraryImport(ObjC, EntryPoint = "sel_registerName", StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint Selector(string name);
    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    private static partial nint Send(nint receiver, nint selector);
    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    private static partial nint SendIndex(nint receiver, nint selector, nuint index);
    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    private static partial byte SendBool(nint receiver, nint selector);
    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    private static partial void SendObject(nint receiver, nint selector, nint argument);
    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    private static partial void SendDouble(nint receiver, nint selector, double value);
    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    private static partial double GetDouble(nint receiver, nint selector);
    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    private static partial void SendByte(nint receiver, nint selector, byte value);
    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    private static partial int GetInt(nint receiver, nint selector);
#endif
}
