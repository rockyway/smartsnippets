using System.IO.Pipes;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using SmartSnippets.App.Services;
using SmartSnippets.Presentation.Services;
using SmartSnippets.TargetIdentity018;
using Windows.Win32.Foundation;

namespace SmartSnippets.WindowsInsertion018;

internal static partial class ClipboardGuardian
{
    internal static async Task<int> RunAsync(Guid run)
    {
        WindowsToken.RequireUnelevatedLauncher();
        using var diagnostics = new PrivateDiagnostics();
        FixedPipe.Receipt("role_private_evidence_token=" + diagnostics.Token);
        using var input = new StreamReader(Console.OpenStandardInput());
        if (await FixedPipe.ReadAsync(input, TimeSpan.FromSeconds(5), CancellationToken.None) != "guardian_capture") throw new ProbeFailure("guardian_permit");
        using var window = new GuardianWindow(run);
        var publication = new OwnClipboardPublication(run);
        // Create the restricted control before allocating a sensitive RAM backup,
        // so control-creation failure cannot skip snapshot zeroing.
        await using var recovery = new NamedPipeServerStream("ss-insertion018-" + run.ToString("N"), PipeDirection.InOut, 1,
            PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        var backup = ClipboardPasteDispatcher.GuardianBackup.Capture(new HWND(window.Handle), publication);
        if (backup is null) { FixedPipe.Receipt("guardian_capture_refused"); return 2; }
        SafeProcessHandle? worker = null;
        var armed = false;
        try
        {
            FixedPipe.Receipt($"guardian_ready window={(nuint)window.Handle} owner={(nuint)backup.OriginalOwner} sequence={backup.OriginalSequence}");
            var command = await FixedPipe.ReadAsync(input, TimeSpan.FromSeconds(10), CancellationToken.None);
            if (command is null) return 2; // No worker permit was issued; safe to zero pre-mutation backup.
            const string prefix = "guardian_arm handle=";
            if (!command.StartsWith(prefix, StringComparison.Ordinal) || !long.TryParse(command.AsSpan(prefix.Length), out var handle) || handle <= 0)
                throw new ProbeFailure("guardian_permit");
            worker = new SafeProcessHandle((nint)handle, true);
            WindowsToken.RequireSameUnelevatedToken(worker);
            if (!IsWorkerImage(worker) || WaitForSingleObject(worker, 0) != 258) throw new ProbeFailure("worker_identity");
            armed = true;
            diagnostics.Receipt("guardian_armed_full_ram_snapshot");
            FixedPipe.Receipt("guardian_armed");
            // EOF, malformed command and ordinary request ALL take the stopped-worker
            // recovery path. Never let protocol failure dispose the sole backup.
            try { _ = await FixedPipe.ReadOrEofAsync(input); }
            catch (Exception) { }
            while (WaitForSingleObject(worker, 100) == 258) await Task.Delay(20);
            if (WaitForSingleObject(worker, 0) != 0) throw new ProbeFailure("worker_wait");
            var status = await RecoverAsync(backup, window, publication);
            if (backup.Settled)
            {
                diagnostics.Receipt("guardian_settled_verified_original_representations");
                FixedPipe.Receipt("guardian_settled original=VERIFIED");
                return 0;
            }
            return await KeepBackupAsync(recovery, diagnostics, backup, window, publication, status, run);
        }
        catch (Exception) when (armed)
        {
            // A duplicated signaled handle is required; never guess worker death
            // from EOF or a PID. If its status is unknown, expose status/retry but
            // do not mutate until the retained HANDLE actually becomes signaled.
            return await KeepBackupAsync(recovery, diagnostics, backup, window, publication, RestoreStatus.Failed, run, worker);
        }
        finally
        {
            worker?.Dispose();
            if (!armed || backup.Settled) backup.Dispose();
        }
    }

    private static async Task<RestoreStatus> RecoverAsync(ClipboardPasteDispatcher.GuardianBackup backup,
        GuardianWindow window, OwnClipboardPublication publication)
    {
        var status = RestoreStatus.Busy;
        for (var attempt = 0; attempt < 20; attempt++)
        {
            status = backup.RecoverStopped(new HWND(window.Handle), window.IsPinned, true, publication);
            if (status != RestoreStatus.Busy) break;
            await Task.Delay(50);
        }
        return status;
    }

    private static async Task<int> KeepBackupAsync(NamedPipeServerStream control, PrivateDiagnostics diagnostics,
        ClipboardPasteDispatcher.GuardianBackup backup, GuardianWindow window, OwnClipboardPublication publication,
        RestoreStatus status, Guid run, SafeProcessHandle? worker = null)
    {
        diagnostics.TryFailureReceipt("RECOVERY_PENDING");
        FixedPipe.Receipt($"guardian_recovery_pending status={status} control=ss-insertion018-{run:N}");
        // The externally known run GUID is the pipe's name; no clipboard data,
        // hash, title or user path is part of this recovery channel.
        while (!backup.Settled)
        {
            try
            {
                await control.WaitForConnectionAsync();
                using var reader = new StreamReader(control, leaveOpen: true);
                var command = await FixedPipe.ReadAsync(reader, TimeSpan.FromSeconds(3), CancellationToken.None);
                if (command == "retry" && (worker is null || WaitForSingleObject(worker, 0) == 0)) status = await RecoverAsync(backup, window, publication);
                await FixedPipe.WriteAsync(control, backup.Settled ? "guardian_settled original=VERIFIED" : "RECOVERY_PENDING status=" + status);
            }
            catch (Exception) { } // Keep RAM and retryable control on disconnected/untrusted clients.
            finally { try { control.Disconnect(); } catch (Exception error) when (error is IOException or InvalidOperationException) { } }
        }
        return 0;
    }

    private static unsafe bool IsWorkerImage(SafeProcessHandle worker)
    {
        if (GetProcessId(worker) is 0 || GetProcessId(worker) == (uint)Environment.ProcessId) return false;
        var path = stackalloc char[32768];
        uint size = 32768;
        return QueryFullProcessImageNameW(worker, 0, path, ref size) != 0 &&
            new ReadOnlySpan<char>(path, (int)size).Equals(Environment.ProcessPath, StringComparison.OrdinalIgnoreCase);
    }
    [LibraryImport("kernel32.dll")] private static partial uint GetProcessId(SafeProcessHandle worker);
    [LibraryImport("kernel32.dll")] private static unsafe partial int QueryFullProcessImageNameW(SafeProcessHandle worker, uint flags, char* path, ref uint size);
    [LibraryImport("kernel32.dll")] private static partial uint WaitForSingleObject(SafeProcessHandle handle, uint milliseconds);
}
