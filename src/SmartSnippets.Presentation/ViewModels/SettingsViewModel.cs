using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartSnippets.Core.Settings;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Presentation.Text;

namespace SmartSnippets.Presentation.ViewModels;

/// <summary>Settings page model. Every change is applied and saved immediately; there is no Save button.</summary>
public sealed partial class SettingsViewModel : ObservableObject, IDisposable
{
    public const string InvalidGestureMessage = "Include Ctrl, Alt or Win and a non-modifier key";

    /// <summary>Theme radio order in the design: Light, Dark, System.</summary>
    private static readonly ThemeMode[] ThemeOrder = [ThemeMode.Light, ThemeMode.Dark, ThemeMode.System];

    private static readonly string[] ModifierPrefixes = ["Ctrl+", "Alt+", "Shift+", "Win+"];

    private readonly ISettingsStore _settingsStore;
    private readonly IThemeService _theme;
    private readonly IStartupRegistration _startup;
    private readonly IHotkeyManager _hotkey;
    private readonly IElevation _elevation;
    private readonly Action? _requestExit;

    private AppSettings _settings;
    private bool _suppressStartupToggle;

    [ObservableProperty]
    private ThemeMode _selectedTheme;

    [ObservableProperty]
    private TextSizePreset _selectedTextSizePreset;

    [ObservableProperty]
    private double _customTextSize;

    [ObservableProperty]
    private bool _isCustomTextSize;

    [ObservableProperty]
    private bool _startAtLogin;

    [ObservableProperty]
    private HotkeyGesture? _candidateGesture;

