using System;
using System.Runtime.InteropServices;

namespace SmartSnippets.UnoProbe;

/// <summary>Ephemeral owned registration only. No key posting, event tap,
/// popup activation, clipboard or persistent settings. Delivery is NOT tested.</summary>
internal sealed partial class NativeShortcutRegistrationProbe
{
    private string _stage = "preflight";
#if WINDOWS
    private bool _owned;
    private const int Id = 0x5A18;
#else
    private nint _owned;
    private nint _pool;
    [StructLayout(LayoutKind.Sequential)]
    private struct HotKeyId { public uint Signature, Id; }
#endif
    private bool HasLease =>
#if WINDOWS
        _owned;
#else
        _owned != 0;
#endif

    internal static int Run()
    {
        var probe = new NativeShortcutRegistrationProbe();
        var ok = false;
        var cleanup = true;
        long foregroundBefore = 0;
        try
        {
            probe.Preflight();
            foregroundBefore = ProbeWindowSurface.ForegroundOwnerToken;
            if (foregroundBefore == 0) throw new InvalidOperationException();
            Console.WriteLine("shortcut_registration_started");
            probe._stage = "register_initial";
            probe.Acquire();
            Console.WriteLine("shortcut_registration_acquired");
            probe._stage = "release_initial";
            probe.Release();
            Console.WriteLine("shortcut_registration_released");
            probe.Preflight();
            probe._stage = "register_again";
            probe.Acquire();
            Console.WriteLine("shortcut_registration_reacquired");
            ok = true;
        }
        catch (Exception error)
        {
            var code = error is ApiFailure api ? api.Code : 0;
            Console.Error.WriteLine($"shortcut_registration_failed stage={probe._stage} kind={error.GetType().Name} code={code}");
        }
        finally
        {
            try
            {
                if (probe.HasLease) probe.Release();
            }
            catch (Exception error)
            {
                cleanup = false;
                Console.Error.WriteLine("shortcut_registration_cleanup_failed kind=" + error.GetType().Name);
            }
            try
            {
                if (foregroundBefore != 0 && ProbeWindowSurface.ForegroundOwnerToken != foregroundBefore)
                    throw new InvalidOperationException();
            }
            catch (Exception error)
            {
                cleanup = false;
                Console.Error.WriteLine("shortcut_registration_foreground_changed kind=" + error.GetType().Name);
            }
#if !WINDOWS
            try { if (probe._pool != 0) SendVoid(probe._pool, Sel("drain")); }
            catch (Exception error)
            {
                cleanup = false;
                Console.Error.WriteLine("shortcut_registration_pool_cleanup_failed kind=" + error.GetType().Name);
            }
#endif
        }
        if (!ok || !cleanup) return 1;
        Console.WriteLine("shortcut_registration_complete cleanup=VERIFIED foreground_token=UNCHANGED input=NONE delivery=NOT_RUN");
        return 0;
    }

    private void Preflight()
    {
        _stage = "preflight";
#if WINDOWS
        if (!OperatingSystem.IsWindows() || RuntimeInformation.ProcessArchitecture != Architecture.X64)
            throw new PlatformNotSupportedException();
        var last = new LastInput { Size = (uint)Marshal.SizeOf<LastInput>() };
        if (GetLastInputInfo(ref last) == 0 || unchecked((uint)GetTickCount64() - last.Tick) < 1500)
            throw new InvalidOperationException("Input not idle.");
        foreach (var key in new[] { 1, 2, 4, 5, 6, 0x10, 0x11, 0x12, 0x5B, 0x5C, 0x87 })
            if ((GetAsyncKeyState(key) & 0x8000) != 0) throw new InvalidOperationException("Chord or pointer held.");
#else
        if (!OperatingSystem.IsMacOS() || RuntimeInformation.ProcessArchitecture != Architecture.Arm64)
            throw new PlatformNotSupportedException();
        if (_pool == 0)
        {
            _ = NativeLibrary.Load("/System/Library/Frameworks/AppKit.framework/AppKit");
            _pool = Send(Send(GetClass("NSAutoreleasePool"), Sel("alloc")), Sel("init"));
            if (_pool == 0) throw new InvalidOperationException();
        }
        var idle = CGEventSourceSecondsSinceLastEventType(0, uint.MaxValue);
        if (!double.IsFinite(idle) || idle < 1.5)
            throw new InvalidOperationException("Input not idle.");
        for (ushort key = 54; key <= 63; key++)
            if (CGEventSourceKeyState(0, key) != 0) throw new InvalidOperationException("Modifier held.");
        if (CGEventSourceKeyState(0, 90) != 0) throw new InvalidOperationException("Test chord held.");
        for (var button = 0; button < 5; button++)
            if (CGEventSourceButtonState(0, button) != 0) throw new InvalidOperationException("Pointer held.");
        // Registration only: no exemption is made to the full held-key guard
        // required by OwnedPreviewEnter and future external-target dispatch.
#endif
    }

