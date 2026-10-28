namespace SmartSnippets.Presentation.Services;

/// <summary>The window a snippet is pasted into, captured when the popup opens. <see cref="DisplayName"/> can contain a window title, so it is never logged.</summary>
public sealed record InsertionTarget(nint WindowHandle, uint ProcessId, nint MonitorHandle, string DisplayName);

public enum InsertionOutcome
{
    Inserted,              // dispatched; usage counted; Message empty, or a notice (e.g. clipboard not fully restored)
    TargetClosed, TargetElevated, FocusFailed, ClipboardBusy, PasteFailed, Busy, SnippetMissing, // not dispatched; nothing changed
    ClipboardNotPreserved, // not dispatched; clipboard untouched; caller may retry with AllowClipboardLoss
    MaybePasted,           // uncertain dispatch; no usage; do not offer retry
    CopiedForManualPaste,  // AllowClipboardLoss path could not re-target; snippet left on clipboard; no usage
}

public sealed record InsertionResult(InsertionOutcome Outcome, string Message)
{
    public bool Succeeded => Outcome == InsertionOutcome.Inserted;
}

public readonly record struct InsertionOptions(bool AllowClipboardLoss);

public interface IInsertionService
{
    /// <summary>
    /// Resolves the snippet text from the store, normalizes line endings to CRLF, dispatches the paste,
    /// and records usage only when Outcome == Inserted. Single-flight: a concurrent call returns Busy with no side effects.
    /// </summary>
    Task<InsertionResult> InsertAsync(InsertionTarget target, long snippetId, InsertionOptions options = default, CancellationToken ct = default);
}
