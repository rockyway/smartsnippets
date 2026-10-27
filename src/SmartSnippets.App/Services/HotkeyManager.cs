using SmartSnippets.Core.Logging;
using SmartSnippets.Core.Settings;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Presentation.Text;
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
            SetStatus(new HotkeyStatus(true, saved, $"Hotkey {saved.ToDisplayString()} registered."));
        }
        else
        {
            AppLog.Info("hotkey_startup_failed", $"win32={result.Win32Error}");
            SetStatus(new HotkeyStatus(false, saved, $"{saved.ToDisplayString()} could not be registered. {Reason(result.Win32Error)}"));
        }

        return _status;
    }

    public HotkeyChangeResult TryChange(HotkeyGesture gesture)
    {
        if (_status.IsRegistered && _status.Gesture == gesture)
        {
            return new HotkeyChangeResult(true, $"Hotkey is already {gesture.ToDisplayString()}.");
        }

        var spareId = _activeId == 1 ? 2 : 1;
        var result = _service.Register(spareId, ToModifiers(gesture), gesture.Key);
        if (!result.Success)
        {
            AppLog.Info("hotkey_change_conflict", $"win32={result.Win32Error}");
            return new HotkeyChangeResult(false, ChangeFailure(result.Win32Error, gesture));
        }

        _service.Unregister(_activeId);
        _activeId = spareId;
        AppLog.Info("hotkey_changed");
        SetStatus(new HotkeyStatus(true, gesture, $"Hotkey {gesture.ToDisplayString()} registered."));
        return new HotkeyChangeResult(true, $"Hotkey changed to {gesture.ToDisplayString()}.");
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

    /// <summary>Message for a failed change; the old hotkey is still in place, so the text names it.</summary>
    private string ChangeFailure(int win32Error, HotkeyGesture attempted)
    {
        var attemptedText = attempted.ToDisplayString();
        if (!_status.IsRegistered)
        {
            return win32Error == HotkeyRegistration.ErrorHotkeyAlreadyRegistered
                ? $"Another app is already using {attemptedText}. Try a different combination."
                : $"Windows refused {attemptedText} (error {win32Error}). Try a different combination.";
        }

        var keptText = _status.Gesture.ToDisplayString();
        return win32Error == HotkeyRegistration.ErrorHotkeyAlreadyRegistered
            ? UiText.HotkeyConflict(attemptedText, keptText)
            : $"Windows refused {attemptedText} (error {win32Error}). Your hotkey is still {keptText}. Try a different combination.";
    }

    private void SetStatus(HotkeyStatus status)
    {
        _status = status;
        StatusChanged?.Invoke(this, EventArgs.Empty);
    }
}
