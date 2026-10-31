using SmartSnippets.Core.Data;
using SmartSnippets.Core.Logging;
using SmartSnippets.Core.Settings;
using SmartSnippets.Core.Store;
using SmartSnippets.Presentation.Navigation;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Presentation.ViewModels;
using SmartSnippets.Uno.Platforms.Windows;

namespace SmartSnippets.Uno;

/// <summary>Single UI-thread composition root, used unchanged by the native head and headless flow tests.</summary>
internal sealed class UnoAppHost(CandidateHostBoundary boundary, Func<CandidateProfile> openProfile) : IDisposable
{
    private readonly HashSet<Task> _operations = [];
    private CandidateProfile? _profile;
    private IDisposable? _message;
    private ICandidateMainWindow? _main;
    private ICandidatePopupWindow? _popup;
    private ICandidateTray? _tray;
    private DeferredPopupHost? _popupHost;
    private IThemeService? _theme;
    private bool _exiting, _disposed, _started, _ownsLog;
    internal ISnippetStore Store { get; private set; } = null!;
    internal AppSettings Settings { get; private set; } = null!;
    internal ISettingsStore SettingsStore { get; private set; } = null!;
    internal ITextSizeService TextSize { get; private set; } = null!;
    internal IDialogService Dialogs { get; private set; } = null!;
    internal SnippetsPageViewModel Snippets { get; private set; } = null!;
    internal FoldersPageViewModel Folders { get; private set; } = null!;
    internal SettingsViewModel SettingsPage { get; private set; } = null!;
    internal PopupViewModel Popup { get; private set; } = null!;
    internal CandidateNativeGate Gate { get; } = new();

    internal async Task StartAsync(bool background, LaunchOptions options)
    {
        if (_started || _disposed) return;
        _started = true;
        try
        {
            _profile = openProfile();
            AppLog.Initialize(_profile.Paths.LogDirectory);
            _ownsLog = true;
            var factory = new SqliteConnectionFactory(_profile.Paths.DatabasePath);
            new Migrator(factory).MigrateToLatest();
            var store = new SnippetStore(factory, TimeProvider.System);
            store.Load(); Store = store;
            SettingsStore = new SettingsRepository(factory);
            Settings = AppSettings.Load(SettingsStore);
            TextSize = new TextSizeService();
            TextSize.Apply(Settings.TextSizePreset, Settings.CustomTextSize);
            _theme = boundary.CreateTheme(Settings);
            _message = boundary.CreateMessageWindow();
            Dialogs = boundary.CreateDialogs(false);
            var elevation = new CandidateElevation();
            var sort = new SortPreference(SettingsStore, Settings.PopupSort);
            _popupHost = new DeferredPopupHost(new Popup.RecoverablePopupHost(EnsurePopup, () => _popup?.Hide()));
            Popup = new PopupViewModel(Store, SettingsStore, Settings, Gate, Gate, elevation, _popupHost,
                boundary.CreateDialogs(true), new HostNotifier(this), () => boundary.Dispatcher.Post(() => _ = ExitAsync()),
                TextSize, action => boundary.Dispatcher.Post(() => { if (!_disposed) action(); }), sort);
            Snippets = new SnippetsPageViewModel(Store, Dialogs, Gate, files: boundary.CreateFiles(), textSize: TextSize,
                insertRow: id => Popup.InsertFromMainAsync(id), canInsertRow: () => false, sortPreference: sort);
            Folders = new FoldersPageViewModel(Store, Dialogs);
            SettingsPage = new SettingsViewModel(Settings, SettingsStore, _theme, TextSize,
                new CandidateStartupRegistration(), new CandidateHotkeyManager(Settings.Hotkey), elevation,
                () => boundary.Dispatcher.Post(() => _ = ExitAsync()), () => ShowPreview());
            RefreshClipboardAvailability();
            _main = boundary.CreateMain(this);
            _main.HiddenByClose += OnHiddenByClose;
            _tray = boundary.CreateTray(() => PostNavigation(PageKind.Snippets),
                () => boundary.Dispatcher.Post(() => ShowPreview()), () => PostNavigation(PageKind.Settings),
                () => boundary.Dispatcher.Post(() => _ = ExitAsync()));
            EnsurePopup().Prewarm();
            await ApplyLaunchOptionsAsync(options, !background);
            AppLog.Info("candidate_started");
        }
        catch (Exception ex)
        {
            if (_ownsLog) AppLog.Error("candidate_startup_failed", ex);
            Dispose();
            boundary.ReportStartupFailure();
            boundary.Exit();
        }
    }

    internal void RefreshClipboardAvailability()
    {
        // No runtime authorization exists; keep every active editor/footer command truthful.
        Snippets.RefreshClipboardAvailability();
        Popup.RefreshClipboardAvailability();
    }

