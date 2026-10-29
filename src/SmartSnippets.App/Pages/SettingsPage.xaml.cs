using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SmartSnippets.Core.Settings;
using SmartSnippets.Presentation.ViewModels;

namespace SmartSnippets.App.Pages;

public sealed partial class SettingsPage : Page
{
    private SettingsViewModel? _viewModel;
    private bool _rebinding;

    public SettingsPage()
    {
        InitializeComponent();
    }

    /// <summary>Set by the creator right after construction (no DI container).</summary>
    public SettingsViewModel ViewModel
    {
        get => _viewModel!;
        set
        {
            _rebinding = true;
            try { _viewModel = value; Bindings.Update(); }
            finally { _rebinding = false; }
        }
    }

    // Theme radios are Light/Dark/System (the view-model owns that mapping); text sizes follow enum
    // order Small/Medium/Large/Custom.
    // Instance (not static): x:Bind function bindings call these through the page instance.
    private int ThemeToIndex(ThemeMode mode) => SettingsViewModel.ThemeToIndex(mode);

    private int TextSizeToIndex(TextSizePreset preset) => (int)preset;

    private InfoBarSeverity ToInfoBarSeverity(StatusSeverity severity) => severity switch
    {
        StatusSeverity.Success => InfoBarSeverity.Success,
        StatusSeverity.Warning => InfoBarSeverity.Warning,
        StatusSeverity.Error => InfoBarSeverity.Error,
        _ => InfoBarSeverity.Informational,
    };

    private void OnThemeSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var index = ThemeRadios.SelectedIndex;
        if (!_rebinding && _viewModel is not null && index >= 0)
        {
            _viewModel.SelectedTheme = SettingsViewModel.IndexToTheme(index);
        }
    }

    private void OnTextSizeSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var index = TextSizeRadios.SelectedIndex;
        if (!_rebinding && _viewModel is not null && index >= 0)
        {
            _viewModel.SelectedTextSizePreset = (TextSizePreset)index;
        }
    }

    private void OnCustomSizeChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (!_rebinding && _viewModel is not null && sender is Slider slider && slider.FocusState != FocusState.Unfocused)
            _viewModel.CustomTextSize = e.NewValue;
    }

    private void OnStartupToggled(object sender, RoutedEventArgs e)
    {
        if (!_rebinding && _viewModel is not null)
        {
            _viewModel.StartAtLogin = StartupToggle.IsOn;
        }
    }

    // Return Visibility, not bool: a bool-returning function binding on Visibility generated uncompilable
    // code (CS0103 'obj' in SettingsPage.g.cs, CI run #32).
    private Visibility IsError(StatusSeverity severity) =>
        severity == StatusSeverity.Error ? Visibility.Visible : Visibility.Collapsed;

    private Visibility IsNotError(StatusSeverity severity) =>
        severity != StatusSeverity.Error ? Visibility.Visible : Visibility.Collapsed;

    private void OnChangeClick(object sender, RoutedEventArgs e)
    {
        KeycapsPanel.Visibility = Visibility.Collapsed;
        Recorder.Visibility = Visibility.Visible;
        DispatcherQueue.TryEnqueue(() => Recorder.Focus(FocusState.Programmatic));
    }

    private void OnGestureCaptured(object? sender, HotkeyGesture gesture)
    {
        EndCapture();
        _viewModel?.SetCandidate(gesture);
    }

    private void OnRecorderCleared(object? sender, EventArgs e) => EndCapture();

    private void OnRecorderCanceled(object? sender, EventArgs e) => EndCapture();

    private void OnRecorderLostFocus(object sender, RoutedEventArgs e) => EndCapture();

    private void EndCapture()
    {
        Recorder.Visibility = Visibility.Collapsed;
        KeycapsPanel.Visibility = Visibility.Visible;
    }
}
