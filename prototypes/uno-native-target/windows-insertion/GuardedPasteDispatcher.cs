using SmartSnippets.App.Services;
using SmartSnippets.Presentation.Services;
using Windows.Win32.Foundation;

namespace SmartSnippets.WindowsInsertion018;

internal sealed class GuardedPasteDispatcher : IPasteDispatcher
{
    private readonly ClipboardPasteDispatcher native;
    private readonly GuardianPin guardian;
    private readonly ITargetProbe probe;
    private readonly InsertionTarget target;
    private readonly nint originalOwner;
    private readonly uint originalSequence;
    private readonly OwnClipboardPublication publication;
    private readonly Action afterSwap;
    private bool nativeInputAttempted;
    internal bool KeyboardRecoveryPending { get; private set; }
    internal int DispatchCalls { get; private set; }

    internal GuardedPasteDispatcher(GuardianPin guardian, ITargetProbe probe, InsertionTarget target,
        nint originalOwner, uint originalSequence, OwnClipboardPublication publication, Action afterSwap)
    {
        this.guardian = guardian;
        this.probe = probe;
        this.target = target;
        this.originalOwner = originalOwner;
        this.originalSequence = originalSequence;
        this.publication = publication;
        this.afterSwap = afterSwap;
        native = new ClipboardPasteDispatcher(new HWND(guardian.Window));
    }
    public async Task<FocusPath?> RestoreFocusAsync(InsertionTarget captured, CancellationToken ct)
    {
        if (captured != target || probe.Check(target) != TargetCheck.Ok || !guardian.IsPinned()) return null;
        await Task.Yield(); // Duplicate insertion must observe Busy before any mutation.
        await SmartSnippets.TargetIdentity018.FocusPreflight.RequireAsync(ct);
        return guardian.IsPinned() && InputPreflight.QuietOwnedTarget(probe, target) ? FocusPath.Direct : null;
    }
    // Never call shipping ReleaseHeldModifiers: held physical input is a refusal,
    // not permission to synthesize unowned modifier releases.
    public bool ReleaseHeldModifiers() => guardian.IsPinned() && InputPreflight.QuietOwnedTarget(probe, target);
    public ClipboardSwap TrySwapClipboard(string text, bool allowIncomplete)
    {
        if (allowIncomplete || text != OwnClipboardPublication.Multiline) return new(SwapStatus.NotPreserved, null);
        var swap = native.TryGuardedSwap(text, guardian.IsPinned, originalOwner, originalSequence, publication);
        if (swap.Status == SwapStatus.Swapped) afterSwap();
        return swap;
    }
    public SwapStatus TryPlaceText(string text) => SwapStatus.NotPreserved; // No loss/manual-paste fallback in this fixture.
    public DispatchState SendPaste()
    {
        DispatchCalls++;
        if (!guardian.IsPinned() || !InputPreflight.QuietOwnedTarget(probe, target)) return DispatchState.NotDispatched;
        nativeInputAttempted = true;
        KeyboardRecoveryPending = true;
        var state = native.SendPaste();
        // Count alone is not proof of WHICH events were accepted. Do not infer a
        // prefix ledger and send key-ups which might release simultaneous physical
        // input. Partial native delivery is an explicit unrecovered keyboard state.
        KeyboardRecoveryPending = state == DispatchState.Uncertain;
        return state;
    }
    public bool ReleasePasteKeys() => !nativeInputAttempted; // Injected-uncertain tests have no native downs.
    public RestoreStatus TryRestoreClipboard(IClipboardSnapshot snapshot) => native.TryGuardedRestore(snapshot, guardian.IsPinned, publication);
    public void DiscardClipboardSnapshot(IClipboardSnapshot snapshot) => throw new InvalidOperationException("Clipboard loss is prohibited.");
}
