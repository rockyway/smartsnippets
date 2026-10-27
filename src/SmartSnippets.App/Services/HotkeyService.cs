using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace SmartSnippets.App.Services;

internal readonly record struct HotkeyRegistration(bool Success, int Win32Error)
{
    public const int ErrorHotkeyAlreadyRegistered = 1409;

    public string Describe() => Success
        ? "Hotkey Ctrl+Shift+Space registered."
        : Win32Error == ErrorHotkeyAlreadyRegistered
            ? $"Hotkey in use by another application (Win32 error {Win32Error})."
            : $"Hotkey registration failed (Win32 error {Win32Error}).";
}

internal sealed class HotkeyService : IDisposable
{
    private const int HotkeyId = 1;

    private readonly MessageWindow _window;
    private bool _registered;

    public HotkeyService(MessageWindow window)
    {
        _window = window;
        _window.MessageReceived += OnMessage;
    }

    /// <summary>Raised on the UI thread with the Stopwatch timestamp taken when WM_HOTKEY arrived.</summary>
    public event Action<long>? HotkeyPressed;

    public HotkeyRegistration Register(HOT_KEY_MODIFIERS modifiers, uint virtualKey)
    {
        if (_registered)
        {
            return new HotkeyRegistration(true, 0);
        }

        if (PInvoke.RegisterHotKey(_window.Handle, HotkeyId, modifiers, virtualKey))
        {
            _registered = true;
            return new HotkeyRegistration(true, 0);
        }

        return new HotkeyRegistration(false, Marshal.GetLastPInvokeError());
    }

    public void Unregister()
    {
        if (!_registered)
        {
            return;
        }

        PInvoke.UnregisterHotKey(_window.Handle, HotkeyId);
        _registered = false;
    }

    public void Dispose()
    {
        Unregister();
        _window.MessageReceived -= OnMessage;
    }

    private void OnMessage(uint message, nuint wParam, nint lParam)
    {
        if (message == PInvoke.WM_HOTKEY && (int)wParam == HotkeyId)
        {
            HotkeyPressed?.Invoke(Stopwatch.GetTimestamp());
        }
    }
}
