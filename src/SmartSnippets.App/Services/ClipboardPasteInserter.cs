using SmartSnippets.Core.Domain;
using SmartSnippets.Core.Logging;
using SmartSnippets.Presentation.Services;

namespace SmartSnippets.App.Services;

// Temporary prototype bridge until 006 replaces the popup. Reuses the same safety boundary as
// production; it never grants clipboard-loss consent and never maintains a second Win32 path.
internal sealed class ClipboardPasteInserter(ClipboardPasteDispatcher dispatcher) : IInserter
{
    private readonly Win32TargetProbe _probe = new();
    private readonly InsertionTimings _timings = InsertionTimings.Default;

    public async Task<InsertResult> InsertAsync(ForegroundTargetInfo captured, string text, CancellationToken ct = default)
    {
        var target = ToTarget(captured);
        IClipboardSnapshot? snapshot = null;
        var delayRestore = false;
        var result = new InsertResult(InsertOutcome.SendInputFailed, "Couldn't paste. Nothing was pasted.");
        try
        {
            ct.ThrowIfCancellationRequested();
            var check = _probe.Check(target);
            if (check != TargetCheck.Ok)
                return check switch
                {
                    TargetCheck.Closed => new(InsertOutcome.TargetClosed, "The target window was closed."),
                    TargetCheck.Elevated => new(InsertOutcome.TargetElevated, "The target app runs as administrator."),
                    _ => result,
                };
            if (await dispatcher.RestoreFocusAsync(target, ct) is null)
                return new(InsertOutcome.FocusFailed, "Could not return focus to the target window.");
            if (!dispatcher.ReleaseHeldModifiers()) return result;
            ClipboardSwap swap = new(SwapStatus.Busy, null);
            for (var attempt = 0; attempt < _timings.ClipboardAttempts; attempt++)
            {
                ct.ThrowIfCancellationRequested();
                try { swap = dispatcher.TrySwapClipboard(LineEndings.ToCrLf(text), false); }
                catch (ClipboardSwapException ex) { swap = new(SwapStatus.Failed, ex.Snapshot); }
                snapshot = swap.Snapshot;
                if (swap.Status != SwapStatus.Busy) break;
                if (attempt + 1 < _timings.ClipboardAttempts) await Task.Delay(_timings.ClipboardRetryDelay, ct);
            }
            if (swap.Status == SwapStatus.NotPreserved)
                result = new(InsertOutcome.SendInputFailed, "The clipboard can't be safely preserved. Nothing was pasted.");
            else if (swap.Status == SwapStatus.Busy)
                result = new(InsertOutcome.ClipboardBusy, "The clipboard is in use by another application.");
            else if (swap.Status == SwapStatus.Swapped)
            {
                ct.ThrowIfCancellationRequested();
                if (!_probe.IsTargetForeground(target)) result = new(InsertOutcome.FocusFailed, "Could not return focus to the target window.");
                else
                {
                    DispatchState state;
                    try { state = dispatcher.SendPaste(); }
                    catch (Exception) { state = DispatchState.Uncertain; }
                    if (state == DispatchState.Uncertain)
                    {
                        delayRestore = true;
                        result = new(InsertOutcome.SendInputFailed, "Smart Snippets may have pasted. Check before trying again.");
                        try { if (!dispatcher.ReleasePasteKeys()) AppLog.Info("paste_key_release_failed"); }
                        catch (Exception ex) { AppLog.Info("paste_key_release_failed", $"kind={ex.GetType().Name}"); }
                    }
                    else if (state == DispatchState.Dispatched)
                    {
                        delayRestore = true;
                        result = new(InsertOutcome.Success, "Inserted.");
                    }
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) { AppLog.Info("prototype_insert_failed", $"kind={ex.GetType().Name}"); }
        finally
        {
            if (snapshot is not null)
            {
                if (delayRestore) await Task.Delay(_timings.RestoreDelay);
                var (status, attempts) = await ClipboardSafety.RetryRestoreAsync(
                    () => dispatcher.TryRestoreClipboard(snapshot), _timings.ClipboardAttempts,
                    _timings.ClipboardRetryDelay, delay => Task.Delay(delay));
                AppLog.Info("clipboard_restore", $"status={status} attempts={attempts}");
                if (status is RestoreStatus.Busy or RestoreStatus.Failed or RestoreStatus.Partial)
                    result = result.Outcome == InsertOutcome.Success
                        ? new(InsertOutcome.SendInputFailed, "Pasted, but your previous clipboard couldn't be fully restored.")
                        : result with { Message = result.Message + " Your previous clipboard couldn't be fully restored." };
            }
        }
        return result;
    }

    private static unsafe InsertionTarget ToTarget(ForegroundTargetInfo target) =>
        new((nint)target.Hwnd.Value, target.ProcessId, (nint)target.Monitor.Value, "the target app");
}
