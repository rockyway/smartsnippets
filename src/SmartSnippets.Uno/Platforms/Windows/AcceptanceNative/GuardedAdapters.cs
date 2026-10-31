using System;
using System.Threading;
using System.Threading.Tasks;
using SmartSnippets.Presentation.Services;
using SmartSnippets.TargetIdentity018;

namespace SmartSnippets.Uno.Platforms.Windows.AcceptanceNative;

internal enum AcceptanceOperation { Capture, Insert, Copy, Shortcut, Focus, Input, Clipboard }

internal interface IGuardianChannel : IDisposable
{
    string RecoveryControl { get; }
    bool IsPinned { get; }
    bool RecoveryPending { get; }
    bool CanPublish { get; }
    bool SnapshotAcknowledged { get; }
    int SavedCount { get; }
    long ByteCount { get; }
    SwapStatus Publish(AcceptanceOperation operation);
    bool CommitCopy();
    RestoreStatus Restore();
    void CloseAdmission();
}

internal interface IAcceptanceInput
{
    bool Quiet(ITargetProbe probe, InsertionTarget target);
    DispatchState SendPaste();
}

/// <summary>No focus transfer, foreign enumeration, modifier releases, manual-paste or clipboard-loss path.</summary>
internal sealed class GuardedPasteDispatcher(ITargetProbe probe, InsertionTarget exactTarget,
    Func<AcceptanceOperation, bool> permitted, IGuardianChannel guardian, IAcceptanceInput input,
    IAcceptanceFocus? focus = null) : IPasteDispatcher, IClipboardService
{
    private readonly RecoverySnapshot snapshot = new(guardian);
    private bool closed, attempted, swapped;
    internal bool KeyboardRecoveryPending { get; private set; }
    internal bool RecoveryPending => guardian.RecoveryPending || KeyboardRecoveryPending;
    private bool Allowed(AcceptanceOperation operation) => !closed && !KeyboardRecoveryPending && guardian.IsPinned &&
        permitted(operation) && probe.Check(exactTarget) == TargetCheck.Ok;
    private bool Quiet(AcceptanceOperation operation) => Allowed(operation) && input.Quiet(probe, exactTarget) && Allowed(operation);

    public async Task<FocusPath?> RestoreFocusAsync(InsertionTarget target, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (target != exactTarget || !Allowed(AcceptanceOperation.Insert) || !permitted(AcceptanceOperation.Focus)) return null;
        if (focus is not null)
        {
            if (!await focus.RestoreTargetAsync(ct)) return null;
        }
        else await Task.Yield();
        ct.ThrowIfCancellationRequested();
        return permitted(AcceptanceOperation.Focus) && Quiet(AcceptanceOperation.Insert) ? FocusPath.Direct : null;
    }
    public bool ReleaseHeldModifiers() => permitted(AcceptanceOperation.Input) && Quiet(AcceptanceOperation.Insert);
    public ClipboardSwap TrySwapClipboard(string text, bool allowIncomplete)
    {
        if (allowIncomplete || text != GuardianClipboard.SyntheticText || swapped || !guardian.CanPublish || guardian.RecoveryPending ||
            !permitted(AcceptanceOperation.Clipboard) || !Quiet(AcceptanceOperation.Insert))
            return new(SwapStatus.NotPreserved, null);
        try
        {
            var result = guardian.Publish(AcceptanceOperation.Insert);
            swapped = result == SwapStatus.Swapped;
            return new(result, guardian.RecoveryPending ? snapshot : null);
        }
        catch (Exception error)
        {
            // Protocol loss is conservatively pending: the helper owns recovery, not this token.
            throw new ClipboardSwapException(snapshot, error);
        }
    }
    public SwapStatus TryPlaceText(string text) => SwapStatus.NotPreserved;
    public DispatchState SendPaste()
    {
        if (!swapped || attempted || !permitted(AcceptanceOperation.Input) || !Quiet(AcceptanceOperation.Insert) || !probe.IsTargetForeground(exactTarget)) return DispatchState.NotDispatched;
        attempted = true;
        KeyboardRecoveryPending = true;
        var state = input.SendPaste();
        // Zero proves no event was delivered; partial or throwing native calls remain pending.
        KeyboardRecoveryPending = state == DispatchState.Uncertain;
        return state;
    }
    public bool ReleasePasteKeys() => !KeyboardRecoveryPending; // NEVER synthesize unowned key-ups.
    public RestoreStatus TryRestoreClipboard(IClipboardSnapshot saved) => ReferenceEquals(saved, snapshot) ? guardian.Restore() : RestoreStatus.Failed;
    public void DiscardClipboardSnapshot(IClipboardSnapshot saved) => throw new InvalidOperationException("Clipboard loss is prohibited.");
    private bool CopyReady() => Allowed(AcceptanceOperation.Copy) &&
        (focus?.ReadyForAdmission() ?? input.Quiet(probe, exactTarget)) && Allowed(AcceptanceOperation.Copy);
    public bool IsAvailable => guardian.CanPublish && !guardian.RecoveryPending && !swapped && CopyReady();
    public void SetText(string text) => _ = TrySetText(text);
    public bool TrySetText(string text)
    {
        if (text != GuardianClipboard.SyntheticText || !IsAvailable) return false;
        try
        {
            if (guardian.Publish(AcceptanceOperation.Copy) != SwapStatus.Swapped) return false;
            // Separate Copy permission is checked again at commit; Insert permission is irrelevant.
            return CopyReady() && guardian.CommitCopy();
        }
        catch (Exception) { return false; }
        finally
        {
            // Failed cleanup remains owned by the independent guardian; Copy never throws or claims success.
            try { if (guardian.RecoveryPending) _ = guardian.Restore(); }
            catch (Exception) { }
        }
    }
    internal RestoreStatus CloseAdmissionAndRestore()
    {
        closed = true;
        guardian.CloseAdmission();
        return guardian.RecoveryPending ? guardian.Restore() : RestoreStatus.Restored;
    }
    private sealed class RecoverySnapshot(IGuardianChannel channel) : IClipboardSnapshot
    {
        // Only numeric preservation metadata crosses the process boundary, never original bytes.
        // A lost acknowledgement is an unknown backup, not a fabricated complete snapshot.
        public int SavedCount => channel.SavedCount;
        public int SkippedByDesignCount => 0;
        public int UnsavedCount => channel.SnapshotAcknowledged ? 0 : 1;
        public bool CapExceeded => false;
        public long ByteCount => channel.ByteCount;
    }
}

internal sealed class GuardedTargetProbe(OwnedTargetProbe owned, Func<AcceptanceOperation, bool> permitted) : ITargetProbe
{
    internal InsertionTarget Capture(string fixtureWindow)
    {
        if (!permitted(AcceptanceOperation.Capture)) throw new InvalidOperationException("Capture is unavailable.");
        var target = owned.Capture(fixtureWindow); // Retained direct-child A/B identities only, never foreground enumeration.
        if (!permitted(AcceptanceOperation.Capture) || owned.Check(target) != TargetCheck.Ok)
            throw new InvalidOperationException("Owned target is unavailable.");
        return target;
    }
    public TargetCheck Check(InsertionTarget target) => permitted(AcceptanceOperation.Insert) || permitted(AcceptanceOperation.Copy) || permitted(AcceptanceOperation.Capture)
        ? owned.Check(target) : TargetCheck.Closed;
    public bool IsTargetForeground(InsertionTarget target) => Check(target) == TargetCheck.Ok && owned.IsTargetForeground(target);
}
