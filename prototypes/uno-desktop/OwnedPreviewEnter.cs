using System;
using System.Runtime.InteropServices;

namespace SmartSnippets.UnoProbe;

/// <summary>Enter only, from the owned ready foreground popup; no modifiers or clipboard writes.</summary>
internal static unsafe partial class OwnedPreviewEnter
{
    internal static string GuardState { get; private set; } = "not_attempted";

    internal static long ClipboardEpoch
    {
        get
        {
#if WINDOWS
            var epoch = GetClipboardSequenceNumber();
            if (epoch == 0) throw new InvalidOperationException("Clipboard epoch unavailable.");
            return epoch;
#else
            var board = Send(GetClass("NSPasteboard"), Selector("generalPasteboard"));
            if (board == 0) throw new InvalidOperationException("Pasteboard unavailable.");
            return Send(board, Selector("changeCount")).ToInt64();
#endif
        }
    }

    internal static bool TryPost(ProbeWindowSurface surface, Func<bool> logicalFocus, Action beforePost)
    {
        GuardState = "focus";
        if (!surface.IsForeground || !logicalFocus()) return false;
#if WINDOWS
        RequireInputDesktop();
        var last = new LastInput { Size = (uint)Marshal.SizeOf<LastInput>() };
        if (GetLastInputInfo(ref last) == 0) throw new InvalidOperationException("Input idle unavailable.");
        if (unchecked((uint)GetTickCount64() - last.Tick) < 1500) return false;
        if (WindowsInputHeld()) return false;
        if (!surface.IsForeground || !logicalFocus()) return false;
        var events = new Input[2];
        events[0].Type = events[1].Type = 1;
        events[0].Data.Keyboard.Key = events[1].Data.Keyboard.Key = 0x0D;
        events[1].Data.Keyboard.Flags = 2;
        if (Marshal.SizeOf<Input>() != 40) throw new InvalidOperationException("Input ABI mismatch.");
        beforePost();
        RequireInputDesktop();
        if (!surface.IsForeground || !logicalFocus() || WindowsInputHeld()) throw new InvalidOperationException("Enter target changed.");
        var sent = SendInput(2, events, Marshal.SizeOf<Input>());
        if (sent == 1)
        {
            var release = new[] { events[1] };
            if (SendInput(1, release, Marshal.SizeOf<Input>()) != 1)
                throw new InvalidOperationException("Enter release incomplete.");
        }
        if (sent != 2) throw new InvalidOperationException("Enter dispatch incomplete.");
#else
        if (CGPreflightPostEventAccess() == 0) throw new InvalidOperationException("Event permission unavailable.");
        GuardState = "input_idle";
        if (CGEventSourceSecondsSinceLastEventType(0, uint.MaxValue) < 1.5) return false;
        GuardState = "held_input";
        if (MacInputHeld()) return false;
        var down = CGEventCreateKeyboardEvent(0, 36, 1);
        var up = CGEventCreateKeyboardEvent(0, 36, 0);
        try
        {
            if (down == 0 || up == 0) throw new InvalidOperationException("Enter allocation failed.");
            CGEventSetFlags(down, 0);
            CGEventSetFlags(up, 0);
            if (!surface.IsForeground || !logicalFocus()) return false;
            // PID-targeted events cannot be redirected to a foreign application.
            // Void posting is NOT delivery evidence; the actual routed event is required.
            beforePost();
            if (!surface.IsForeground || !logicalFocus() || MacInputHeld())
                throw new InvalidOperationException("Enter target changed.");
            CGEventPostToPid(Environment.ProcessId, down);
            CGEventPostToPid(Environment.ProcessId, up);
        }
        finally { if (down != 0) CFRelease(down); if (up != 0) CFRelease(up); }
#endif
        GuardState = "posted";
        return true;
    }

#if WINDOWS
    private static bool WindowsInputHeld()
    {
        for (var key = 1; key < 255; key++) if ((GetAsyncKeyState(key) & 0x8000) != 0) return true;
        return false;
    }

