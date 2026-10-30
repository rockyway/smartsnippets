using Microsoft.UI;
using Microsoft.UI.Xaml;
using SmartSnippets.Core.Logging;
using SmartSnippets.Core.Settings;
using SmartSnippets.Presentation.Services;
using Windows.UI;
using Windows.UI.ViewManagement;
using Microsoft.UI.Dispatching;

namespace SmartSnippets.App.Services;

/// <summary>Applies the theme per window through the root element's RequestedTheme and recolors the title-bar buttons.</summary>
internal sealed class ThemeService : IThemeService, IDisposable
{
    private readonly List<Window> _windows = [];
    private readonly List<FrameworkElement> _transientSurfaces = [];
    private bool _refreshQueued;
    private readonly PaletteResources _paletteResources = new();
    private readonly UISettings _uiSettings = new();
    private readonly DispatcherQueue _dispatcher = DispatcherQueue.GetForCurrentThread();
    private bool _disposed;
    private MessageWindow? _messageWindow;

    public ThemeService(ThemeMode initial, ColorPalette palette = ColorPalette.Default)
    {
        Current = initial;
        CurrentPalette = palette;
        AppLog.Info("palette_loaded", $"palette={palette}");
        _paletteResources.Update(palette, _uiSettings);
        _paletteResources.BaselineChanged += QueueSystemRefresh;
        FlyoutSurface.ResolveMenuBrush = _paletteResources.MenuBrush;
        Controls.HoverDetailsSurface.ResolveAppearance = (border, text, theme, companion) =>
        {
            var highContrast = WindowSurface.IsHighContrast();
            border.Background = highContrast
                ? new Microsoft.UI.Xaml.Media.SolidColorBrush(WindowSurface.SystemColor(5))
                : companion
                    ? new Microsoft.UI.Xaml.Media.SolidColorBrush(theme == ElementTheme.Dark
                        ? Color.FromArgb(255, 32, 32, 32) : Color.FromArgb(255, 243, 243, 243))
                    : _paletteResources.BackgroundBrush(theme);
            var foreground = highContrast ? WindowSurface.SystemColor(8) : theme == ElementTheme.Dark ? Colors.White : Colors.Black;
            text.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(foreground);
            border.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(foreground) { Opacity = highContrast ? 1 : companion ? 0.18 : 0.35 };
        };
        FlyoutSurface.ResolveRequestedTheme = () => RequestedTheme;
        _uiSettings.ColorValuesChanged += OnSystemColorsChanged;
    }

    public void ObserveSystemMessages(MessageWindow window)
    {
        _messageWindow = window;
        window.MessageReceived += OnSystemMessage;
    }

    private void OnSystemMessage(uint message, nuint wParam, nint lParam)
    {
        // WM_SYSCOLORCHANGE, WM_SETTINGCHANGE and WM_THEMECHANGED. No user-provided payload is read.
        if (message is 0x0015 or 0x001A or 0x031A) QueueSystemRefresh();
    }

    private void OnSystemColorsChanged(UISettings sender, object args) => QueueSystemRefresh();

