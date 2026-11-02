using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SmartSnippets.Target018;

internal static unsafe partial class Program
{
    private const int EditorId = 101;
    private const string UnicodeFixture = "AΩ中🙂e\u0301";
    private const string MultilineFixture = "AΩ中🙂e\u0301\r\nsecond line\r\n";
    internal static bool AllowedCommand(string command) => command is
        "foreground_grant" or "close_a" or "close_b" or "empty_a" or "empty_b" or "focus_a" or "focus_b" or
        "verify_unicode_a" or "verify_unicode_b" or "verify_multiline_a" or "verify_multiline_b";

    internal static bool VerifyFixture(string command, ReadOnlySpan<char> text) =>
        command is "verify_unicode_a" or "verify_unicode_b" ? text.SequenceEqual(UnicodeFixture) :
        command is "verify_multiline_a" or "verify_multiline_b" && text.SequenceEqual(MultilineFixture);

    // Retained handle pins the bootstrap-selected candidate lifetime, not just its numeric PID.
    private static Process? grantee;
    internal static bool MatchesGrantee(uint expectedPid, long expectedStarted, uint actualPid, long actualStarted, bool alive) =>
        expectedPid != 0 && expectedPid != uint.MaxValue && expectedStarted > 0 && alive &&
        expectedPid == actualPid && expectedStarted == actualStarted;

    private static Process? PinGrantee()
    {
        var pidText = Environment.GetEnvironmentVariable("SS_TARGET_GRANTEE_PID");
        var startedText = Environment.GetEnvironmentVariable("SS_TARGET_GRANTEE_STARTED");
        Environment.SetEnvironmentVariable("SS_TARGET_GRANTEE_PID", null);
        Environment.SetEnvironmentVariable("SS_TARGET_GRANTEE_STARTED", null);
        if (!uint.TryParse(pidText, NumberStyles.None, CultureInfo.InvariantCulture, out var pid) ||
            !long.TryParse(startedText, NumberStyles.None, CultureInfo.InvariantCulture, out var started) ||
            pid == (uint)Environment.ProcessId || pid > int.MaxValue || pid == 0) return null;
        Process? process = null;
        try
        {
            process = Process.GetProcessById((int)pid);
            _ = process.SafeHandle; // Keep the exact process object alive through the grant.
            if (MatchesGrantee(pid, started, (uint)process.Id, process.StartTime.ToUniversalTime().ToFileTimeUtc(), !process.HasExited))
                return process;
        }
        catch (Exception) { /* Fixed failed receipt only; no process details. */ }
        process?.Dispose();
        return null;
    }

    private static string GrantForeground(nint a)
    {
        try
        {
            if (grantee is null || grantee.HasExited || IsWindow(a) == 0 || SmartSnippets.TargetIdentity018.ForegroundIdentity.Root(GetForegroundWindow()) != a) return "failed";
            VerifyOwner(a);
            return AllowSetForegroundWindow((uint)grantee.Id) != 0 ? "granted" : "refused";
        }
        catch (Exception) { return "failed"; }
    }

