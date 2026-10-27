using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SmartSnippets.Core.Settings;
using SmartSnippets.Presentation.ViewModels;

namespace SmartSnippets.App.Pages;

public sealed partial class SettingsPage : Page
{
    private SettingsViewModel? _viewModel;

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
            _viewModel = value;
            Bindings.Update();
        }
    }

    // Enum order matches the radio item order: System/Light/Dark and Small/Medium/Large/Custom.
    private static int ThemeToIndex(ThemeMode mode) => (int)mode;

    private static int TextSizeToIndex(TextSizePreset preset) => (int)preset;

    private static InfoBarSeverity ToInfoBarSeverity(StatusSeverity severity) => severity switch
    {
        StatusSeverity.Success => InfoBarSeverity.Success,
        StatusSeverity.Warning => InfoBarSeverity.Warning,
        StatusSeverity.Error => InfoBarSeverity.Error,
        _ => InfoBarSeverity.Informational,
    };

    private void OnThemeSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var index = ThemeRadios.SelectedIndex;
        if (_viewModel is not null && index >= 0)
        {
            _viewModel.SelectedTheme = (ThemeMode)index;
        }
    }

    private void OnTextSizeSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var index = TextSizeRadios.SelectedIndex;
        if (_viewModel is not null && index >= 0)
        {
            _viewModel.SelectedTextSizePreset = (TextSizePreset)index;
        }
    }

    private void OnStartupToggled(object sender, RoutedEventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.StartAtLogin = StartupToggle.IsOn;
        }
    }

    private void OnGestureCaptured(object? sender, HotkeyGesture gesture) =>
        _viewModel?.SetCandidate(gesture);

    private void OnRecorderCleared(object? sender, EventArgs e) =>
        _viewModel?.ClearCandidate();
}
