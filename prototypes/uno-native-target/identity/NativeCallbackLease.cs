using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SmartSnippets.TargetIdentity018;

// Main-thread-only, one installed callback table. Does not synthesize HID input.
internal sealed unsafe partial class NativeCallbackLease : IDisposable
{
    internal const uint Signature = 0x53533138;
    private const uint HotKeyMessage = 0x312;
    private const string Carbon = "/System/Library/Frameworks/Carbon.framework/Carbon";
    private static NativeCallbackLease? current;
    private readonly int thread = Environment.CurrentManagedThreadId;
    private readonly CallbackCaptureGate gate;
    private nint window, handler, hotkey;
    private ushort atom;
    private bool registered, disposed;
    private readonly string className = "SS018Callback" + Guid.NewGuid().ToString("N");

    private NativeCallbackLease(CallbackCaptureGate gate) => this.gate = gate;

    internal static NativeCallbackLease Install(CallbackCaptureGate gate)
    {
        if (current is not null) throw new ProbeFailure("callback_already_installed");
        var lease = new NativeCallbackLease(gate);
        current = lease;
        gate.Active = false;
        try
        {
            if (OperatingSystem.IsWindows())
            {
                var cls = new WindowClass { Size = (uint)sizeof(WindowClass), Procedure = &WindowProc, Instance = GetModuleHandleW(0) };
                fixed (char* name = lease.className)
                {
                    cls.Name = name;
                    lease.atom = RegisterClassExW(ref cls);
                    if (lease.atom == 0) throw new ProbeFailure("callback_class");
                    lease.window = CreateWindowExW(0, name, name, 0, 0, 0, 0, 0, -3, 0, cls.Instance, 0);
                }
                if (lease.window == 0) throw new ProbeFailure("callback_window");
            }
            else
            {
                if (pthread_main_np() != 1) throw new ProbeFailure("callback_not_main_thread");
                var type = new EventType { Class = 0x6B657962, Kind = 5 }; // kEventClassKeyboard / kEventHotKeyPressed
                Check(InstallEventHandler(GetApplicationEventTarget(), &CarbonHandler, 1, ref type, 0, out lease.handler), "callback_install");
                if (lease.handler == 0) throw new ProbeFailure("callback_install");
            }
            return lease;
        }
        catch { lease.Dispose(); throw; }
    }

    internal static void RequireChordReleased()
    {
        var held = OperatingSystem.IsWindows() ? (GetAsyncKeyState(0x87) & 0x8000) != 0 :
            CGEventSourceKeyState(0, 90) != 0;
        if (held) throw new ProbeFailure("callback_chord_held");
    }

    internal bool TryAcquire()
    {
        RequireThread();
        if (registered) throw new ProbeFailure("callback_double_acquire");
        if (OperatingSystem.IsWindows())
            registered = RegisterHotKey(window, (int)gate.Generation, 0x400F, 0x87) != 0; // Win+Ctrl+Alt+Shift+F24, NOREPEAT
        else
        {
            var status = RegisterEventHotKey(90, 0x1A00, new HotKeyId { Signature = Signature, Id = gate.Generation }, GetApplicationEventTarget(), 1, out var owned);
            registered = status == 0 && owned != 0;
            if (registered) hotkey = owned;
        }
        gate.Active = registered;
        return registered;
    }

    internal void RequireConflict()
    {
        RequireThread();
        if (!registered) throw new ProbeFailure("callback_not_acquired");
        // Separate native registration identity, same exclusive chord; never override.
        if (OperatingSystem.IsWindows())
        {
            if (RegisterHotKey(window, (int)gate.Generation + 1000, 0x400F, 0x87) != 0)
            {
                _ = UnregisterHotKey(window, (int)gate.Generation + 1000);
                throw new ProbeFailure("callback_conflict_not_refused");
            }
        }
        else
        {
            var status = RegisterEventHotKey(90, 0x1A00, new HotKeyId { Signature = Signature, Id = gate.Generation + 1000 }, GetApplicationEventTarget(), 1, out var second);
            if (status == 0)
            {
                if (second != 0) Check(UnregisterEventHotKey(second), "callback_conflict_cleanup");
                throw new ProbeFailure("callback_conflict_not_refused");
            }
        }
    }

