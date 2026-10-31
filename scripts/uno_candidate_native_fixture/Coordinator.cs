using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;
using SmartSnippets.Uno.Platforms.Windows.AcceptanceNative;

namespace SmartSnippets.Uno;

/// <summary>Never jobbed; the sole kill Job contains only the retained candidate and direct owned fixture.</summary>
internal static class Coordinator
{
    internal static string Digest(string path) { using var file = File.OpenRead(path); return Convert.ToHexString(SHA256.HashData(file)); }
    internal static async Task<int> RunAsync(string[] config)
    {
        var isPositive = PositiveProtocol.IsPositive(config[0]);
        var scene = config[0] == "abort" ? NativeScene.Abort : NativeScene.Restore;
        if (isPositive) PositiveProtocol.RequireReviewed(config[0]);
        else if (config[0] is not ("restore" or "abort")) throw new InvalidOperationException("native_scene");
        var state = new NativeSceneState(scene);
        var keyboard = new PositiveKeyboardLifetime();
        using var self = Process.GetCurrentProcess();
        if (!RetainedIdentity.OutsideJob(self.SafeHandle)) throw new InvalidOperationException("native_coordinator_job");
        using var targetImage = PinnedNativeImage.Open(config[1], config[2], "SmartSnippets.Target018.exe");
        using var guardianImage = PinnedNativeImage.Open(config[3], config[4], "SmartSnippets.UnoCandidate.Guardian.exe");
        using var commands = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);
        using var responses = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);
        using var inheritedSelf = NativeFixtureOs.InheritableSelf(self.SafeHandle);
        var info = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = Program.PrivateRoot };
        info.Environment.Clear();
        info.Environment["SystemRoot"] = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        info.Environment["WINDIR"] = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        foreach (var arg in new[] { "--internal-worker", commands.GetClientHandleAsString(), responses.GetClientHandleAsString(), inheritedSelf.DangerousGetHandle().ToString() }.Concat(config)) info.ArgumentList.Add(arg);
        var foreground = NativeFixtureOs.Foreground();
        using var job = NativeFixtureOs.CreateWorkerJob();
        using var worker = Process.Start(info) ?? throw new InvalidOperationException("native_worker_start");
        commands.DisposeLocalCopyOfClientHandle(); responses.DisposeLocalCopyOfClientHandle();
        SafeProcessHandle? fixture = null, guardian = null;
        var assigned = false;
        try
        {
            using var ct = new CancellationTokenSource(TimeSpan.FromSeconds(isPositive ? 75 : 35));
            var owned = (await BootstrapProtocol.ReadAsync(responses, ct.Token)).Split(' ');
            if (owned.Length != 2 || owned[0] != "fixture_owned") throw new InvalidOperationException("native_fixture_receipt");
            fixture = new(BootstrapProtocol.Handle(owned[1]), ownsHandle: true);
            if (RetainedIdentity.Parent(fixture) != (uint)worker.Id || !RetainedIdentity.Pinned(fixture, targetImage)) throw new InvalidOperationException("native_fixture_provenance");
            var ready = BootstrapProtocol.Ready(await BootstrapProtocol.ReadAsync(responses, ct.Token));
            if (BootstrapProtocol.Handle(ready[1]) != fixture.DangerousGetHandle()) throw new InvalidOperationException("native_fixture_handle_changed");
            guardian = new(BootstrapProtocol.Handle(ready[2]), ownsHandle: true);
            if (RetainedIdentity.Parent(worker.SafeHandle) != (uint)Environment.ProcessId ||
                RetainedIdentity.Parent(fixture) != (uint)worker.Id || RetainedIdentity.Parent(guardian) != (uint)worker.Id ||
                !RetainedIdentity.Pinned(fixture, targetImage) || !RetainedIdentity.Pinned(guardian, guardianImage) ||
                !RetainedIdentity.OutsideJob(guardian) || !RetainedIdentity.OutsideJob(worker.SafeHandle) ||
                !RetainedIdentity.OutsideJob(fixture) || NativeFixtureOs.Foreground() != foreground)
                throw new InvalidOperationException("native_bootstrap_provenance");
            using (var recoveryReceipt = new StreamWriter(new FileStream(
                Path.Combine(Program.PrivateRoot, "recovery-control.txt"), FileMode.CreateNew, FileAccess.Write, FileShare.Read)))
                recoveryReceipt.WriteLine(ready[3]); // Allowlisted pipe token only; never clipboard bytes.
            state.Ready();
            // This is the FIRST Job assignment, after the guardian's authenticated ready handshake.
            NativeFixtureOs.Assign(job, worker.SafeHandle); assigned = true;
            NativeFixtureOs.Assign(job, fixture); state.Assign();
            if (!RetainedIdentity.OutsideJob(guardian)) throw new InvalidOperationException("native_guardian_job");
            Console.WriteLine("native_phase=guardian_ready");
            if (isPositive)
            {
                var surfaces = await BootstrapProtocol.ReadAsync(responses, ct.Token);
                PositiveProtocol.RequireSurfaces(surfaces);
                var fields = surfaces.Split(' ').Skip(1).Select(BootstrapProtocol.Handle).ToArray();
                if (!NativeFixtureOs.WindowOwnedBy(fields[0], (uint)RetainedIdentity.Id(fixture)) ||
                    !NativeFixtureOs.WindowOwnedBy(fields[1], (uint)RetainedIdentity.Id(fixture)) ||
                    !NativeFixtureOs.WindowOwnedBy(fields[2], (uint)worker.Id) || !NativeFixtureOs.WindowOwnedBy(fields[3], (uint)worker.Id))
                    throw new InvalidOperationException("native_surfaces_identity");
                // External observer accepts only these exact owned surfaces plus the unchanged initial foreground.
                var surfacesPath = Path.Combine(Program.PrivateRoot, "positive-surfaces.txt");
                await File.WriteAllTextAsync(surfacesPath + ".tmp", string.Join(" ", fields), ct.Token);
                File.Move(surfacesPath + ".tmp", surfacesPath);
                Console.WriteLine("native_phase=awaiting_owned_a");
                await BootstrapProtocol.WriteAsync(commands, "lease_positive", ct.Token);
                if (PositiveProtocol.Inserts(config[0]))
                {
                    if (await BootstrapProtocol.ReadAsync(responses, ct.Token) != "keyboard_begin") throw new InvalidOperationException("native_keyboard_order");
                    keyboard.Arm(); // Must precede permit. EOF/timeout after this point never kills possibly held keys.
                    await BootstrapProtocol.WriteAsync(commands, "keyboard_permit", ct.Token);
                    if (await BootstrapProtocol.ReadAsync(responses, ct.Token) != "keyboard_settled") throw new InvalidOperationException("native_keyboard_pending");
                    keyboard.Settle();
                }
                if (await BootstrapProtocol.ReadAsync(responses, ct.Token) != "positive_verified" ||
                    await BootstrapProtocol.ReadAsync(responses, ct.Token) != "positive_restored") throw new InvalidOperationException("native_positive_receipt");
                await worker.WaitForExitAsync(ct.Token);
                if (worker.ExitCode != 0) throw new InvalidOperationException("native_worker_exit");
                await WaitStoppedAsync(fixture, TimeSpan.FromSeconds(5));
                if (await RetainGuardianAsync(guardian) != 0) throw new InvalidOperationException("native_guardian_exit");
                Console.WriteLine(PositiveProtocol.Result(config[0]));
                return 0;
            }
            await BootstrapProtocol.WriteAsync(commands, "lease_clipboard_control", ct.Token);
            if (await BootstrapProtocol.ReadAsync(responses, ct.Token) != "published recovery_pending") throw new InvalidOperationException("native_publish_receipt");
            state.Publish(); Console.WriteLine("native_phase=published");
            if (NativeFixtureOs.Foreground() != foreground) throw new InvalidOperationException("native_foreground_changed");
            if (scene == NativeScene.Abort)
            {
                NativeFixtureOs.TerminateWorkers(job);
                await WaitStoppedAsync(worker.SafeHandle, TimeSpan.FromSeconds(5));
                await WaitStoppedAsync(fixture, TimeSpan.FromSeconds(5));
                var code = await RetainGuardianAsync(guardian);
                state.GuardianStopped(code);
                Console.WriteLine("native_result=abort_restored");
            }
            else
            {
                await BootstrapProtocol.WriteAsync(commands, "restore", ct.Token);
                if (await BootstrapProtocol.ReadAsync(responses, ct.Token) != "restored") throw new InvalidOperationException("native_restore_receipt");
                state.Restored();
                await worker.WaitForExitAsync(ct.Token);
                if (worker.ExitCode != 0) throw new InvalidOperationException("native_worker_exit");
                await WaitStoppedAsync(fixture, TimeSpan.FromSeconds(5));
                if (await RetainGuardianAsync(guardian) != 0) throw new InvalidOperationException("native_guardian_exit");
                Console.WriteLine("native_result=regular_restored");
            }
            if (!state.Settled || NativeFixtureOs.Foreground() != foreground) throw new InvalidOperationException("native_scene_incomplete");
            return 0;
        }
        catch
        {
            Console.WriteLine("native_result=failed");
            if (keyboard.Armed)
                await keyboard.RetainAsync(() => Console.WriteLine("native_result=KEYBOARD_RECOVERY_PENDING coordinator_retained=1"), () => Task.Delay(1000));
            await NativeRecoveryLifetime.CleanupAsync(async () =>
            {
                try
                {
                    if (assigned) NativeFixtureOs.TerminateWorkers(job);
                    else
                    {
                        // Before fixture-handle transfer the worker is the only owner that can
                        // reap its child. EOF asks its bounded bootstrap to unwind first.
                        try { commands.Dispose(); } catch (IOException) { }
                        try { await WaitStoppedAsync(worker.SafeHandle, TimeSpan.FromSeconds(5)); }
                        catch (InvalidOperationException)
                        {
                            if (!worker.HasExited) worker.Kill(entireProcessTree: false);
                        }
                    }
                }
                finally
                {
                    try { await WaitStoppedAsync(worker.SafeHandle, TimeSpan.FromSeconds(5)); }
                    finally
                    {
                        if (fixture is not null)
                        {
                            try { if (RetainedIdentity.Alive(fixture)) NativeFixtureOs.TerminateFixture(fixture); }
                            finally { await WaitStoppedAsync(fixture, TimeSpan.FromSeconds(5)); }
                        }
                    }
                }
            }, async () =>
            {
                if (guardian is not null) _ = await RetainGuardianAsync(guardian);
            }, () => Console.WriteLine("native_cleanup=worker_pending"));
            return 2;
        }
        finally { fixture?.Dispose(); guardian?.Dispose(); }
    }
    private static async Task WaitStoppedAsync(SafeProcessHandle handle, TimeSpan timeout)
    {
        var watch = Stopwatch.StartNew();
        while (!RetainedIdentity.Stopped(handle))
        {
            if (watch.Elapsed >= timeout) throw new InvalidOperationException("native_worker_pending");
            await Task.Delay(50);
        }
    }
    private static Task<uint> RetainGuardianAsync(SafeProcessHandle guardian) => NativeRecoveryLifetime.RetainAsync(
        () => RetainedIdentity.Stopped(guardian), () => NativeFixtureOs.ExitCode(guardian), () => Task.Delay(200),
        () => Console.WriteLine("native_result=RECOVERY_PENDING coordinator_retained=1"));

}

