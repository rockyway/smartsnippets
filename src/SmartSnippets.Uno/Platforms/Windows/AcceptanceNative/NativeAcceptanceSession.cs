using System;
using System.Threading;
using System.Threading.Tasks;
using SmartSnippets.Core.Settings;
using SmartSnippets.Presentation.Services;
using SmartSnippets.TargetIdentity018;
using Microsoft.Win32.SafeHandles;

namespace SmartSnippets.Uno.Platforms.Windows.AcceptanceNative;

internal sealed class NativeAcceptanceSession(OwnedTargetProbe owned, GuardedTargetProbe probe,
    InsertionTarget target, IGuardianChannel guardian, GuardedPasteDispatcher dispatcher,
    IAcceptanceFocus focus, IOwnedFocusOs popup, Func<AcceptanceOperation, bool> permitted,
    Action close, Action disposePopup, Func<bool>? visibleDesktop = null, Func<bool>? quietInput = null) : ICandidateNativeOperations, IDisposable
{
    private bool closed, disposed;
    internal Guid SessionId { get; } = Guid.NewGuid();
    internal InsertionTarget Target => target;
    internal uint DiagnosticGuardianPid => guardian is GuardianClient client ? client.DiagnosticProcessId : 0;
    internal bool RecoveryPending => dispatcher.RecoveryPending;
    internal string RecoveryControl => guardian.RecoveryControl;
    internal nint DuplicateGuardianHandle(SafeProcessHandle recipient) => guardian.DuplicateGuardianHandle(recipient);
    // Read-only classification in the original short-circuit guard order.
    internal ClipboardControlAdmission ClassifyClipboardControl()
    {
        if (closed) return ClipboardControlAdmission.Closed;
        if (!permitted(AcceptanceOperation.ClipboardControl)) return ClipboardControlAdmission.Permission;
        if (!guardian.CanPublish) return ClipboardControlAdmission.GuardianUnavailable;
        if (!guardian.IsPinned) return ClipboardControlAdmission.GuardianUnpinned;
        if (!popup.PopupPinned) return ClipboardControlAdmission.PopupUnpinned;
        if (owned.Check(target) != TargetCheck.Ok) return ClipboardControlAdmission.TargetIdentity;
        if (!(visibleDesktop ?? NativeDesktop.IsVisibleCurrent)()) return ClipboardControlAdmission.Desktop;
        if (!(quietInput ?? NativeOwnedPopup.QuietAllInput)()) return ClipboardControlAdmission.Unquiet;
        return ClipboardControlAdmission.Ready;
    }
    internal SwapStatus PublishClipboardControl()
    {
        if (ClassifyClipboardControl() != ClipboardControlAdmission.Ready) return SwapStatus.NotPreserved;
        return guardian.Publish(AcceptanceOperation.ClipboardControl);
    }
    internal bool VerifyOwnedOwnerChangeRefusal() => !closed && permitted(AcceptanceOperation.ClipboardControl) &&
        guardian.RecoveryPending && guardian.IsPinned && popup.PopupPinned && owned.Check(target) == TargetCheck.Ok &&
        (visibleDesktop ?? NativeDesktop.IsVisibleCurrent)() && (quietInput ?? NativeOwnedPopup.QuietAllInput)() &&
        guardian.VerifyOwnedOwnerChangeRefusal();
    // Cleanup does not authorize new control mutation and must survive admission expiry.
    internal RestoreStatus CleanupOwnedOwnerControl() => guardian.CleanupOwnedOwnerControl();
    public Task<bool> WaitForCopyAdmissionAsync(CancellationToken ct) => closed ? Task.FromResult(false) : focus.ReadyForAdmissionAsync(ct);
    internal RestoreStatus RestoreClipboardControl() => guardian.Restore();
    internal Task<bool> ReadyForAdmissionAsync(CancellationToken ct) => closed ? Task.FromResult(false) : focus.ReadyForAdmissionAsync(ct);
    // Invoke ONLY from the existing popup coordinator's ready-surface focus callback.
    public bool TryFocusOwnedPopup() => !closed && permitted(AcceptanceOperation.Focus) && focus.TryFocusPopup();
    internal AcceptanceHotkey CreateHotkey(HotkeyGesture explicitlySelected, nint ownedMessageWindow, Action callback) =>
        new(explicitlySelected, () => !closed && permitted(AcceptanceOperation.Shortcut) && owned.Check(target) == TargetCheck.Ok,
            new NativeAcceptanceHotkey(ownedMessageWindow), callback);

    public bool Validate(CandidateNativeOperation operation, Guid sessionId, nint window, uint processId)
    {
        const CandidateNativeOperation known = CandidateNativeOperation.Capture | CandidateNativeOperation.Insert | CandidateNativeOperation.Copy |
            CandidateNativeOperation.Focus | CandidateNativeOperation.Input | CandidateNativeOperation.Clipboard;
        if (closed || sessionId != SessionId || window != target.WindowHandle || processId != target.ProcessId ||
            (operation & ~known) != 0 || !guardian.IsPinned || dispatcher.KeyboardRecoveryPending ||
            !popup.PopupPinned || owned.Check(target) != TargetCheck.Ok || !(visibleDesktop ?? NativeDesktop.IsVisibleCurrent)()) return false;
        bool Allowed(CandidateNativeOperation flag, AcceptanceOperation required) => (operation & flag) == 0 || permitted(required);
        if (!Allowed(CandidateNativeOperation.Capture, AcceptanceOperation.Capture) || !Allowed(CandidateNativeOperation.Insert, AcceptanceOperation.Insert) ||
            !Allowed(CandidateNativeOperation.Copy, AcceptanceOperation.Copy) || !Allowed(CandidateNativeOperation.Focus, AcceptanceOperation.Focus) ||
            !Allowed(CandidateNativeOperation.Input, AcceptanceOperation.Input) || !Allowed(CandidateNativeOperation.Clipboard, AcceptanceOperation.Clipboard)) return false;
        if ((operation & CandidateNativeOperation.Copy) != 0 && !dispatcher.IsAvailable) return false;
        if ((operation & CandidateNativeOperation.Capture) != 0 &&
            (guardian.RecoveryPending || !focus.ReadyForAdmission() || !probe.IsTargetForeground(target))) return false;
        if ((operation & (CandidateNativeOperation.Input | CandidateNativeOperation.Clipboard)) != 0 &&
            (!(quietInput ?? NativeOwnedPopup.QuietAllInput)() || !probe.IsTargetForeground(target))) return false;
        // Insert/Focus alone are nonmutating admission: no target-foreground/idle promise here.
        // RestoreFocusAsync performs the bounded quiet wait and exact-owned foreground transfer.
        return true;
    }
    public InsertionTarget? CaptureOwnedTarget(nint window, uint processId) =>
        Validate(CandidateNativeOperation.Capture, SessionId, window, processId) ? target : null;
    public TargetCheck Check(InsertionTarget captured) => captured == target ? probe.Check(captured) : TargetCheck.Closed;
    public bool IsTargetForeground(InsertionTarget captured) => captured == target && probe.IsTargetForeground(captured);
    public Task<FocusPath?> RestoreFocusAsync(InsertionTarget captured, CancellationToken ct) => dispatcher.RestoreFocusAsync(captured, ct);
    public bool ReleaseHeldModifiers() => dispatcher.ReleaseHeldModifiers();
    public ClipboardSwap TrySwapClipboard(string text, bool allowIncomplete) => dispatcher.TrySwapClipboard(text, allowIncomplete);
    public SwapStatus TryPlaceText(string text) => SwapStatus.NotPreserved;
    public DispatchState SendPaste() => dispatcher.SendPaste();
    public bool ReleasePasteKeys() => dispatcher.ReleasePasteKeys();
    public RestoreStatus TryRestoreClipboard(IClipboardSnapshot snapshot) => dispatcher.TryRestoreClipboard(snapshot);
    public void DiscardClipboardSnapshot(IClipboardSnapshot snapshot) => dispatcher.DiscardClipboardSnapshot(snapshot);
    public bool IsAvailable => !closed && dispatcher.IsAvailable;
    public bool TrySetText(string text) => !closed && dispatcher.TrySetText(text);
    public void SetText(string text) => _ = TrySetText(text);
    public Task<bool> DrainOwnedRecoveryAsync()
    {
        closed = true; close();
        var result = dispatcher.CloseAdmissionAndRestore();
        return Task.FromResult(result == RestoreStatus.Restored && !dispatcher.RecoveryPending);
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try { _ = DrainOwnedRecoveryAsync(); }
        finally
        {
            // This detaches control but never terminates an unsettled guardian.
            owned.Dispose(); guardian.Dispose(); disposePopup();
        }
    }
}
