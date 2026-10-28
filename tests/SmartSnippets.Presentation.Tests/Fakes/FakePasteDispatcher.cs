using System.Text;
using SmartSnippets.Presentation.Services;

namespace SmartSnippets.Presentation.Tests.Fakes;

internal sealed class FakePasteDispatcher(List<string> calls) : IPasteDispatcher
{
    private sealed record Snapshot(Dictionary<string, byte[]> Formats, int UnsavedCount, bool CapExceeded) : IClipboardSnapshot
    {
        public int SavedCount => Formats.Count;
        public int SkippedByDesignCount => 0;
        public long ByteCount => Formats.Values.Sum(value => (long)value.Length);
    }
    public Dictionary<string, byte[]> Clipboard { get; } = new() { ["text"] = [1, 2], ["html"] = [3, 4, 5], ["image"] = [6, 7, 8, 9] };
    public IClipboardSnapshot? Pending { get; private set; }
    public IClipboardSnapshot? LastSnapshot { get; private set; }
    public IClipboardSnapshot? RestoredSnapshot { get; private set; }
    public IClipboardSnapshot? DiscardedSnapshot { get; private set; }
    public List<InsertionTarget> FocusTargets { get; } = [];
    public FocusPath? Focus { get; set; } = FocusPath.Direct;
    public Func<Task<FocusPath?>>? FocusOverride { get; set; }
    public DispatchState Dispatch { get; set; } = DispatchState.Dispatched;
    public Queue<SwapStatus> Swaps { get; } = new();
    public Queue<RestoreStatus> Restores { get; } = new();
    public SwapStatus Place { get; set; } = SwapStatus.Swapped;
    public int Unsaved { get; set; }
    public bool CapExceeded { get; set; }
    public bool Emptied { get; private set; }
    public string? Text { get; private set; }
    public bool AllowIncomplete { get; private set; }
    public Action? OnSwap { get; set; }
    public Action? OnSend { get; set; }
    public bool ThrowSend { get; set; }
    public bool FailAfterEmpty { get; set; }
    public Task<FocusPath?> RestoreFocusAsync(InsertionTarget target, CancellationToken ct) { calls.Add("focus"); FocusTargets.Add(target); return FocusOverride?.Invoke() ?? Task.FromResult(Focus); }
    public bool ModifiersReleased { get; set; } = true;
    public bool PasteKeysReleased { get; set; } = true;
    public bool ReleaseHeldModifiers() { calls.Add("modifiers"); return ModifiersReleased; }
    public ClipboardSwap TrySwapClipboard(string text, bool allowIncomplete)
    {
        calls.Add("swap"); Text = text; AllowIncomplete = allowIncomplete;
        OnSwap?.Invoke();
        var status = Swaps.Count > 0 ? Swaps.Dequeue() : SwapStatus.Swapped;
        if (status == SwapStatus.Busy) return new(status, null);
        if (Unsaved > 0 && !allowIncomplete) return new(SwapStatus.NotPreserved, null, Unsaved, CapExceeded);
        var formats = Clipboard.Take(Math.Max(0, Clipboard.Count - Unsaved)).ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
        var snapshot = new Snapshot(formats, Unsaved, CapExceeded);
        LastSnapshot = Pending = snapshot;
        Emptied = true;
        Clipboard.Clear();
        Clipboard["snippet"] = Encoding.UTF8.GetBytes(text);
        return new(FailAfterEmpty ? SwapStatus.Failed : status, snapshot);
    }
    public SwapStatus TryPlaceText(string text)
    {
        calls.Add("place"); Text = text;
        if (Place == SwapStatus.Swapped) { Clipboard.Clear(); Clipboard["snippet"] = Encoding.UTF8.GetBytes(text); }
        return Place;
    }
    public DispatchState SendPaste() { calls.Add("send"); OnSend?.Invoke(); if (ThrowSend) throw new InvalidOperationException(); return Dispatch; }
    public bool ReleasePasteKeys() { calls.Add("release"); return PasteKeysReleased; }
    public RestoreStatus TryRestoreClipboard(IClipboardSnapshot snapshot)
    {
        calls.Add("restore");
        if (!ReferenceEquals(snapshot, Pending)) throw new InvalidOperationException("Wrong snapshot");
        RestoredSnapshot = snapshot;
        var status = Restores.Count > 0 ? Restores.Dequeue() : RestoreStatus.Restored;
        if (status == RestoreStatus.Restored)
        {
            Clipboard.Clear();
            foreach (var pair in ((Snapshot)snapshot).Formats) Clipboard.Add(pair.Key, pair.Value.ToArray());
            Pending = null;
        }
        else if (status != RestoreStatus.Busy) Pending = null;
        return status;
    }
    public void DiscardClipboardSnapshot(IClipboardSnapshot snapshot)
    {
        calls.Add("discard");
        if (!ReferenceEquals(snapshot, Pending)) throw new InvalidOperationException("Wrong snapshot");
        DiscardedSnapshot = snapshot;
        Pending = null;
    }
}
