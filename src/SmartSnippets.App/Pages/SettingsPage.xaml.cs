using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using SmartSnippets.Core.Settings;
using SmartSnippets.Presentation.ViewModels;
using SmartSnippets.Presentation.Text;

namespace SmartSnippets.App.Pages;

public sealed partial class SettingsPage : Page
{
    private SettingsViewModel? _viewModel;
    private bool _rebinding;
    private bool _customSizeEditing;

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
            _customSizeEditing = false;
            try { _viewModel = value; Bindings.Update(); }
            finally { _rebinding = false; }
        }
    }

    // Theme radios are Light/Dark/System (the view-model owns that mapping); text sizes follow enum
    // order Small/Medium/Large/Custom.
    // Instance (not static): x:Bind function bindings call these through the page instance.
    private int ThemeToIndex(ThemeMode mode) => SettingsViewModel.ThemeToIndex(mode);

    private int TextSizeToIndex(TextSizePreset preset) => (int)preset;

    private string PaletteTitle => UiText.ColorPaletteTitle;
    private string PaletteDefault => UiText.PaletteDefault;
    private string PaletteOcean => UiText.PaletteOcean;
    private string PaletteForest => UiText.PaletteForest;
    private string PalettePlum => UiText.PalettePlum;
    private int PaletteToIndex(ColorPalette palette) => SettingsViewModel.PaletteToIndex(palette);

    private void OnPaletteSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var index = ColorPaletteRadios.SelectedIndex;
        if (!_rebinding && _viewModel is not null && index >= 0)
            _viewModel.SelectedPalette = SettingsViewModel.IndexToPalette(index);
    }

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

    private static bool HasFocusWithin(FrameworkElement owner)
    {
        if (owner.XamlRoot is null) return false;
        var focused = FocusManager.GetFocusedElement(owner.XamlRoot) as DependencyObject;
        for (var current = focused; current is not null; current = VisualTreeHelper.GetParent(current))
            if (ReferenceEquals(current, owner)) return true;
        return false;
    }

    private void OnCustomSizeGotFocus(object sender, RoutedEventArgs e)
    {
        if (!_rebinding) _customSizeEditing = true;
    }

    private void OnCustomSizeLostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not NumberBox box || !_customSizeEditing) return;
        var session = _viewModel;
        // NumberBox can commit its internal TextBox when focus leaves. Keep the edit
        // session alive through that routed event, and read the final value afterward.
        DispatcherQueue.TryEnqueue(() =>
        {
            if (!ReferenceEquals(session, _viewModel) || _rebinding) return;
            if (_customSizeEditing && double.IsFinite(box.Value) && session is not null)
                session.CustomTextSize = box.Value;
            _customSizeEditing = HasFocusWithin(box);
        });
    }

    private void OnCustomSizeChanged(NumberBox sender, NumberBoxValueChangedEventArgs e)
    {
        if (!_rebinding && _viewModel is not null && (_customSizeEditing || HasFocusWithin(sender)) && double.IsFinite(e.NewValue))
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
