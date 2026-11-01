using System.Diagnostics;
using SmartSnippets.Core.Logging;
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
        var startupSamples = new List<string>();
        var diagnostics = new NativeStartupDiagnostics(NativeFixtureOs.Foreground,
            window => { NativeFixtureOs.GetWindowThreadProcessId(window, out var pid); return pid; },
            (uint)Environment.ProcessId, () => fixture?.Lease.Readiness.WindowA ?? 0, () => fixture?.Lease.Readiness.WindowB ?? 0);
        void FlushSamples()
        {
            foreach (var sample in startupSamples) AppLog.Info("native_startup_foreground", sample);
            startupSamples.Clear();
        }
        try
        {
            NativeStage.Reset();
            var config = Program.Configuration;
            var isPositive = PositiveProtocol.IsPositive(config[0]);
            var scene = config[0] == "abort" ? NativeScene.Abort : NativeScene.Restore;
            NativeStage.Reach("review_gate");
            if (isPositive) PositiveProtocol.RequireReviewed(config[0]);
            else if (config[0] is not ("restore" or "abort" or "owner-refusal")) throw new InvalidOperationException("native_scene");
            // Target creates its A/B windows with SW_SHOWNOACTIVATE. Independently observe foreground across launch.
            var foreground = NativeFixtureOs.Foreground();
            NativeStage.Reach("fixture_start");
            startupSamples.Add(diagnostics.Sample("target_launch"));
            fixture = await AcceptanceFixture.StartAsync(TargetNativeRoot(config[1]), config[1], config[2], ct);
            var targetHandle = NativeFixtureOs.DuplicateForJob(fixture.Child.SafeHandle, Program.CoordinatorHandle);
            await BootstrapProtocol.WriteAsync(Program.Responses, $"fixture_owned {targetHandle}", ct);
            NativeStage.Reach("fixture_owned");
            if (NativeFixtureOs.Foreground() != foreground) throw new InvalidOperationException("native_launch_activation");
            var id = Guid.NewGuid();
            var capability = new CandidateNativeCapability(id, fixture.Lease.Readiness.WindowA, fixture.Lease.ExpectedPid,
                isPositive ? PositiveProtocol.Permissions(config[0]) : (CandidateNativeOperation)0,
                TimeSpan.FromSeconds(config[0] == "expired" ? 20 : 55), TimeProvider.System, broker);
            // Positive capabilities are still late-bound and denied until the coordinator grants its post-Job permit.
            var boundary = new NativeFixtureBoundary(capability);
            host = new UnoAppHost(boundary, () => CandidateProfile.OpenOwnedRoot(Program.ProfileRoot));
            NativeStage.Reach("host_start");
            await host.StartAsync(true, Program.InitialOptions);
            NativeStage.Reach("host_ready");
            startupSamples.Add(diagnostics.Sample("host_ready"));
            FlushSamples();
            if (!NativeHostReadiness.Check(() => boundary.Popup is not null, () => boundary.Popup!.NativeHandle != 0,
                () => boundary.Main is not null, () => boundary.Main!.NativeHandle != 0,
                () => host.Store is not null, () => host.Popup is not null, () => !host.Gate.CanInsert,
                () => NativeFixtureOs.Foreground() == foreground,
                reason => AppLog.Info("native_host_readiness", $"reason={reason}")))
                throw new InvalidOperationException("native_host_ready");
            NativeStage.Reach("session_create");
            session = NativeAcceptanceFactory.Create(fixture, "a", boundary.Popup!.NativeHandle, config[3], config[4],
                DateTimeOffset.UtcNow.AddSeconds(55), isPositive && (PositiveProtocol.NeedsCapturedPopup(config[0]) || config[0] == "expired"), isPositive && !PositiveProtocol.Inserts(config[0]),
                operation => PositiveNativeMutation.Permits(operation == AcceptanceOperation.Capture, lease, isPositive,
                    operation == AcceptanceOperation.ClipboardControl,
                    operation is AcceptanceOperation.Input or AcceptanceOperation.Clipboard, positive?.NativeMutationArmed == true),
                allowClipboardControl: !isPositive, ownedMainWindow: isPositive ? boundary.Main!.NativeHandle : 0, knownPriorToken: Program.KnownPriorToken,
                stage: NativeStage.Reach);
            NativeStage.Reach("broker_bind");
            broker.Bind(session, session.SessionId);
            NativeStage.Reach("outside_job");
            using var self = Process.GetCurrentProcess();
            if (!RetainedIdentity.OutsideJob(self.SafeHandle)) throw new InvalidOperationException("native_premature_job");
            NativeStage.Reach("guardian_dup");
            var guardianHandle = session.DuplicateGuardianHandle(Program.CoordinatorHandle);
            await BootstrapProtocol.WriteAsync(Program.Responses, $"guardian_ready {targetHandle} {guardianHandle} {session.RecoveryControl}", ct);
            NativeStage.Reach("guardian_ready");
            if (isPositive)
            {
                // RunAsync reads the distinct post-guardian/post-Job lease; binding never admits.
                positive = new(host, boundary, fixture, session, broker, capability, () => lease = true, foreground, diagnostics);
                await positive.RunAsync(config[0], ct);
                lease = false;
                await host.ExitAsync();
                if (!boundary.Exited) throw new InvalidOperationException("native_host_exit");
                var closedPath = Path.Combine(Program.PrivateRoot, "positive-foreground-closed");
                File.WriteAllText(closedPath + ".tmp", "closed\n");
                File.Move(closedPath + ".tmp", closedPath);
                session.Dispose(); session = null;
                await fixture.DisposeAsync(); fixture = null;
                await BootstrapProtocol.WriteAsync(Program.Responses, "positive_restored", ct);
                boundary.ExitApplication();
                return;
            }
            if (await BootstrapProtocol.ReadAsync(Program.Commands, ct) != "lease_clipboard_control") throw new InvalidOperationException("native_lease");
            lease = true; // Only the distinct clipboard-control operation is admitted; broker stays denied.
            var ready = fixture.Lease.Readiness;
            nint admissionForeground = 0;
            void ForegroundRefused(string check, int order)
            {
                var fg = admissionForeground;
                var thread = NativeFixtureOs.GetWindowThreadProcessId(fg, out var pid);
                var ownedThread = NativeFixtureOs.GetWindowThreadProcessId(ready.WindowA, out var ownedPid);
                AppLog.Info("native_control_foreground", NativeControlDiagnostics.Record(fg, ready.WindowA, ready.WindowB,
                    pid, session.DiagnosticGuardianPid, (uint)Environment.ProcessId, ownedPid,
                    NativeFixtureOs.IsWindowVisible(fg) != 0, NativeFixtureOs.IsWindowEnabled(fg) != 0,
                    thread, ownedThread, check, order));
            }
            var admissionSamples = 0;
            var admission = await ClipboardControlReadiness.WaitAsync(session.ClassifyClipboardControl,
                () => { admissionSamples++; admissionForeground = NativeFixtureOs.Foreground(); return admissionForeground == foreground; }, ct, refused: ForegroundRefused);
            if (admission == ClipboardControlAdmission.Ready && !NativeControlDiagnostics.CheckIdle(
                () => { admissionForeground = NativeFixtureOs.Foreground(); return admissionForeground == foreground; },
                ref admissionSamples, ForegroundRefused))
                admission = ClipboardControlAdmission.ForegroundChanged;
            // Enum values are fixed, content-free diagnostic tokens; no native identity is logged.
            AppLog.Info("native_control_admission", $"reason={admission}");
            var publishStatus = SwapStatus.NotPreserved;
            if (admission == ClipboardControlAdmission.Ready)
                publishStatus = session.PublishClipboardControl(); // Rechecks every guard before atomic guardian publication.
            AppLog.Info("native_control_publish", publishStatus == SwapStatus.Swapped ? "status=swapped" : "status=refused");
            if (publishStatus != SwapStatus.Swapped || !session.RecoveryPending)
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
        catch (Exception ex)
        {
            positive?.CloseNativeMutationAdmission();
            // Pre-host stages fail before the host opens app.log; open only the owned profile's log directory.
            if (NativeStage.BeforeHostLog) AppLog.Initialize(Path.Combine(Program.ProfileRoot, "logs"));
            FlushSamples();
            AppLog.Error(NativeStage.FailureEvent, ex); // Fixed stage token + exception type/HResult only.
            lease = false; broker.Close();
            if (positive?.Keyboard.Armed == true)
            {
                // Close and drain the actual host before independent guardian restoration. Never abort uncertain input.
                await positive.DrainFailedKeyboardAsync();
                if (positive.Keyboard.Armed)
                {
                    try { await BootstrapProtocol.WriteAsync(Program.Responses, "failed keyboard_pending", CancellationToken.None); } catch { }
                    await positive.Keyboard.RetainAsync(() => { }, async () =>
                    {
                        await Task.Delay(1000);
                        await positive.DrainFailedKeyboardAsync();
                    });
                }
                // A known-safe keyboard settlement is still a FAILED scene, not positive_verified.
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
