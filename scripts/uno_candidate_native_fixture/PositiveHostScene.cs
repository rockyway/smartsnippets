using System.Diagnostics;
using SmartSnippets.Core.Domain;
using SmartSnippets.Core.Settings;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Uno.Platforms.Windows;
using SmartSnippets.Uno.Platforms.Windows.AcceptanceNative;

namespace SmartSnippets.Uno;

/// <summary>Actual host/store/VMs and native surfaces. Posted WM_HOTKEY is callback proof, NOT physical shortcut proof.</summary>
internal sealed class PositiveHostScene(UnoAppHost host, NativeFixtureBoundary boundary, AcceptanceFixture fixture,
    NativeAcceptanceSession session, LateNativeOperations broker, CandidateNativeCapability capability, Action admitSession, nint originalForeground)
{
    internal readonly PositiveKeyboardLifetime Keyboard = new();
    private readonly PositiveEnterGuard enter = new();
    private readonly HotkeyGesture gesture = new(HotkeyGesture.ModControl | HotkeyGesture.ModAlt | HotkeyGesture.ModShift, 0x78); // F9, never production.
    private AcceptanceHotkey? hotkey;
    private int callbacks;
    private readonly TaskCompletionSource captured = new();
    internal async Task RunAsync(string scene, CancellationToken ct)
    {
        PositiveProtocol.RequireReviewed(scene);
        var created = host.Store.CreateSnippet(new(GuardianClipboard.SyntheticText.Replace("\r\n", "\n"), "Native synthetic", true, null, ""));
        if (!created.Success) throw new InvalidOperationException("native_seed");
        var id = created.Value!.Id;
        host.Popup.EnsureCurrent();
        var ready = fixture.Lease.Readiness;
        await BootstrapProtocol.WriteAsync(Program.Responses,
            $"positive_surfaces {ready.WindowA} {ready.WindowB} {boundary.Popup!.NativeHandle} {boundary.Main!.NativeHandle}", ct);
        if (await BootstrapProtocol.ReadAsync(Program.Commands, ct) != "lease_positive") throw new InvalidOperationException("native_positive_lease");
        admitSession();
        if (scene != "expired") { broker.Admit(); host.RefreshClipboardAvailability(); }
        SmartSnippets.Core.Logging.AppLog.Info("owned_initial_focus", "phase=waiting");
        try
        {
            if (OwnedTargetPreparation.ShouldPrepare(scene))
            {
                // The expiry scene admits only initial Focus validation, never capture/Copy/Insert,
                // while retaining the real capability's monotonic deadline and the session's lease.
                var preparationOnly = scene == "expired";
                if (preparationOnly) broker.BeginPreparationFocus();
                try
                {
                    var result = await OwnedTargetPreparation.PrepareAsync(new OwnedPreparationWindows(fixture, session, capability),
                        () => new OwnedPreparationAutomation(), originalForeground, ready.WindowA, ready.EditorA, ct,
                        reason => SmartSnippets.Core.Logging.AppLog.Info("owned_preparation_admission", $"reason={reason}"));
                    SmartSnippets.Core.Logging.AppLog.Info("owned_target_preparation", $"reason={result}");
                    if (result is not (OwnedPreparationResult.Prepared or OwnedPreparationResult.AlreadyA))
                        throw new InvalidOperationException("native_owned_preparation_refused");
                }
                finally { if (preparationOnly) broker.EndPreparationFocus(); }
            }
            // Original manual check remains even after reviewed preparation. No fallback on preparation failure.
            await WaitAsync(() => session.IsTargetForeground(session.Target) && NativeOwnedPopup.QuietAllInput(), TimeSpan.FromSeconds(15), ct);
            await fixture.Lease.CommandAsync("focus_a", ct); // Already foreground, fixed owned editor only.
            SmartSnippets.Core.Logging.AppLog.Info("owned_initial_focus", "phase=ready");
        }
        catch
        {
            SmartSnippets.Core.Logging.AppLog.Info("owned_initial_focus", "phase=refused");
            throw;
        }
        await fixture.Lease.CommandAsync("empty_a", ct);
        await fixture.Lease.CommandAsync("empty_b", ct);
        if (PositiveProtocol.Inserts(scene)) await InsertAsync(scene, id, ct);
        else if (scene == "copy")
        {
            // This is the actual host-tracked bounded Copy admission and main-row VM callback, not UI click evidence.
            await host.RunCopyOperationAsync(() => host.Snippets.CopyRow(id));
            if (broker.CopyCommits != 1 || !PositiveNativeInput.VerifyCopiedSyntheticText()) throw new InvalidOperationException("native_copy_readback");
            await fixture.Lease.CommandAsync("empty_a", ct);
        }
        else
        {
            if (scene == "expired")
            {
                // Wait for the real monotonic capability deadline (not a fake revocation).
                var expiryWait = Stopwatch.GetTimestamp();
                await Task.Delay(TimeSpan.FromSeconds(21), ct); // Capability's entire lifetime is 20s from BEFORE host creation.
                // Independently measure beyond its entire lifetime, so adapter refusal cannot masquerade as expiry.
                if (Stopwatch.GetElapsedTime(expiryWait) < TimeSpan.FromSeconds(20) || capability.Allows(CandidateNativeOperation.Copy))
                    throw new InvalidOperationException("native_expiry");
                // Binding stays denied until expiry is proved; no pre-expiry capability can reach the adapter.
                broker.Admit(); host.RefreshClipboardAvailability();
            }
            host.ShowPreview();
            await WaitAsync(() => boundary.PopupShown && host.Popup.IsPreview, TimeSpan.FromSeconds(4), ct);
            var row = host.Popup.Items.Single(item => item.Kind == SmartSnippets.Presentation.ViewModels.PopupItemKind.Snippet);
            await host.RunCopyOperationAsync(() => host.Snippets.CopyRow(id));
            host.Popup.CopyRow(row);
            await host.Snippets.InsertRowAsync(id);
            await host.Popup.InsertRowAsync(row); // Selection closes preview and intentionally releases the main Copy lock.
            host.ShowPreview();
            await WaitAsync(() => boundary.PopupShown && host.Popup.IsPreview, TimeSpan.FromSeconds(4), ct);
            await host.Popup.InsertSelectedAsync();
            if (scene == "expired")
            {
                // Preview is hidden: only real capability expiry, not preview policy, denies these calls.
                if (!session.Validate(CandidateNativeOperation.Copy | CandidateNativeOperation.Insert | CandidateNativeOperation.Capture,
                    session.SessionId, session.Target.WindowHandle, session.Target.ProcessId))
                    throw new InvalidOperationException("native_expired_adapter_lease");
                if (boundary.PopupShown || host.ShowCapturedPopup() || host.Gate.IsAvailable || host.Snippets.CanCopyRow)
                    throw new InvalidOperationException("native_expired_admission");
                await host.RunCopyOperationAsync(() => host.Snippets.CopyRow(id));
                host.Popup.CopyRow(row);
                await host.Snippets.InsertRowAsync(id);
                if ((await host.Gate.InsertAsync(session.Target, id)).Outcome != InsertionOutcome.PasteFailed)
                    throw new InvalidOperationException("native_expired_insert");
            }
            if (broker.CopyCommits != 0 || broker.PasteCalls != 0 || broker.CaptureCalls != 0) throw new InvalidOperationException("native_nonmutating");
            await fixture.Lease.CommandAsync("empty_a", ct);
        }
        await fixture.Lease.CommandAsync("empty_b", ct);
        host.Store.Load(); // Persisted usage, not merely a view-model counter.
        var expectedUsage = PositiveProtocol.Inserts(scene) ? 1 : 0;
        if (host.Store.Snippets.Single().Snippet.UsageCount != expectedUsage || broker.PasteCalls != expectedUsage ||
            broker.CaptureCalls != expectedUsage || broker.CopyCommits != (scene == "copy" ? 1 : 0))
            throw new InvalidOperationException("native_exact_once");
        await fixture.Lease.RequireQuietProtocolAsync(ct);
        broker.Close();
        if (!await session.DrainOwnedRecoveryAsync()) throw new InvalidOperationException("native_recovery_pending");
        if (Keyboard.Armed)
        {
            if (enter.Pending) throw new InvalidOperationException("native_keyboard_pending");
            await BootstrapProtocol.WriteAsync(Program.Responses, "keyboard_settled", ct);
            Keyboard.Settle();
        }
        await BootstrapProtocol.WriteAsync(Program.Responses, "positive_verified", ct);
    }
    internal Task<bool> DrainFailedKeyboardAsync() => Keyboard.DrainAndSettleAsync(
        host.Gate.CloseAdmission,
        async () =>
        {
            _ = await host.Gate.DrainAsync();
            // Gate completion releases the dispatcher; let the VM's awaiting continuation finish too.
            while (host.Popup.IsInserting) await Task.Delay(25);
        },
        async () =>
        {
            // Restore is ownership-checked and bounded by the guardian protocol, even after lease expiry.
            if (session.RecoveryPending) _ = session.RestoreClipboardControl();
            _ = await session.DrainOwnedRecoveryAsync();
        },
        () => enter.Pending || session.RecoveryPending || host.Popup.IsInserting || !host.Gate.IsIdle,
        () => BootstrapProtocol.WriteAsync(Program.Responses, "keyboard_settled", CancellationToken.None));

    private async Task InsertAsync(string scene, long id, CancellationToken ct)
    {
        if (boundary.Message is null) throw new InvalidOperationException("native_message_missing");
        hotkey = session.CreateHotkey(gesture, boundary.MessageHandle, () => _ = ShortcutAsync(ct));
        boundary.Message.MessageReceived += OnMessage;
        try
        {
            if (!hotkey.TryChange(gesture).Success) throw new InvalidOperationException("native_shortcut_conflict");
            hotkey.Dispose(); // Prove release/reacquire on our exact host message window, never steal a conflicting registration.
            hotkey = session.CreateHotkey(gesture, boundary.MessageHandle, () => _ = ShortcutAsync(ct));
            if (!hotkey.TryChange(gesture).Success) throw new InvalidOperationException("native_shortcut_reacquire");
            PositiveNativeInput.PostShortcut(boundary.MessageHandle, gesture.Modifiers, gesture.Key);
            await captured.Task.WaitAsync(TimeSpan.FromSeconds(5), ct);
            try
            {
                await WaitAsync(() => boundary.PopupShown && host.Popup.Target == session.Target &&
                    NativeFixtureOs.Foreground() == boundary.Popup!.NativeHandle && NativeOwnedPopup.QuietAllInput(), TimeSpan.FromSeconds(5), ct);
            }
            catch
            {
                // Read-only fixed-schema diagnostics before keyboard arm; no focus retry or guard waiver.
                SmartSnippets.Core.Logging.AppLog.Info("native_popup_readiness",
                    $"shown={boundary.PopupShown} target={host.Popup.Target == session.Target} popup_foreground={NativeFixtureOs.Foreground() == boundary.Popup!.NativeHandle} target_foreground={session.IsTargetForeground(session.Target)} quiet={NativeOwnedPopup.QuietAllInput()}");
                throw;
            }
            // Coordinator must arm BEFORE Enter or the insertion callback can possibly dispatch native keys.
            await BootstrapProtocol.WriteAsync(Program.Responses, "keyboard_begin", ct);
            if (await BootstrapProtocol.ReadAsync(Program.Commands, ct) != "keyboard_permit") throw new InvalidOperationException("native_keyboard_permit");
            Keyboard.Arm();
            if (scene == "posted-enter")
            {
                using var popup = new NativeOwnedPopup(boundary.Popup!.NativeHandle);
                var state = enter.Send(() => host.Gate.CanInsert && capability.Allows(CandidateNativeOperation.Focus),
                    () => PositiveNativeInput.Enter(popup, () => host.Gate.CanInsert && capability.Allows(CandidateNativeOperation.Focus)));
                if (state == DispatchState.NotDispatched)
                {
                    // Zero accepted events / preflight refusal proves no Enter downs, and no VM callback was started.
                    await BootstrapProtocol.WriteAsync(Program.Responses, "keyboard_settled", ct);
                    Keyboard.Settle();
                }
                if (state != DispatchState.Dispatched) throw new InvalidOperationException("native_enter_delivery");
                // Actual native Enter is delivered to SearchBox, through PopupWindow.OnKeyDown and host RunOperation.
                await WaitAsync(() => host.Popup.IsInserting, TimeSpan.FromSeconds(3), ct);
                await DuplicateBusyAsync(id);
                await WaitAsync(() => !host.Popup.IsInserting, TimeSpan.FromSeconds(8), ct);
            }
            else
            {
                // Actual shared row/main callback wiring; intentionally NOT represented as native pointer/UI event proof.
                var first = scene == "posted-main" ? host.Snippets.InsertRowAsync(id) : host.Popup.InsertRowAsync(host.Popup.Items.Single(item => item.SnippetId == id));
                await DuplicateBusyAsync(id);
                await first.WaitAsync(TimeSpan.FromSeconds(8), ct);
            }
            await fixture.Lease.VerifyMultilineAAsync(ct); // Exact fixed Unicode/CRLF including final CRLF, utf16_count=22.
            if (callbacks != 1 || broker.PasteCalls != 1 || session.RecoveryPending) throw new InvalidOperationException("native_insertion_receipt");
        }
        finally
        {
            boundary.Message.MessageReceived -= OnMessage;
            hotkey.Dispose(); hotkey = null;
        }
    }
    private async Task DuplicateBusyAsync(long id)
    {
        if (!host.Popup.IsInserting || host.Gate.IsIdle || host.ShowCapturedPopup()) throw new InvalidOperationException("native_duplicate_not_busy");
        if ((await host.Gate.InsertAsync(session.Target, id)).Outcome != InsertionOutcome.Busy)
            throw new InvalidOperationException("native_duplicate_gate");
        await host.Snippets.InsertRowAsync(id);
        await host.Popup.InsertSelectedAsync();
    }
    private void OnMessage(uint message, nuint id, nint data) => hotkey?.HandleMessage(message, id, data);
    private async Task ShortcutAsync(CancellationToken ct)
    {
        try
        {
            if (++callbacks != 1 || !await session.ReadyForAdmissionAsync(ct) || !host.ShowCapturedPopup())
                throw new InvalidOperationException("native_shortcut_admission");
            captured.TrySetResult();
        }
        catch (Exception ex) { captured.TrySetException(ex); }
    }
    private static async Task WaitAsync(Func<bool> condition, TimeSpan timeout, CancellationToken ct)
    {
        var watch = Stopwatch.StartNew();
        while (!condition())
        {
            if (watch.Elapsed >= timeout) throw new InvalidOperationException("native_positive_timeout");
            await Task.Delay(25, ct);
        }
    }
}
