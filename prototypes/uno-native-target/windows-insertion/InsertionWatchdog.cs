using System.Globalization;
using System.Security.Cryptography;
using SmartSnippets.TargetIdentity018;

namespace SmartSnippets.WindowsInsertion018;

internal static class InsertionWatchdog
{
    internal static async Task<int> RunAsync(string target, string digest, InsertionScene scene)
    {
        using var diagnostics = new PrivateDiagnostics();
        // Retain our actual native image while both role processes start; never
        // launch an arbitrary sibling binary or managed/JIT child.
        var image = Environment.ProcessPath ?? throw new ProbeFailure("self_path");
        TargetArtifact.RefuseLinks(image);
        using var pinnedImage = new FileStream(image, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (pinnedImage.Length is < 32 or > 134217728) throw new ProbeFailure("self_image");
        var data = new byte[(int)pinnedImage.Length];
        pinnedImage.ReadExactly(data);
        TargetArtifact.RequireNativeHeader(data, TargetPlatform.Windows);
        diagnostics.Receipt("native_self_sha256=" + Convert.ToHexStringLower(SHA256.HashData(data)));
        var run = Guid.NewGuid();
        FixedPipe.Receipt("private_evidence_token=" + diagnostics.Token);
        FixedPipe.Receipt("recovery_control=ss-insertion018-" + run.ToString("N"));
        using var guardian = OwnedRoleProcess.Start("guardian", run);
        OwnedRoleProcess? worker = null;
        WorkerJob? job = null;
        var armSent = false;
        var settled = false;
        var recoveryReplyReceived = false;
        try
        {
            await guardian.WriteAsync("guardian_capture");
            var captured = await guardian.ReadAsync(); // Provider hang timeout is BEFORE mutation permission.
            if (captured == "guardian_capture_refused")
            {
                await guardian.RequireNaturalExitAsync();
                FixedPipe.Receipt("windows_insertion=REFUSED clipboard=UNTOUCHED reason=incomplete_capture");
                return 2;
            }
            var fields = captured.Split(' ');
            if (fields.Length != 4 || fields[0] != "guardian_ready" || !Number(fields[1], "window=", false) ||
                !Number(fields[2], "owner=", true) || !uint.TryParse(fields[3].AsSpan("sequence=".Length), NumberStyles.None,
                    CultureInfo.InvariantCulture, out _) || !fields[3].StartsWith("sequence=", StringComparison.Ordinal))
                throw new ProbeFailure("guardian_ready");
            worker = OwnedRoleProcess.Start("worker", run, target, digest, scene);
            job = WorkerJob.Assign(worker); // Before the child can start its native target or swap.
            var workerInGuardian = OwnedRoleProcess.DuplicateInto(worker, guardian);
            var guardianInWorker = OwnedRoleProcess.DuplicateInto(guardian, worker);
            // Treat an attempted arm write as armed: a broken acknowledgement is
            // NOT permission to kill a process potentially holding the sole backup.
            armSent = true;
            await guardian.WriteAsync("guardian_arm handle=" + (nuint)workerInGuardian);
            if (await guardian.ReadAsync() != "guardian_armed") throw new ProbeFailure("guardian_arm");
            diagnostics.Receipt("independent_ram_guardian_armed_before_worker_permit");
            await worker.WriteAsync($"worker_start handle={(nuint)guardianInWorker} {fields[1]} {fields[2]} {fields[3]}");
            var result = await worker.ReadAsync(45);
            if (scene == InsertionScene.AbortAfterSwap)
            {
                if (result != "worker_swap_ready" || worker.HasError) throw new ProbeFailure("worker_abort_stage");
                // Actual handle/job termination after full swap, BEFORE SendPaste.
                job.Dispose(); job = null;
                await worker.Process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
                diagnostics.Receipt("worker_hard_aborted_before_input");
            }
            else
            {
                if (result != "worker_insert_observed duplicate=BUSY dispatch=1 usage=1 readback=EXACT sibling=EMPTY")
                    throw new ProbeFailure(result == "worker_keyboard_recovery_pending" ? "KEYBOARD_RECOVERY_PENDING" : "worker_result");
                await worker.RequireNaturalExitAsync();
            }
            job?.Dispose(); job = null; // All target descendants stopped before backup settlement.
            await guardian.WriteAsync("guardian_recover");
            var recoveryReply = await guardian.ReadAsync();
            recoveryReplyReceived = true;
            if (recoveryReply != "guardian_settled original=VERIFIED") throw new ProbeFailure("RECOVERY_PENDING");
            await guardian.RequireNaturalExitAsync();
            settled = true;
            diagnostics.Receipt(scene == InsertionScene.Positive ? "insertion_composition_observed" : "worker_abort_recovery_observed");
            FixedPipe.Receipt(scene == InsertionScene.Positive ?
                "windows_insertion=OBSERVED usage=1 readback=EXACT clipboard=VERIFIED cleanup=STOPPED" :
                "windows_worker_abort=OBSERVED input=NONE clipboard=VERIFIED cleanup=STOPPED");
            return 0;
        }
        finally
        {
            // On interruption/timeout, close the worker job FIRST. Its target is
            // contained, but the independent guardian can never be in that job.
            settled = await RecoveryAfterWorkerStop.RunAsync(
                async () =>
                {
                    job?.Dispose();
                    if (worker is not null)
                    {
                        try { await worker.StopAndReapAsync(); }
                        finally { worker.Dispose(); }
                    }
                },
                async () =>
                {
                    if (!armSent) { await guardian.StopAndReapAsync(); return true; }
                    if (settled) return true;
                    // A replied non-settlement already moved the guardian into
                    // its recovery-control loop; do not repeat the stdin RPC.
                    if (recoveryReplyReceived) return false;
                    await guardian.WriteAsync("guardian_recover");
                    if (await guardian.ReadAsync() != "guardian_settled original=VERIFIED") return false;
                    await guardian.RequireNaturalExitAsync();
                    return true;
                });
            if (armSent && !settled)
            {
                diagnostics.TryFailureReceipt("RECOVERY_PENDING independent_guardian_retained");
                FixedPipe.Receipt("RECOVERY_PENDING control=ss-insertion018-" + run.ToString("N"));
            }
        }
    }
    private static bool Number(string field, string prefix, bool zero) => field.StartsWith(prefix, StringComparison.Ordinal) &&
        ulong.TryParse(field.AsSpan(prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var value) &&
        value <= (ulong)nint.MaxValue && (zero || value != 0);
}
