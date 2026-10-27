using SmartSnippets.Core.Logging;
using SmartSnippets.Core.Settings;
using SmartSnippets.Presentation.Services;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace SmartSnippets.App.Services;

/// <summary>Owns the global hotkey. Changes register the new gesture under a spare id before releasing the old one.</summary>
internal sealed class HotkeyManager : IHotkeyManager
{
    private readonly HotkeyService _service;
    private int _activeId = 1;
    private HotkeyStatus _status;

    public HotkeyManager(HotkeyService service)
    {
        _service = service;
        _status = new HotkeyStatus(false, HotkeyGesture.Default, "Hotkey not registered yet.");
    }

    public HotkeyStatus Status => _status;

    public event EventHandler? StatusChanged;

    /// <summary>Registers the saved gesture at startup. On failure the app keeps running without a hotkey.</summary>
    public HotkeyStatus Initialize(HotkeyGesture saved)
    {
        var result = _service.Register(_activeId, ToModifiers(saved), saved.Key);
        if (result.Success)
        {
            AppLog.Info("hotkey_registered");
            SetStatus(new HotkeyStatus(true, saved, $"Hotkey {Format(saved)} registered."));
        }
        else
        {
            AppLog.Info("hotkey_startup_failed", $"win32={result.Win32Error}");
            SetStatus(new HotkeyStatus(false, saved, $"{Format(saved)} could not be registered. {Reason(result.Win32Error)}"));
        }

        return _status;
    }

    public HotkeyChangeResult TryChange(HotkeyGesture gesture)
    {
        if (_status.IsRegistered && _status.Gesture == gesture)
        {
            return new HotkeyChangeResult(true, $"Hotkey is already {Format(gesture)}.");
        }

        var spareId = _activeId == 1 ? 2 : 1;
        var result = _service.Register(spareId, ToModifiers(gesture), gesture.Key);
        if (!result.Success)
        {
            AppLog.Info("hotkey_change_conflict", $"win32={result.Win32Error}");
            return new HotkeyChangeResult(false, Reason(result.Win32Error));
        }

        _service.Unregister(_activeId);
        _activeId = spareId;
        AppLog.Info("hotkey_changed");
        SetStatus(new HotkeyStatus(true, gesture, $"Hotkey {Format(gesture)} registered."));
        return new HotkeyChangeResult(true, $"Hotkey changed to {Format(gesture)}.");
    }

    public HotkeyChangeResult RestoreDefault() => TryChange(HotkeyGesture.Default);

    public void UnregisterAll()
    {
        _service.UnregisterAll();
        _status = _status with { IsRegistered = false };
    }

    private static HOT_KEY_MODIFIERS ToModifiers(HotkeyGesture gesture) =>
        (HOT_KEY_MODIFIERS)gesture.Modifiers | HOT_KEY_MODIFIERS.MOD_NOREPEAT;

    private static string Reason(int win32Error) => win32Error == HotkeyRegistration.ErrorHotkeyAlreadyRegistered
        ? "Already used by another application."
        : $"Windows refused this shortcut (error {win32Error}).";

    // Local formatter so this compiles regardless of Track A's HotkeyGesture.ToDisplayString.
    private static string Format(HotkeyGesture gesture)
    {
        var parts = new List<string>(5);
        if ((gesture.Modifiers & HotkeyGesture.ModControl) != 0) { parts.Add("Ctrl"); }
        if ((gesture.Modifiers & HotkeyGesture.ModAlt) != 0) { parts.Add("Alt"); }
        if ((gesture.Modifiers & HotkeyGesture.ModShift) != 0) { parts.Add("Shift"); }
        if ((gesture.Modifiers & HotkeyGesture.ModWin) != 0) { parts.Add("Win"); }
        parts.Add(KeyName(gesture.Key));
        return string.Join('+', parts);
    }

    private static string KeyName(uint vk) => vk switch
    {
        0x20 => "Space",
        >= 0x30 and <= 0x39 or >= 0x41 and <= 0x5A => ((char)vk).ToString(),
        >= 0x70 and <= 0x87 => $"F{vk - 0x6F}",
        _ => $"VK_0x{vk:X2}",
    };

    private void SetStatus(HotkeyStatus status)
    {
        _status = status;
        StatusChanged?.Invoke(this, EventArgs.Empty);
    }
}
