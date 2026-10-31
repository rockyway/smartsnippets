using System.Runtime.InteropServices;

namespace SmartSnippets.TargetIdentity018;

/// <summary>Focus only: active session/desktop, idle pointer and released modifiers.
/// This is NOT a held-key guard or authorization to post any input.</summary>
internal static partial class FocusPreflight
{
    // Public CGSession.h macro expansion, not the macro's identifier.
    internal const string OnConsoleDictionaryKey = "kCGSSessionOnConsoleKey";
    internal static async Task RequireAsync(CancellationToken ct)
    {
        var deadline = Environment.TickCount64 + 5000;
        while (Environment.TickCount64 < deadline)
        {
            if (OperatingSystem.IsWindows() ? WindowsQuiet() : OperatingSystem.IsMacOS() && MacQuiet()) return;
            await Task.Delay(50, ct);
        }
        throw new ProbeFailure("focus_preflight");
    }

    private static unsafe bool WindowsQuiet()
    {
        var desktop = OpenInputDesktop(0, 0, 1); // DESKTOP_READOBJECTS only.
        if (desktop == 0) return false;
        try
        {
            var current = GetThreadDesktop(GetCurrentThreadId());
            var a = stackalloc char[256];
            var b = stackalloc char[256];
            if (current == 0 || GetUserObjectInformationW(desktop, 2, a, 512, out _) == 0 ||
                GetUserObjectInformationW(current, 2, b, 512, out _) == 0 ||
                new ReadOnlySpan<char>(a, NullLength(a)).SequenceEqual(new ReadOnlySpan<char>(b, NullLength(b))) == false)
                return false;
            var last = new LastInput { Size = (uint)sizeof(LastInput) };
            if (GetLastInputInfo(ref last) == 0 || unchecked((uint)GetTickCount64() - last.Tick) < 1500) return false;
            foreach (var key in new[] { 0x10, 0x11, 0x12, 0x5b, 0x5c, 1, 2, 4, 5, 6 })
                if ((GetAsyncKeyState(key) & 0x8000) != 0) return false;
            return true;
        }
        finally { _ = CloseDesktop(desktop); }
    }

    private static unsafe int NullLength(char* text)
    {
        for (var index = 0; index < 256; index++) if (text[index] == 0) return index;
        return 0;
    }

    private static bool MacQuiet()
    {
        var session = CGSessionCopyCurrentDictionary();
        if (session == 0) return false;
        try
        {
            // CGSession.h defines kCGSessionOnConsoleKey as CFSTR("kCGSSessionOnConsoleKey").
            if (!SessionBoolean(session, OnConsoleDictionaryKey, required: true) ||
                !SessionBoolean(session, "kCGSessionLoginDoneKey", required: true) ||
                SessionBoolean(session, "CGSSessionScreenIsLocked", required: false)) return false;
        }
        finally { CFRelease(session); }
        var idle = CGEventSourceSecondsSinceLastEventType(0, uint.MaxValue);
        if (!double.IsFinite(idle) || idle < 1.5) return false;
        for (ushort key = 54; key < 64; key++) if (CGEventSourceKeyState(0, key) != 0) return false;
        for (uint button = 0; button < 5; button++) if (CGEventSourceButtonState(0, button) != 0) return false;
        return true;
    }

    private static bool SessionBoolean(nint session, string name, bool required)
    {
        var key = CFStringCreateWithCString(0, name, 0x08000100);
        if (key == 0) throw new ProbeFailure("focus_preflight");
        try
        {
            var value = CFDictionaryGetValue(session, key);
            if (value == 0)
            {
                if (required) throw new ProbeFailure("focus_preflight");
                return false; // Unlocked sessions omit the lock key.
            }
            if (CFGetTypeID(value) != CFBooleanGetTypeID()) throw new ProbeFailure("focus_preflight");
            return CFBooleanGetValue(value) != 0;
        }
        finally { CFRelease(key); }
    }

    [StructLayout(LayoutKind.Sequential)] private struct LastInput { public uint Size, Tick; }
    [LibraryImport("user32.dll")] private static partial nint OpenInputDesktop(uint flags, int inherit, uint access);
    [LibraryImport("user32.dll")] private static partial int CloseDesktop(nint desktop);
    [LibraryImport("user32.dll")] private static partial nint GetThreadDesktop(uint thread);
    [LibraryImport("kernel32.dll")] private static partial uint GetCurrentThreadId();
    [LibraryImport("kernel32.dll")] private static partial ulong GetTickCount64();
    [LibraryImport("user32.dll")] private static partial int GetLastInputInfo(ref LastInput info);
    [LibraryImport("user32.dll")] private static partial short GetAsyncKeyState(int key);
    [LibraryImport("user32.dll")] private static unsafe partial int GetUserObjectInformationW(nint handle, int index, char* buffer, uint size, out uint needed);
    private const string CG = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";
    private const string CF = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    [LibraryImport(CG)] private static partial nint CGSessionCopyCurrentDictionary();
    [LibraryImport(CG)] private static partial double CGEventSourceSecondsSinceLastEventType(int state, uint type);
    [LibraryImport(CG)] private static partial byte CGEventSourceKeyState(int state, ushort key);
    [LibraryImport(CG)] private static partial byte CGEventSourceButtonState(int state, uint button);
    [LibraryImport(CF)] private static partial void CFRelease(nint value);
    [LibraryImport(CF)] private static partial nint CFDictionaryGetValue(nint dictionary, nint key);
    [LibraryImport(CF)] private static partial nuint CFGetTypeID(nint value);
    [LibraryImport(CF)] private static partial nuint CFBooleanGetTypeID();
    [LibraryImport(CF)] private static partial byte CFBooleanGetValue(nint value);
    [LibraryImport(CF, StringMarshalling = StringMarshalling.Utf8)] private static partial nint CFStringCreateWithCString(nint allocator, string text, uint encoding);
}
