using System.Diagnostics;
using Microsoft.UI.Xaml;
using SmartSnippets.Core.Settings;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Uno.Platforms.Windows;
using SmartSnippets.Uno.Platforms.Windows.AcceptanceNative;

namespace SmartSnippets.Uno;

public partial class App : Application
{
    private UnoAppHost? host;
    private NativeAcceptanceSession? session;
    private AcceptanceFixture? fixture;
    private readonly LateNativeOperations broker = new();
    private bool lease;
    public App() { InitializeComponent(); DispatcherShutdownMode = DispatcherShutdownMode.OnExplicitShutdown; }
    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(50));
        var ct = deadline.Token;
        try
        {
            var config = Program.Configuration;
            var scene = config[0] switch { "restore" => NativeScene.Restore, "abort" => NativeScene.Abort, _ => throw new InvalidOperationException("native_scene") };
            // Target creates its A/B windows with SW_SHOWNOACTIVATE. Independently observe foreground across launch.
            var foreground = NativeFixtureOs.Foreground();
            fixture = await AcceptanceFixture.StartAsync(TargetNativeRoot(config[1]), config[1], config[2], ct);
            var targetHandle = NativeFixtureOs.DuplicateForJob(fixture.Child.SafeHandle, Program.CoordinatorHandle);
            await BootstrapProtocol.WriteAsync(Program.Responses, $"fixture_owned {targetHandle}", ct);
            if (NativeFixtureOs.Foreground() != foreground) throw new InvalidOperationException("native_launch_activation");
            var id = Guid.NewGuid();
            var capability = new CandidateNativeCapability(id, fixture.Lease.Readiness.WindowA, fixture.Lease.ExpectedPid,
                (CandidateNativeOperation)0, TimeSpan.FromSeconds(55), TimeProvider.System, broker);
            // Zero positive permissions: these controls cannot insert, focus, Copy or dispatch through real VMs.
            var boundary = new NativeFixtureBoundary(capability);
            host = new UnoAppHost(boundary, () => CandidateProfile.OpenOwnedRoot(Program.ProfileRoot));
            await host.StartAsync(true, Program.InitialOptions);
            if (boundary.Popup is null || boundary.Popup.NativeHandle == 0 || host.Store is null || host.Popup is null || host.Gate.CanInsert)
                throw new InvalidOperationException("native_host_ready");
            if (NativeFixtureOs.Foreground() != foreground) throw new InvalidOperationException("native_host_activation");
            session = NativeAcceptanceFactory.Create(fixture, "a", boundary.Popup.NativeHandle, config[3], config[4],
                DateTimeOffset.UtcNow.AddSeconds(55), false, false,
                operation => operation == AcceptanceOperation.Capture || (lease && operation == AcceptanceOperation.ClipboardControl), allowClipboardControl: true);
            broker.Bind(session, session.SessionId);
            using var self = Process.GetCurrentProcess();
            if (!RetainedIdentity.OutsideJob(self.SafeHandle)) throw new InvalidOperationException("native_premature_job");
            var guardianHandle = session.DuplicateGuardianHandle(Program.CoordinatorHandle);
            await BootstrapProtocol.WriteAsync(Program.Responses, $"guardian_ready {targetHandle} {guardianHandle} {session.RecoveryControl}", ct);
            if (await BootstrapProtocol.ReadAsync(Program.Commands, ct) != "lease_clipboard_control") throw new InvalidOperationException("native_lease");
            lease = true; // Only the distinct clipboard-control operation is admitted; broker stays denied.
            if (NativeFixtureOs.Foreground() != foreground || session.PublishClipboardControl() != SwapStatus.Swapped || !session.RecoveryPending)
                throw new InvalidOperationException("native_publication_refused");
            await BootstrapProtocol.WriteAsync(Program.Responses, "published recovery_pending", ct);
            if (scene == NativeScene.Abort)
            {
                // Coordinator must kill only the worker Job. No finally/disposal is relied on for recovery.
                await Task.Delay(Timeout.Infinite, ct);
                throw new InvalidOperationException("native_abort_timeout");
            }
            if (await BootstrapProtocol.ReadAsync(Program.Commands, ct) != "restore") throw new InvalidOperationException("native_restore_order");
            lease = false; broker.Close();
            if (!await session.DrainOwnedRecoveryAsync()) throw new InvalidOperationException("native_recovery_pending");
            await host.ExitAsync();
            if (!boundary.Exited) throw new InvalidOperationException("native_host_exit");
            session.Dispose(); session = null;
            await fixture.DisposeAsync(); fixture = null;
            await BootstrapProtocol.WriteAsync(Program.Responses, "restored", ct);
            boundary.ExitApplication();
        }
        catch
        {
            lease = false; broker.Close();
            // Always ask guardian, including an ambiguous publish failure and zero local VM snapshots.
            var settled = session is null || await session.DrainOwnedRecoveryAsync();
            try { await BootstrapProtocol.WriteAsync(Program.Responses, settled ? "failed settled" : "failed recovery_pending", CancellationToken.None); } catch { }
            if (settled)
            {
                session?.Dispose();
                if (fixture is not null) await fixture.DisposeAsync();
                host?.Dispose();
            }
            // Pending: exit detaches worker; independent unjobbed guardian retains the only backup.
            Environment.ExitCode = 2; Program.StopActivations(); Exit();
        }
    }
    private static string TargetNativeRoot(string path)
    {
        for (var directory = new DirectoryInfo(Path.GetDirectoryName(path)!); directory is not null; directory = directory.Parent)
            if (directory.Name == "bin" && directory.Parent?.Name == "windows") return directory.Parent.Parent!.FullName;
        throw new InvalidOperationException("native_target_provenance");
    }
}

internal sealed class NativeFixtureBoundary(CandidateNativeCapability capability) : CandidateHostBoundary
{
    private readonly WindowsCandidateBoundary native = new();
    internal ICandidatePopupWindow? Popup;
    internal bool Exited { get; private set; }
    internal override CandidateNativeCapability NativeCapability => capability;
    internal override IUiDispatcher Dispatcher => native.Dispatcher;
    internal override IThemeService CreateTheme(AppSettings settings) => native.CreateTheme(settings);
    internal override IDisposable CreateMessageWindow() => native.CreateMessageWindow();
    internal override IDialogService CreateDialogs(bool popup, ITextSizeService textSize) => native.CreateDialogs(popup, textSize);
    internal override ISnippetFileService CreateFiles() => native.CreateFiles();
    internal override ICandidateMainWindow CreateMain(UnoAppHost host) => native.CreateMain(host);
    internal override ICandidatePopupWindow CreatePopup(UnoAppHost host) => Popup = native.CreatePopup(host);
    internal override ICandidateTray CreateTray(Action open, Action preview, Action settings, Action exit) => native.CreateTray(open, preview, settings, exit);
    internal override void ReportStartupFailure() => throw new InvalidOperationException("native_startup_failed");
    internal override void Exit() => Exited = true;
    internal void ExitApplication() => native.Exit();
}
