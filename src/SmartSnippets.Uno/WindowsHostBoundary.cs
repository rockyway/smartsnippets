using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using SmartSnippets.Core.Settings;
using SmartSnippets.Presentation.Navigation;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Presentation.Text;
using SmartSnippets.Uno.Pages;
using SmartSnippets.Uno.Popup;
using SmartSnippets.Uno.Services;
using Windows.Win32.Foundation;

namespace SmartSnippets.Uno;

internal sealed class WindowsHostBoundary : HostBoundary
{
    private MainWindow? _main;
    private PopupWindow? _popup;
    private MessageWindow? _message;
    private ThemeService? _theme;
    private IHotkeyManager? _hotkeys;
    private UnoAppHost? _host;
    // Read-only linked-fixture observation; no production activation or native unlock.
    internal MainWindow? MainWindow => _main;
    internal PopupWindow? PopupWindow => _popup;
    internal override IUiDispatcher Dispatcher { get; } = new UiDispatcher(DispatcherQueue.GetForCurrentThread());
    internal override IDisposable? CreateIdleMemoryTrimmer(Func<bool> idle)
    {
#if SMARTSNIPPETS_MEASURE
        // Candidate builds retain independent switches; the measurement driver owns their trim.
        return null;
#else
        bool PopupSettled() => _popup is not null && !_popup.AppWindow.IsVisible && _popup.IsCaptureSurfaceReady && idle();
        var trimmer = new IdleMemoryTrimmer(() => _main is { IsHidden: true } && PopupSettled(), settled: PopupSettled);
        // Startup has just finished; the main window outlives the trimmer, which ignores requests once disposed.
        trimmer.RequestSettleTrim();
        // A new monitor may mean another graphics adapter; keep postponing while the drag continues.
        if (_main is not null) _main.Moved += (_, monitorChanged) =>
        {
            if (monitorChanged) trimmer.RequestSettleTrim(); else trimmer.PostponeSettleTrim();
        };
        return trimmer;
#endif
    }
    internal override IThemeService CreateTheme(AppSettings settings) => _theme = new ThemeService(settings.Theme, settings.ColorPalette);
    internal override IDisposable CreateMessageWindow()
    {
        _message = new MessageWindow();
        _theme!.ObserveSystemMessages(_message);
        return _message;
    }
    internal override Platforms.Windows.IDesktopServices? CreateDesktopServices(UnoAppHost host, AppSettings settings, Action hotkey)
    {
        _host = host;
        if (Program.InitialOptions.HasRequest) return null;
        var desktop = new WindowsDesktopServices(_message!, settings,
            () => _popup is null ? 0 : WinRT.Interop.WindowNative.GetWindowHandle(_popup), hotkey, () => _main?.IsHidden ?? true);
        _hotkeys = desktop.Hotkeys;
        return desktop;
    }
    internal override IDialogService CreateDialogs(bool popup, ITextSizeService textSize) => new DialogService(
        () => popup ? _popup?.ContentXamlRoot : _main?.ContentXamlRoot,
        () => { if (!popup) _main?.BringToFront(); }, textSize, _theme,
        () => popup ? _popup?.ContentXamlRoot?.IsHostVisible == true : _main is { IsHidden: false });
    internal override ISnippetFileService CreateFiles() => new SnippetFileService(() => _main?.AppWindow.Id);
    internal override IMainWindow CreateMain(UnoAppHost host)
    {
        UIElement CreatePage(PageKind page) => page switch
        {
            PageKind.Snippets => new SnippetsPage { RunOperation = host.RunOperationAsync, RunCopy = host.RunCopyOperationAsync, TextSize = host.TextSize, ViewModel = host.Snippets },
            PageKind.Folders => new FoldersPage { RunOperation = host.RunOperationAsync, TextSize = host.TextSize, ViewModel = host.Folders },
            _ => new SettingsPage { ViewModel = host.SettingsPage },
        };
        _main = new MainWindow(CreatePage, Program.InitialOptions.HasRequest);
        _theme!.Register(_main);
        return new MainSurface(_main);
    }
    internal override IPopupWindow CreatePopup(UnoAppHost host)
    {
        var window = new PopupWindow(host.TextSize, () => { if (_main is { } main) _ = main.ShowPageWithoutActivationAsync(PageKind.Snippets, true); });
        _popup = window;
        window.CanShow = () => ReferenceEquals(_popup, window) && host.CanPresentPopup;
        window.CanInsertNative = () => ReferenceEquals(_popup, window) && host.Gate.CanInsert;
        window.TryFocusCapturedPopup = () => ReferenceEquals(_popup, window) && host.Gate.TryFocusOwnedPopup();
        window.ViewModel = host.Popup;
        window.RunOperation = host.RunOperationAsync;
        window.RunCopy = host.RunCopyOperationAsync;
        window.UseQaPlacement = Program.InitialOptions.HasRequest;
        _theme!.Register(window);
        window.Closed += (_, _) => { if (ReferenceEquals(_popup, window)) _popup = null; };
        var surface = new PopupSurface(window,
            () => _main is null ? default : new HWND(WinRT.Interop.WindowNative.GetWindowHandle(_main)),
            () => ReferenceEquals(_popup, window) && host.Gate.CanInsert);
        surface.AttachClosed();
        return surface;
    }
    internal override ITray CreateTray(Action open, Action<nint> preview, Action settings, Action exit)
    {
        // Tray supplies the pre-menu target HWND; the host captures it before showing.
        var tray = new TrayIcon(_message!, open, preview, settings, () => { if (_host is { } host) Dispatcher.Post(() => _ = host.RestartAsAdministratorAsync()); }, exit, _hotkeys is not null && _host?.IsElevated == false,
            () => _hotkeys?.Status is { IsRegistered: true } status ? status.Gesture.ToDisplayString() : null, _hotkeys is not null)
            { State = TrayStatePolicy.Compute(_hotkeys?.Status.IsRegistered == true, false) };
        tray.Add();
        return new TraySurface(tray);
    }
    // Automated QA uses existing --page/preview options and must exit without
    // modal focus changes. Preserve actionable error UI for interactive launches.
    internal override void ReportStartupFailure() => UiPolicy.ReportStartupFailure(
        Program.StartInBackground, Program.InitialOptions.HasRequest,
        () => Windows.Win32.PInvoke.MessageBox(default,
            "Smart Snippets could not start.",
            "Smart Snippets", Windows.Win32.UI.WindowsAndMessaging.MESSAGEBOX_STYLE.MB_OK),
        () => Console.Error.WriteLine("app_startup_failed"));
    internal override void Exit() { Program.StopActivations(); Application.Current.Exit(); }
    private sealed class MainSurface(MainWindow window) : IMainWindow
    {
        public nint NativeHandle => WinRT.Interop.WindowNative.GetWindowHandle(window);
        public event EventHandler? HiddenByClose { add => window.HiddenByClose += value; remove => window.HiddenByClose -= value; }
        public Task NavigateAsync(PageKind page, bool show) => window.ShowPageWithoutActivationAsync(page, show);
        public Task<bool> CanExitAsync() => window.CanExitAsync();
        public void SetInteractionEnabled(bool enabled) => window.SetInteractionEnabled(enabled);
        public void SetHotkeyStatus(HotkeyStatus status) => window.SetHotkeyStatus(status);
        public void Dispose() => window.CloseForExit();
    }
    private sealed class PopupSurface(PopupWindow window, Func<HWND> owner, Func<bool> canShowCaptured) : IPopupWindow
    {
        public event EventHandler? Closed;
        public event EventHandler? Hidden;
        public event EventHandler<bool>? PresentationChanged;
        private bool _disposed;
        public void Prewarm() => window.Prewarm();
        public void ShowPreview(string? query, bool openSortMenu) => window.ShowPreview(owner(), query, false, openSortMenu);
        public nint NativeHandle => WinRT.Interop.WindowNative.GetWindowHandle(window);
        public bool SupportsCapturedPopup => true;
        public bool IsCaptureSurfaceReady => !_disposed && window.IsCaptureSurfaceReady;
        public void ShowCaptured(InsertionTarget target) => window.ShowCaptured(target, canShowCaptured);
        public void Hide() => window.Hide();
        public void ShowAgain() => window.ShowAgain();
        public void OpenMainWindow() => window.OpenMainWindow();
        public void EditSnippet(long id) => window.EditSnippet(id);
        public void Focus(PopupFocusTarget target) => window.Focus(target);
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            window.Hide();
            window.Close();
        }
        // Native Closed must invalidate the forwarding surface as well as the underlying HWND.
        internal void AttachClosed()
        {
            window.VisibleChanged += (_, visible) => { if (!visible) Hidden?.Invoke(this, EventArgs.Empty); };
            window.PresentationChanged += (_, shown) => PresentationChanged?.Invoke(this, shown);
            window.Closed += (_, _) => { _disposed = true; Closed?.Invoke(this, EventArgs.Empty); };
        }
    }
    private sealed class TraySurface(TrayIcon tray) : ITray
    {
        public void Notify(string title, string message) => tray.ShowBalloon(title, message);
        public bool TryNotify(string title, string message) => tray.ShowBalloon(title, message);
        public void SetHotkeyState(bool registered, bool popupVisible) => tray.State = TrayStatePolicy.Compute(registered, popupVisible);
        public void Dispose() => tray.Dispose();
    }
}
