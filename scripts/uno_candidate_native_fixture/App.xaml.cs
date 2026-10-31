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
    private PositiveHostScene? positive;
    public App() { InitializeComponent(); DispatcherShutdownMode = DispatcherShutdownMode.OnExplicitShutdown; }
    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Bootstrap must cancel and clean its target before the coordinator's 35s deadline.
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(PositiveProtocol.IsPositive(Program.Configuration[0]) ? 65 : 25));
        var ct = deadline.Token;
        try
        {
            var config = Program.Configuration;
            var isPositive = PositiveProtocol.IsPositive(config[0]);
            var scene = config[0] == "abort" ? NativeScene.Abort : NativeScene.Restore;
            if (isPositive) PositiveProtocol.RequireReviewed(config[0]);
            else if (config[0] is not ("restore" or "abort" or "owner-refusal")) throw new InvalidOperationException("native_scene");
            // Target creates its A/B windows with SW_SHOWNOACTIVATE. Independently observe foreground across launch.
            var foreground = NativeFixtureOs.Foreground();
            fixture = await AcceptanceFixture.StartAsync(TargetNativeRoot(config[1]), config[1], config[2], ct);
            var targetHandle = NativeFixtureOs.DuplicateForJob(fixture.Child.SafeHandle, Program.CoordinatorHandle);
            await BootstrapProtocol.WriteAsync(Program.Responses, $"fixture_owned {targetHandle}", ct);
            if (NativeFixtureOs.Foreground() != foreground) throw new InvalidOperationException("native_launch_activation");
            var id = Guid.NewGuid();
            var capability = new CandidateNativeCapability(id, fixture.Lease.Readiness.WindowA, fixture.Lease.ExpectedPid,
                isPositive ? PositiveProtocol.Permissions(config[0]) : (CandidateNativeOperation)0,
                TimeSpan.FromSeconds(config[0] == "expired" ? 20 : 55), TimeProvider.System, broker);
            // Positive capabilities are still late-bound and denied until the coordinator grants its post-Job permit.
            var boundary = new NativeFixtureBoundary(capability);
            host = new UnoAppHost(boundary, () => CandidateProfile.OpenOwnedRoot(Program.ProfileRoot));
            await host.StartAsync(true, Program.InitialOptions);
            if (boundary.Popup is null || boundary.Popup.NativeHandle == 0 || boundary.Main is null || boundary.Main.NativeHandle == 0 || host.Store is null || host.Popup is null || host.Gate.CanInsert)
                throw new InvalidOperationException("native_host_ready");
            if (NativeFixtureOs.Foreground() != foreground) throw new InvalidOperationException("native_host_activation");
            session = NativeAcceptanceFactory.Create(fixture, "a", boundary.Popup.NativeHandle, config[3], config[4],
                DateTimeOffset.UtcNow.AddSeconds(55), isPositive && PositiveProtocol.Inserts(config[0]), isPositive && !PositiveProtocol.Inserts(config[0]),
                operation => operation == AcceptanceOperation.Capture || (lease && (isPositive || operation == AcceptanceOperation.ClipboardControl)),
                allowClipboardControl: !isPositive, ownedMainWindow: isPositive ? boundary.Main.NativeHandle : 0);
            broker.Bind(session, session.SessionId);
            using var self = Process.GetCurrentProcess();
            if (!RetainedIdentity.OutsideJob(self.SafeHandle)) throw new InvalidOperationException("native_premature_job");
            var guardianHandle = session.DuplicateGuardianHandle(Program.CoordinatorHandle);
            await BootstrapProtocol.WriteAsync(Program.Responses, $"guardian_ready {targetHandle} {guardianHandle} {session.RecoveryControl}", ct);
            if (isPositive)
            {
                // RunAsync reads the distinct post-guardian/post-Job lease; binding never admits.
                positive = new(host, boundary, fixture, session, broker, capability, () => lease = true);
                await positive.RunAsync(config[0], ct);
                lease = false;
                await host.ExitAsync();
                if (!boundary.Exited) throw new InvalidOperationException("native_host_exit");
                session.Dispose(); session = null;
                await fixture.DisposeAsync(); fixture = null;
                await BootstrapProtocol.WriteAsync(Program.Responses, "positive_restored", ct);
                boundary.ExitApplication();
                return;
            }
            if (await BootstrapProtocol.ReadAsync(Program.Commands, ct) != "lease_clipboard_control") throw new InvalidOperationException("native_lease");
            lease = true; // Only the distinct clipboard-control operation is admitted; broker stays denied.
            if (NativeFixtureOs.Foreground() != foreground || session.PublishClipboardControl() != SwapStatus.Swapped || !session.RecoveryPending)
                throw new InvalidOperationException("native_publication_refused");
            await BootstrapProtocol.WriteAsync(Program.Responses, "published recovery_pending", ct);
            if (config[0] == "owner-refusal")
            {
                if (!session.VerifyOwnedOwnerChangeRefusal() || !session.RecoveryPending)
                    throw new InvalidOperationException("native_owner_refusal");
                await BootstrapProtocol.WriteAsync(Program.Responses, "owner_refused original_pending", ct);
            }
            if (scene == NativeScene.Abort)
            {
                // Coordinator must kill only the worker Job. No finally/disposal is relied on for recovery.
                await Task.Delay(Timeout.Infinite, ct);
                throw new InvalidOperationException("native_abort_timeout");
            }
            if (await BootstrapProtocol.ReadAsync(Program.Commands, ct) != "restore") throw new InvalidOperationException("native_restore_order");
            lease = false; broker.Close();
            if (config[0] == "owner-refusal" && session.CleanupOwnedOwnerControl() != RestoreStatus.Restored)
                throw new InvalidOperationException("native_owned_owner_cleanup");
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
            if (positive?.Keyboard.Armed == true)
            {
                // Unknown Enter/paste delivery forbids worker abort/disposal. Coordinator also retains its unjobbed lifetime.
                try { await BootstrapProtocol.WriteAsync(Program.Responses, "failed keyboard_pending", CancellationToken.None); } catch { }
                await positive.Keyboard.RetainAsync(() => { }, () => Task.Delay(1000));
                return;
            }
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
    internal ICandidateMainWindow? Main;
    internal Services.MessageWindow? Message;
    internal unsafe nint MessageHandle => Message is null ? 0 : (nint)Message.Handle.Value;
    internal bool PopupShown { get; private set; }
    internal bool Exited { get; private set; }
    internal override CandidateNativeCapability NativeCapability => capability;
    internal override IUiDispatcher Dispatcher => native.Dispatcher;
    internal override IThemeService CreateTheme(AppSettings settings) => native.CreateTheme(settings);
    internal override IDisposable CreateMessageWindow() => Message = (Services.MessageWindow)native.CreateMessageWindow();
    internal override IDialogService CreateDialogs(bool popup, ITextSizeService textSize) => native.CreateDialogs(popup, textSize);
    internal override ISnippetFileService CreateFiles() => native.CreateFiles();
    internal override ICandidateMainWindow CreateMain(UnoAppHost host) => Main = native.CreateMain(host);
    internal override ICandidatePopupWindow CreatePopup(UnoAppHost host)
    {
        Popup = native.CreatePopup(host);
        Popup.PresentationChanged += (_, shown) => PopupShown = shown;
        Popup.Hidden += (_, _) => PopupShown = false;
        return Popup;
    }
    internal override ICandidateTray CreateTray(Action open, Action preview, Action settings, Action exit) => native.CreateTray(open, preview, settings, exit);
    internal override void ReportStartupFailure() => throw new InvalidOperationException("native_startup_failed");
    internal override void Exit() => Exited = true;
    internal void ExitApplication() => native.Exit();
}
