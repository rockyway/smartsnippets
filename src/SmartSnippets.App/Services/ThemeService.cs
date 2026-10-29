using Microsoft.UI;
using Microsoft.UI.Xaml;
using SmartSnippets.Core.Logging;
using SmartSnippets.Core.Settings;
using SmartSnippets.Presentation.Services;
using Windows.UI;

namespace SmartSnippets.App.Services;

/// <summary>Applies the theme per window through the root element's RequestedTheme and recolors the title-bar buttons.</summary>
internal sealed class ThemeService : IThemeService
{
    private readonly List<Window> _windows = [];

    public ThemeService(ThemeMode initial) => Current = initial;

    public ThemeMode Current { get; private set; }

    public void Register(Window window)
    {
        if (_windows.Contains(window))
        {
            return;
        }

        _windows.Add(window);
        ApplyTo(window);

        if (window.Content is FrameworkElement root)
        {
            void OnActualThemeChanged(FrameworkElement sender, object args) => UpdateTitleBar(window, sender.ActualTheme);
            root.ActualThemeChanged += OnActualThemeChanged;
            window.Closed += (_, _) =>
            {
                root.ActualThemeChanged -= OnActualThemeChanged;
                _windows.Remove(window);
            };
        }
        else
        {
            window.Closed += (_, _) => _windows.Remove(window);
        }
    }

    public void Apply(ThemeMode mode)
    {
        Current = mode;
        foreach (var window in _windows.ToArray())
        {
            ApplyTo(window);
        }

        AppLog.Info("theme_applied", $"mode={mode}");
    }

    private void ApplyTo(Window window)
    {
        if (window.Content is not FrameworkElement root)
        {
            return;
        }

        root.RequestedTheme = Current switch
        {
            ThemeMode.Light => ElementTheme.Light,
            ThemeMode.Dark => ElementTheme.Dark,
            _ => ElementTheme.Default,
        };
        UpdateTitleBar(window, root.ActualTheme);
    }

    private void UpdateTitleBar(Window window, ElementTheme effective)
    {
        var titleBar = window.AppWindow.TitleBar;
        var dark = ThemeSource.IsDark(Current, effective == ElementTheme.Dark);
        WindowSurface.SetDark(window, dark);
        var foreground = dark ? Colors.White : Colors.Black;
        var hoverBackground = dark ? Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0x33, 0x00, 0x00, 0x00);
        var pressedBackground = dark ? Color.FromArgb(0x22, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0x22, 0x00, 0x00, 0x00);
        var inactive = dark ? Color.FromArgb(0xFF, 0x9A, 0x9A, 0x9A) : Color.FromArgb(0xFF, 0x6E, 0x6E, 0x6E);

        titleBar.ButtonBackgroundColor = Colors.Transparent;
        titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        titleBar.ButtonForegroundColor = foreground;
        titleBar.ButtonInactiveForegroundColor = inactive;
        titleBar.ButtonHoverBackgroundColor = hoverBackground;
        titleBar.ButtonHoverForegroundColor = foreground;
        titleBar.ButtonPressedBackgroundColor = pressedBackground;
        titleBar.ButtonPressedForegroundColor = foreground;
        // Title-bar/dark-mode updates must not restore the popup's default accent border.
        if (window is Popup.PopupWindow popup) popup.RefreshFrame();
    }
}
