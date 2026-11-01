using SmartSnippets.Core.Logging;
using SmartSnippets.Core.Store;
using SmartSnippets.Presentation.Services;

namespace SmartSnippets.Uno.Platforms.Windows;

/// <summary>Default-denied outer service and guarded boundaries around the real insertion policy.</summary>
internal sealed class CandidateNativeGate : IClipboardService, IInsertionService, ITargetProbe, IPasteDispatcher
{
    internal const string Unavailable = "Clipboard and insertion are unavailable in the Uno candidate. Use management or target-free preview.";
    private readonly CandidateNativeCapability? _capability;
    private readonly Func<bool> _preview;
    private readonly HashSet<IClipboardSnapshot> _snapshots = new(ReferenceEqualityComparer.Instance);
    private InsertionService? _insertion;
    private InsertionTarget? _captured, _activeTarget;
    private TaskCompletionSource? _active;
    private Task<bool>? _drain;
    private bool _closed, _suspended, _pasteAttempted, _untrackedRecovery;
    internal event Action? AvailabilityChanged;
    private bool HasRecovery => _untrackedRecovery || _snapshots.Count != 0;
    private void NotifyAvailabilityChanged()
    {
        try { AvailabilityChanged?.Invoke(); }
        catch { AppLog.Info("candidate_availability_notification_failed"); }
    }
    internal void ClearCapturedTarget()
    {
        _captured = null;
        NotifyAvailabilityChanged();
    }

    internal CandidateNativeGate(CandidateNativeCapability? capability = null, Func<bool>? preview = null)
    { _capability = capability; _preview = preview ?? (() => false); }
    internal void Compose(ISnippetStore store)
    {
        _insertion = new InsertionService(store, this, this);
        AppLog.Info("candidate_native_admission", _capability is null ? "state=denied" : "state=scoped");
    }
    private bool Allows(CandidateNativeOperation operation) => !_closed && !_suspended && !_preview()
        && _capability?.Allows(operation) == true;
    private bool AllowsInsert(CandidateNativeOperation operation) => _activeTarget is { } target
        && _capability?.Owns(target) == true && Allows(CandidateNativeOperation.Insert | operation);
    internal bool CanInsert => IsIdle && _captured is not null && Allows(CandidateNativeOperation.Insert);
    internal bool IsIdle => _active is null && !HasRecovery;
    public bool IsAvailable => IsIdle && Allows(CandidateNativeOperation.Copy);
    public bool TrySetText(string text)
    {
        if (!IsAvailable) return false;
        // A false/throwing Copy can still have published before recovery failed.
        // Keep admission closed until the adapter explicitly verifies settlement.
        _untrackedRecovery = true;
        try
        {
            var copied = _capability!.Operations.TrySetText(text);
            if (copied) _untrackedRecovery = false;
            return copied;
        }
        catch { return false; }
        finally { NotifyAvailabilityChanged(); }
    }
    public void SetText(string text) => _ = TrySetText(text);
    internal async Task<bool> WaitForCopyAdmissionAsync(CancellationToken ct = default)
    {
        if (!IsAvailable) return false;
        try { return await _capability!.Operations.WaitForCopyAdmissionAsync(ct) && IsAvailable; }
        catch { return false; }
    }

    internal bool TryFocusOwnedPopup()
    {
        if (!IsIdle || _captured is null || !Allows(CandidateNativeOperation.Focus))
        { AppLog.Info("candidate_popup_focus", "reason=denied"); return false; }
        try
        {
            var focused = _capability!.Operations.TryFocusOwnedPopup();
            AppLog.Info("candidate_popup_focus", focused ? "reason=accepted" : "reason=refused");
            return focused;
        }
        catch { AppLog.Info("candidate_popup_focus", "reason=failed"); return false; }
    }

    internal InsertionTarget? Capture()
    {
        if (_active is not null) return null;
        ClearCapturedTarget();
        if (HasRecovery || !Allows(CandidateNativeOperation.Capture)) return null;
        try
        {
            var target = _capability!.Operations.CaptureOwnedTarget(_capability.Window, _capability.ProcessId);
            if (target is null || !_capability.Owns(target) || !Allows(CandidateNativeOperation.Capture)) return null;
            _captured = target;
            NotifyAvailabilityChanged();
            return target;
        }
        catch { return null; }
    }

    public async Task<InsertionResult> InsertAsync(InsertionTarget target, long snippetId,
        InsertionOptions options = default, CancellationToken ct = default)
    {
        if (_active is not null && target == _captured && Allows(CandidateNativeOperation.Insert))
            return new(InsertionOutcome.Busy, "An insertion is already in progress.");
        if (!CanInsert || target != _captured || _insertion is null
            || (options.AllowClipboardLoss && !Allows(CandidateNativeOperation.ClipboardLoss)))
            return new(InsertionOutcome.PasteFailed, _closed ? "The candidate is shutting down." : Unavailable);
        var active = new TaskCompletionSource();
        _active = active; _activeTarget = target; _pasteAttempted = false;
        NotifyAvailabilityChanged();
        try { return await _insertion.InsertAsync(target, snippetId, options, ct); }
        finally
        {
            _activeTarget = null; _active = null;
            if (HasRecovery) _captured = null;
            NotifyAvailabilityChanged();
            active.TrySetResult();
        }
    }

