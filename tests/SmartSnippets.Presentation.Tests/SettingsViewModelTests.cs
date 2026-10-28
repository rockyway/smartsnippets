using SmartSnippets.Core.Settings;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Presentation.Tests.Fakes;
using SmartSnippets.Presentation.Text;
using SmartSnippets.Presentation.ViewModels;

namespace SmartSnippets.Presentation.Tests;

public sealed class SettingsViewModelTests
{
    private static readonly HotkeyGesture CtrlAltK = new(HotkeyGesture.ModControl | HotkeyGesture.ModAlt, 0x4B);

    private readonly MemorySettingsStore _store = new();
    private readonly FakeThemeService _theme = new();
    private readonly TextSizeService _textSize = new();
    private readonly FakeStartupRegistration _startup = new();
    private readonly FakeHotkeyManager _hotkey = new();
    private readonly FakeElevation _elevation = new();
    private int _exitRequests;

    private SettingsViewModel Create(AppSettings? initial = null) =>
        new(initial ?? new AppSettings(), _store, _theme, _textSize, _startup, _hotkey, _elevation, () => _exitRequests++);

    [Fact]
    public void LoadsPersistedValues()
    {
        _startup.Enabled = true;
        var initial = new AppSettings
        {
            Theme = ThemeMode.Dark,
            TextSizePreset = TextSizePreset.Custom,
            CustomTextSize = 22,
        }.WithHotkey(CtrlAltK);

        var vm = Create(initial);

        Assert.Equal(ThemeMode.Dark, vm.SelectedTheme);
        Assert.Equal(TextSizePreset.Custom, vm.SelectedTextSizePreset);
        Assert.True(vm.IsCustomTextSize);
        Assert.Equal(22, vm.CustomTextSize);
        Assert.True(vm.StartAtLogin);
        Assert.Equal("Ctrl+Alt+K", vm.CurrentHotkeyDisplay);
        Assert.True(vm.CanRestartAsAdmin);
        Assert.Empty(_theme.Applied);
        Assert.Empty(_store.Values);
        Assert.False(vm.IsStatusVisible);
    }

    [Fact]
    public void ThemeChange_AppliesAndPersists()
    {
        var vm = Create();

        vm.SelectedTheme = ThemeMode.Dark;

        Assert.Equal([ThemeMode.Dark], _theme.Applied);
        Assert.Equal("Dark", _store.Values[SettingsKeys.Theme]);
    }

    [Fact]
    public void ThemeChange_AfterPopupLayoutChange_KeepsPopupLayout()
    {
        using var vm = Create();
        _store.Set(SettingsKeys.PopupLayout, PopupLayout.Folder.ToString());
        _store.Set(SettingsKeys.PopupSort, SortField.UsageCount.ToString());

        vm.SelectedTheme = ThemeMode.Dark;

        Assert.Equal(PopupLayout.Folder, AppSettings.Load(_store).PopupLayout);
        Assert.Equal(SortField.UsageCount, AppSettings.Load(_store).PopupSort);
    }

    [Fact]
    public void TextSizePreset_AppliesAndPersists()
    {
        var vm = Create();

        vm.SelectedTextSizePreset = TextSizePreset.Large;

        Assert.Equal(18, _textSize.FontSize);
        Assert.False(vm.IsCustomTextSize);
        Assert.Equal("Large", _store.Values[SettingsKeys.TextSizePreset]);
    }

    [Theory]
    [InlineData(100, 32)]
    [InlineData(2, 10)]
    [InlineData(double.NaN, 14)]
    [InlineData(double.NegativeInfinity, 14)]
    public void CustomSize_InvalidInput_Clamped(double input, double expected)
    {
        var vm = Create();
        vm.SelectedTextSizePreset = TextSizePreset.Custom;

        vm.CustomTextSize = input;

        Assert.Equal(expected, vm.CustomTextSize);
        Assert.Equal(expected, _textSize.FontSize);
        Assert.Equal(AppSettings.Load(_store).CustomTextSize, expected);
    }