    private static int _closed;
    private const uint CommandMessage = 0x8001;
    private static readonly ConcurrentQueue<string> Commands = new();

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)]
    private struct Point { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)]
    private struct Message
    {
        public nint Window;
        public uint Id;
        public nuint WParam;
        public nint LParam;
        public uint Time;
        public Point Position;
        public uint Private;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct WindowClass
    {
        public uint Size, Style;
        public delegate* unmanaged[Stdcall]<nint, uint, nuint, nint, nint> Procedure;
        public int ClassExtra, WindowExtra;
        public nint Instance, Icon, Cursor, Background, MenuName, ClassName, SmallIcon;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo { public uint Size; public Rect Monitor, Work; public uint Flags; }
    private struct Selection { public int Found, Failed; public Rect Monitor, Work; }

    #if !TARGET_PROTOCOL_TESTS
    [STAThread]
    public static int Main(string[] args)
    {
        // No default GUI, file association, single-instance key or clipboard path.
        if (!OperatingSystem.IsWindows() || args.Length != 2 || args[0] != "--authorized-probe-target" ||
            !Guid.TryParseExact(args[1], "D", out var run)) return 2;
        try { return Run(run); }
        catch (Exception error)
        {
            Console.Error.WriteLine("target_setup_failed kind=" + error.GetType().Name);
            return 1;
        }
    }

    #endif

    private static int Run(Guid run)
    {
        using var pinnedGrantee = PinGrantee();
        grantee = pinnedGrantee;
        var selected = new Selection();
        Require(EnumDisplayMonitors(0, 0, &VisitMonitor, (nint)(&selected)) != 0 && selected.Failed == 0 && selected.Found != 0);
        var area = selected.Work;
        Require(area.Right - area.Left >= 960 && area.Bottom - area.Top >= 360);
        var instance = GetModuleHandleW(null);
        Require(instance != 0);
        var name = "SS_Target018_" + run.ToString("N");
        nint a = 0, b = 0;
        fixed (char* className = name)
        {
            var windowClass = new WindowClass
            {
                Size = (uint)sizeof(WindowClass), Procedure = &WindowProcedure, Instance = instance,
                Cursor = LoadCursorW(0, 32512), Background = 6, ClassName = (nint)className
            };
            Require(RegisterClassExW(ref windowClass) != 0);
            try
            {
                a = CreateTarget(name, "SS-Target018-A-" + run.ToString("D"), area.Left + 16, area.Top + 16, instance, area);
                b = CreateTarget(name, "SS-Target018-B-" + run.ToString("D"), area.Left + 480, area.Top + 16, instance, area);
                var editA = GetDlgItem(a, EditorId);
                var editB = GetDlgItem(b, EditorId);
                Require(editA != 0 && editB != 0 && IsChild(a, editA) != 0 && IsChild(b, editB) != 0);
                VerifyOwner(a); VerifyOwner(b); VerifyOwner(editA); VerifyOwner(editB);
                Console.WriteLine($"target_ready pid={Environment.ProcessId} a={(nuint)a} b={(nuint)b} edit_a={(nuint)editA} edit_b={(nuint)editB}");
                Console.Out.Flush();
                var thread = GetCurrentThreadId();
                _ = Task.Run(() => ReadCommands(thread));
                while (true)
                {
                    var result = GetMessageW(out var message, 0, 0, 0);
                    Require(result >= 0);
                    if (result == 0) break;
                    if (message.Window == 0 && message.Id == CommandMessage)
                    {
                        while (Commands.TryDequeue(out var command)) ExecuteCommand(command, a, b, editA, editB);
                        continue;
                    }
                    _ = TranslateMessage(ref message);
                    _ = DispatchMessageW(ref message);
                }
                return 0;
            }
            finally
            {
                if (a != 0 && IsWindow(a) != 0) _ = DestroyWindow(a);
                if (b != 0 && IsWindow(b) != 0) _ = DestroyWindow(b);
                _ = UnregisterClassW(name, instance);
            }
        }
    }

    private static void ReadCommands(uint thread)
    {
        // The harness owns stdin. EOF is not a shutdown request (startup-only
        // smoke uses DEVNULL); never touch UI/native windows on this worker.
        try
        {
            while (Console.In.ReadLine() is { } command)
            {
                if (!AllowedCommand(command))
                {
                    Console.Error.WriteLine("target_command_refused");
                    continue;
                }
                Commands.Enqueue(command);
                if (PostThreadMessageW(thread, CommandMessage, 0, 0) == 0) return;
            }
        }
        catch (Exception error) { Console.Error.WriteLine("target_reader_failed kind=" + error.GetType().Name); }
    }

    private static void ExecuteCommand(string command, nint a, nint b, nint editA, nint editB)
    {
        if (command == "foreground_grant")
        {
            var result = GrantForeground(a);
            Console.WriteLine("target_foreground_grant result=" + result);
            Console.Out.Flush();
            return;
        }
        var window = command.EndsWith("_a", StringComparison.Ordinal) ? a : b;
        var editor = window == a ? editA : editB;
        var ok = IsWindow(window) != 0 && IsWindow(editor) != 0;
        var count = -1;
        if (ok)
        {
            VerifyOwner(window); VerifyOwner(editor);
            ok = IsChild(window, editor) != 0;
            if (ok && command.StartsWith("close_", StringComparison.Ordinal))
                ok = DestroyWindow(window) != 0 && IsWindow(window) == 0;
            else if (ok && command.StartsWith("focus_", StringComparison.Ordinal))
            {
                // One UI-thread attempt; no queue attachment, input synthesis or foreign activation.
                ok = SetForegroundWindow(window) != 0;
                if (ok) _ = SetFocus(editor);
                ok = ok && SmartSnippets.TargetIdentity018.ForegroundIdentity.Root(GetForegroundWindow()) == window && GetFocus() == editor;
            }
            else if (ok)
            {
                var expected = command.StartsWith("verify_unicode_", StringComparison.Ordinal) ? UnicodeFixture :
                    command.StartsWith("verify_multiline_", StringComparison.Ordinal) ? MultilineFixture : "";
                count = checked((int)SendMessageW(editor, 0x000E, 0, 0));
                ok = count == expected.Length;
                if (ok)
                {
                    var buffer = new char[count + 1];
                    fixed (char* text = buffer)
                        ok = SendMessageW(editor, 0x000D, (nuint)buffer.Length, (nint)text) == count &&
                            (command.StartsWith("verify_", StringComparison.Ordinal) ? VerifyFixture(command, buffer.AsSpan(0, count)) : count == 0);
                }
            }
        }
        Console.WriteLine($"target_command name={command} ok={(ok ? 1 : 0)}" +
            (command.StartsWith("verify_", StringComparison.Ordinal) ? $" utf16_count={count}" : ""));
        Console.Out.Flush();
    }

    private static nint CreateTarget(string name, string title, int x, int y, nint instance, Rect area)
    {
        var window = CreateWindowExW(0, name, title, 0x00CF0000, x, y, 430, 300, 0, 0, instance, 0);
        Require(window != 0);
        try
        {
            var edit = CreateWindowExW(0, "EDIT", "", 0x50A01044, 12, 12, 380, 220, window, EditorId, instance, 0);
            Require(edit != 0);
            _ = SendMessageW(edit, 0x00C5, 65536, 0); // EM_SETLIMITTEXT: bound this synthetic target buffer.
            _ = SendMessageW(edit, 0x0030, (nuint)GetStockObject(17), 1); // Shared DEFAULT_GUI_FONT.
            LayoutEditor(window);
            Require(SetWindowPos(window, 0, x, y, 430, 300, 0x14) != 0);
            // Reapply after any initial cross-DPI transition.
            var bounds = new Rect();
            Require(SetWindowPos(window, 0, x, y, 430, 300, 0x14) != 0 && GetWindowRect(window, out bounds) != 0);
            Require(bounds.Left >= area.Left && bounds.Top >= area.Top && bounds.Right <= area.Right && bounds.Bottom <= area.Bottom);
            _ = ShowWindow(window, 4); // SW_SHOWNOACTIVATE: receiving focus is an explicit later test step.
            return window;
        }
        catch
        {
            _ = DestroyWindow(window);
            throw;
        }
    }

    private static void LayoutEditor(nint window)
    {
        var editor = GetDlgItem(window, EditorId);
        if (editor != 0 && GetClientRect(window, out var client) != 0)
            _ = MoveWindow(editor, 12, 12, Math.Max(1, client.Right - 24), Math.Max(1, client.Bottom - 24), 1);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static nint WindowProcedure(nint window, uint message, nuint wParam, nint lParam)
    {
        // No managed exception crosses this native callback. Layout calls below
        // are bounded arithmetic + Win32 return values, with no user callbacks.
        if (message == 0x0005) { LayoutEditor(window); return 0; } // WM_SIZE
        if (message == 0x0007) // WM_SETFOCUS
        {
            var editor = GetDlgItem(window, EditorId);
            if (editor != 0) _ = SetFocus(editor);
            return 0;
        }
        if (message == 0x0002) { if (++_closed == 2) PostQuitMessage(0); return 0; } // WM_DESTROY
        return DefWindowProcW(window, message, wParam, lParam);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int VisitMonitor(nint monitor, nint dc, Rect* clip, nint context)
    {
        var selected = (Selection*)context;
        var info = new MonitorInfo { Size = (uint)sizeof(MonitorInfo) };
        if (GetMonitorInfoW(monitor, ref info) == 0) { selected->Failed = 1; return 0; }
        if (selected->Found == 0 || info.Monitor.Left < selected->Monitor.Left ||
            (info.Monitor.Left == selected->Monitor.Left && info.Monitor.Top > selected->Monitor.Top))
        { selected->Found = 1; selected->Monitor = info.Monitor; selected->Work = info.Work; }
        return 1;
    }

    private static void VerifyOwner(nint window) => Require(GetWindowThreadProcessId(window, out var pid) != 0 && pid == (uint)Environment.ProcessId);
    private static void Require(bool condition) { if (!condition) throw new InvalidOperationException("Owned target setup failed."); }

    [LibraryImport("kernel32.dll")]
    private static partial uint GetCurrentThreadId();
    [LibraryImport("user32.dll")]
    private static partial int PostThreadMessageW(uint thread, uint message, nuint wParam, nint lParam);
    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial nint GetModuleHandleW(string? name);
    [LibraryImport("user32.dll")]
    private static partial ushort RegisterClassExW(ref WindowClass windowClass);
    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int UnregisterClassW(string name, nint instance);
    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial nint CreateWindowExW(uint extended, string name, string title, uint style, int x, int y, int width, int height, nint parent, nint menu, nint instance, nint parameter);
    [LibraryImport("user32.dll")]
    private static partial nint LoadCursorW(nint instance, nint name);
    [LibraryImport("gdi32.dll")]
    private static partial nint GetStockObject(int index);
    [LibraryImport("user32.dll")]
    private static partial nint GetDlgItem(nint window, int id);
    [LibraryImport("user32.dll")]
    private static partial int IsChild(nint parent, nint child);
    [LibraryImport("user32.dll")]
    private static partial int IsWindow(nint window);
    [LibraryImport("user32.dll")]
    private static partial uint GetWindowThreadProcessId(nint window, out uint pid);
    [LibraryImport("user32.dll")]
    private static partial int GetClientRect(nint window, out Rect rect);
    [LibraryImport("user32.dll")]
    private static partial int GetWindowRect(nint window, out Rect rect);
    [LibraryImport("user32.dll")]
    private static partial int MoveWindow(nint window, int x, int y, int width, int height, int repaint);
    [LibraryImport("user32.dll")]
    private static partial int SetWindowPos(nint window, nint after, int x, int y, int width, int height, uint flags);
    [LibraryImport("user32.dll")]
    private static partial int ShowWindow(nint window, int command);
    [LibraryImport("user32.dll")]
    private static partial int SetForegroundWindow(nint window);
    [LibraryImport("user32.dll")]
    private static partial int AllowSetForegroundWindow(uint processId);
    [LibraryImport("user32.dll")]
    private static partial nint GetForegroundWindow();
    [LibraryImport("user32.dll")]
    private static partial nint GetFocus();
    [LibraryImport("user32.dll")]
    private static partial nint SetFocus(nint window);
    [LibraryImport("user32.dll")]
    private static partial nint SendMessageW(nint window, uint message, nuint wParam, nint lParam);
    [LibraryImport("user32.dll")]
    private static partial int DestroyWindow(nint window);
    [LibraryImport("user32.dll")]
    private static partial void PostQuitMessage(int code);
    [LibraryImport("user32.dll")]
    private static partial int GetMessageW(out Message message, nint window, uint min, uint max);
    [LibraryImport("user32.dll")]
    private static partial int TranslateMessage(ref Message message);
    [LibraryImport("user32.dll")]
    private static partial nint DispatchMessageW(ref Message message);
    [LibraryImport("user32.dll")]
    private static partial nint DefWindowProcW(nint window, uint message, nuint wParam, nint lParam);
    [LibraryImport("user32.dll")]
    private static partial int EnumDisplayMonitors(nint dc, nint clip, delegate* unmanaged[Stdcall]<nint, nint, Rect*, nint, int> callback, nint context);
    [LibraryImport("user32.dll")]
    private static partial int GetMonitorInfoW(nint monitor, ref MonitorInfo info);
}
