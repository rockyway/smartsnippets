using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppLifecycle;
using SmartSnippets.App.Diagnostics;
using SmartSnippets.App.Popup;
using SmartSnippets.App.Services;
using SmartSnippets.Core;
using SmartSnippets.Core.Data;
using SmartSnippets.Core.Logging;

namespace SmartSnippets.App;

public partial class App : Application
{
    private DispatcherQueue? _dispatcher;
    private MessageWindow? _messageWindow;
    private HotkeyService? _hotkey;
    private PopupWindow? _popup;
    private TrayIcon? _tray;
    private DiagnosticsWindow? _diagnostics;
    private string _hotkeyStatus = "Hotkey not registered yet.";
    private string? _migrationStatus;

    public App()
    {
        InitializeComponent();
        DispatcherShutdownMode = DispatcherShutdownMode.OnExplicitShutdown;
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _dispatcher = DispatcherQueue.GetForCurrentThread();
        var paths = new AppPaths();

        try
        {
            Directory.CreateDirectory(paths.Root);
            var applied = new Migrator(new SqliteConnectionFactory(paths.DatabasePath)).MigrateToLatest();
            _migrationStatus = $"Migrations applied: {applied}.";
        }
        catch (Exception ex)
        {
            _migrationStatus = $"Migration failed: {ex.Message}";
            AppLog.Error("migrations_failed", ex);
        }

        try
        {
            _messageWindow = new MessageWindow();
            _hotkey = new HotkeyService(_messageWindow);
            _hotkey.HotkeyPressed += OnHotkeyPressed;

            // TODO(T4): AppHost loads the saved gesture from AppSettings.
            var status = new HotkeyManager(_hotkey).Initialize(SmartSnippets.Core.Settings.HotkeyGesture.Default);
            _hotkeyStatus = status.Message;

            _popup = new PopupWindow(new ClipboardPasteInserter(_messageWindow.Handle));
            _popup.Prewarm();

            _tray = new TrayIcon(
                _messageWindow,
                () => Enqueue(ShowDiagnostics),
                () => Enqueue(ShowDiagnostics), // TODO(T4): Settings page
                () => { }, // TODO(T4): restart as administrator (menu item hidden below)
                () => Enqueue(ShowDiagnostics),
                () => Enqueue(ExitApp),
                showRestartAdmin: false);
            _tray.Add();
        }
        catch (Exception ex)
        {
            _hotkeyStatus = $"Startup failed: {ex.Message}";
            AppLog.Error("startup_failed", ex);
        }

        AppInstance.GetCurrent().Activated += (_, _) => Enqueue(ShowDiagnostics);

        if (!Program.StartInBackground)
        {
            ShowDiagnostics();
        }
    }

    private void Enqueue(Action action) => _dispatcher?.TryEnqueue(() => action());

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

    private void ShowDiagnostics()
    {
        if (_diagnostics is null)
        {
            var window = new DiagnosticsWindow();
            window.Closed += (_, _) => _diagnostics = null;
            window.ProbePanel.Children.Add(new TextBlock { Text = _hotkeyStatus, TextWrapping = TextWrapping.Wrap });
            if (_migrationStatus is not null)
            {
                window.ProbePanel.Children.Add(new TextBlock { Text = _migrationStatus, TextWrapping = TextWrapping.Wrap });
            }

            _diagnostics = window;
        }

        _diagnostics.AppWindow.Show();
        _diagnostics.Activate();
    }

    private void ExitApp()
    {
        _hotkey?.UnregisterAll();
        AppLog.Info("exit_unregistered");
        _tray?.Dispose();
        _popup?.Close();
        _diagnostics?.Close();
        _hotkey?.Dispose();
        _messageWindow?.Dispose();
        AppLog.Shutdown();
        Exit();
    }
}