    [Fact]
    public void StartupToggle_Success_CallsEnableAndDisable()
    {
        var vm = Create();

        vm.StartAtLogin = true;
        Assert.True(_startup.Enabled);
        vm.StartAtLogin = false;

        Assert.False(_startup.Enabled);
        Assert.Equal(2, _startup.Calls);
        Assert.False(vm.IsStatusVisible);
    }

    [Fact]
    public void StartupToggleFailure_RevertsAndShowsMessage()
    {
        _startup.NextResult = new StartupResult(false, "Registry write denied");
        var vm = Create();

        vm.StartAtLogin = true;

        Assert.False(vm.StartAtLogin);
        Assert.Equal(1, _startup.Calls);
        Assert.Equal("Registry write denied", vm.StatusMessage);
        Assert.Equal(StatusSeverity.Error, vm.StatusSeverity);
        Assert.True(vm.IsStatusVisible);
    }

    [Fact]
    public void Capture_Conflict_KeepsCurrent_ShowsConflictText()
    {
        var conflict = UiText.HotkeyConflict("Ctrl+Alt+K", "Ctrl+Shift+Space");
        _hotkey.NextResult = new HotkeyChangeResult(false, conflict);
        var vm = Create();

        vm.SetCandidate(CtrlAltK);

        Assert.Equal([CtrlAltK], _hotkey.TryChangeCalls);
        Assert.Equal("Ctrl+Shift+Space", vm.CurrentHotkeyDisplay);
        Assert.Equal(conflict, vm.HotkeyFeedback);
        Assert.Equal(StatusSeverity.Error, vm.HotkeyFeedbackSeverity);
        Assert.Equal(
            "Another app is already using Ctrl+Alt+K. Your hotkey is still Ctrl+Shift+Space. Try a different combination.",
            vm.HotkeyFeedback);
        Assert.Equal(HotkeyGesture.Default, AppSettings.Load(_store).Hotkey);
        Assert.Empty(_store.Values);
    }

    [Fact]
    public void Capture_ValidGesture_AppliesImmediatelyOnce()
    {
        var vm = Create();
        Assert.Equal(UiText.HotkeyHint, vm.HotkeyFeedback);

        vm.SetCandidate(CtrlAltK);

        Assert.Equal([CtrlAltK], _hotkey.TryChangeCalls);
        Assert.Equal("Ctrl+Alt+K", vm.CurrentHotkeyDisplay);
        Assert.Equal(["Ctrl", "Alt", "K"], vm.CurrentHotkeyParts);
        Assert.Equal(CtrlAltK, AppSettings.Load(_store).Hotkey);
        Assert.Null(vm.CandidateGesture);
        Assert.Equal(StatusSeverity.Success, vm.HotkeyFeedbackSeverity);
        Assert.Equal("Shortcut set to Ctrl+Alt+K", vm.HotkeyFeedback);
    }

    [Fact]
    public void Capture_InvalidGesture_NoRegistrationAttempt()
    {
        var vm = Create();

        vm.SetCandidate(new HotkeyGesture(HotkeyGesture.ModShift, 0x4B));

        Assert.Empty(_hotkey.TryChangeCalls);
        Assert.Null(vm.CandidateGesture);
        Assert.Equal("Ctrl+Shift+Space", vm.CurrentHotkeyDisplay);
        Assert.Equal(SettingsViewModel.InvalidGestureMessage, vm.HotkeyFeedback);
        Assert.Equal(StatusSeverity.Warning, vm.HotkeyFeedbackSeverity);
    }

    [Fact]
    public void HotkeyParts_SplitModifiersAndKey()
    {
        Assert.Equal(["Ctrl", "Shift", "Space"], Create().CurrentHotkeyParts);
        var numPlus = new HotkeyGesture(HotkeyGesture.ModControl, 0x6B);
        Assert.Equal(["Ctrl", "Num +"], Create(new AppSettings().WithHotkey(numPlus)).CurrentHotkeyParts);
    }

    [Fact]
    public void RestoreDefault_RegistersDefault()
    {
        var vm = Create(new AppSettings().WithHotkey(CtrlAltK));

        vm.RestoreDefaultHotkeyCommand.Execute(null);

        Assert.Equal(1, _hotkey.RestoreDefaultCalls);
        Assert.Equal("Ctrl+Shift+Space", vm.CurrentHotkeyDisplay);
        Assert.Equal(HotkeyGesture.Default, AppSettings.Load(_store).Hotkey);
    }

