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
    internal bool RecoveryPending => dispatcher.RecoveryPending;
    internal string RecoveryControl => guardian.RecoveryControl;
    internal nint DuplicateGuardianHandle(SafeProcessHandle recipient) => guardian.DuplicateGuardianHandle(recipient);
    internal SwapStatus PublishClipboardControl()
    {
        if (closed || !permitted(AcceptanceOperation.ClipboardControl) || !guardian.CanPublish
            || !guardian.IsPinned || !popup.PopupPinned || owned.Check(target) != TargetCheck.Ok
            || !(visibleDesktop ?? NativeDesktop.IsVisibleCurrent)()
            || !(quietInput ?? NativeOwnedPopup.QuietAllInput)()) return SwapStatus.NotPreserved;
        return guardian.Publish(AcceptanceOperation.ClipboardControl);
    }
    internal Task<bool> ReadyForAdmissionAsync(CancellationToken ct) => closed ? Task.FromResult(false) : focus.ReadyForAdmissionAsync(ct);
    // Invoke ONLY from the existing popup coordinator's ready-surface focus callback.
    internal bool TryFocusOwnedPopup() => !closed && permitted(AcceptanceOperation.Focus) && focus.TryFocusPopup();
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
