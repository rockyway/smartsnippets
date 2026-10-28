using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppLifecycle;
using SmartSnippets.App.Pages;
using SmartSnippets.App.Popup;
using SmartSnippets.App.Services;
using SmartSnippets.Core;
using SmartSnippets.Core.Data;
using SmartSnippets.Core.Logging;
using SmartSnippets.Core.Settings;
using SmartSnippets.Core.Store;
using SmartSnippets.Presentation.Navigation;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Presentation.ViewModels;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace SmartSnippets.App;

/// <summary>
/// The single composition root: builds every service and window in start-up order (plan 004 section 4.4),
/// wires tray, hotkey and lifecycle, and owns the exit sequence. App.xaml.cs only delegates here.
/// </summary>
internal sealed class AppHost
{
    private const string CloseHintTitle = "Smart Snippets";
    private const string CloseHintText = "Smart Snippets is still running. Use the tray icon to open it or exit.";

    private DispatcherQueue? _queue;
    private UiDispatcher? _ui;
    private ISettingsStore? _settingsStore;
    private ThemeService? _theme;
    private MessageWindow? _messageWindow;
    private ClipboardPasteDispatcher? _pasteDispatcher;
    private InsertionShutdownCoordinator? _insertionCoordinator;
    private HotkeyService? _hotkeyService;
    private HotkeyManager? _hotkeyManager;
    private PopupWindow? _popup;
    private PopupViewModel? _popupViewModel;
    private DeferredPopupHost? _popupHost;
    private bool _popupVisible;
    private MainWindow? _mainWindow;
    private TrayIcon? _tray;
    private SnippetsPageViewModel? _snippetsViewModel;
    private FoldersPageViewModel? _foldersViewModel;
    private bool _exiting;

    /// <summary>Created once at start-up; plan 005 view-models take it through their constructors.</summary>
    internal ISnippetStore Store { get; private set; } = null!;

    /// <summary>Settings as loaded at start-up; the Settings view-model owns later changes.</summary>
    internal AppSettings Settings { get; private set; } = null!;

    internal ITextSizeService TextSize { get; private set; } = null!;

    internal IDialogService Dialogs { get; private set; } = null!;

    internal INotifier Notices { get; private set; } = null!;

    internal IUiDispatcher Dispatcher => _ui!;

    internal IInsertionService Insertion { get; private set; } = null!;

    public void Start(bool startInBackground)
    {
        var executable = Environment.ProcessPath;
        var version = executable is null ? null : System.Diagnostics.FileVersionInfo.GetVersionInfo(executable).ProductVersion;
        AppLog.Info("app_started", $"version={version ?? "unknown"}");
        _queue = DispatcherQueue.GetForCurrentThread();
        _ui = new UiDispatcher(_queue);

        try
        {
            if (!TryLoadData())
            {
                return;
            }

            Compose(startInBackground);
        }
        catch (Exception ex)
        {
            AppLog.Error("startup_failed", ex);
            FailStartup("Smart Snippets could not start. Details were written to the log in " + new AppPaths().LogDirectory);
        }
    }

    // Steps 1-3: migrations, store, settings. Failure here means there is nothing to show, so the app exits.
    private bool TryLoadData()
    {
        var paths = new AppPaths();
        try
        {
            Directory.CreateDirectory(paths.Root);
            var factory = new SqliteConnectionFactory(paths.DatabasePath);
            new Migrator(factory).MigrateToLatest();

            var store = new SnippetStore(factory, TimeProvider.System);
            store.Load();
            Store = store;

            var repository = new SettingsRepository(factory);
            _settingsStore = repository;
            Settings = AppSettings.Load(repository);
            AppLog.Info("settings_loaded");
            return true;
        }
        catch (MigrationException ex)
        {
            AppLog.Error("migrations_failed", ex);
            FailStartup(ex.Message);
        }
        catch (StoreException ex)
        {
            AppLog.Error("store_load_failed", ex);
            FailStartup("Smart Snippets could not read its snippet database. " + ex.Message);
        }
        catch (Exception ex)
        {
            AppLog.Error("data_load_failed", ex);
            FailStartup("Smart Snippets could not open its snippet database. Details were written to the log in " + paths.LogDirectory);
        }

        return false;
    }