    private void QueueSystemRefresh()
    {
        // ColorValuesChanged can arrive off-thread. Coalesce only on the UI thread.
        _dispatcher.TryEnqueue(() =>
        {
            if (_disposed || _refreshQueued) return;
            _refreshQueued = true;
            if (!_dispatcher.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                _refreshQueued = false;
                if (!_disposed) RefreshPalette();
            })) _refreshQueued = false;
        });
    }

    private void RefreshPalette()
    {
        Controls.HoverDetailsSurface.CancelActive();
        _paletteResources.Update(CurrentPalette, _uiSettings);
        FlyoutSurface.Refresh();
        foreach (var window in _windows.ToArray())
            if (window.Content is FrameworkElement root)
            {
                if (window is Popup.PopupWindow popup) popup.SetPaletteBackground(_paletteResources.BackgroundBrush(root.ActualTheme));
                UpdateTitleBar(window, root.ActualTheme);
            }
    }

    public void Dispose()
    {
        _disposed = true;
        _uiSettings.ColorValuesChanged -= OnSystemColorsChanged;
        _paletteResources.BaselineChanged -= QueueSystemRefresh;
        if (_messageWindow is not null) _messageWindow.MessageReceived -= OnSystemMessage;
        Controls.HoverDetailsSurface.CancelActive();
        Controls.HoverDetailsSurface.ResolveAppearance = null;
        FlyoutSurface.ResolveMenuBrush = null;
        FlyoutSurface.ResolveRequestedTheme = null;
        _transientSurfaces.Clear();
    }

    public ThemeMode Current { get; private set; }

    public ColorPalette CurrentPalette { get; private set; }

    public void ApplyPalette(ColorPalette palette)
    {
        CurrentPalette = Enum.IsDefined(palette) ? palette : ColorPalette.Default;
        RefreshPalette();
        AppLog.Info("palette_applied", $"palette={CurrentPalette}");
    }

    public void Register(Window window)
    {
        if (_windows.Contains(window))
        {
            return;
        }

        _windows.Add(window);
        if (window.Content is FrameworkElement content)
        {
            _paletteResources.InstallRootResources(content);
            _paletteResources.AttachBaselines(content);
        }
        ApplyTo(window);
        RefreshPalette();

        if (window.Content is FrameworkElement root)
        {
            void OnActualThemeChanged(FrameworkElement sender, object args) => RefreshPalette();
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

    public void RegisterTransient(FrameworkElement surface)
    {
        _transientSurfaces.Add(surface);
        surface.RequestedTheme = RequestedTheme;
    }

    public void UnregisterTransient(FrameworkElement surface) => _transientSurfaces.Remove(surface);

    private ElementTheme RequestedTheme => Current switch
    {
        ThemeMode.Light => ElementTheme.Light,
        ThemeMode.Dark => ElementTheme.Dark,
        _ => ElementTheme.Default,
    };

    public void Apply(ThemeMode mode)
    {
        Current = mode;
        foreach (var window in _windows.ToArray())
        {
            ApplyTo(window);
        }

        foreach (var surface in _transientSurfaces.ToArray()) surface.RequestedTheme = RequestedTheme;
        RefreshPalette();
        AppLog.Info("theme_applied", $"mode={mode}");
    }

    private void ApplyTo(Window window)
    {
        if (window.Content is not FrameworkElement root)
        {
            return;
        }

        root.RequestedTheme = RequestedTheme;
        UpdateTitleBar(window, root.ActualTheme);
    }

    private void UpdateTitleBar(Window window, ElementTheme effective)
    {
        var titleBar = window.AppWindow.TitleBar;
        var dark = ThemeSource.IsDark(Current, effective == ElementTheme.Dark);
        WindowSurface.SetDark(window, dark);
        var highContrast = WindowSurface.IsHighContrast();
        var foreground = highContrast ? WindowSurface.SystemColor(8) : dark ? Colors.White : Colors.Black;
        var hoverBackground = dark ? Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0x33, 0x00, 0x00, 0x00);
        var pressedBackground = dark ? Color.FromArgb(0x22, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0x22, 0x00, 0x00, 0x00);
        var inactive = dark ? Color.FromArgb(0xFF, 0x9A, 0x9A, 0x9A) : Color.FromArgb(0xFF, 0x6E, 0x6E, 0x6E);

        titleBar.ButtonBackgroundColor = Colors.Transparent;
        titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        titleBar.ButtonForegroundColor = foreground;
        titleBar.ButtonInactiveForegroundColor = highContrast ? WindowSurface.SystemColor(17) : inactive;
        titleBar.ButtonHoverBackgroundColor = highContrast ? WindowSurface.SystemColor(13) : hoverBackground;
        titleBar.ButtonHoverForegroundColor = highContrast ? WindowSurface.SystemColor(14) : foreground;
        titleBar.ButtonPressedBackgroundColor = highContrast ? WindowSurface.SystemColor(13) : pressedBackground;
        titleBar.ButtonPressedForegroundColor = highContrast ? WindowSurface.SystemColor(14) : foreground;
        // Title-bar/dark-mode updates must not restore the popup's default accent border.
        if (window is Popup.PopupWindow popup) popup.RefreshFrame();
    }
}