    private void PostNavigation(PageKind page) => boundary.Dispatcher.Post(() =>
    {
        if (!_exiting && !_disposed && _main is { } main) _ = main.NavigateAsync(page, true);
    });
    private void OnHiddenByClose(object? sender, EventArgs e)
    {
        if (_disposed || SettingsStore.Get(SettingsKeys.ShellCloseHintShown) == "1") return;
        _tray?.Notify("Smart Snippets — Uno candidate", "Still running in the candidate tray. Clipboard, shortcuts and startup registration are unavailable.");
        SettingsStore.Set(SettingsKeys.ShellCloseHintShown, "1");
    }
    internal async Task ApplyLaunchOptionsAsync(LaunchOptions options, bool showMain = true)
    {
        if (_exiting || _disposed || _main is null) return;
        if (options.Page is { } page) await _main.NavigateAsync(page, showMain);
        else if (!options.PreviewPopup) await _main.NavigateAsync(PageKind.Snippets, showMain);
        if (options.PreviewPopup && !_exiting && !_disposed) ShowPreview(options.PreviewQuery, options.PreviewPopupSortMenu);
    }
    internal void ShowPreview(string? query = null, bool sortMenu = false)
    {
        if (_exiting || _disposed) return;
        // No HWND enumeration/capture/title inspection exists on the closed candidate route.
        EnsurePopup().ShowPreview(query, sortMenu);
    }
    private ICandidatePopupWindow EnsurePopup()
    {
        if (_disposed || _exiting) throw new InvalidOperationException("Candidate is shutting down.");
        if (_popup is not null) return _popup;
        var popup = boundary.CreatePopup(this);
        _popup = popup;
        popup.Closed += (_, _) => { if (ReferenceEquals(_popup, popup)) _popup = null; };
        return popup;
    }
    internal async Task ExitAsync()
    {
        if (_exiting || _disposed) return;
        _exiting = true;
        _main?.SetInteractionEnabled(false);
        _popupHost?.Suspend();
        _popup?.Hide();
        try
        {
            await DrainManagementAsync();
            if ((_main is not null && !await _main.CanExitAsync()) || !await CanExitPopupAsync())
            {
                _exiting = false; _main?.SetInteractionEnabled(true); _popupHost?.Resume(); return;
            }
            await DrainManagementAsync();
            Gate.CloseAdmission();
            // Closed composition owns no clipboard swap or restore continuation to drain.
            _popupHost?.Commit();
            Dispose();
            boundary.Exit();
        }
        catch (Exception ex)
        {
            _exiting = false; _main?.SetInteractionEnabled(true); _popupHost?.Resume(); AppLog.Error("candidate_exit_failed", ex);
        }
    }
    private async Task<bool> CanExitPopupAsync()
    {
        if (Popup?.Editor is not { IsDirty: true, IsDeleted: false } editor) return true;
        var choice = await Dialogs.AskSaveDiscardCancelAsync("Unsaved popup changes", "Save the popup editor before exiting the Uno candidate?");
        if (choice == SaveChoice.Save && await editor.SaveAsync()) return true;
        if (choice == SaveChoice.Discard) { editor.ResetToSaved(); return true; }
        // Preserve and recover the existing editor through the shared deferred recovery gate.
        _popupHost?.ShowAgain();
        return false;
    }
    internal async Task RunOperationAsync(Func<Task> operation)
    {
        if (_exiting || _disposed) return;
        Task? task = null;
        try
        {
            task = operation(); _operations.Add(task);
            await task;
        }
        catch (Exception ex) { AppLog.Error("candidate_operation_failed", ex); }
        finally { if (task is not null) _operations.Remove(task); }
    }
    private Task[] ManagementTasks() => new Task?[] { Snippets.PendingSelection, Snippets.PendingDelete,
        Snippets.NewCommand.ExecutionTask, Snippets.DeleteSelectedCommand.ExecutionTask,
        Snippets.Porting.ImportCommand.ExecutionTask, Snippets.Porting.ExportCommand.ExecutionTask,
        Folders.CreateCommand.ExecutionTask, Folders.DeleteCommand.ExecutionTask,
        Snippets.Editor?.SaveCommand.ExecutionTask, Snippets.Editor?.RetrySaveCommand.ExecutionTask,
        Snippets.Editor?.CreateFolderCommand.ExecutionTask, Popup.Editor?.SaveCommand.ExecutionTask,
        Popup.Editor?.CreateFolderCommand.ExecutionTask }.Where(task => task is not null).Cast<Task>().Concat(_operations).ToArray();
    private async Task DrainManagementAsync()
    {
        if (Snippets is null) return;
        while (true)
        {
            var tasks = ManagementTasks().Where(task => !task.IsCompleted).Append(WaitForPortingIdleAsync());
            try { await Task.WhenAll(tasks); }
            catch (Exception ex) { AppLog.Error("candidate_operation_failed", ex); }
            if (!Snippets.Porting.IsBusy && ManagementTasks().All(task => task.IsCompleted)) return;
        }
    }
    private Task WaitForPortingIdleAsync()
    {
        if (!Snippets.Porting.IsBusy) return Task.CompletedTask;
        var idle = new TaskCompletionSource();
        void Changed(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
        {
            if (args.PropertyName != nameof(SnippetPortingViewModel.IsBusy) || Snippets.Porting.IsBusy) return;
            Snippets.Porting.PropertyChanged -= Changed; idle.TrySetResult();
        }
        Snippets.Porting.PropertyChanged += Changed;
        return idle.Task;
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true; _exiting = true;
        Gate.CloseAdmission(); _popupHost?.Commit();
        if (_main is not null) _main.HiddenByClose -= OnHiddenByClose;
        Release(() => _tray?.Dispose()); Release(() => Popup?.Dispose());
        Release(() => _popup?.Dispose()); _popup = null;
        Release(() => Snippets?.Dispose()); Release(() => Folders?.Dispose()); Release(() => SettingsPage?.Dispose());
        Release(() => _main?.Dispose()); Release(() => (_theme as IDisposable)?.Dispose()); Release(() => _message?.Dispose());
        if (_ownsLog) { AppLog.Shutdown(); _ownsLog = false; }
        _profile?.Dispose();
    }
    private void Release(Action release)
    {
        try { release(); }
        catch (Exception ex) { if (_ownsLog) AppLog.Error("candidate_cleanup_failed", ex); }
    }
    private sealed class HostNotifier(UnoAppHost host) : INotifier
    {
        public void Notify(string title, string message) => host._tray?.Notify(title, message);
    }
}
