using Microsoft.UI.Xaml;
using SmartSnippets.Core.Settings;
using SmartSnippets.Presentation.Services;

namespace SmartSnippets.Uno;

public partial class App : Application
{
    private readonly FixtureBoundary _boundary = new();
    private UnoAppHost? _host;
    public App()
    {
        InitializeComponent();
        DispatcherShutdownMode = DispatcherShutdownMode.OnExplicitShutdown;
        UnhandledException += (_, args) => SmartSnippets.Core.Logging.AppLog.Error("fixture_unhandled", args.Exception);
        // Diagnostic only: do not set Handled or alter the framework's failure policy.
    }
    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            _host = new UnoAppHost(_boundary, () => CandidateProfile.OpenOwnedRoot(Program.ProfileRoot));
            await _host.StartAsync(Program.StartInBackground, Program.InitialOptions);
            CandidateAcceptanceFlow.RequireReady(_host, Program.ProfileRoot);
            if (Program.RenderedUiMode)
            {
                if (Program.FilePickerScene)
                {
                    FilePickerAssertDriver.PrepareFilePicker(_host, Program.ProfileRoot);
                    Program.Receipt("ui-ready"); await Program.WaitAsync("ui-ready");
                    if (_host.Snippets.Porting.ImportCommand.ExecutionTask is { } cancelImport) await cancelImport;
                    FilePickerAssertDriver.RequirePickerImportCancelled(_host, Program.ProfileRoot);
                    Program.Receipt("ui-import-cancelled"); await Program.WaitAsync("ui-import-cancelled");
                    if (_host.Snippets.Porting.ImportCommand.ExecutionTask is { } import) await import;
                    var importedIds = FilePickerAssertDriver.RequirePickerImported(_host, Program.ProfileRoot);
                    Program.Receipt("ui-imported"); await Program.WaitAsync("ui-imported");
                    if (_host.Snippets.Porting.ExportCommand.ExecutionTask is { } cancelExport) await cancelExport;
                    FilePickerAssertDriver.RequirePickerExportCancelled(_host, Program.ProfileRoot, importedIds);
                    Program.Receipt("ui-export-cancelled"); await Program.WaitAsync("ui-export-cancelled");
                    if (_host.Snippets.Porting.ExportCommand.ExecutionTask is { } export) await export;
                    FilePickerAssertDriver.RequirePickerExported(_host, Program.ProfileRoot, importedIds);
                    Program.Receipt("ui-exported"); await Program.WaitAsync("ui-exported");
                    await _host.ExitAsync();
                    return;
                }
                if (Program.SettingsScene)
                {
                    UiAssertDriver.RequireSettingsFresh(_host);
                    Program.Receipt("ui-ready"); await Program.WaitAsync("ui-ready");
                    UiAssertDriver.RequireSettingsTheme(_host);
                    if (_boundary.Theme?.Current != ThemeMode.Dark)
                        throw new InvalidOperationException("ui_settings_theme_surface");
                    Program.Receipt("ui-settings-theme"); await Program.WaitAsync("ui-settings-theme");
                    UiAssertDriver.RequireSettingsTextSize(_host);
                    Program.Receipt("ui-settings-size"); await Program.WaitAsync("ui-settings-size");
                    UiAssertDriver.RequireSettingsReloaded(_host, Program.ProfileRoot);
                    Program.Receipt("ui-settings-reloaded"); await Program.WaitAsync("ui-settings-reloaded");
                    await _host.ExitAsync();
                    return;
                }
                if (Program.FolderScene)
                {
                    UiAssertDriver.RequireFresh(_host);
                    Program.Receipt("ui-ready"); await Program.WaitAsync("ui-ready");
                    var folderId = UiAssertDriver.RequireFolderCreated(_host);
                    // Explicit fixture setup ONLY: not evidence of rendered snippet assignment.
                    var snippetId = FolderSceneSeed.AttachSyntheticSnippet(_host, folderId);
                    UiAssertDriver.RequireFolderAttached(_host, folderId);
                    Program.Receipt("ui-folder-created"); await Program.WaitAsync("ui-folder-created");
                    UiAssertDriver.RequireFolderRenamed(_host, folderId);
                    Program.Receipt("ui-folder-renamed"); await Program.WaitAsync("ui-folder-renamed");
                    UiAssertDriver.RequireFolderDeleted(_host, snippetId);
                    Program.Receipt("ui-folder-deleted"); await Program.WaitAsync("ui-folder-deleted");
                    await _host.ExitAsync();
                    return;
                }
                UiAssertDriver.RequireFresh(_host);
                Program.Receipt("ui-ready"); await Program.WaitAsync("ui-ready");
                UiAssertDriver.RequireNew(_host);
                Program.Receipt("ui-new"); await Program.WaitAsync("ui-new");
                var savedId = UiAssertDriver.RequireSaved(_host);
                Program.Receipt("ui-saved"); await Program.WaitAsync("ui-saved");
                UiAssertDriver.RequireCancelled(_host);
                Program.Receipt("ui-cancel"); await Program.WaitAsync("ui-cancel");
                UiAssertDriver.RequireDirtySaved(_host, savedId);
                Program.Receipt("ui-dirty-saved"); await Program.WaitAsync("ui-dirty-saved");
                var discardedEditor = UiAssertDriver.RequireDiscardDirty(_host, savedId);
                Program.Receipt("ui-discard-dirty"); await Program.WaitAsync("ui-discard-dirty");
                UiAssertDriver.RequireDiscarded(_host, savedId, discardedEditor);
                Program.Receipt("ui-discarded"); await Program.WaitAsync("ui-discarded");
                await _host.ExitAsync();
                return;
            }
            Program.Receipt("ready"); await Program.WaitAsync("ready");
            var id = await CandidateAcceptanceFlow.ManagementAsync(_host);
            Program.Receipt("management"); await Program.WaitAsync("management");
            _host.ShowPreview("acceptance synthetic");
            Program.Receipt("preview"); await Program.WaitAsync("preview");
            // Observer acknowledges only after two real uncloaked windows remain visible.
            // Yield first: native XAML warmup is asynchronous, unlike headless AutoReady.
            CandidateAcceptanceFlow.RequirePreview(_host, id);
            _host.Popup.Query = "unmatched-recreation-sentinel";
            _boundary.Popup!.Dispose();
            _host.ShowPreview("acceptance synthetic");
            Program.Receipt("recreated"); await Program.WaitAsync("recreated");
            CandidateAcceptanceFlow.RequirePreview(_host, id);
            if (_boundary.PopupCreations != 2) throw new InvalidOperationException("fixture_recreation");
            await _host.ExitAsync();
        }
        catch
        {
            _host?.Dispose();
            Console.Error.WriteLine("fixture_failed");
            Environment.ExitCode = 1;
            Program.StopActivations(); Exit();
        }
    }
}

