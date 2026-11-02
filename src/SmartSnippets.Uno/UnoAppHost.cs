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
    private ICandidateDesktopServices? _desktop;
    private ICandidateMainWindow? _main;
    private ICandidatePopupWindow? _popup;
    private ICandidateTray? _tray;
    private DeferredPopupHost? _popupHost;
    private IThemeService? _theme;
    private bool _exiting, _disposed, _started, _ownsLog, _disposing, _disposeRequested;
    private bool _previewActive, _previewHidden, _pendingCaptured, _popupVisible;
    internal bool CanPresentPopup => !_exiting && !ExitSuperseded;
    private bool ExitSuperseded => _disposed || _disposeRequested;
    internal ISnippetStore Store { get; private set; } = null!;
    internal AppSettings Settings { get; private set; } = null!;
    internal ISettingsStore SettingsStore { get; private set; } = null!;
    internal ITextSizeService TextSize { get; private set; } = null!;
    internal IDialogService Dialogs { get; private set; } = null!;
    internal SnippetsPageViewModel Snippets { get; private set; } = null!;
    internal FoldersPageViewModel Folders { get; private set; } = null!;
    internal SettingsViewModel SettingsPage { get; private set; } = null!;
    internal PopupViewModel Popup { get; private set; } = null!;
    private CandidateNativeGate? _gate;
    private CandidateNativeCapability? _nativeCapability;
    internal CandidateNativeGate Gate => _gate ??= new(_nativeCapability = boundary.NativeCapability, () => _previewActive);

    internal async Task StartAsync(bool background, LaunchOptions options)
    {
        if (_started || ExitSuperseded) return;
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
            _desktop = boundary.CreateDesktopServices(this, Settings, () =>
            {
                // WM_HOTKEY is on the UI thread. Capture now, never after posting show/focus.
                if (CanPresentPopup) ShowCapturedPopup();
            });
            if (_desktop is not null) Gate.ComposeDesktop(_desktop.Operations);
            Gate.Compose(Store);
            Dialogs = boundary.CreateDialogs(false, TextSize);
            var elevation = _desktop?.Elevation ?? new CandidateElevation();
            var sort = new SortPreference(SettingsStore, Settings.PopupSort);
            _popupHost = new DeferredPopupHost(new Popup.RecoverablePopupHost(EnsurePopup, () => _popup?.Hide()));
            Popup = new PopupViewModel(Store, SettingsStore, Settings, Gate, Gate, elevation, _popupHost,
                boundary.CreateDialogs(true, TextSize), new HostNotifier(this), () => boundary.Dispatcher.Post(() => _ = ExitAsync()),
                TextSize, action => boundary.Dispatcher.Post(() => { if (!_disposed) action(); }), sort);
            Snippets = new SnippetsPageViewModel(Store, Dialogs, Gate, files: boundary.CreateFiles(), textSize: TextSize,
                insertRow: id => RunOperationAsync(() => Popup.InsertFromMainAsync(id)),
                canInsertRow: () => !_exiting && Gate.CanInsert && Popup.CanInsertFromMain, sortPreference: sort);
            Folders = new FoldersPageViewModel(Store, Dialogs);
            SettingsPage = new SettingsViewModel(Settings, SettingsStore, _theme, TextSize,
                _desktop?.Startup ?? new CandidateStartupRegistration(), _desktop?.Hotkeys ?? new CandidateHotkeyManager(Settings.Hotkey), elevation,
                () => boundary.Dispatcher.Post(() => _ = ExitAsync()), () => ShowPreview());
            if (_nativeCapability is { } capability) capability.AuthorizationChanged += OnAuthorizationChanged;
            Gate.AvailabilityChanged += RefreshClipboardAvailability;
            Popup.PropertyChanged += OnPopupPropertyChanged;
            RefreshClipboardAvailability();
            _main = boundary.CreateMain(this);
            _main.HiddenByClose += OnHiddenByClose;
            if (_desktop is not null) _desktop.Hotkeys.StatusChanged += OnHotkeyStatusChanged;
            _tray = boundary.CreateTray(() => PostNavigation(PageKind.Snippets),
                window => { if (_desktop is null) boundary.Dispatcher.Post(ShowPopupFromNativeRequest); else if (window != 0) ShowCapturedPopup(window); }, () => PostNavigation(PageKind.Settings),
                () => boundary.Dispatcher.Post(() => _ = ExitAsync()));
            RefreshHotkeyUi();
            EnsurePopup();
            if (_desktop is not null && !_desktop.Hotkeys.Status.IsRegistered)
                _tray.Notify("Shortcut not registered", _desktop.Hotkeys.Status.Message);
            await ApplyLaunchOptionsAsync(options, !background);
            if (ExitSuperseded) return;
            AppLog.Info("candidate_started");
        }
        catch (Exception ex)
        {
            if (ExitSuperseded) return;
            if (_ownsLog) AppLog.Error("candidate_startup_failed", ex);
            Dispose();
            boundary.ReportStartupFailure();
            boundary.Exit();
        }
    }

    private void OnHotkeyStatusChanged(object? sender, EventArgs args) => boundary.Dispatcher.Post(RefreshHotkeyUi);
    private void RefreshHotkeyUi()
    {
        if (_disposed) return;
        var status = _desktop?.Hotkeys.Status ?? new CandidateHotkeyManager(Settings.Hotkey).Status;
        _main?.SetHotkeyStatus(status);
        _tray?.SetHotkeyState(status.IsRegistered, _popupVisible);
    }

    private void OnAuthorizationChanged() => boundary.Dispatcher.Post(() =>
    {
        if (!_disposed) RefreshClipboardAvailability();
    });
    private void OnPopupPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
    {
        if (args.PropertyName is nameof(PopupViewModel.IsInserting) or nameof(PopupViewModel.Editor))
        {
            if (_previewHidden && !Popup.IsEditorOpen) _previewActive = false;
            RefreshClipboardAvailability();
        }
    }
    internal void RefreshClipboardAvailability()
    {
        if (_disposed || Snippets is null || Popup is null) return;
        // Execution also rechecks the lease; notification never grants authorization.
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
        _tray?.Notify("Smart Snippets", "Still running in the tray. Use your global shortcut to insert snippets, or choose Exit to quit.");
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
        if (!CanPresentPopup || Popup.IsInserting || (Popup.IsEditorOpen && !Popup.IsPreview)) return;
        _previewActive = true; _previewHidden = false; _pendingCaptured = false;
        Gate.ClearCapturedTarget();
        // QA/settings preview remains target-free, including with D80 desktop services.
        EnsurePopup().ShowPreview(query, sortMenu);
        RefreshClipboardAvailability();
    }
    internal void ShowPopupFromNativeRequest()
    {
        if (_exiting || _disposed) return;
        if (!ShowCapturedPopup()) ShowPreview();
    }
    internal bool ShowCapturedPopup(nint window = 0)
    {
        if (!CanPresentPopup || !Gate.IsIdle || Popup.IsEditorOpen || _previewActive) return false;
        var surface = EnsurePopup();
        if (!surface.SupportsCapturedPopup) { Gate.ClearCapturedTarget(); return false; }
        var target = Gate.Capture(window);
        if (target is null || !Gate.CanInsert) { Gate.ClearCapturedTarget(); return false; }
        // Capture and its exact identity validation precede all popup show/focus work.
        _pendingCaptured = true;
        try { surface.ShowCaptured(target); }
        catch
        {
            _pendingCaptured = false;
            Gate.ClearCapturedTarget();
            AppLog.Info("candidate_popup_show_failed");
            return false;
        }
        RefreshClipboardAvailability();
        return true;
    }
    private void OnPopupHidden(object? sender, EventArgs args)
    {
        if (!ReferenceEquals(sender, _popup)) return;
        _popupVisible = false;
        RefreshHotkeyUi();
        _previewHidden = true;
        _previewActive = Popup.IsPreview && Popup.IsEditorOpen;
        if (_pendingCaptured) Gate.ClearCapturedTarget();
        _pendingCaptured = false;
        RefreshClipboardAvailability();
    }
    private void OnPopupPresentationChanged(object? sender, bool shown)
    {
        if (!ReferenceEquals(sender, _popup)) return;
        _popupVisible = shown;
        RefreshHotkeyUi();
        if (!shown)
        {
            if (_pendingCaptured) Gate.ClearCapturedTarget();
            _previewHidden = true;
            _previewActive = Popup.IsPreview && Popup.IsEditorOpen;
        }
        else
        {
            _previewHidden = false;
            _previewActive = Popup.IsPreview;
        }
        _pendingCaptured = false;
        RefreshClipboardAvailability();
    }
    private ICandidatePopupWindow EnsurePopup()
    {
        if (_disposed || _exiting) throw new InvalidOperationException("Candidate is shutting down.");
        if (_popup is not null) return _popup;
        var popup = boundary.CreatePopup(this);
        _popup = popup;
        popup.Hidden += OnPopupHidden;
        popup.PresentationChanged += OnPopupPresentationChanged;
        popup.Closed += (_, _) =>
        {
            if (!ReferenceEquals(_popup, popup)) return;
            OnPopupHidden(popup, EventArgs.Empty);
            popup.Hidden -= OnPopupHidden;
            popup.PresentationChanged -= OnPopupPresentationChanged;
            _popup = null;
        };
        popup.Prewarm();
        return popup;
    }
    internal async Task ExitAsync()
    {
        if (_exiting || ExitSuperseded) return;
        _exiting = true;
        Gate.SuspendAdmission();
        if (ExitSuperseded) return;
        AppLog.Info("candidate_shutdown_started");
        _popupHost?.Suspend();
        try
        {
            _main?.SetInteractionEnabled(false);
            _popup?.Hide();
            var recovered = await Gate.DrainAsync();
            if (ExitSuperseded) return;
            if (!recovered)
            {
                CancelExit();
                AppLog.Info("candidate_shutdown_blocked", "reason=recovery");
                _tray?.Notify("Clipboard recovery pending", "Owned clipboard recovery must settle before the app can exit.");
                return;
            }
            await DrainManagementAsync();
            if (ExitSuperseded) return;
            var canExitMain = _main is null || await _main.CanExitAsync();
            if (ExitSuperseded) return;
            if (!canExitMain) { CancelExit(); return; }
            var canExitPopup = await CanExitPopupAsync();
            if (ExitSuperseded) return;
            if (!canExitPopup) { CancelExit(); return; }
            await DrainManagementAsync();
            if (ExitSuperseded) return;
            Gate.CloseAdmission();
            _popupHost?.Commit();
            ReleaseResources();
            boundary.Exit();
        }
        catch (Exception ex)
        {
            if (ExitSuperseded) return;
            CancelExit(); AppLog.Error("candidate_exit_failed", ex);
        }
    }
    private void CancelExit()
    {
        if (ExitSuperseded) return;
        _exiting = false;
        Gate.ResumeAdmission();
        if (ExitSuperseded) return;
        _main?.SetInteractionEnabled(true);
        if (ExitSuperseded) return;
        if (Popup.IsEditorOpen) _popupHost?.ShowAgain();
        _popupHost?.Resume();
        RefreshClipboardAvailability();
    }
    private async Task<bool> CanExitPopupAsync()
    {
        if (Popup?.Editor is not { IsDirty: true, IsDeleted: false } editor) return true;
        var choice = await Dialogs.AskSaveDiscardCancelAsync("Unsaved popup changes", "Save the popup editor before exiting Smart Snippets?");
        if (ExitSuperseded) return false;
        if (choice == SaveChoice.Save)
        {
            var saved = await editor.SaveAsync();
            if (ExitSuperseded) return false;
            if (saved) return true;
        }
        if (choice == SaveChoice.Discard) { editor.ResetToSaved(); return true; }
        // CancelExit recovers the existing editor exactly once through the deferred gate.
        return false;
    }
    internal Task RunCopyOperationAsync(Action copy) => RunOperationAsync(async () =>
    {
        if (!await Gate.WaitForCopyAdmissionAsync() || _exiting || _disposing || _disposed)
        {
            if (!_exiting && !_disposing && !_disposed)
                _tray?.Notify("Copy unavailable", "Copy was not performed. Wait for a quiet desktop and try again.");
            return;
        }
        copy(); // Shared VM rechecks availability/result and owns truthful feedback/logging.
    });
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
            var tasks = ManagementTasks().Where(task => !task.IsCompleted)
                .Append(WaitForPortingIdleAsync()).Append(WaitForPopupIdleAsync());
            try { await Task.WhenAll(tasks); }
            catch (Exception ex) { if (_ownsLog) AppLog.Error("candidate_operation_failed", ex); }
            if (_disposed) return;
            if (!Snippets.Porting.IsBusy && !Popup.IsInserting && ManagementTasks().All(task => task.IsCompleted)) return;
        }
    }
    private Task WaitForPopupIdleAsync()
    {
        if (!Popup.IsInserting) return Task.CompletedTask;
        var idle = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void Changed(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
        {
            if (args.PropertyName != nameof(PopupViewModel.IsInserting) || Popup.IsInserting) return;
            Popup.PropertyChanged -= Changed; idle.TrySetResult();
        }
        Popup.PropertyChanged += Changed;
        return idle.Task;
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
        if (_disposed || _disposing) return;
        _disposing = true;
        _disposeRequested = true;
        _exiting = true;
        Gate.CloseAdmission(); _popupHost?.Suspend();
        Release(() => _main?.SetInteractionEnabled(false));
        Release(() => _popup?.Hide());
        var drain = Gate.DrainAsync();
        if (drain.IsCompletedSuccessfully && drain.Result
            && (Snippets is null || (!Popup.IsInserting && !Snippets.Porting.IsBusy
                && ManagementTasks().All(task => task.IsCompleted))))
        { ReleaseResources(); _disposing = false; return; }
        _ = DisposeAfterDrainAsync(drain);
    }
    private async Task DisposeAfterDrainAsync(Task<bool> drain)
    {
        try
        {
            var recovered = await drain;
            if (_disposed) return;
            if (!recovered) { AppLog.Info("candidate_shutdown_blocked", "reason=recovery"); return; }
            await DrainManagementAsync();
            if (_disposed) return;
            ReleaseResources();
        }
        catch (Exception ex) { if (_ownsLog) AppLog.Error("candidate_cleanup_failed", ex); }
        finally { _disposing = false; }
    }
    private void ReleaseResources()
    {
        if (_disposed) return;
        _disposed = true;
        if (_ownsLog) AppLog.Info("candidate_cleanup_stage", "stage=begin");
        if (_nativeCapability is { } capability)
        {
            capability.AuthorizationChanged -= OnAuthorizationChanged;
            capability.Revoke();
        }
        if (_desktop is not null) _desktop.Hotkeys.StatusChanged -= OnHotkeyStatusChanged;
        Gate.AvailabilityChanged -= RefreshClipboardAvailability;
        if (Popup is not null) Popup.PropertyChanged -= OnPopupPropertyChanged;
        _popupHost?.Commit();
        if (_main is not null) _main.HiddenByClose -= OnHiddenByClose;
        ReleaseResource("tray", () => _tray?.Dispose()); ReleaseResource("popup_vm", () => Popup?.Dispose());
        ReleaseResource("popup_window", () => _popup?.Dispose()); _popup = null;
        ReleaseResource("snippets", () => Snippets?.Dispose()); ReleaseResource("folders", () => Folders?.Dispose());
        ReleaseResource("settings", () => SettingsPage?.Dispose()); ReleaseResource("main_window", () => _main?.Dispose());
        ReleaseResource("desktop_services", () => _desktop?.Dispose());
        ReleaseResource("theme", () => (_theme as IDisposable)?.Dispose()); ReleaseResource("message_window", () => _message?.Dispose());
        if (_ownsLog) { AppLog.Info("candidate_shutdown_complete"); AppLog.Shutdown(); _ownsLog = false; }
        _profile?.Dispose();
    }
    private void ReleaseResource(string stage, Action release)
    {
        if (_ownsLog) AppLog.Info("candidate_cleanup_stage", $"stage={stage}");
        Release(release);
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
