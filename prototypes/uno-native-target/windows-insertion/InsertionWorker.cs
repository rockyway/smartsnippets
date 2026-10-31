using System.Globalization;
using Microsoft.Win32.SafeHandles;
using SmartSnippets.Core.Data;
using SmartSnippets.Core.Domain;
using SmartSnippets.Core.Logging;
using SmartSnippets.Core.Store;
using SmartSnippets.Presentation.Services;
using SmartSnippets.TargetIdentity018;

namespace SmartSnippets.WindowsInsertion018;

internal enum InsertionScene { Positive, AbortAfterSwap }

internal static class InsertionWorker
{
    internal static async Task<int> RunAsync(string targetPath, string digest, Guid run, InsertionScene scene)
    {
        AppLog.Shutdown(); // Never let Migrator's raw exceptions reach a log.
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(40));
        using var diagnostics = new PrivateDiagnostics(); // ACL installed before SQLite/log writes.
        FixedPipe.Receipt("role_private_evidence_token=" + diagnostics.Token);
        using var input = new StreamReader(Console.OpenStandardInput());
        var permit = await FixedPipe.ReadAsync(input, TimeSpan.FromSeconds(8), stop.Token);
        var fields = permit.Split(' ');
        if (fields.Length != 5 || fields[0] != "worker_start") throw new ProbeFailure("worker_permit");
        diagnostics.Receipt("worker_permit_received");
        using var guardian = GuardianPin.FromCoordinatorHandle(new SafeProcessHandle(Number(fields[1], "handle="), true), Number(fields[2], "window="), run);
        var owner = Number(fields[3], "owner=", zeroAllowed: true);
        if (!uint.TryParse(fields[4].AsSpan("sequence=".Length), NumberStyles.None, CultureInfo.InvariantCulture, out var sequence) ||
            !fields[4].StartsWith("sequence=", StringComparison.Ordinal)) throw new ProbeFailure("worker_permit");
        diagnostics.Receipt("worker_guardian_pin_valid");
        var publication = new OwnClipboardPublication(run);
        if (scene == InsertionScene.AbortAfterSwap)
        {
            // Clipboard-only prerequisite: no target launch, focus change or
            // keyboard/pointer API is reachable through this scene.
            if (!await ClipboardOnlyWorkerFlow.WaitForDesktopAsync(
                () => InputPreflight.QuietCurrentDesktop(), () => Task.Delay(100, stop.Token)))
            {
                _ = InputPreflight.QuietCurrentDesktop(diagnostics.Receipt);
                diagnostics.Receipt("clipboard_only_desktop_refused");
                throw new ProbeFailure("clipboard_desktop_refused");
            }
            diagnostics.Receipt("clipboard_only_desktop_ready");
            var clipboard = new SmartSnippets.App.Services.ClipboardPasteDispatcher(
                new Windows.Win32.Foundation.HWND(guardian.Window));
            ClipboardOnlyWorkerFlow.Execute(
                () =>
                {
                    var swap = clipboard.TryGuardedSwap(OwnClipboardPublication.Multiline,
                        guardian.IsPinned, owner, sequence, publication);
                    diagnostics.Receipt(swap.Status switch
                    {
                        SwapStatus.Swapped => "clipboard_only_swap_swapped",
                        SwapStatus.Busy => "clipboard_only_swap_busy",
                        SwapStatus.NotPreserved => "clipboard_only_swap_not_preserved",
                        _ => "clipboard_only_swap_failed"
                    });
                    return swap;
                },
                () =>
                {
                    FixedPipe.Receipt("worker_swap_ready");
                    Thread.Sleep(Timeout.Infinite); // Watchdog aborts only this worker job.
                });
            throw new ProbeFailure("clipboard_only_returned");
        }
        using var artifact = await TargetArtifact.OpenAsync(NativeRoot(), targetPath, digest, TargetPlatform.Windows,
            (_, _) => throw new ProbeFailure("platform_refused"), stop.Token);
        await using var lease = await OwnedFixtureLease.StartAsync(artifact, NativeChild.Start, stop.Token);
        using var probe = lease.CreateProbe();
        var target = probe.Capture("a");
        var other = probe.Capture("b");
        await lease.CommandAsync("empty_a", stop.Token);
        await lease.CommandAsync("empty_b", stop.Token);
        var factory = new SqliteConnectionFactory(Path.Combine(diagnostics.DirectoryPath, "synthetic.db"));
        new Migrator(factory).MigrateToLatest();
        var store = new SnippetStore(factory, TimeProvider.System);
        store.Load();
        var created = store.CreateSnippet(new SnippetDraft(OwnClipboardPublication.Multiline.Replace("\r\n", "\n"), "Synthetic018", true, null, ""));
        if (!created.Success || created.Value is null) throw new ProbeFailure("fixture_store");
        await FocusPreflight.RequireAsync(stop.Token);
        await lease.CommandAsync("focus_a", stop.Token);
        if (!probe.IsTargetForeground(target) || probe.IsTargetForeground(other)) throw new ProbeFailure("exact_target");
        var dispatcher = new GuardedPasteDispatcher(guardian, probe, target, owner, sequence, publication, () => { });
        var (result, duplicate) = await InsertionWorkerFlow.ExecuteAsync(store, probe, dispatcher, target, created.Value.Id);
        if (duplicate?.Outcome != InsertionOutcome.Busy) throw new ProbeFailure("duplicate_gate");
        if (dispatcher.KeyboardRecoveryPending)
        {
            diagnostics.Receipt("KEYBOARD_RECOVERY_PENDING");
            Console.WriteLine("worker_keyboard_recovery_pending");
            return 1;
        }
        if (result.Outcome != InsertionOutcome.Inserted || dispatcher.DispatchCalls != 1) throw new ProbeFailure("insert_outcome");
        await lease.VerifyMultilineAAsync(stop.Token);
        await lease.CommandAsync("empty_b", stop.Token);
        store.Load();
        if (store.Snippets.Single().Snippet.UsageCount != 1) throw new ProbeFailure("usage_once");
        await lease.RequireQuietProtocolAsync(stop.Token);
        diagnostics.Receipt("actual_service_native_dispatch_exact_readback_usage_once");
        Console.WriteLine("worker_insert_observed duplicate=BUSY dispatch=1 usage=1 readback=EXACT sibling=EMPTY");
        return 0;
    }

    private static nint Number(string value, string prefix, bool zeroAllowed = false)
    {
        if (!value.StartsWith(prefix, StringComparison.Ordinal) || !ulong.TryParse(value.AsSpan(prefix.Length), NumberStyles.None,
            CultureInfo.InvariantCulture, out var number) || number > (ulong)nint.MaxValue || (!zeroAllowed && number == 0)) throw new ProbeFailure("worker_permit");
        return (nint)number;
    }
    private static string NativeRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (directory.Name == "uno-native-target" && Directory.Exists(Path.Combine(directory.FullName, "windows"))) return directory.FullName;
        throw new ProbeFailure("worker_path");
    }
}
