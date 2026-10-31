namespace SmartSnippets.TargetIdentity018;

// Isolated synchronous head: the async identity flow resumes on pool threads and
// cannot own Carbon callbacks. Only IO is awaited synchronously here; native
// install/dispatch/release and target probing stay on the original entry thread.
internal static class CallbackHarness
{
    internal const string Result = "owned_callback=OBSERVED delivery=INPROCESS_NATIVE_EVENT physical_shortcut=NOT_RUN cleanup=STOPPED input=NONE clipboard=UNTOUCHED";

    internal static int Run(string[] args)
    {
        PrivateDiagnostics? diagnostics = null;
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        using var input = Console.OpenStandardInput();
        ConsoleCancelEventHandler interrupt = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += interrupt;
        try
        {
            var options = HarnessOptions.Parse(args);
            if (!options.NativeCallback) throw new ProbeFailure("callback_authorization");
            var platform = IdentityHarness.HostPlatform();
            diagnostics = new PrivateDiagnostics();
            Console.WriteLine("private_evidence_token=" + diagnostics.Token);
            Console.Out.Flush();
            var permit = IdentityHarness.ReadWatchdogPermitAsync(input, stop.Token).WaitAsync(TimeSpan.FromSeconds(5), stop.Token).GetAwaiter().GetResult();
            if (permit != "owned_watchdog_ready") throw new ProbeFailure("watchdog_refused");
            _ = Task.Run(() => IdentityHarness.CancelOnWatchdogEof(input, stop));
            using var artifact = TargetArtifact.OpenAsync(IdentityHarness.NativeRoot(), options.Executable, options.Digest,
                platform, IdentityHarness.VerifySignatureAsync, stop.Token).GetAwaiter().GetResult();
            var lease = OwnedFixtureLease.StartAsync(artifact, NativeChild.Start, stop.Token).GetAwaiter().GetResult();
            try { Observe(lease, diagnostics.Receipt, stop.Token); }
            finally { lease.DisposeAsync().AsTask().GetAwaiter().GetResult(); }
            diagnostics.Receipt("owned_child_stopped");
            Console.WriteLine(Result);
            return 0;
        }
        catch (Exception error)
        {
            var token = error is ProbeFailure failure ? failure.Token : error is OperationCanceledException ? "cancelled" : "unexpected";
            diagnostics?.TryFailureReceipt("owned_callback_failed reason=" + token);
            try { Console.Error.WriteLine("owned_callback_failed reason=" + token); } catch (IOException) { }
            return 1;
        }
        finally { diagnostics?.Dispose(); Console.CancelKeyPress -= interrupt; }
    }

    private static void Observe(OwnedFixtureLease lease, Action<string> receipt, CancellationToken ct)
    {
        using var probe = lease.CreateProbe(); // Actual owned lease and ITargetProbe, not a PID-only stand-in.
        var a = probe.Capture("a");
        var b = probe.Capture("b");
        Command("empty_a");
        Command("empty_b");
        Focus("a");
        for (uint generation = 101; generation <= 102; generation++)
        {
            FocusPreflight.RequireAsync(ct).GetAwaiter().GetResult();
            NativeCallbackLease.RequireChordReleased();
            Require(probe.IsTargetForeground(a) && !probe.IsTargetForeground(b), "callback_pre_registration_foreground");
            var gate = new CallbackCaptureGate(probe, a, generation);
            using (var native = NativeCallbackLease.Install(gate))
            {
                Require(native.TryAcquire(), "callback_registration_refused");
                native.RequireConflict();
                Require(probe.IsTargetForeground(a) && !probe.IsTargetForeground(b), "callback_registration_changed_foreground");
                native.Send(0, generation); // Unknown native ID/signature.
                native.Send(NativeCallbackLease.Signature, generation - 1); // Old generation.
                Require(gate.Deliveries == 0 && gate.Captures == 0, "callback_invalid_id_captured");
                native.Send(NativeCallbackLease.Signature, generation);
                RequireCapture(gate, 1, 1);
                Require(gate.Captured == a, "callback_original_identity");
                Focus("b");
                Require(probe.IsTargetForeground(b) && !probe.IsTargetForeground(a), "callback_sibling_foreground");
                native.Send(NativeCallbackLease.Signature, generation);
                RequireCapture(gate, 2, 1); // Same PID does not authorize sibling B.
                Focus("a");
                native.Release();
                native.Send(NativeCallbackLease.Signature, generation);
                RequireCapture(gate, 2, 1); // Released native delivery cannot capture.
                Require(probe.IsTargetForeground(a) && !probe.IsTargetForeground(b), "callback_release_changed_foreground");
            }
            Require(probe.IsTargetForeground(a) && !probe.IsTargetForeground(b), "callback_disposal_changed_foreground");
            receipt("inprocess_native_callback_generation_verified");
        }
        lease.RequireQuietProtocolAsync(ct).GetAwaiter().GetResult();
        receipt("inprocess_native_event_only_physical_shortcut_not_run");

        void Command(string command) => lease.CommandAsync(command, ct).GetAwaiter().GetResult();
        void Focus(string window)
        {
            FocusPreflight.RequireAsync(ct).GetAwaiter().GetResult();
            Command("focus_" + window);
        }
    }

    internal static void RequireCapture(CallbackCaptureGate gate, int deliveries, int captures) =>
        Require(!gate.Failed && gate.Deliveries == deliveries && gate.Captures == captures, "callback_missing_or_wrong_capture");

    private static void Require(bool value, string token) { if (!value) throw new ProbeFailure(token); }
}