    internal void SuspendAdmission() { _suspended = true; NotifyAvailabilityChanged(); }
    internal void ResumeAdmission() { if (!_closed) { _suspended = false; NotifyAvailabilityChanged(); } }
    internal void CloseAdmission()
    {
        if (_closed) return;
        _closed = true;
        ClearCapturedTarget();
        AppLog.Info("candidate_native_admission", "state=closed");
    }
    internal Task<bool> DrainAsync()
    {
        if (!_closed && !_suspended) throw new InvalidOperationException("Close admission before draining.");
        if (_drain is { IsCompleted: false }) return _drain;
        return _drain = DrainCoreAsync();
    }
    private async Task<bool> DrainCoreAsync()
    {
        if (_active is { } active) await active.Task;
        if (_capability is null) return !HasRecovery;
        // Adapter-owned Copy/control/keyboard recovery can exist without a local snapshot.
        // Always ask the authorized adapter, including when no insertion was started.
        // This is recovery of already-owned state, not permission to begin a new mutation.
        // The adapter must verify clipboard ownership and retain its RAM backup on failure.
        try
        {
            if (!await _capability!.Operations.DrainOwnedRecoveryAsync()) return false;
            _snapshots.Clear(); _untrackedRecovery = false;
            NotifyAvailabilityChanged();
            return true;
        }
        catch { return false; }
    }

    TargetCheck ITargetProbe.Check(InsertionTarget target) => target == _activeTarget && AllowsInsert(0)
        ? _capability!.Operations.Check(target) : TargetCheck.Closed;
    bool ITargetProbe.IsTargetForeground(InsertionTarget target) => target == _activeTarget && AllowsInsert(CandidateNativeOperation.Input)
        && _capability!.Operations.IsTargetForeground(target);
    async Task<FocusPath?> IPasteDispatcher.RestoreFocusAsync(InsertionTarget target, CancellationToken ct)
    {
        if (target != _activeTarget || !AllowsInsert(CandidateNativeOperation.Focus)) return null;
        var result = await _capability!.Operations.RestoreFocusAsync(target, ct);
        return AllowsInsert(CandidateNativeOperation.Focus) ? result : null;
    }
    bool IPasteDispatcher.ReleaseHeldModifiers() => AllowsInsert(CandidateNativeOperation.Input)
        && _capability!.Operations.ReleaseHeldModifiers();
    ClipboardSwap IPasteDispatcher.TrySwapClipboard(string text, bool allowIncomplete)
    {
        if (!AllowsInsert(CandidateNativeOperation.Clipboard)
            || (allowIncomplete && !AllowsInsert(CandidateNativeOperation.ClipboardLoss))) return new(SwapStatus.Failed, null);
        try
        {
            var swap = _capability!.Operations.TrySwapClipboard(text, allowIncomplete);
            if (swap.Snapshot is { } snapshot) _snapshots.Add(snapshot);
            if (swap.Status == SwapStatus.Swapped && swap.Snapshot is null)
            {
                // A faulty adapter may have emptied the clipboard without transferring its backup.
                // Only its owned recovery drain can settle that state; never dispatch without a snapshot.
                _untrackedRecovery = true;
                NotifyAvailabilityChanged();
                return new(SwapStatus.Failed, null);
            }
            NotifyAvailabilityChanged();
            return swap.Status == SwapStatus.NotPreserved && !AllowsInsert(CandidateNativeOperation.ClipboardLoss)
                ? swap with { Status = SwapStatus.Failed } : swap;
        }
        catch (ClipboardSwapException ex) { _snapshots.Add(ex.Snapshot); NotifyAvailabilityChanged(); throw; }
        catch { _untrackedRecovery = true; NotifyAvailabilityChanged(); throw; }
    }
    SwapStatus IPasteDispatcher.TryPlaceText(string text) => AllowsInsert(CandidateNativeOperation.Clipboard | CandidateNativeOperation.ClipboardLoss)
        ? _capability!.Operations.TryPlaceText(text) : SwapStatus.Failed;
    DispatchState IPasteDispatcher.SendPaste()
    {
        try
        {
            if (!AllowsInsert(CandidateNativeOperation.Input)) return DispatchState.NotDispatched;
            // Authorization checks must not separate the final native HWND/PID check from dispatch.
            if (!_capability!.Operations.IsTargetForeground(_activeTarget!)) return DispatchState.NotDispatched;
        }
        catch { return DispatchState.NotDispatched; }
        _pasteAttempted = true;
        return _capability.Operations.SendPaste();
    }
    bool IPasteDispatcher.ReleasePasteKeys() => _pasteAttempted && _capability!.Operations.ReleasePasteKeys();
    RestoreStatus IPasteDispatcher.TryRestoreClipboard(IClipboardSnapshot snapshot)
    {
        if (!_snapshots.Contains(snapshot)) return RestoreStatus.Failed;
        var result = _capability!.Operations.TryRestoreClipboard(snapshot);
        if (result == RestoreStatus.Restored) _snapshots.Remove(snapshot);
        NotifyAvailabilityChanged();
        return result;
    }
    void IPasteDispatcher.DiscardClipboardSnapshot(IClipboardSnapshot snapshot)
    {
        if (!_snapshots.Contains(snapshot) || !AllowsInsert(CandidateNativeOperation.ClipboardLoss))
            throw new InvalidOperationException("Clipboard discard is not authorized.");
        _capability!.Operations.DiscardClipboardSnapshot(snapshot);
        _snapshots.Remove(snapshot);
        NotifyAvailabilityChanged();
    }
}
