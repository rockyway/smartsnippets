using SmartSnippets.Presentation.Services;

namespace SmartSnippets.Uno.Platforms.Windows;

/// <summary>Closed candidate boundary. No native adapter or runtime authorization exists in this checkpoint.</summary>
internal sealed class CandidateNativeGate : IClipboardService, IInsertionService
{
    internal const string Unavailable = "Clipboard and insertion are unavailable in the Uno candidate. Use management or target-free preview.";
    private bool _closed;
    public bool IsAvailable => false;
    public bool TrySetText(string text) => false;
    public void SetText(string text) => _ = TrySetText(text);
    public Task<InsertionResult> InsertAsync(InsertionTarget target, long snippetId,
        InsertionOptions options = default, CancellationToken ct = default) => Task.FromResult(
            new InsertionResult(InsertionOutcome.PasteFailed, _closed ? "The candidate is shutting down." : Unavailable));
    internal void CloseAdmission() => _closed = true;
}
