using System;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SmartSnippets.TargetIdentity018;

namespace SmartSnippets.Uno.Platforms.Windows.AcceptanceNative;

/// <summary>A pumping, nonactivating message-only owner. No visible window/input APIs.</summary>
internal sealed partial class OwnedOwnerWindow : IDisposable
{
    private readonly Thread thread;
    private readonly TaskCompletionSource<nint> ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private uint threadId;
    internal nint Handle { get; }
    internal string ClassName { get; }
    internal OwnedOwnerWindow(Guid run)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        ClassName = "SS_OwnedOwnerControl019_" + run.ToString("N");
        thread = new Thread(Pump) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Handle = ready.Task.WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
    }
    internal unsafe bool IsPinned()
    {
        if (!thread.IsAlive || IsWindow(Handle) == 0 || GetWindowThreadProcessId(Handle, out var pid) != threadId ||
            pid != (uint)Environment.ProcessId) return false;
        var name = stackalloc char[256];
        var count = GetClassNameW(Handle, name, 256);
        return count == ClassName.Length && new ReadOnlySpan<char>(name, count).SequenceEqual(ClassName);
    }

    private unsafe void Pump()
    {
        nint window = 0;
        try
        {
            threadId = GetCurrentThreadId();
            var instance = GetModuleHandleW(null);
            fixed (char* name = ClassName)
            {
                var cls = new WindowClass { Size = (uint)sizeof(WindowClass), Instance = instance, Name = (nint)name, Procedure = &Procedure };
                if (RegisterClassExW(ref cls) == 0) throw new ProbeFailure("guardian_window");
            }
            window = CreateWindowExW(0, ClassName, "", 0, 0, 0, 0, 0, -3, 0, instance, 0); // HWND_MESSAGE
            if (window == 0) throw new ProbeFailure("guardian_window");
            ready.SetResult(window);
            int result;
            while ((result = GetMessageW(out var message, 0, 0, 0)) > 0) DispatchMessageW(ref message);
            if (result < 0) throw new ProbeFailure("guardian_pump");
        }
        catch (Exception error) { ready.TrySetException(error); }
        finally
        {
            if (window != 0) DestroyWindow(window);
            UnregisterClassW(ClassName, GetModuleHandleW(null));
        }
    }
    public void Dispose()
    {
        if (thread.IsAlive && PostThreadMessageW(threadId, 0x0012, 0, 0) == 0) throw new ProbeFailure("guardian_cleanup");
        if (!thread.Join(TimeSpan.FromSeconds(3))) throw new ProbeFailure("guardian_cleanup");
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static nint Procedure(nint window, uint message, nuint wParam, nint lParam) => DefWindowProcW(window, message, wParam, lParam);
    [StructLayout(LayoutKind.Sequential)] private unsafe struct WindowClass
    {
        public uint Size, Style;
        public delegate* unmanaged[Stdcall]<nint, uint, nuint, nint, nint> Procedure;
        public int ClassExtra, WindowExtra;
        public nint Instance, Icon, Cursor, Background, Menu, Name, SmallIcon;
    }
    [StructLayout(LayoutKind.Sequential)] private struct Message
    {
        public nint Window; public uint Id; public nuint WParam; public nint LParam; public uint Time; public int X, Y; public uint Private;
    }
    [LibraryImport("user32.dll")] private static unsafe partial int GetClassNameW(nint window, char* name, int maximum);
    [LibraryImport("user32.dll")] private static partial int IsWindow(nint window);
    [LibraryImport("user32.dll")] private static partial uint GetWindowThreadProcessId(nint window, out uint pid);
    [LibraryImport("kernel32.dll")] private static partial uint GetCurrentThreadId();
    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16)] private static partial nint GetModuleHandleW(string? name);
    [LibraryImport("user32.dll")] private static partial ushort RegisterClassExW(ref WindowClass cls);
    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)] private static partial int UnregisterClassW(string name, nint instance);
    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)] private static partial nint CreateWindowExW(uint extended, string name, string title, uint style, int x, int y, int width, int height, nint parent, nint menu, nint instance, nint parameter);
    [LibraryImport("user32.dll")] private static partial int GetMessageW(out Message message, nint window, uint min, uint max);
    [LibraryImport("user32.dll")] private static partial nint DispatchMessageW(ref Message message);
    [LibraryImport("user32.dll")] private static partial nint DefWindowProcW(nint window, uint message, nuint wParam, nint lParam);
    [LibraryImport("user32.dll")] private static partial int DestroyWindow(nint window);
    [LibraryImport("user32.dll")] private static partial int PostThreadMessageW(uint thread, uint message, nuint wParam, nint lParam);
}