    private void Acquire()
    {
        if (HasLease) throw new InvalidOperationException();
#if WINDOWS
        // Ctrl+Alt+Shift+F24, thread-owned; MOD_NOREPEAT. Not the product chord.
        if (RegisterHotKey(0, Id, 0x4007, 0x87) == 0) throw new ApiFailure(Marshal.GetLastPInvokeError());
        _owned = true;
#else
        // Ctrl+Option+Shift+F20. Carbon modifier bits and key code verified in
        // Xcode Events.h. Exclusive registration fails rather than overriding
        // an existing registrant (CarbonEvents.h kEventHotKeyExclusive).
        if (Marshal.SizeOf<HotKeyId>() != 8) throw new InvalidOperationException();
        var target = GetApplicationEventTarget();
        if (target == 0) throw new InvalidOperationException();
        var status = RegisterEventHotKey(90, 0x1A00, new HotKeyId { Signature = 0x53533138, Id = 1 }, target, 1, out var owned);
        if (status != 0) throw new ApiFailure(status); // An error out-ref is not a valid owned lease.
        if (owned == 0) throw new InvalidOperationException();
        _owned = owned;
#endif
    }

    private void Release()
    {
        if (!HasLease) throw new InvalidOperationException();
#if WINDOWS
        if (UnregisterHotKey(0, Id) == 0) throw new ApiFailure(Marshal.GetLastPInvokeError());
        _owned = false;
#else
        var status = UnregisterEventHotKey(_owned);
        if (status != 0) throw new ApiFailure(status);
        _owned = 0;
#endif
    }

    private sealed class ApiFailure(int code) : Exception { internal int Code { get; } = code; }
#if WINDOWS
    [StructLayout(LayoutKind.Sequential)]
    private struct LastInput { public uint Size, Tick; }
    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial int RegisterHotKey(nint window, int id, uint modifiers, uint key);
    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial int UnregisterHotKey(nint window, int id);
    [LibraryImport("user32.dll")]
    private static partial int GetLastInputInfo(ref LastInput last);
    [LibraryImport("user32.dll")]
    private static partial short GetAsyncKeyState(int key);
    [LibraryImport("kernel32.dll")]
    private static partial ulong GetTickCount64();
#else
    private const string Carbon = "/System/Library/Frameworks/Carbon.framework/Carbon";
    private const string CG = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";
    private const string ObjC = "/usr/lib/libobjc.A.dylib";
    [LibraryImport(Carbon)]
    private static partial nint GetApplicationEventTarget();
    [LibraryImport(Carbon)]
    private static partial int RegisterEventHotKey(uint key, uint modifiers, HotKeyId id, nint target, uint options, out nint result);
    [LibraryImport(Carbon)]
    private static partial int UnregisterEventHotKey(nint hotkey);
    [LibraryImport(CG)]
    private static partial double CGEventSourceSecondsSinceLastEventType(int state, uint type);
    [LibraryImport(CG)]
    private static partial byte CGEventSourceKeyState(int state, ushort key);
    [LibraryImport(CG)]
    private static partial byte CGEventSourceButtonState(int state, int button);
    [LibraryImport(ObjC, EntryPoint = "objc_getClass", StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint GetClass(string name);
    [LibraryImport(ObjC, EntryPoint = "sel_registerName", StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint Sel(string name);
    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    private static partial nint Send(nint receiver, nint selector);
    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    private static partial void SendVoid(nint receiver, nint selector);
#endif
}
