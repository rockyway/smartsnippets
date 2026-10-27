using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace SmartSnippets.App.Services;

internal delegate void WindowMessageHandler(uint message, nuint wParam, nint lParam);

internal sealed unsafe class MessageWindow : IDisposable
{
    private const string ClassName = "SmartSnippets.MessageWindow";

    private static MessageWindow? s_instance;

    public MessageWindow()
    {
        if (s_instance is not null)
        {
            throw new InvalidOperationException("MessageWindow already created.");
        }

        var module = PInvoke.GetModuleHandle(default(PCWSTR));
        var instance = new HINSTANCE(module.Value);

        fixed (char* className = ClassName)
        {
            var wc = default(WNDCLASSEXW);
            wc.cbSize = (uint)sizeof(WNDCLASSEXW);
            wc.lpfnWndProc = &WndProc;
            wc.hInstance = instance;
            wc.lpszClassName = className;

            if (PInvoke.RegisterClassEx(&wc) == 0)
            {
                throw new InvalidOperationException($"RegisterClassEx failed: {Marshal.GetLastPInvokeError()}");
            }

            s_instance = this;
            Handle = PInvoke.CreateWindowEx(
                0,
                className,
                default(PCWSTR),
                0,
                0,
                0,
                0,
                0,
                HWND.HWND_MESSAGE,
                default,
                instance,
                null);
        }

        if (Handle.IsNull)
        {
            var error = Marshal.GetLastPInvokeError();
            s_instance = null;
            throw new InvalidOperationException($"CreateWindowEx failed: {error}");
        }
    }

    public HWND Handle { get; private set; }

    public event WindowMessageHandler? MessageReceived;

    public void Dispose()
    {
        if (!Handle.IsNull)
        {
            PInvoke.DestroyWindow(Handle);
            Handle = default;
        }

        if (ReferenceEquals(s_instance, this))
        {
            s_instance = null;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static LRESULT WndProc(HWND hwnd, uint message, WPARAM wParam, LPARAM lParam)
    {
        try
        {
            s_instance?.MessageReceived?.Invoke(message, wParam.Value, lParam.Value);
        }
        catch
        {
            // An exception must never cross the unmanaged boundary.
        }

        return PInvoke.DefWindowProc(hwnd, message, wParam, lParam);
    }
}