    [Fact]
    public void RestoreDefault_Failure_KeepsCurrent()
    {
        var conflict = UiText.HotkeyConflict("Ctrl+Shift+Space", "Ctrl+Alt+K");
        _hotkey.NextResult = new HotkeyChangeResult(false, conflict);
        var vm = Create(new AppSettings().WithHotkey(CtrlAltK));

        vm.RestoreDefaultHotkeyCommand.Execute(null);

        Assert.Equal("Ctrl+Alt+K", vm.CurrentHotkeyDisplay);
        Assert.Equal(conflict, vm.HotkeyFeedback);
        Assert.Empty(_store.Values);
    }

    [Theory]
    [InlineData(ThemeMode.Light, 0)]
    [InlineData(ThemeMode.Dark, 1)]
    [InlineData(ThemeMode.System, 2)]
    public void ThemeIndex_FollowsRadioOrder_AndRoundTrips(ThemeMode mode, int index)
    {
        Assert.Equal(index, SettingsViewModel.ThemeToIndex(mode));
        Assert.Equal(mode, SettingsViewModel.IndexToTheme(index));
        Assert.Equal(ThemeMode.System, SettingsViewModel.IndexToTheme(-1));
    }

    [Fact]
    public void TextSizeHint_FollowsFontSize()
    {
        var vm = Create();
        Assert.Equal("14 units. Custom accepts 10 to 32.", vm.TextSizeHint);

        vm.SelectedTextSizePreset = TextSizePreset.Large;

        Assert.Equal(UiText.TextSizeHint(18), vm.TextSizeHint);
    }

    [Fact]
    public void StartupHotkeyFailure_ShowsBanner()
    {
        _hotkey.Status = new HotkeyStatus(false, CtrlAltK, "Already used by another application");

        var vm = Create(new AppSettings().WithHotkey(CtrlAltK));

        Assert.True(vm.IsHotkeyBannerVisible);
        Assert.Equal("Already used by another application", vm.HotkeyStatusMessage);

        _hotkey.Status = new HotkeyStatus(true, CtrlAltK, string.Empty);

        Assert.False(vm.IsHotkeyBannerVisible);
        Assert.Equal(string.Empty, vm.HotkeyStatusMessage);
    }

    [Fact]
    public void RestartCancelled_ShowsInfo_StaysRunning()
    {
        _elevation.NextResult = new ElevationResult(ElevationOutcome.Cancelled, "Administrator restart was cancelled");
        var vm = Create();

        vm.RestartAsAdministratorCommand.Execute(null);

        Assert.Equal(0, _exitRequests);
        Assert.Equal(StatusSeverity.Info, vm.StatusSeverity);
        Assert.Equal("Administrator restart was cancelled", vm.StatusMessage);
    }

    [Fact]
    public void RestartFailed_ShowsError_StaysRunning()
    {
        _elevation.NextResult = new ElevationResult(ElevationOutcome.Failed, "Could not start");
        var vm = Create();

        vm.RestartAsAdministratorCommand.Execute(null);

        Assert.Equal(0, _exitRequests);
        Assert.Equal(StatusSeverity.Error, vm.StatusSeverity);
    }

    [Fact]
    public void RestartStarted_RequestsExit()
    {
        var vm = Create();

        vm.RestartAsAdministratorCommand.Execute(null);

        Assert.Equal(1, _elevation.Calls);
        Assert.Equal(1, _exitRequests);
    }

    [Fact]
    public void Elevated_CannotRestartAsAdmin()
    {
        _elevation.IsElevated = true;

        Assert.False(Create().CanRestartAsAdmin);
    }

    [Fact]
    public void ThemeToIndex_UnknownValue_MapsToSystem()
    {
        var index = SettingsViewModel.ThemeToIndex((ThemeMode)99);

        Assert.Equal(ThemeMode.System, SettingsViewModel.IndexToTheme(index));
    }
}