    [ObservableProperty]
    private string _candidateDisplay = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentHotkeyParts))]
    private string _currentHotkeyDisplay;

    /// <summary>Text under the hotkey: the hint when idle, otherwise the outcome of the last change.</summary>
    [ObservableProperty]
    private string _hotkeyFeedback = UiText.HotkeyHint;

    [ObservableProperty]
    private StatusSeverity _hotkeyFeedbackSeverity = StatusSeverity.Info;

    [ObservableProperty]
    private string _hotkeyStatusMessage = string.Empty;

    [ObservableProperty]
    private bool _isHotkeyBannerVisible;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStatusVisible))]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private StatusSeverity _statusSeverity = StatusSeverity.Info;

    public SettingsViewModel(
        AppSettings initial,
        ISettingsStore settingsStore,
        IThemeService theme,
        ITextSizeService textSize,
        IStartupRegistration startup,
        IHotkeyManager hotkey,
        IElevation elevation,
        Action? requestExit = null)
    {
        _settings = initial;
        _settingsStore = settingsStore;
        _theme = theme;
        TextSize = textSize;
        _startup = startup;
        _hotkey = hotkey;
        _elevation = elevation;
        _requestExit = requestExit;

        _selectedTheme = initial.Theme;
        _selectedTextSizePreset = initial.TextSizePreset;
        _customTextSize = TextSizeService.Clamp(initial.CustomTextSize);
        _isCustomTextSize = initial.TextSizePreset == TextSizePreset.Custom;
        _startAtLogin = startup.IsEnabled();
        _currentHotkeyDisplay = initial.Hotkey.ToDisplayString();

        RefreshHotkeyStatus();
        _hotkey.StatusChanged += OnHotkeyStatusChanged;
    }

    public ITextSizeService TextSize { get; }

    public bool CanRestartAsAdmin => !_elevation.IsElevated;

    /// <summary>One entry per key of the current hotkey (for example Ctrl, Shift, Space), shown as keycaps.</summary>
    public List<string> CurrentHotkeyParts => [.. SplitKeys(CurrentHotkeyDisplay)];

    public string StartAtLoginHint => UiText.StartAtLoginHint;

    /// <summary>Hint under the text size options.</summary>
    public string TextSizeHint => UiText.TextSizeHint(TextSize.FontSize);

    /// <summary>Index of <paramref name="mode"/> in the Light, Dark, System radio order.</summary>
    /// <summary>Radio index for a theme; unknown values map to System, matching <see cref="IndexToTheme"/>.</summary>
    public static int ThemeToIndex(ThemeMode mode)
    {
        var index = Array.IndexOf(ThemeOrder, mode);
        return index >= 0 ? index : Array.IndexOf(ThemeOrder, ThemeMode.System);
    }

    /// <summary>Inverse of <see cref="ThemeToIndex"/>; out-of-range indexes fall back to System.</summary>
    public static ThemeMode IndexToTheme(int index) =>
        index >= 0 && index < ThemeOrder.Length ? ThemeOrder[index] : ThemeMode.System;

    public bool IsStatusVisible => StatusMessage.Length > 0;

    public void Dispose() => _hotkey.StatusChanged -= OnHotkeyStatusChanged;

    /// <summary>
    /// Called by the shortcut recorder with the captured keys. A valid gesture is applied immediately;
    /// an invalid one is rejected without registering.
    /// </summary>
    public void SetCandidate(HotkeyGesture gesture)
    {
        if (!gesture.IsValid)
        {
            ClearCandidate();
            SetHotkeyFeedback(InvalidGestureMessage, StatusSeverity.Warning);
            return;
        }

        CandidateGesture = gesture;
        CandidateDisplay = gesture.ToDisplayString();
        CommitHotkey(_hotkey.TryChange(gesture), gesture);
    }

    public void ClearCandidate()
    {
        CandidateGesture = null;
        CandidateDisplay = string.Empty;
    }

    partial void OnSelectedThemeChanged(ThemeMode value)
    {
        _theme.Apply(value);
        _settings = _settings with { Theme = value };
        Persist();
    }

    partial void OnSelectedTextSizePresetChanged(TextSizePreset value)
    {
        IsCustomTextSize = value == TextSizePreset.Custom;
        _settings = _settings with { TextSizePreset = value };
        ApplyTextSize();
    }

    partial void OnCustomTextSizeChanged(double value)
    {
        var normalized = TextSizeService.Clamp(value);
        if (normalized != value)
        {
            CustomTextSize = normalized;
            return;
        }

        _settings = _settings with { CustomTextSize = normalized };
        ApplyTextSize();
    }

    partial void OnStartAtLoginChanged(bool value)
    {
        if (_suppressStartupToggle)
        {
            return;
        }

        var result = value ? _startup.Enable() : _startup.Disable();
        if (result.Success)
        {
            ClearStatus();
            return;
        }

        _suppressStartupToggle = true;
        StartAtLogin = !value;
        _suppressStartupToggle = false;
        SetStatus(result.Message, StatusSeverity.Error);
    }

    [RelayCommand]
    private void RestoreDefaultHotkey() =>
        CommitHotkey(_hotkey.RestoreDefault(), HotkeyGesture.Default);

    [RelayCommand]
    private void RestartAsAdministrator()
    {
        var result = _elevation.RestartAsAdministrator();
        switch (result.Outcome)
        {
            case ElevationOutcome.Started:
                _requestExit?.Invoke();
                break;
            case ElevationOutcome.Cancelled:
                SetStatus(result.Message, StatusSeverity.Info);
                break;
            default:
                SetStatus(result.Message, StatusSeverity.Error);
                break;
        }
    }

    private void CommitHotkey(HotkeyChangeResult result, HotkeyGesture gesture)
    {
        if (!result.Success)
        {
            ClearCandidate();
            SetHotkeyFeedback(result.Message, StatusSeverity.Error);
            return;
        }

        _settings = _settings.WithHotkey(gesture);
        Persist();
        CurrentHotkeyDisplay = gesture.ToDisplayString();
        ClearCandidate();
        SetHotkeyFeedback($"Shortcut set to {CurrentHotkeyDisplay}", StatusSeverity.Success);
    }

    private void ApplyTextSize()
    {
        TextSize.Apply(_settings.TextSizePreset, _settings.CustomTextSize);
        OnPropertyChanged(nameof(TextSizeHint));
        Persist();
    }

    private void Persist() => _settings.Save(_settingsStore);

    private void SetStatus(string message, StatusSeverity severity)
    {
        StatusSeverity = severity;
        StatusMessage = message;
    }

    private void SetHotkeyFeedback(string message, StatusSeverity severity)
    {
        HotkeyFeedbackSeverity = severity;
        HotkeyFeedback = message;
    }

    private static string[] SplitKeys(string display)
    {
        var parts = new List<string>();
        var rest = display;
        foreach (var prefix in ModifierPrefixes)
        {
            if (rest.StartsWith(prefix, StringComparison.Ordinal))
            {
                parts.Add(prefix[..^1]);
                rest = rest[prefix.Length..];
            }
        }

        parts.Add(rest);
        return [.. parts];
    }

    private void ClearStatus() => StatusMessage = string.Empty;

    private void OnHotkeyStatusChanged(object? sender, EventArgs e) => RefreshHotkeyStatus();

    private void RefreshHotkeyStatus()
    {
        var status = _hotkey.Status;
        IsHotkeyBannerVisible = !status.IsRegistered;
        HotkeyStatusMessage = status.IsRegistered ? string.Empty : status.Message;
    }
}
