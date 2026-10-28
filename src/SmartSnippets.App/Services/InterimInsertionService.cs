using SmartSnippets.Core.Logging;
using SmartSnippets.Core.Store;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Presentation.Text;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

namespace SmartSnippets.App.Services;

// Deleted by plan 007.
internal sealed class InterimInsertionService(ISnippetStore store, IInserter inserter) : IInsertionService
{
    private int _busy;

    public async Task<InsertionResult> InsertAsync(InsertionTarget target, long snippetId, InsertionOptions options = default, CancellationToken ct = default)
    {
        if (Interlocked.Exchange(ref _busy, 1) != 0) return new(InsertionOutcome.Busy, string.Empty);
        try
        {
            var snippet = store.Snippets.FirstOrDefault(v => v.Snippet.Id == snippetId)?.Snippet;
            if (snippet is null) return new(InsertionOutcome.SnippetMissing, "The snippet no longer exists.");
            var info = new ForegroundTargetInfo(new HWND(target.WindowHandle), target.ProcessId, new HMONITOR(target.MonitorHandle));
            var result = await inserter.InsertAsync(info, snippet.Text, ct);
            var outcome = result.Outcome switch
            {
                InsertOutcome.Success => InsertionOutcome.Inserted,
                InsertOutcome.TargetClosed => InsertionOutcome.TargetClosed,
                InsertOutcome.TargetElevated => InsertionOutcome.TargetElevated,
                InsertOutcome.FocusFailed => InsertionOutcome.FocusFailed,
                InsertOutcome.ClipboardBusy => InsertionOutcome.ClipboardBusy,
                _ => InsertionOutcome.MaybePasted,
            };
            if (outcome == InsertionOutcome.Inserted)
            {
                try { store.RecordUsage(snippetId); }
                catch (StoreException ex) { AppLog.Info("usage_record_failed", $"kind={ex.Failure}"); }
            }
            var message = outcome switch
            {
                InsertionOutcome.Inserted => string.Empty,
                InsertionOutcome.MaybePasted => UiText.MaybePastedFallback,
                _ => result.Message,
            };
            return new(outcome, message);
        }
        finally { Volatile.Write(ref _busy, 0); }
    }
}
