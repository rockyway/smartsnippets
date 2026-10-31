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
    private bool _closed, _suspended, _pasteAttempted;

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
    internal bool CanInsert => _captured is not null && Allows(CandidateNativeOperation.Insert);
    internal bool IsIdle => _active is null;
    public bool IsAvailable => _active is null && _snapshots.Count == 0 && Allows(CandidateNativeOperation.Copy);
    public bool TrySetText(string text)
    {
        if (!IsAvailable) return false;
        try { return _capability!.Operations.TrySetText(text); }
        catch { return false; }
    }
    public void SetText(string text) => _ = TrySetText(text);

    internal InsertionTarget? Capture()
    {
        if (_active is not null || !Allows(CandidateNativeOperation.Capture)) return null;
        _captured = null;
        try
        {
            var target = _capability!.Operations.CaptureOwnedTarget(_capability.Window, _capability.ProcessId);
            if (target is null || !_capability.Owns(target) || !Allows(CandidateNativeOperation.Capture)) return null;
            return _captured = target;
        }
        catch { return null; }
    }

    public async Task<InsertionResult> InsertAsync(InsertionTarget target, long snippetId,
        InsertionOptions options = default, CancellationToken ct = default)
    {
        if (!CanInsert || target != _captured || _insertion is null
            || (options.AllowClipboardLoss && !Allows(CandidateNativeOperation.ClipboardLoss)))
            return new(InsertionOutcome.PasteFailed, _closed ? "The candidate is shutting down." : Unavailable);
        if (_active is not null) return new(InsertionOutcome.Busy, "An insertion is already in progress.");
        if (_snapshots.Count != 0) return new(InsertionOutcome.PasteFailed, Unavailable);
        var active = new TaskCompletionSource();
        _active = active; _activeTarget = target; _pasteAttempted = false;
        try { return await _insertion.InsertAsync(target, snippetId, options, ct); }
        finally { _activeTarget = null; _active = null; active.TrySetResult(); }
    }

    internal void SuspendAdmission() => _suspended = true;
    internal void ResumeAdmission() { if (!_closed) _suspended = false; }
    internal void CloseAdmission()
    {
        if (_closed) return;
        _closed = true;
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
        if (_snapshots.Count == 0) return true;
        // This is recovery of already-owned state, not permission to begin a new mutation.
        // The adapter must verify clipboard ownership and retain its RAM backup on failure.
        try
        {
            if (!await _capability!.Operations.DrainOwnedRecoveryAsync()) return false;
            _snapshots.Clear();
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
            return swap;
        }
        catch (ClipboardSwapException ex) { _snapshots.Add(ex.Snapshot); throw; }
    }
    SwapStatus IPasteDispatcher.TryPlaceText(string text) => AllowsInsert(CandidateNativeOperation.Clipboard | CandidateNativeOperation.ClipboardLoss)
        ? _capability!.Operations.TryPlaceText(text) : SwapStatus.Failed;
    DispatchState IPasteDispatcher.SendPaste()
    {
        if (!AllowsInsert(CandidateNativeOperation.Input)) return DispatchState.NotDispatched;
        // Authorization checks must not separate the final native HWND/PID check from dispatch.
        if (!_capability!.Operations.IsTargetForeground(_activeTarget!)) return DispatchState.NotDispatched;
        _pasteAttempted = true;
        return _capability.Operations.SendPaste();
    }
    bool IPasteDispatcher.ReleasePasteKeys() => _pasteAttempted && _capability!.Operations.ReleasePasteKeys();
    RestoreStatus IPasteDispatcher.TryRestoreClipboard(IClipboardSnapshot snapshot)
    {
        if (!_snapshots.Contains(snapshot)) return RestoreStatus.Failed;
        var result = _capability!.Operations.TryRestoreClipboard(snapshot);
        if (result is RestoreStatus.Restored or RestoreStatus.ClipboardChanged) _snapshots.Remove(snapshot);
        return result;
    }
    void IPasteDispatcher.DiscardClipboardSnapshot(IClipboardSnapshot snapshot)
    {
        if (!_snapshots.Contains(snapshot) || !AllowsInsert(CandidateNativeOperation.ClipboardLoss))
            throw new InvalidOperationException("Clipboard discard is not authorized.");
        _capability!.Operations.DiscardClipboardSnapshot(snapshot);
        _snapshots.Remove(snapshot);
    }
}
