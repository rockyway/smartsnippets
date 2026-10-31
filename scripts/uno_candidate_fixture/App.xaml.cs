using Microsoft.UI.Xaml;
using SmartSnippets.Core.Settings;
using SmartSnippets.Presentation.Services;

namespace SmartSnippets.Uno;

public partial class App : Application
{
    private readonly FixtureBoundary _boundary = new();
    private UnoAppHost? _host;
    public App() { InitializeComponent(); DispatcherShutdownMode = DispatcherShutdownMode.OnExplicitShutdown; }
    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            _host = new UnoAppHost(_boundary, () => CandidateProfile.OpenOwnedRoot(Program.ProfileRoot));
            await _host.StartAsync(Program.StartInBackground, Program.InitialOptions);
            CandidateAcceptanceFlow.RequireReady(_host, Program.ProfileRoot);
            if (Program.RenderedUiMode)
            {
                UiAssertDriver.RequireFresh(_host);
                Program.Receipt("ui-ready"); await Program.WaitAsync("ui-ready");
                UiAssertDriver.RequireNew(_host);
                Program.Receipt("ui-new"); await Program.WaitAsync("ui-new");
                UiAssertDriver.RequireSaved(_host);
                Program.Receipt("ui-saved"); await Program.WaitAsync("ui-saved");
                UiAssertDriver.RequireCancelled(_host);
                Program.Receipt("ui-cancel"); await Program.WaitAsync("ui-cancel");
                // Cancellation deliberately leaves a dirty editor. Disposing the host is cleanup,
                // not a second navigation/exit confirmation callback under test.
                _host.Dispose();
                _boundary.Exit();
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
    internal override IUiDispatcher Dispatcher => _native.Dispatcher;
    internal override IThemeService CreateTheme(AppSettings settings) => _native.CreateTheme(settings);
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