internal static partial class NativeFixtureOs
{
    [LibraryImport("user32.dll", EntryPoint = "GetForegroundWindow")] internal static partial nint Foreground();
    internal static bool WindowOwnedBy(nint window, uint expectedPid) => expectedPid != 0 && GetWindowThreadProcessId(window, out var pid) != 0 && pid == expectedPid;
    [LibraryImport("user32.dll")] private static partial uint GetWindowThreadProcessId(nint window, out uint pid);
    internal static SafeProcessHandle InheritableSelf(SafeProcessHandle self)
    {
        if (DuplicateHandle(GetCurrentProcess(), self, GetCurrentProcess(), out var duplicate, 0x101040, 1, 0) == 0) throw new InvalidOperationException("native_parent_handle");
        return new(duplicate, ownsHandle: true);
    }
    internal static nint DuplicateForJob(SafeProcessHandle source, SafeProcessHandle recipient)
    {
        if (DuplicateHandle(GetCurrentProcess(), source, recipient.DangerousGetHandle(), out var duplicate, 0x101501, 0, 0) == 0) throw new InvalidOperationException("native_fixture_handle");
        return duplicate;
    }
    internal static SafeFileHandle CreateWorkerJob()
    {
        var job = new SafeFileHandle(CreateJobObjectW(0, 0), ownsHandle: true);
        var limits = new ExtendedLimits { Basic = new() { Flags = 0x2000 } };
        if (job.IsInvalid || SetInformationJobObject(job, 9, in limits, (uint)Marshal.SizeOf<ExtendedLimits>()) == 0) { job.Dispose(); throw new InvalidOperationException("native_job"); }
        return job;
    }
    internal static void Assign(SafeFileHandle job, SafeProcessHandle child)
    { if (AssignProcessToJobObject(job, child) == 0) throw new InvalidOperationException("native_job_assign"); }
    internal static void TerminateWorkers(SafeFileHandle job)
    { if (TerminateJobObject(job, 2) == 0) throw new InvalidOperationException("native_job_terminate"); }
    internal static void TerminateFixture(SafeProcessHandle process)
    { if (TerminateProcess(process, 2) == 0 && !RetainedIdentity.Stopped(process)) throw new InvalidOperationException("native_fixture_cleanup"); }
    [LibraryImport("kernel32.dll")] private static partial int TerminateProcess(SafeProcessHandle process, uint exitCode);
    internal static uint ExitCode(SafeProcessHandle process)
    { if (GetExitCodeProcess(process, out var code) == 0 || code == 259) throw new InvalidOperationException("native_exit_unknown"); return code; }
    [StructLayout(LayoutKind.Sequential)] private struct BasicLimits
    { public long ProcessTime, JobTime; public uint Flags; public nuint Minimum, Maximum; public uint Active; public nuint Affinity; public uint Priority, Scheduling; }
    [StructLayout(LayoutKind.Sequential)] private struct ExtendedLimits
    { public BasicLimits Basic; public ulong ReadOperations, WriteOperations, OtherOperations, ReadBytes, WriteBytes, OtherBytes; public nuint ProcessMemory, JobMemory, PeakProcessMemory, PeakJobMemory; }
    [LibraryImport("kernel32.dll")] private static partial nint GetCurrentProcess();
    [LibraryImport("kernel32.dll")] private static partial int DuplicateHandle(nint sourceProcess, SafeProcessHandle source, nint targetProcess, out nint target, uint access, int inherit, uint options);
    [LibraryImport("kernel32.dll")] private static partial nint CreateJobObjectW(nint attributes, nint name);
    [LibraryImport("kernel32.dll")] private static partial int SetInformationJobObject(SafeFileHandle job, int kind, in ExtendedLimits information, uint size);
    [LibraryImport("kernel32.dll")] private static partial int AssignProcessToJobObject(SafeFileHandle job, SafeProcessHandle process);
    [LibraryImport("kernel32.dll")] private static partial int TerminateJobObject(SafeFileHandle job, uint exitCode);
    [LibraryImport("kernel32.dll")] private static partial int GetExitCodeProcess(SafeProcessHandle process, out uint code);
}
