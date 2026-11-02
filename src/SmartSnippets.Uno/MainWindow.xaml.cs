using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SmartSnippets.Core.Logging;
using SmartSnippets.Presentation.Navigation;
using SmartSnippets.Presentation.Services;
using Windows.Graphics;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;

namespace SmartSnippets.Uno;

/// <summary>Snippets / Folders / Settings shell. Closing hides the window; the app lives in the tray.</summary>
public sealed partial class MainWindow : Window
{
    private const int LogicalWidth = 1000;
    private const int LogicalHeight = 700;
    private const int MinimumLogicalWidth = 640;
    private const int MinimumLogicalHeight = 480;

    private readonly Func<PageKind, UIElement> _pageFactory;
    private readonly Dictionary<PageKind, UIElement> _pages = [];
    private PageKind? _current;
    private bool _suppressSelection;
    private bool _closingForExit;
    private bool _closed;
    private bool _guardInProgress;
    private HMONITOR _monitor;

    public MainWindow(Func<PageKind, UIElement> pageFactory, bool qaPlacement = false)
    {
        _pageFactory = pageFactory;
        InitializeComponent();
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "SmartSnippets.ico");
        if (File.Exists(iconPath)) AppWindow.SetIcon(iconPath);
        else AppLog.Info("window_icon_missing", "name=SmartSnippets.ico");
        Title = "Smart Snippets";
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        var hwnd = new HWND(WinRT.Interop.WindowNative.GetWindowHandle(this));
        // Place before minimums, so mixed-DPI QA bounds are not clamped by the primary DPI.
        var presenter = OverlappedPresenter.Create();
        AppWindow.SetPresenter(presenter);
        bool Place() => Services.WindowPlacement.PlaceManagement(AppWindow, LogicalWidth, LogicalHeight, qaPlacement);
        if (!Place()) throw new InvalidOperationException("Window placement could not be verified.");
        var dpi = PInvoke.GetDpiForWindow(hwnd);
        var scale = dpi == 0 ? 1.0 : dpi / 96.0;
        presenter.PreferredMinimumWidth = Math.Min(AppWindow.Size.Width, (int)(MinimumLogicalWidth * scale));
        presenter.PreferredMinimumHeight = Math.Min(AppWindow.Size.Height, (int)(MinimumLogicalHeight * scale));
        _monitor = PInvoke.MonitorFromWindow(hwnd, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
        AppWindow.Changed += (_, args) =>
        {
            if (_closingForExit || _closed) return;
            if (args.DidPresenterChange && presenter.State == OverlappedPresenterState.Minimized)
                Backgrounded?.Invoke(this, EventArgs.Empty);
            if (!args.DidPositionChange && !args.DidSizeChange) return;
            // Re-read after move or size/DPI processing, rather than using the
            // monitor on which the hidden window was originally constructed.
            DispatcherQueue.TryEnqueue(() =>
            {
                if (_closingForExit || _closed) return;
                var currentDpi = PInvoke.GetDpiForWindow(hwnd);
                if (currentDpi == 0) return;
                var currentScale = currentDpi / 96.0;
                var width = (int)(MinimumLogicalWidth * currentScale);
                var height = (int)(MinimumLogicalHeight * currentScale);
                if (presenter.PreferredMinimumWidth != width) presenter.PreferredMinimumWidth = width;
                if (presenter.PreferredMinimumHeight != height) presenter.PreferredMinimumHeight = height;
                var monitor = PInvoke.MonitorFromWindow(hwnd, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
                var monitorChanged = monitor != _monitor;
                _monitor = monitor;
                Moved?.Invoke(this, monitorChanged);
            });
        };
        if (!Place()) throw new InvalidOperationException("Window placement could not be verified.");
        AppWindow.Closing += OnClosing;
        Activated += (_, args) =>
        {
            if (_closingForExit || _closed) return;
            if (args.WindowActivationState == WindowActivationState.Deactivated)
            {
                Controls.HoverDetailsSurface.CancelActive(ContentXamlRoot);
                Backgrounded?.Invoke(this, EventArgs.Empty);
            }
            else Foregrounded?.Invoke(this, EventArgs.Empty);
        };
        // Content is no longer readable during Closed. Hover cancellation happens before Close/hide.
        Closed += (_, _) => _closed = true;

        Show(PageKind.Snippets, activate: false);
    }

    /// <summary>The XamlRoot for ContentDialogs; null until the window content has been created.</summary>
    public XamlRoot? ContentXamlRoot => _closingForExit || _closed ? null : Content?.XamlRoot;

    /// <summary>Raised after the user's close button hid the window (not for exit and not when the page's guard refused).</summary>
    public event EventHandler? HiddenByClose;

    /// <summary>Raised after each move or resize; the argument is true when the window is now on a different monitor.</summary>
    internal event EventHandler<bool>? Moved;

    /// <summary>Raised when the window is minimized or stops being the active window.</summary>
    internal event EventHandler? Backgrounded;

    /// <summary>Raised when the window becomes the active window again.</summary>
    internal event EventHandler? Foregrounded;

    internal void SetInteractionEnabled(bool enabled) => Navigation.IsEnabled = enabled;
    internal void SetHotkeyStatus(HotkeyStatus status) => NativeStatusText.Text = status.IsRegistered
        ? status.Message + " Focus a target app, press the shortcut, then Enter to insert. Copy and start at login are available."
        : status.Message;

    public bool IsHidden => !AppWindow.IsVisible;

    public PageKind? CurrentPage => _current;

    /// <summary>Navigates to the page (asking the current page's guard first), then shows and activates the window.</summary>
    public void ShowPage(PageKind kind, bool flashOnFocusDenied = true)
    {
        BringToFront(flashOnFocusDenied);
        _ = NavigateGuardedAsync(kind);
    }

    /// <summary>Navigate through the normal unsaved-changes guard without taking foreground.</summary>
    internal async Task ShowPageWithoutActivationAsync(PageKind kind, bool show)
    {
        if (await NavigateGuardedAsync(kind) && show) AppWindow.Show(false);
    }

    /// <summary>Shows and activates the window on the page it already displays.</summary>
    public void BringToFront(bool flashOnFocusDenied = true)
    {
        // App shell navigation never transfers foreground to or from another process.
        AppWindow.Show(false);
        AppLog.Info("main_window_activation", "focus=app_no_activation");
    }

    /// <summary>For tray Exit: true when the current page allows leaving; otherwise the window is brought forward.</summary>
    public async Task<bool> CanExitAsync()
    {
        Controls.HoverDetailsSurface.CancelActive(ContentXamlRoot);
        if (CurrentGuard() is not { } guard)
        {
            return true;
        }

        var allowed = await guard.CanLeaveAsync();
        if (!allowed)
        {
            BringToFront();
        }

        return allowed;
    }

    /// <summary>Really closes the window (the normal X button only hides it).</summary>
    public void CloseForExit()
    {
        if (_closingForExit || _closed) return;
        Controls.HoverDetailsSurface.CancelActive(ContentXamlRoot);
        _closingForExit = true;
        Close();
    }

    private void Show(PageKind kind, bool activate)
    {
        Navigate(kind);
        if (activate)
        {
            BringToFront();
        }
    }

    private void Navigate(PageKind kind)
    {
        Controls.HoverDetailsSurface.CancelActive(ContentXamlRoot);
        if (!_pages.TryGetValue(kind, out var page))
        {
            page = _pageFactory(kind);
            _pages[kind] = page;
        }

        _current = kind;
#if SMARTSNIPPETS_MEASURE
        RestoreMeasurementPageState(page);
#endif
        // The shortcut status is a Snippets-page hint; other pages have their own titles.
        var showHint = kind == PageKind.Snippets;
        NativeStatusText.Visibility = showHint ? Visibility.Visible : Visibility.Collapsed;
        // A collapsed header still reserves its row; hide it so page titles sit at the top edge.
        Navigation.AlwaysShowHeader = showHint;
        Navigation.Content = page;
        _suppressSelection = true;
        Navigation.SelectedItem = ItemFor(kind);
        _suppressSelection = false;
    }

    private NavigationViewItem ItemFor(PageKind kind) => kind switch
    {
        PageKind.Folders => FoldersItem,
        PageKind.Settings => SettingsItem,
        _ => SnippetsItem,
    };

    private IGuardedPage? CurrentGuard() => Navigation.Content as IGuardedPage;

    private async Task<bool> NavigateGuardedAsync(PageKind kind)
    {
        if (_guardInProgress)
        {
            return false;
        }

        Controls.HoverDetailsSurface.CancelActive(ContentXamlRoot);
        _guardInProgress = true;
        try
        {
            return await UiPolicy.NavigateAsync(
                () => _current != kind && CurrentGuard() is { } guard ? guard.CanLeaveAsync() : Task.FromResult(true),
                () => Navigate(kind), false, () => AppWindow.Show(false));
        }
        catch (Exception ex)
        {
            AppLog.Error("navigate_failed", ex);
            return false;
        }
        finally
        {
            _guardInProgress = false;
        }
    }

    private void OnSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (_suppressSelection || args.SelectedItem is not NavigationViewItem { Tag: string tag })
        {
            return;
        }

        var kind = tag switch
        {
            "Folders" => PageKind.Folders,
            "Settings" => PageKind.Settings,
            _ => PageKind.Snippets,
        };

        if (kind == _current)
        {
            return;
        }

        // Selection has already moved; put it back until the current page's guard agrees to leave.
        if (_current is { } previous)
        {
            _suppressSelection = true;
            sender.SelectedItem = ItemFor(previous);
            _suppressSelection = false;
        }

        _ = NavigateGuardedAsync(kind);
    }

    private void OnClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (_closingForExit)
        {
            return;
        }

        // Cancel synchronously (the handler cannot await), then hide once the page's guard agrees.
        args.Cancel = true;
        _ = HideAfterGuardAsync();
    }

    private async Task HideAfterGuardAsync()
    {
        Controls.HoverDetailsSurface.CancelActive(ContentXamlRoot);
        try
        {
            if (CurrentGuard() is { } guard && !await guard.CanLeaveAsync())
            {
                return;
            }

            AppWindow.Hide();
            HiddenByClose?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            AppLog.Error("hide_failed", ex);
        }
    }
}
