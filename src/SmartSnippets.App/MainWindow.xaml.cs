using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SmartSnippets.App.Services;
using SmartSnippets.Core.Logging;
using SmartSnippets.Presentation.Navigation;
using Windows.Graphics;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace SmartSnippets.App;

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
    private bool _guardInProgress;

    public MainWindow(Func<PageKind, UIElement> pageFactory)
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
        var dpi = PInvoke.GetDpiForWindow(hwnd);
        var scale = dpi == 0 ? 1.0 : dpi / 96.0;
        // Install the presenter explicitly; the pre-show default may not expose
        // OverlappedPresenter, which otherwise silently skips the size limits.
        var presenter = OverlappedPresenter.Create();
        presenter.PreferredMinimumWidth = (int)(MinimumLogicalWidth * scale);
        presenter.PreferredMinimumHeight = (int)(MinimumLogicalHeight * scale);
        AppWindow.SetPresenter(presenter);
        AppWindow.Changed += (_, args) =>
        {
            if (!args.DidPositionChange && !args.DidSizeChange) return;
            // Re-read after move or size/DPI processing, rather than using the
            // monitor on which the hidden window was originally constructed.
            DispatcherQueue.TryEnqueue(() =>
            {
                var currentDpi = PInvoke.GetDpiForWindow(hwnd);
                if (currentDpi == 0) return;
                var currentScale = currentDpi / 96.0;
                var width = (int)(MinimumLogicalWidth * currentScale);
                var height = (int)(MinimumLogicalHeight * currentScale);
                if (presenter.PreferredMinimumWidth != width) presenter.PreferredMinimumWidth = width;
                if (presenter.PreferredMinimumHeight != height) presenter.PreferredMinimumHeight = height;
            });
        };
        AppWindow.Resize(new SizeInt32((int)(LogicalWidth * scale), (int)(LogicalHeight * scale)));
        AppWindow.Closing += OnClosing;

        Show(PageKind.Snippets, activate: false);
    }

    /// <summary>The XamlRoot for ContentDialogs; null until the window content has been created.</summary>
    public XamlRoot? ContentXamlRoot => Content?.XamlRoot;

    /// <summary>Raised after the user's close button hid the window (not for exit and not when the page's guard refused).</summary>
    public event EventHandler? HiddenByClose;

    public bool IsHidden => !AppWindow.IsVisible;

    public PageKind? CurrentPage => _current;

    /// <summary>Navigates to the page (asking the current page's guard first), then shows and activates the window.</summary>
    public void ShowPage(PageKind kind)
    {
        BringToFront();
        _ = NavigateGuardedAsync(kind);
    }

    /// <summary>Navigate through the normal unsaved-changes guard without taking foreground.</summary>
    internal async Task ShowPageWithoutActivationAsync(PageKind kind, bool show)
    {
        await NavigateGuardedAsync(kind);
        if (show) AppWindow.Show(false);
    }

    /// <summary>Shows and activates the window on the page it already displays.</summary>
    public void BringToFront()
    {
        AppWindow.Show();
        if (AppWindow.Presenter is OverlappedPresenter { State: OverlappedPresenterState.Minimized } presenter)
        {
            presenter.Restore();
        }

        Activate();
        // Activate alone can leave an already-open window behind other apps. The user
        // action (popup/tray/dialog) grants this process foreground rights. Try Win32
        // directly, then the bounded input-queue fallback (detached in finally).
        var hwnd = new HWND(WinRT.Interop.WindowNative.GetWindowHandle(this));
        _ = ForegroundActivation.TryActivate(hwnd, out var path);
        AppLog.Info("main_window_activation", $"focus={path}");
    }

    /// <summary>For tray Exit: true when the current page allows leaving; otherwise the window is brought forward.</summary>
    public async Task<bool> CanExitAsync()
    {
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
        if (!_pages.TryGetValue(kind, out var page))
        {
            page = _pageFactory(kind);
            _pages[kind] = page;
        }

        _current = kind;
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

    private async Task NavigateGuardedAsync(PageKind kind)
    {
        if (_guardInProgress)
        {
            return;
        }

        _guardInProgress = true;
        try
        {
            if (_current != kind && CurrentGuard() is { } guard && !await guard.CanLeaveAsync())
            {
                return;
            }

            Navigate(kind);
        }
        catch (Exception ex)
        {
            AppLog.Error("navigate_failed", ex);
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
