namespace SmartSnippets.Presentation.Services;

public enum TargetCheck { Ok, Closed, OwnProcess, Elevated }

public interface ITargetProbe
{
    /// <summary>Checks window existence and PID, then own process and elevation, in that order.</summary>
    TargetCheck Check(InsertionTarget target);
    /// <summary>Checks both the foreground HWND and its PID against the captured target.</summary>
    bool IsTargetForeground(InsertionTarget target);
}

public enum FocusPath { Direct, AttachThreadInput }
public enum SwapStatus { Swapped, Busy, Failed, NotPreserved }
public enum RestoreStatus { Restored, Partial, Busy, ClipboardChanged, Failed }
public enum DispatchState { NotDispatched, Dispatched, Uncertain }

/// <summary>Opaque clipboard snapshot. Unsaved includes unreadable formats and formats beyond the 64 MB cap; synthesized, owner-display and private handle formats are skipped by design. HGLOBAL and enhanced metafile data must be saved.</summary>
public interface IClipboardSnapshot
{
    int SavedCount { get; }
    int SkippedByDesignCount { get; }
    int UnsavedCount { get; }
    bool CapExceeded { get; }
    long ByteCount { get; }
    bool IsComplete => UnsavedCount == 0;
}

/// <summary>Snapshot is non-null exactly when the clipboard was emptied. NotPreserved never empties it.</summary>
public sealed record ClipboardSwap(SwapStatus Status, IClipboardSnapshot? Snapshot, int UnsavedCount = 0, bool CapExceeded = false);

/// <summary>When a dispatcher throws after emptying, it must transfer the snapshot with the exception.</summary>
public sealed class ClipboardSwapException(IClipboardSnapshot snapshot, Exception innerException)
    : Exception("Clipboard swap failed after emptying.", innerException)
{
    public IClipboardSnapshot Snapshot { get; } = snapshot;
}

public interface IPasteDispatcher
{
    Task<FocusPath?> RestoreFocusAsync(InsertionTarget target, CancellationToken ct);
    /// <summary>False means release was incomplete; do not mutate the clipboard or paste.</summary>
    bool ReleaseHeldModifiers();
    /// <summary>Snapshots before emptying; incomplete snapshots must not be emptied unless allowed. Any exception after emptying must carry the snapshot in <see cref="ClipboardSwapException"/>.</summary>
    ClipboardSwap TrySwapClipboard(string text, bool allowIncomplete);
    SwapStatus TryPlaceText(string text);
    /// <summary>Four injected events mean Dispatched, zero NotDispatched, and one to three Uncertain.</summary>
    DispatchState SendPaste();
    /// <summary>Attempts key-up cleanup and reports whether all events were injected.</summary>
    bool ReleasePasteKeys();
    RestoreStatus TryRestoreClipboard(IClipboardSnapshot snapshot);
    /// <summary>Discards a pending restore when the snippet is intentionally left for manual paste.</summary>
    void DiscardClipboardSnapshot(IClipboardSnapshot snapshot);
}

public sealed record InsertionTimings(TimeSpan RestoreDelay, int ClipboardAttempts, TimeSpan ClipboardRetryDelay)
{
    public static InsertionTimings Default { get; } = new(TimeSpan.FromMilliseconds(250), 10, TimeSpan.FromMilliseconds(20));
}
