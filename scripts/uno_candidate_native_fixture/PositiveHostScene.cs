using System.Diagnostics;
using SmartSnippets.Core.Domain;
using SmartSnippets.Core.Settings;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Uno.Platforms.Windows;
using SmartSnippets.Uno.Platforms.Windows.AcceptanceNative;

namespace SmartSnippets.Uno;

/// <summary>Actual host/store/VMs and native surfaces. Posted WM_HOTKEY is callback proof, NOT physical shortcut proof.</summary>
internal sealed class PositiveHostScene(UnoAppHost host, NativeFixtureBoundary boundary, AcceptanceFixture fixture,
    NativeAcceptanceSession session, LateNativeOperations broker, CandidateNativeCapability capability, Action admitSession)
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
        admitSession(); broker.Admit(); host.RefreshClipboardAvailability();
        // Operator explicitly selects A after guardian-ready. Never activate a foreign window to make admission pass.
        await WaitAsync(() => session.IsTargetForeground(session.Target) && NativeOwnedPopup.QuietAllInput(), TimeSpan.FromSeconds(15), ct);
        await fixture.Lease.CommandAsync("focus_a", ct); // Already foreground, fixed owned editor only.
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
                await Task.Delay(TimeSpan.FromSeconds(21), ct); // Capability's entire lifetime is 20s from BEFORE host creation.
                if (capability.Allows(CandidateNativeOperation.Copy)) throw new InvalidOperationException("native_expiry");
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
            await WaitAsync(() => boundary.PopupShown && host.Popup.Target == session.Target &&
                NativeFixtureOs.Foreground() == boundary.Popup!.NativeHandle && NativeOwnedPopup.QuietAllInput(), TimeSpan.FromSeconds(5), ct);
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