    private void Compose(bool startInBackground)
    {
        var settings = Settings;
        var settingsStore = _settingsStore!;

        // Steps 4-5: text size and theme.
        var textSize = new TextSizeService();
        textSize.Apply(settings.TextSizePreset, settings.CustomTextSize);
        TextSize = textSize;
        var theme = new ThemeService(settings.Theme);
        _theme = theme;

        // Step 6: Win32 services and the saved hotkey.
        var messageWindow = new MessageWindow();
        _messageWindow = messageWindow;
        var pasteDispatcher = new ClipboardPasteDispatcher(messageWindow.Handle);
        _pasteDispatcher = pasteDispatcher;
        _insertionCoordinator = new InsertionShutdownCoordinator(new InsertionService(Store, new Win32TargetProbe(), pasteDispatcher));
        Insertion = _insertionCoordinator;
        AppLog.Info("insertion_ready");
        var hotkeyService = new HotkeyService(messageWindow);
        _hotkeyService = hotkeyService;
        hotkeyService.HotkeyPressed += OnHotkeyPressed;
        var hotkeyManager = new HotkeyManager(hotkeyService);
        _hotkeyManager = hotkeyManager;
        var hotkeyStatus = hotkeyManager.Initialize(settings.Hotkey);

        var elevation = new ElevationService(() => _mainWindow?.IsHidden ?? true);
        var startup = new StartupRegistration();
        Dialogs = new DialogService(() => _mainWindow?.ContentXamlRoot, () => _mainWindow?.BringToFront(), textSize);
        AppLog.Info("porting_ready");

        // Step 7: popup, then the main window (created hidden).
        var popup = new PopupWindow(textSize, () => _mainWindow?.ShowPage(PageKind.Snippets));
        _popup = popup;
        popup.CanShow = () => !_exiting;

        UIElement CreatePage(PageKind kind) => kind switch
        {
            // Pages are created once (MainWindow caches them); the view-models are kept so exit can dispose them.
            PageKind.Snippets => new SnippetsPage
            {
                TextSize = textSize,
                ViewModel = _snippetsViewModel = new SnippetsPageViewModel(Store, Dialogs, new ClipboardService(),
                    files: new SnippetFileService(() => _mainWindow?.AppWindow.Id)),
            },
            PageKind.Folders => new FoldersPage
            {
                TextSize = textSize,
                ViewModel = _foldersViewModel = new FoldersPageViewModel(Store, Dialogs),
            },
            _ => new SettingsPage
            {
                ViewModel = new SettingsViewModel(
                    settings,
                    settingsStore,
                    theme,
                    textSize,
                    startup,
                    hotkeyManager,
                    elevation,
                    requestExit: () => _ui!.Post(() => _ = ShutDownAsync())),
            },
        };

        var mainWindow = new MainWindow(CreatePage);
        _mainWindow = mainWindow;
        theme.Register(mainWindow);
        mainWindow.HiddenByClose += (_, _) => ShowCloseHintOnce();

        // Step 8: tray and notices. Tray callbacks run inside the message window's WndProc, so each is posted to the UI queue.
        var tray = new TrayIcon(
            messageWindow,
            onOpen: () => _ui!.Post(() => _mainWindow?.ShowPage(PageKind.Snippets)),
            onShowPopup: hwnd => _ui!.Post(() => ShowPopupFromTray(hwnd)),
            onSettings: () => _ui!.Post(() => _mainWindow?.ShowPage(PageKind.Settings)),
            onRestartAdmin: () => _ui!.Post(() => RestartAsAdministrator(elevation)),
            onExit: () => _ui!.Post(() => _ = ExitAsync()),
            showRestartAdmin: !elevation.IsElevated,
            hotkeyDisplay: () => hotkeyManager.Status.IsRegistered ? hotkeyManager.Status.Gesture.ToDisplayString() : null);
        _tray = tray;
        hotkeyManager.StatusChanged += OnHotkeyStatusChanged;
        tray.Add();
        Notices = new Notifier(tray);
        UpdateTrayState();
        var popupDialogs = new DialogService(() => popup.ContentXamlRoot, () => { });
        
        _popupHost = new DeferredPopupHost(popup);
        _popupViewModel = new PopupViewModel(Store, settingsStore, settings, Insertion, new ClipboardService(), elevation,
            _popupHost, popupDialogs, Notices, () => _ui!.Post(() => _ = ShutDownAsync()));
        popup.ViewModel = _popupViewModel;
        popup.NotifyReopenFailure = message => Notices.Notify("Smart Snippets", message);
        popup.VisibleChanged += OnPopupVisibleChanged;
        popup.Prewarm();
        theme.Register(popup);

        if (!hotkeyStatus.IsRegistered)
        {
            Notices.Notify("Shortcut not registered", hotkeyStatus.Message);
        }

        // Step 9: second launches reach the existing instance here.
        AppInstance.GetCurrent().Activated += (_, _) => _ui!.Post(() => _mainWindow?.ShowPage(PageKind.Snippets));

        if (!startInBackground)
        {
            mainWindow.ShowPage(PageKind.Snippets);
        }
    }

    private void OnHotkeyPressed(long timestamp)
    {
        if (_exiting) return;
        var target = TargetCapture.CaptureForeground();
        if (target is null)
        {
            AppLog.Info("hotkey_ignored_own_or_no_window");
            return;
        }

        _popup?.ShowFor(target, timestamp);
    }

