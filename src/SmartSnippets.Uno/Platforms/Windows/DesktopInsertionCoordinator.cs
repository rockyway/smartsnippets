using SmartSnippets.Core.Logging;
using SmartSnippets.Core.Store;
using SmartSnippets.Presentation.Services;

namespace SmartSnippets.Uno.Platforms.Windows;

/// <summary>Single-flight insertion, preview admission and owned clipboard recovery for Windows desktop services.</summary>
internal sealed class DesktopInsertionCoordinator : IClipboardService, IInsertionService, ITargetProbe, IPasteDispatcher
{
    internal const string Unavailable = "Insertion is unavailable. Focus the target application and press the global shortcut again.";
    private IDailyOperations? _daily;
    private IDailyOperations Operations => _daily ?? throw new InvalidOperationException("Desktop services are unavailable.");
    internal void ComposeDesktop(IDailyOperations operations)
    {
        if (_daily is not null) throw new InvalidOperationException("Native services already composed.");
        _daily = operations;
    }
    private readonly Func<bool> _preview;
    private readonly HashSet<IClipboardSnapshot> _snapshots = new(ReferenceEqualityComparer.Instance);
    private InsertionService? _insertion;
    private InsertionTarget? _captured, _activeTarget;
    private TaskCompletionSource? _active;
    private Task<bool>? _drain;
    private bool _closed, _suspended, _pasteAttempted, _untrackedRecovery;
    internal event Action? AvailabilityChanged;
    private bool HasRecovery => _untrackedRecovery || _snapshots.Count != 0 || _daily?.HasPendingRecovery == true;
    private void NotifyAvailabilityChanged()
    {
        try { AvailabilityChanged?.Invoke(); }
        catch { AppLog.Info("app_availability_notification_failed"); }
    }
    internal void ClearCapturedTarget()
    {
        _captured = null;
        NotifyAvailabilityChanged();
    }

    internal DesktopInsertionCoordinator(Func<bool>? preview = null)
    { _preview = preview ?? (() => false); }
    internal void Compose(ISnippetStore store)
    {
        _insertion = new InsertionService(store, this, this);
        AppLog.Info("candidate_native_admission", _daily is not null ? "state=desktop" : "state=unavailable");
    }
    private bool Allows() => !_closed && !_suspended && !_preview() && _daily is not null;
    private bool AllowsInsert() => _activeTarget is not null && Allows();
    internal bool CanInsert => IsIdle && _captured is not null && Allows();
    internal bool IsIdle => _active is null && !HasRecovery;
    public bool IsAvailable => IsIdle && Allows();
    public bool TrySetText(string text)
    {
        if (!IsAvailable) return false;
        // A false/throwing Copy can still have published before recovery failed.
        // Keep admission closed until the adapter explicitly verifies settlement.
        _untrackedRecovery = true;
        try
        {
            var copied = Operations.TrySetText(text);
            // Ordinary Copy owns no restore snapshot; a failed write leaves text in the editor.
            _untrackedRecovery = false;
            return copied;
        }
        catch { return false; }
        finally { NotifyAvailabilityChanged(); }
    }
    public void SetText(string text) => _ = TrySetText(text);
    internal async Task<bool> WaitForCopyAdmissionAsync(CancellationToken ct = default)
    {
        if (!IsAvailable) return false;
        try { return await Operations.WaitForCopyAdmissionAsync(ct) && IsAvailable; }
        catch { return false; }
    }

    internal bool TryFocusOwnedPopup()
    {
        if (!IsIdle || _captured is null || !Allows())
        { AppLog.Info("app_popup_focus", "reason=denied"); return false; }
        try
        {
            var focused = Operations.TryFocusOwnedPopup();
            AppLog.Info("app_popup_focus", focused ? "reason=accepted" : "reason=refused");
            return focused;
        }
        catch { AppLog.Info("app_popup_focus", "reason=failed"); return false; }
    }

    internal InsertionTarget? Capture(nint window = 0)
    {
        if (_active is not null) return null;
        ClearCapturedTarget();
        if (HasRecovery || !Allows()) return null;
        try
        {
            var target = window == 0 ? Operations.CaptureForeground() : Operations.CaptureFrom(window);
            if (target is null || !Allows()) return null;
            _captured = target;
            NotifyAvailabilityChanged();
            return target;
        }
        catch { return null; }
    }

