using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace SmartSnippets.Uno.Services;

internal delegate void WindowMessageHandler(uint message, nuint wParam, nint lParam);
internal sealed unsafe class MessageWindow : IDisposable
{
    private static MessageWindow? s_instance;
    private readonly MessageWindowLifetime _lifetime;
    internal MessageWindow()
    {
        if (s_instance is not null) throw new InvalidOperationException("App message window already exists.");
        s_instance = this;
        try { _lifetime = new MessageWindowLifetime(new Native()); }
        catch { s_instance = null; throw; }
    }
    internal HWND Handle => new(_lifetime.Handle);
    internal event WindowMessageHandler? MessageReceived;
    public void Dispose()
    {
        try { _lifetime.Dispose(); }
        finally { if (ReferenceEquals(s_instance, this)) s_instance = null; }
    }
    private sealed class Native : IMessageWindowNative
    {
        private readonly HINSTANCE _module = new(PInvoke.GetModuleHandle(default(PCWSTR)).Value);
        public bool RegisterClass()
        {
            fixed (char* name = MessageWindowLifetime.ClassName)
            {
                var wc = new WNDCLASSEXW { cbSize = (uint)sizeof(WNDCLASSEXW), lpfnWndProc = &WndProc,
                    hInstance = _module, lpszClassName = name };
                return PInvoke.RegisterClassEx(&wc) != 0;
            }
        }
        public nint CreateWindow()
        {
            fixed (char* name = MessageWindowLifetime.ClassName)
                return (nint)PInvoke.CreateWindowEx(0, name, default(PCWSTR), 0, 0, 0, 0, 0,
                    HWND.Null, default, _module, null).Value;
        }
        public bool DestroyWindow(nint handle) => PInvoke.DestroyWindow(new HWND(handle));
        public bool UnregisterClass()
        {
            fixed (char* name = MessageWindowLifetime.ClassName) return PInvoke.UnregisterClass(name, _module);
        }
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static LRESULT WndProc(HWND hwnd, uint message, WPARAM wParam, LPARAM lParam)
    {
        try { s_instance?.MessageReceived?.Invoke(message, wParam.Value, lParam.Value); }
        catch { /* Never cross an unmanaged callback boundary. */ }
        return PInvoke.DefWindowProc(hwnd, message, wParam, lParam);
    }
}