    private void ShowPopupFromTray(HWND hwnd)
    {
        if (_exiting) return;
        var target = TargetCapture.CaptureFrom(hwnd);
        if (target is not null) _popup?.ShowFor(target, System.Diagnostics.Stopwatch.GetTimestamp());
    }

    private void OnHotkeyStatusChanged(object? sender, EventArgs e) => _ui?.Post(UpdateTrayState);

    private void OnPopupVisibleChanged(object? sender, bool visible)
    {
        _popupVisible = visible;
        UpdateTrayState();
    }

    private void UpdateTrayState()
    {
        if (_tray is not null && _hotkeyManager is not null)
            _tray.State = TrayStatePolicy.Compute(_hotkeyManager.Status.IsRegistered, _popupVisible);
    }

    private void ShowCloseHintOnce()
    {
        try
        {
            var store = _settingsStore;
            if (store is null || store.Get(SettingsKeys.ShellCloseHintShown) == "1")
            {
                return;
            }

            Notices.Notify(CloseHintTitle, CloseHintText);
            store.Set(SettingsKeys.ShellCloseHintShown, "1");
        }
        catch (Exception ex)
        {
            AppLog.Error("close_hint_failed", ex);
        }
    }

    private void RestartAsAdministrator(ElevationService elevation)
    {
        var result = elevation.RestartAsAdministrator();
        if (result.Outcome == ElevationOutcome.Started)
        {
            // The elevated copy waits for this process to exit, so do not stop for an unsaved-changes prompt.
            _ = ShutDownAsync();
            return;
        }

        Notices.Notify("Restart as administrator", result.Message);
    }

    /// <summary>Tray Exit: asks the current page whether leaving is fine (unsaved changes), then shuts down.</summary>
    private async Task ExitAsync()
    {
        if (!BeginShutdown()) return;
        try
        {
            if (_mainWindow is { } window && !await window.CanExitAsync())
            {
                CancelShutdown();
                return;
            }

            await FinishShutdownAsync();
        }
        catch (Exception ex)
        {
            CancelShutdown();
            AppLog.Error("exit_failed", ex);
        }
    }

    private bool BeginShutdown()
    {
        if (_exiting || (_insertionCoordinator is { } coordinator && !coordinator.TryBeginShutdown())) return false;
        _exiting = true;
        _popupHost?.Suspend();
        try { _popup?.Hide(); }
        catch { CancelShutdown(); throw; }
        return true;
    }

    private void CancelShutdown()
    {
        _insertionCoordinator?.ResumeAdmission();
        _exiting = false;
        _popupHost?.Resume();
    }

    private async Task ShutDownAsync()
    {
        if (!BeginShutdown()) return;
        await FinishShutdownAsync();
    }

    private async Task FinishShutdownAsync()
    {
        try
        {
            if (_insertionCoordinator is { } coordinator)
                await coordinator.WaitForIdleAsync();
            if (_pasteDispatcher is { } dispatcher)
            {
                var status = await dispatcher.RestorePendingAsync();
                if (status == RestoreStatus.Busy)
                {
                    CancelShutdown();
                    Notices?.Notify("Clipboard in use", "Smart Snippets could not restore your clipboard. Close the app using it and try Exit again.");
                    return;
                }
                if (status is RestoreStatus.Failed or RestoreStatus.Partial)
                    Notices?.Notify("Clipboard restore failed", "Your previous clipboard couldn't be fully restored.");
            }
            _popupHost?.Commit();
            ReleaseResources();
            AppLog.Shutdown();
            Application.Current.Exit();
        }
        catch (Exception ex)
        {
            CancelShutdown();
            AppLog.Error("exit_failed", ex);
        }
    }

    private void ReleaseResources()
    {
        try
        {
            if (_hotkeyManager is not null) _hotkeyManager.StatusChanged -= OnHotkeyStatusChanged;
            if (_popup is not null) _popup.VisibleChanged -= OnPopupVisibleChanged;
            _hotkeyManager?.UnregisterAll();
            AppLog.Info("exit_unregistered");
            _tray?.Dispose();
            _popupViewModel?.Dispose();
            _popup?.Close();
            _mainWindow?.CloseForExit();
            _snippetsViewModel?.Dispose();
            _foldersViewModel?.Dispose();
            _hotkeyService?.Dispose();
            _messageWindow?.Dispose();
        }
        catch (Exception ex)
        {
            AppLog.Error("exit_cleanup_failed", ex);
        }
    }

    private void FailStartup(string message)
    {
        PInvoke.MessageBox(
            HWND.Null,
            message,
            "Smart Snippets",
            MESSAGEBOX_STYLE.MB_OK | MESSAGEBOX_STYLE.MB_ICONERROR | MESSAGEBOX_STYLE.MB_TOPMOST | MESSAGEBOX_STYLE.MB_SETFOREGROUND);
        _ = ShutDownAsync();
    }
}