    public async Task<InsertionResult> InsertAsync(InsertionTarget target, long snippetId,
        InsertionOptions options = default, CancellationToken ct = default)
    {
        if (_active is not null && target == _captured && Allows())
            return new(InsertionOutcome.Busy, "An insertion is already in progress.");
        if (!CanInsert || target != _captured || _insertion is null)
            return new(InsertionOutcome.PasteFailed, _closed ? "Smart Snippets is shutting down." : Unavailable);
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
        if (_daily is null) return !HasRecovery;
        // Adapter-owned clipboard/keyboard recovery can exist without a local snapshot.
        // Always drain the desktop adapter, including when no insertion was started.
        // This is recovery of already-owned state, not permission to begin a new mutation.
        // The adapter must verify clipboard ownership and retain its RAM backup on failure.
        try
        {
            if (!await Operations.DrainOwnedRecoveryAsync()) return false;
            _snapshots.Clear(); _untrackedRecovery = false;
            NotifyAvailabilityChanged();
            return true;
        }
        catch { return false; }
    }

    TargetCheck ITargetProbe.Check(InsertionTarget target) => target == _activeTarget && AllowsInsert()
        ? Operations.Check(target) : TargetCheck.Closed;
    bool ITargetProbe.IsTargetForeground(InsertionTarget target) => target == _activeTarget && AllowsInsert()
        && Operations.IsTargetForeground(target);
    async Task<FocusPath?> IPasteDispatcher.RestoreFocusAsync(InsertionTarget target, CancellationToken ct)
    {
        if (target != _activeTarget || !AllowsInsert()) return null;
        var result = await Operations.RestoreFocusAsync(target, ct);
        return AllowsInsert() ? result : null;
    }
    bool IPasteDispatcher.ReleaseHeldModifiers() => AllowsInsert()
        && Operations.ReleaseHeldModifiers();
    ClipboardSwap IPasteDispatcher.TrySwapClipboard(string text, bool allowIncomplete)
    {
        if (!AllowsInsert()) return new(SwapStatus.Failed, null);
        try
        {
            var swap = Operations.TrySwapClipboard(text, allowIncomplete);
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
            return swap.Status == SwapStatus.NotPreserved && !AllowsInsert()
                ? swap with { Status = SwapStatus.Failed } : swap;
        }
        catch (ClipboardSwapException ex) { _snapshots.Add(ex.Snapshot); NotifyAvailabilityChanged(); throw; }
        catch { _untrackedRecovery = true; NotifyAvailabilityChanged(); throw; }
    }
    SwapStatus IPasteDispatcher.TryPlaceText(string text) => AllowsInsert()
        ? Operations.TryPlaceText(text) : SwapStatus.Failed;
    DispatchState IPasteDispatcher.SendPaste()
    {
        try
        {
            if (!AllowsInsert()) return DispatchState.NotDispatched;
            // Keep the final native HWND/PID check adjacent to dispatch.
            if (!Operations.IsTargetForeground(_activeTarget!)) return DispatchState.NotDispatched;
        }
        catch { return DispatchState.NotDispatched; }
        _pasteAttempted = true;
        return Operations.SendPaste();
    }
    bool IPasteDispatcher.ReleasePasteKeys() => _pasteAttempted && Operations.ReleasePasteKeys();
    RestoreStatus IPasteDispatcher.TryRestoreClipboard(IClipboardSnapshot snapshot)
    {
        if (!_snapshots.Contains(snapshot)) return RestoreStatus.Failed;
        var result = Operations.TryRestoreClipboard(snapshot);
        if (result == RestoreStatus.Restored) _snapshots.Remove(snapshot);
        NotifyAvailabilityChanged();
        return result;
    }
    void IPasteDispatcher.DiscardClipboardSnapshot(IClipboardSnapshot snapshot)
    {
        if (!_snapshots.Contains(snapshot) || !AllowsInsert())
            throw new InvalidOperationException("Clipboard discard requires an active insertion and owned snapshot.");
        Operations.DiscardClipboardSnapshot(snapshot);
        _snapshots.Remove(snapshot);
        NotifyAvailabilityChanged();
    }
}
