using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace SmartSnippets.App.Services;

internal readonly record struct HotkeyRegistration(bool Success, int Win32Error)
{
    public const int ErrorHotkeyAlreadyRegistered = 1409;

    public string Describe() => Success
        ? "Hotkey registered."
        : Win32Error == ErrorHotkeyAlreadyRegistered
            ? $"Hotkey in use by another application (Win32 error {Win32Error})."
            : $"Hotkey registration failed (Win32 error {Win32Error}).";
}

/// <summary>Thin RegisterHotKey wrapper that supports several ids at once so a change can register before it releases.</summary>
internal sealed class HotkeyService : IDisposable
{
    private readonly MessageWindow _window;
    private readonly HashSet<int> _registered = [];

    public HotkeyService(MessageWindow window)
    {
        _window = window;
        _window.MessageReceived += OnMessage;
    }

    /// <summary>Raised on the UI thread with the Stopwatch timestamp taken when WM_HOTKEY arrived, for any registered id.</summary>
    public event Action<long>? HotkeyPressed;

    public HotkeyRegistration Register(int id, HOT_KEY_MODIFIERS modifiers, uint virtualKey)
    {
        if (_registered.Contains(id))
        {
            return new HotkeyRegistration(false, HotkeyRegistration.ErrorHotkeyAlreadyRegistered);
        }

        if (PInvoke.RegisterHotKey(_window.Handle, id, modifiers, virtualKey))
        {
            _registered.Add(id);
            return new HotkeyRegistration(true, 0);
        }

        return new HotkeyRegistration(false, Marshal.GetLastPInvokeError());
    }

    public void Unregister(int id)
    {
        if (_registered.Remove(id))
        {
            PInvoke.UnregisterHotKey(_window.Handle, id);
        }
    }

    public void UnregisterAll()
    {
        foreach (var id in _registered)
        {
            PInvoke.UnregisterHotKey(_window.Handle, id);
        }

        _registered.Clear();
    }

    public void Dispose()
    {
        UnregisterAll();
        _window.MessageReceived -= OnMessage;
    }

    private void OnMessage(uint message, nuint wParam, nint lParam)
    {
        if (message == PInvoke.WM_HOTKEY && _registered.Contains((int)wParam))
        {
            HotkeyPressed?.Invoke(Stopwatch.GetTimestamp());
        }
    }
}
