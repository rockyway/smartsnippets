using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartSnippets.Core.Settings;
using SmartSnippets.Presentation.Services;

namespace SmartSnippets.Presentation.ViewModels;

/// <summary>Settings page model. Every change is applied and saved immediately; there is no Save button.</summary>
public sealed partial class SettingsViewModel : ObservableObject, IDisposable
{
    public const string InvalidGestureMessage = "Include Ctrl, Alt or Win and a non-modifier key";

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
    [NotifyCanExecuteChangedFor(nameof(ApplyHotkeyCommand))]
    private HotkeyGesture? _candidateGesture;

    [ObservableProperty]
    private string _candidateDisplay = string.Empty;

    [ObservableProperty]
    private string _currentHotkeyDisplay;

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

    public bool IsStatusVisible => StatusMessage.Length > 0;

    public void Dispose() => _hotkey.StatusChanged -= OnHotkeyStatusChanged;

    /// <summary>Called by the shortcut recorder with the captured keys; invalid gestures are rejected without registering.</summary>
    public void SetCandidate(HotkeyGesture gesture)
    {
        if (!gesture.IsValid)
        {
            ClearCandidate();
            SetStatus(InvalidGestureMessage, StatusSeverity.Warning);
            return;
        }

        CandidateGesture = gesture;
        CandidateDisplay = gesture.ToDisplayString();
        ClearStatus();
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

    [RelayCommand(CanExecute = nameof(CanApplyHotkey))]
    private void ApplyHotkey()
    {
        if (CandidateGesture is not { } gesture || !gesture.IsValid)
        {
            return;
        }

        CommitHotkey(_hotkey.TryChange(gesture), gesture);
    }

    private bool CanApplyHotkey() => CandidateGesture is not null;

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
            SetStatus(result.Message, StatusSeverity.Error);
            return;
        }

        _settings = _settings.WithHotkey(gesture);
        Persist();
        CurrentHotkeyDisplay = gesture.ToDisplayString();
        ClearCandidate();
        SetStatus($"Shortcut set to {CurrentHotkeyDisplay}", StatusSeverity.Success);
    }

    private void ApplyTextSize()
    {
        TextSize.Apply(_settings.TextSizePreset, _settings.CustomTextSize);
        Persist();
    }

    private void Persist() => _settings.Save(_settingsStore);

    private void SetStatus(string message, StatusSeverity severity)
    {
        StatusSeverity = severity;
        StatusMessage = message;
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
