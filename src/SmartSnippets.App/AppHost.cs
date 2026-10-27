using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppLifecycle;
using SmartSnippets.App.Diagnostics;
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
    private HotkeyService? _hotkeyService;
    private HotkeyManager? _hotkeyManager;
    private PopupWindow? _popup;
    private MainWindow? _mainWindow;
    private TrayIcon? _tray;
    private DiagnosticsWindow? _diagnostics;
    private bool _exiting;

    /// <summary>Created once at start-up; plan 005 view-models take it through their constructors.</summary>
    internal ISnippetStore Store { get; private set; } = null!;

    /// <summary>Settings as loaded at start-up; the Settings view-model owns later changes.</summary>
    internal AppSettings Settings { get; private set; } = null!;

    internal ITextSizeService TextSize { get; private set; } = null!;

    internal IDialogService Dialogs { get; private set; } = null!;

    internal INotifier Notices { get; private set; } = null!;

    internal IUiDispatcher Dispatcher => _ui!;

    public void Start(bool startInBackground)
    {
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
        var hotkeyService = new HotkeyService(messageWindow);
        _hotkeyService = hotkeyService;
        hotkeyService.HotkeyPressed += OnHotkeyPressed;
        var hotkeyManager = new HotkeyManager(hotkeyService);
        _hotkeyManager = hotkeyManager;
        var hotkeyStatus = hotkeyManager.Initialize(settings.Hotkey);

        var elevation = new ElevationService(() => _mainWindow?.IsHidden ?? true);
        var startup = new StartupRegistration();
        Dialogs = new DialogService(() => _mainWindow?.ContentXamlRoot, () => _mainWindow?.BringToFront());

        // Step 7: popup, then the main window (created hidden).
        var popup = new PopupWindow(new ClipboardPasteInserter(messageWindow.Handle));
        _popup = popup;
        popup.Prewarm();
        theme.Register(popup);

        UIElement CreatePage(PageKind kind) => kind switch
        {
            // Plan 005 replaces these two placeholders with `new SnippetsPage { ViewModel = new ...(Store, TextSize, Dialogs, ...) }`.
            PageKind.Snippets => new SnippetsPage(),
            PageKind.Folders => new FoldersPage(),
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
                    requestExit: () => _ui!.Post(ShutDown)),
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
            onSettings: () => _ui!.Post(() => _mainWindow?.ShowPage(PageKind.Settings)),
            onRestartAdmin: () => _ui!.Post(() => RestartAsAdministrator(elevation)),
            onDiagnostics: () => _ui!.Post(ShowDiagnostics),
            onExit: () => _ui!.Post(() => _ = ExitAsync()),
            showRestartAdmin: !elevation.IsElevated);
        _tray = tray;
        tray.Add();
        Notices = new Notifier(tray);

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
        var target = ForegroundTarget.Capture();
        if (target is null)
        {
            AppLog.Info("hotkey_ignored_own_or_no_window");
            return;
        }

        _popup?.ShowFor(target.Value, timestamp);
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
            ShutDown();
            return;
        }

        Notices.Notify("Restart as administrator", result.Message);
    }

    private void ShowDiagnostics()
    {
        if (_diagnostics is null)
        {
            var window = new DiagnosticsWindow();
            window.Closed += (_, _) => _diagnostics = null;
            window.ProbePanel.Children.Add(new TextBlock
            {
                Text = _hotkeyManager?.Status.Message ?? string.Empty,
                TextWrapping = TextWrapping.Wrap,
            });
            _theme?.Register(window);
            _diagnostics = window;
        }

        _diagnostics.AppWindow.Show();
        _diagnostics.Activate();
    }

    /// <summary>Tray Exit: asks the current page whether leaving is fine (unsaved changes), then shuts down.</summary>
    private async Task ExitAsync()
    {
        try
        {
            if (_exiting)
            {
                return;
            }

            if (_mainWindow is { } window && !await window.CanExitAsync())
            {
                return;
            }

            ShutDown();
        }
        catch (Exception ex)
        {
            AppLog.Error("exit_failed", ex);
        }
    }

    private void ShutDown()
    {
        if (_exiting)
        {
            return;
        }

        _exiting = true;
        ReleaseResources();
        AppLog.Shutdown();
        Application.Current.Exit();
    }

    private void ReleaseResources()
    {
        try
        {
            _hotkeyManager?.UnregisterAll();
            AppLog.Info("exit_unregistered");
            _tray?.Dispose();
            _popup?.Close();
            _diagnostics?.Close();
            _mainWindow?.CloseForExit();
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
        ShutDown();
    }
}
