using SmartSnippets.Presentation.Services;
using SmartSnippets.Uno.Platforms.Windows;

namespace SmartSnippets.Uno;

/// <summary>Capability exists before host startup; binding a session is not admission.</summary>
internal sealed class LateNativeOperations : ICandidateNativeOperations
{
    private ICandidateNativeOperations? session;
    private Guid sessionId;
    private bool admitted;
    internal int PasteCalls { get; private set; }
    internal int CopyCommits { get; private set; }
    internal int CaptureCalls { get; private set; }
    internal void Bind(ICandidateNativeOperations value, Guid id)
    {
        if (session is not null) throw new InvalidOperationException("native_rebind");
        session = value; sessionId = id;
    }
    internal void Admit() { if (session is null) throw new InvalidOperationException("native_unbound"); admitted = true; }
    internal void Close() => admitted = false;
    public Task<bool> WaitForCopyAdmissionAsync(CancellationToken ct) => admitted ? session!.WaitForCopyAdmissionAsync(ct) : Task.FromResult(false);
    public bool TryFocusOwnedPopup() => admitted && session!.TryFocusOwnedPopup();
    public bool Validate(CandidateNativeOperation operation, Guid id, nint window, uint pid) => admitted && session!.Validate(operation, sessionId, window, pid);
    public InsertionTarget? CaptureOwnedTarget(nint window, uint pid)
    {
        if (!admitted) return null;
        CaptureCalls++;
        return session!.CaptureOwnedTarget(window, pid);
    }
    public TargetCheck Check(InsertionTarget target) => admitted ? session!.Check(target) : TargetCheck.Closed;
    public bool IsTargetForeground(InsertionTarget target) => admitted && session!.IsTargetForeground(target);
    public Task<FocusPath?> RestoreFocusAsync(InsertionTarget target, CancellationToken ct) => admitted ? session!.RestoreFocusAsync(target, ct) : Task.FromResult<FocusPath?>(null);
    public bool ReleaseHeldModifiers() => admitted && session!.ReleaseHeldModifiers();
    public ClipboardSwap TrySwapClipboard(string text, bool allowIncomplete) => admitted ? session!.TrySwapClipboard(text, allowIncomplete) : new(SwapStatus.NotPreserved, null);
    public SwapStatus TryPlaceText(string text) => SwapStatus.NotPreserved;
    public DispatchState SendPaste()
    {
        if (!admitted) return DispatchState.NotDispatched;
        PasteCalls++;
        return session!.SendPaste();
    }
    public bool ReleasePasteKeys() => admitted && session!.ReleasePasteKeys();
    // Recovery remains reachable after revocation; never discard an obligation.
    public RestoreStatus TryRestoreClipboard(IClipboardSnapshot snapshot) => session?.TryRestoreClipboard(snapshot) ?? RestoreStatus.Failed;
    public void DiscardClipboardSnapshot(IClipboardSnapshot snapshot) => throw new InvalidOperationException("native_loss_denied");
    public bool IsAvailable => admitted && session!.IsAvailable;
    public bool TrySetText(string text)
    {
        if (!admitted || !session!.TrySetText(text)) return false;
        CopyCommits++;
        return true;
    }
    public void SetText(string text) => _ = TrySetText(text);
    public Task<bool> DrainOwnedRecoveryAsync() { Close(); return session?.DrainOwnedRecoveryAsync() ?? Task.FromResult(true); }
}