    private static void RequireInputDesktop()
    {
        var input = OpenInputDesktop(0, 0, 1); // Read identity only; never switch desktops.
        if (input == 0) throw new InvalidOperationException("Input desktop unavailable.");
        try
        {
            var thread = GetThreadDesktop(GetCurrentThreadId());
            char* inputName = stackalloc char[256];
            char* threadName = stackalloc char[256];
            if (thread == 0 || GetUserObjectInformationW(input, 2, inputName, 512, out var inputBytes) == 0 ||
                GetUserObjectInformationW(thread, 2, threadName, 512, out var threadBytes) == 0 ||
                inputBytes < 2 || inputBytes > 512 || threadBytes != inputBytes ||
                new string(inputName, 0, (int)inputBytes / 2 - 1) != new string(threadName, 0, (int)threadBytes / 2 - 1))
                throw new InvalidOperationException("Input desktop changed.");
        }
        finally { if (CloseDesktop(input) == 0) throw new InvalidOperationException("Desktop handle cleanup failed."); }
    }

    [LibraryImport("user32.dll")]
    private static partial nint OpenInputDesktop(uint flags, int inherit, uint access);
    [LibraryImport("user32.dll")]
    private static partial nint GetThreadDesktop(uint thread);
    [LibraryImport("kernel32.dll")]
    private static partial uint GetCurrentThreadId();
    [LibraryImport("user32.dll")]
    private static partial int GetUserObjectInformationW(nint handle, int index, void* buffer, uint size, out uint needed);
    [LibraryImport("user32.dll")]
    private static partial int CloseDesktop(nint desktop);
    [StructLayout(LayoutKind.Sequential)]
    private struct LastInput { public uint Size, Tick; }
    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput { public ushort Key, Scan; public uint Flags, Time; public nuint Extra; }
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    private struct InputUnion { [FieldOffset(0)] public KeyboardInput Keyboard; }
    [StructLayout(LayoutKind.Sequential)]
    private struct Input { public uint Type; public InputUnion Data; }
    [LibraryImport("user32.dll")]
    private static partial uint GetClipboardSequenceNumber();
    [LibraryImport("user32.dll")]
    private static partial int GetLastInputInfo(ref LastInput last);
    [LibraryImport("kernel32.dll")]
    private static partial ulong GetTickCount64();
    [LibraryImport("user32.dll")]
    private static partial short GetAsyncKeyState(int key);
    [LibraryImport("user32.dll")]
    private static partial uint SendInput(uint count, [In] Input[] events, int size);
#else
    private static bool MacInputHeld()
    {
        for (ushort key = 0; key < 128; key++) if (CGEventSourceKeyState(0, key) != 0) return true;
        for (var button = 0; button < 5; button++) if (CGEventSourceButtonState(0, button) != 0) return true;
        return false;
    }

    private const string CG = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";
    private const string ObjC = "/usr/lib/libobjc.A.dylib";
    [LibraryImport(CG)]
    private static partial byte CGPreflightPostEventAccess();
    [LibraryImport(CG)]
    private static partial double CGEventSourceSecondsSinceLastEventType(int state, uint type);
    [LibraryImport(CG)]
    private static partial byte CGEventSourceKeyState(int state, ushort key);
    [LibraryImport(CG)]
    private static partial byte CGEventSourceButtonState(int state, int button);
    [LibraryImport(CG)]
    private static partial nint CGEventCreateKeyboardEvent(nint source, ushort key, byte down);
    [LibraryImport(CG)]
    private static partial void CGEventPostToPid(int pid, nint value);
    [LibraryImport(CG)]
    private static partial void CGEventSetFlags(nint value, ulong flags);
    [LibraryImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static partial void CFRelease(nint value);
    [LibraryImport(ObjC, EntryPoint = "objc_getClass", StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint GetClass(string name);
    [LibraryImport(ObjC, EntryPoint = "sel_registerName", StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint Selector(string name);
    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    private static partial nint Send(nint receiver, nint selector);
#endif
}
