using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using SmartSnippets.Core.Settings;
using SmartSnippets.Presentation.Navigation;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Uno.Pages;
using SmartSnippets.Uno.Popup;
using SmartSnippets.Uno.Services;
using Windows.Win32.Foundation;

namespace SmartSnippets.Uno;

internal sealed class WindowsCandidateBoundary : CandidateHostBoundary
{
    private MainWindow? _main;
    private PopupWindow? _popup;
    private MessageWindow? _message;
    private ThemeService? _theme;
    internal override IUiDispatcher Dispatcher { get; } = new UiDispatcher(DispatcherQueue.GetForCurrentThread());
    internal override IThemeService CreateTheme(AppSettings settings) => _theme = new ThemeService(settings.Theme, settings.ColorPalette);
    internal override IDisposable CreateMessageWindow()
    {
        _message = new MessageWindow();
        _theme!.ObserveSystemMessages(_message);
        return _message;
    }
    internal override IDialogService CreateDialogs(bool popup, ITextSizeService textSize) => new DialogService(
        () => popup ? _popup?.ContentXamlRoot : _main?.ContentXamlRoot,
        () => { if (!popup) _main?.BringToFront(); }, textSize, _theme,
        () => popup ? _popup?.ContentXamlRoot?.IsHostVisible == true : _main is { IsHidden: false });
    internal override ISnippetFileService CreateFiles() => new SnippetFileService(() => _main?.AppWindow.Id);
    internal override ICandidateMainWindow CreateMain(UnoAppHost host)
    {
        UIElement CreatePage(PageKind page) => page switch
        {
            PageKind.Snippets => new SnippetsPage { RunOperation = host.RunOperationAsync, RunCopy = host.RunCopyOperationAsync, TextSize = host.TextSize, ViewModel = host.Snippets },
            PageKind.Folders => new FoldersPage { RunOperation = host.RunOperationAsync, TextSize = host.TextSize, ViewModel = host.Folders },
            _ => new SettingsPage { ViewModel = host.SettingsPage },
        };
        _main = new MainWindow(CreatePage);
        _theme!.Register(_main);
        return new MainSurface(_main);
    }
    internal override ICandidatePopupWindow CreatePopup(UnoAppHost host)
    {
        var window = new PopupWindow(host.TextSize, () => { if (_main is { } main) _ = main.ShowPageWithoutActivationAsync(PageKind.Snippets, true); });
        _popup = window;
        window.CanShow = () => ReferenceEquals(_popup, window) && host.CanPresentPopup;
        window.CanInsertNative = () => ReferenceEquals(_popup, window) && host.Gate.CanInsert;
        window.TryFocusCapturedPopup = () => ReferenceEquals(_popup, window) && host.Gate.TryFocusOwnedPopup();
        window.ViewModel = host.Popup;
        window.RunOperation = host.RunOperationAsync;
        window.RunCopy = host.RunCopyOperationAsync;
        _theme!.Register(window);
        window.Closed += (_, _) => { if (ReferenceEquals(_popup, window)) _popup = null; };
        var surface = new PopupSurface(window,
            () => _main is null ? default : new HWND(WinRT.Interop.WindowNative.GetWindowHandle(_main)),
            () => ReferenceEquals(_popup, window) && host.Gate.CanInsert);
        surface.AttachClosed();
        return surface;
    }
    internal override ICandidateTray CreateTray(Action open, Action preview, Action settings, Action exit)
    {
        // Denied branch is passed directly: there is no native target enumerator/capture adapter in this head.
        var tray = new TrayIcon(_message!, open, preview, settings, () => { }, exit, false, () => null)
            { State = TrayState.HotkeyUnavailable };
        tray.Add();
        return new TraySurface(tray);
    }
    // Automated QA uses existing --page/preview options and must exit without
    // modal focus changes. Preserve actionable error UI for interactive launches.
    internal override void ReportStartupFailure() => CandidateUiPolicy.ReportStartupFailure(
        Program.StartInBackground, Program.InitialOptions.HasRequest,
        () => Windows.Win32.PInvoke.MessageBox(default,
            "The Uno candidate could not start. No production profile or registration was changed.",
            "Smart Snippets — Uno candidate", Windows.Win32.UI.WindowsAndMessaging.MESSAGEBOX_STYLE.MB_OK),
        () => Console.Error.WriteLine("candidate_startup_failed"));
    internal override void Exit() { Program.StopActivations(); Application.Current.Exit(); }
    private sealed class MainSurface(MainWindow window) : ICandidateMainWindow
    {
        public nint NativeHandle => WinRT.Interop.WindowNative.GetWindowHandle(window);
        public event EventHandler? HiddenByClose { add => window.HiddenByClose += value; remove => window.HiddenByClose -= value; }
        public Task NavigateAsync(PageKind page, bool show) => window.ShowPageWithoutActivationAsync(page, show);
        public Task<bool> CanExitAsync() => window.CanExitAsync();
        public void SetInteractionEnabled(bool enabled) => window.SetInteractionEnabled(enabled);
        public void Dispose() => window.CloseForExit();
    }
    private sealed class PopupSurface(PopupWindow window, Func<HWND> owner, Func<bool> canShowCaptured) : ICandidatePopupWindow
    {
        public event EventHandler? Closed;
        public event EventHandler? Hidden;
        public event EventHandler<bool>? PresentationChanged;
        private bool _disposed;
        public void Prewarm() => window.Prewarm();
        public void ShowPreview(string? query, bool openSortMenu) => window.ShowPreview(owner(), query, false, openSortMenu);
        public nint NativeHandle => WinRT.Interop.WindowNative.GetWindowHandle(window);
        public bool SupportsCapturedPopup => true;
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
    private sealed class TraySurface(TrayIcon tray) : ICandidateTray
    {
        public void Notify(string title, string message) => tray.ShowBalloon(title, message);
        public void Dispose() => tray.Dispose();
    }
}
