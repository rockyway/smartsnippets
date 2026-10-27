namespace SmartSnippets.App.Services;

internal enum InsertOutcome
{
    Success,
    TargetClosed,
    TargetElevated,
    FocusFailed,
    ClipboardBusy,
    SendInputFailed,
}

internal sealed record InsertResult(InsertOutcome Outcome, string Message);

internal interface IInserter
{
    Task<InsertResult> InsertAsync(ForegroundTargetInfo target, string text, CancellationToken ct = default);
}
