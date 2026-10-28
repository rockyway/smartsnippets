using SmartSnippets.Presentation.Services;

namespace SmartSnippets.Presentation.Tests.Fakes;

internal sealed class FakePasteDispatcher(List<string> calls) : IPasteDispatcher
{
    private sealed record Snapshot(int UnsavedCount) : IClipboardSnapshot
    {
        public int SavedCount => 1;
        public int SkippedByDesignCount => 0;
        public bool CapExceeded => false;
        public long ByteCount => 3;
    }
    public FocusPath? Focus { get; set; } = FocusPath.Direct;
    public DispatchState Dispatch { get; set; } = DispatchState.Dispatched;
    public Queue<SwapStatus> Swaps { get; } = new();
    public Queue<RestoreStatus> Restores { get; } = new();
    public SwapStatus Place { get; set; } = SwapStatus.Swapped;
    public int Unsaved { get; set; }
    public bool Emptied { get; private set; }
    public string? Text { get; private set; }
    public bool AllowIncomplete { get; private set; }
    public Action? OnSwap { get; set; }
    public Action? OnSend { get; set; }
    public bool ThrowSend { get; set; }
    public bool FailAfterEmpty { get; set; }
    public Task<FocusPath?> RestoreFocusAsync(InsertionTarget target, CancellationToken ct) { calls.Add("focus"); return Task.FromResult(Focus); }
    public void ReleaseHeldModifiers() => calls.Add("modifiers");
    public ClipboardSwap TrySwapClipboard(string text, bool allowIncomplete)
    {
        calls.Add("swap"); Text = text; AllowIncomplete = allowIncomplete;
        OnSwap?.Invoke();
        var status = Swaps.Count > 0 ? Swaps.Dequeue() : SwapStatus.Swapped;
        if (status == SwapStatus.Busy) return new(status, null);
        if (Unsaved > 0 && !allowIncomplete) return new(SwapStatus.NotPreserved, null, Unsaved);
        Emptied = true;
        return new(FailAfterEmpty ? SwapStatus.Failed : status, new Snapshot(Unsaved));
    }
    public SwapStatus TryPlaceText(string text) { calls.Add("place"); Text = text; return Place; }
    public DispatchState SendPaste() { calls.Add("send"); OnSend?.Invoke(); if (ThrowSend) throw new InvalidOperationException(); return Dispatch; }
    public void ReleasePasteKeys() => calls.Add("release");
    public RestoreStatus TryRestoreClipboard(IClipboardSnapshot snapshot) { calls.Add("restore"); return Restores.Count > 0 ? Restores.Dequeue() : RestoreStatus.Restored; }
}