    internal void Release()
    {
        RequireThread();
        gate.Active = false;
        if (!registered) return;
        if (OperatingSystem.IsWindows())
        {
            if (UnregisterHotKey(window, (int)gate.Generation) == 0) throw new ProbeFailure("callback_release");
        }
        else Check(UnregisterEventHotKey(hotkey), "callback_release");
        registered = false;
        hotkey = 0;
    }

    internal void Send(uint signature, uint generation)
    {
        RequireThread();
        if (OperatingSystem.IsWindows())
        {
            // WM_HOTKEY carries a generation-tagged registration ID, not a pointer.
            // Unknown signature maps to a deliberately unknown native ID.
            if (PostMessageW(window, HotKeyMessage, signature == Signature ? generation : 0, (nint)0x0087000F) == 0)
                throw new ProbeFailure("callback_post");
            var deadline = Environment.TickCount64 + 1000;
            var dispatched = false;
            while (PeekMessageW(out var message, window, 0, 0, 1) != 0)
            {
                _ = DispatchMessageW(ref message);
                dispatched = true;
                if (Environment.TickCount64 > deadline) throw new ProbeFailure("callback_pump_timeout");
            }
            if (!dispatched) throw new ProbeFailure("callback_missing");
        }
        else
        {
            Check(CreateEvent(0, 0x6B657962, 5, 0, 0, out var value), "callback_event_create");
            if (value == 0) throw new ProbeFailure("callback_event_create");
            try
            {
                var id = new HotKeyId { Signature = signature, Id = generation };
                Check(SetEventParameter(value, 0x2D2D2D2D, 0x686B6964, 8, ref id), "callback_event_parameter"); // '----', 'hkid'
                // A bounded Carbon queue pump, on the real main thread. No HID event.
                Check(PostEventToQueue(GetMainEventQueue(), value, 1), "callback_event_post");
                var type = new EventType { Class = 0x6B657962, Kind = 5 };
                Check(ReceiveNextEvent(1, ref type, 1, 1, out var received), "callback_event_receive");
                if (received == 0) throw new ProbeFailure("callback_event_receive");
                try
                {
                    if (received != value) throw new ProbeFailure("callback_unexpected_event");
                    Check(SendEventToEventTarget(received, GetApplicationEventTarget()), "callback_event_send");
                }
                finally { ReleaseEvent(received); }
            }
            finally { ReleaseEvent(value); }
        }
        if (gate.Failed) throw new ProbeFailure("callback_capture_failed");
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static nint WindowProc(nint hwnd, uint message, nuint wparam, nint lparam)
    {
        try
        {
            var owner = current;
            if (message == HotKeyMessage && owner is not null && hwnd == owner.window)
            {
                owner.gate.Deliver(Signature, (uint)wparam);
                return 0;
            }
            return DefWindowProcW(hwnd, message, wparam, lparam);
        }
        catch { return 0; }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int CarbonHandler(nint next, nint value, nint context)
    {
        try
        {
            var owner = current;
            if (owner is null || GetEventClass(value) != 0x6B657962 || GetEventKind(value) != 5) return -9874;
            if (GetEventParameter(value, 0x2D2D2D2D, 0x686B6964, 0, 8, out var size, out var id) != 0 || size != 8) return -9874;
            owner.gate.Deliver(id.Signature, id.Id);
            return 0;
        }
        catch { return -9874; }
    }

    public void Dispose()
    {
        if (disposed) return;
        RequireThread();
        try { Release(); }
        finally
        {
            try
            {
                if (handler != 0) Check(RemoveEventHandler(handler), "callback_remove");
                handler = 0;
                if (window != 0 && DestroyWindow(window) == 0) throw new ProbeFailure("callback_destroy");
                window = 0;
                if (atom != 0)
                {
                    fixed (char* name = className)
                        if (UnregisterClassW(name, GetModuleHandleW(0)) == 0) throw new ProbeFailure("callback_unregister_class");
                    atom = 0;
                }
            }
            finally { current = null; disposed = true; }
        }
    }

    private void RequireThread()
    {
        if (Environment.CurrentManagedThreadId != thread || disposed) throw new ProbeFailure("callback_thread_or_lifetime");
    }
    private static void Check(int status, string token) { if (status != 0) throw new ProbeFailure(token); }

    [StructLayout(LayoutKind.Sequential)] private struct HotKeyId { internal uint Signature, Id; }
    [StructLayout(LayoutKind.Sequential)] private struct EventType { internal uint Class, Kind; }
    [StructLayout(LayoutKind.Sequential)] private struct WindowClass
    {
        internal uint Size, Style;
        internal delegate* unmanaged[Stdcall]<nint, uint, nuint, nint, nint> Procedure;
        internal int ClassExtra, WindowExtra;
        internal nint Instance, Icon, Cursor, Background;
        internal char* Menu;
        internal char* Name;
        internal nint SmallIcon;
    }
    [StructLayout(LayoutKind.Sequential)] private struct Message
    {
        internal nint Window; internal uint Id; internal nuint Wparam; internal nint Lparam;
        internal uint Time; internal int X, Y; internal uint Private;
    }
    [LibraryImport("kernel32.dll")] private static partial nint GetModuleHandleW(nint name);
    [LibraryImport("user32.dll")] private static partial ushort RegisterClassExW(ref WindowClass cls);
    [LibraryImport("user32.dll")] private static partial int UnregisterClassW(char* name, nint instance);
    [LibraryImport("user32.dll")] private static partial nint CreateWindowExW(uint ex, char* cls, char* name, uint style, int x, int y, int width, int height, nint parent, nint menu, nint instance, nint param);
    [LibraryImport("user32.dll")] private static partial int DestroyWindow(nint hwnd);
    [LibraryImport("user32.dll")] private static partial nint DefWindowProcW(nint hwnd, uint message, nuint wparam, nint lparam);
    [LibraryImport("user32.dll")] private static partial short GetAsyncKeyState(int key);
    [LibraryImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")] private static partial byte CGEventSourceKeyState(int state, ushort key);
    [LibraryImport("user32.dll")] private static partial int RegisterHotKey(nint hwnd, int id, uint modifiers, uint key);
    [LibraryImport("user32.dll")] private static partial int UnregisterHotKey(nint hwnd, int id);
    [LibraryImport("user32.dll")] private static partial int PostMessageW(nint hwnd, uint message, nuint wparam, nint lparam);
    [LibraryImport("user32.dll")] private static partial int PeekMessageW(out Message message, nint hwnd, uint min, uint max, uint remove);
    [LibraryImport("user32.dll")] private static partial nint DispatchMessageW(ref Message message);
    [LibraryImport("/usr/lib/libSystem.B.dylib")] private static partial int pthread_main_np();
    [LibraryImport(Carbon)] private static partial nint GetApplicationEventTarget();
    [LibraryImport(Carbon)] private static partial int InstallEventHandler(nint target, delegate* unmanaged[Cdecl]<nint, nint, nint, int> callback, nuint count, ref EventType types, nint data, out nint handler);
    [LibraryImport(Carbon)] private static partial int RemoveEventHandler(nint handler);
    [LibraryImport(Carbon)] private static partial int RegisterEventHotKey(uint key, uint modifiers, HotKeyId id, nint target, uint options, out nint hotkey);
    [LibraryImport(Carbon)] private static partial int UnregisterEventHotKey(nint hotkey);
    [LibraryImport(Carbon)] private static partial int CreateEvent(nint allocator, uint cls, uint kind, double time, uint flags, out nint value);
    [LibraryImport(Carbon)] private static partial int SetEventParameter(nint value, uint name, uint type, nuint size, ref HotKeyId data);
    [LibraryImport(Carbon)] private static partial nint GetMainEventQueue();
    [LibraryImport(Carbon)] private static partial int PostEventToQueue(nint queue, nint value, short priority);
    [LibraryImport(Carbon)] private static partial int ReceiveNextEvent(nuint count, ref EventType types, double timeout, byte pull, out nint value);
    [LibraryImport(Carbon)] private static partial int SendEventToEventTarget(nint value, nint target);
    [LibraryImport(Carbon)] private static partial void ReleaseEvent(nint value);
    [LibraryImport(Carbon)] private static partial uint GetEventClass(nint value);
    [LibraryImport(Carbon)] private static partial uint GetEventKind(nint value);
    [LibraryImport(Carbon)] private static partial int GetEventParameter(nint value, uint name, uint type, nint actualType, nuint size, out nuint actualSize, out HotKeyId data);
}