/// <summary>Delegates every native surface to the actual production Windows boundary.</summary>
internal sealed class FixtureBoundary : CandidateHostBoundary
{
    private readonly WindowsCandidateBoundary _native = new();
    internal ICandidatePopupWindow? Popup;
    internal int PopupCreations;
    internal IThemeService? Theme;
    internal override IUiDispatcher Dispatcher => _native.Dispatcher;
    internal override IThemeService CreateTheme(AppSettings settings) => Theme = _native.CreateTheme(settings);
    internal override IDisposable CreateMessageWindow() => _native.CreateMessageWindow();
    internal override IDialogService CreateDialogs(bool popup, ITextSizeService textSize) => _native.CreateDialogs(popup, textSize);
    internal override ISnippetFileService CreateFiles() => _native.CreateFiles();
    internal override ICandidateMainWindow CreateMain(UnoAppHost host) => _native.CreateMain(host);
    internal override ICandidatePopupWindow CreatePopup(UnoAppHost host)
    { PopupCreations++; return Popup = _native.CreatePopup(host); }
    internal override ICandidateTray CreateTray(Action open, Action preview, Action settings, Action exit) => _native.CreateTray(open, preview, settings, exit);
    internal override void ReportStartupFailure() => throw new InvalidOperationException("fixture_startup_failed");
    internal override void Exit()
    {
        var log = File.ReadAllText(Path.Combine(Program.ProfileRoot, "logs", "app.log"));
        if (log.Contains(" ERROR ", StringComparison.Ordinal)) throw new InvalidOperationException("fixture_cleanup_failed");
        // Host must release its profile lease before native Application.Exit.
        using (File.Open(Path.Combine(Program.ProfileRoot, ".candidate-owner"), FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
        Program.Receipt("graceful");
        _native.Exit();
    }
}
