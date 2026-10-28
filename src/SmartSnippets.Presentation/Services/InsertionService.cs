using System.Diagnostics;
using SmartSnippets.Core.Domain;
using SmartSnippets.Core.Logging;
using SmartSnippets.Core.Store;
using SmartSnippets.Presentation.Text;

namespace SmartSnippets.Presentation.Services;

/// <summary>Coordinates a single paste without changing the stored snippet.</summary>
public sealed class InsertionService(
    ISnippetStore store, ITargetProbe probe, IPasteDispatcher dispatcher,
    InsertionTimings? timings = null, Func<TimeSpan, CancellationToken, Task>? delay = null) : IInsertionService
{
    private readonly InsertionTimings _timings = timings ?? InsertionTimings.Default;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay = delay ?? Task.Delay;
    private int _inserting;

    public async Task<InsertionResult> InsertAsync(InsertionTarget target, long snippetId, InsertionOptions options = default, CancellationToken ct = default)
    {
        if (Interlocked.Exchange(ref _inserting, 1) != 0)
            return Result(InsertionOutcome.Busy, target, false);

        var watch = Stopwatch.StartNew();
        var outcome = InsertionOutcome.PasteFailed;
        var notice = false;
        IClipboardSnapshot? snapshot = null;
        var waitBeforeRestore = false;
        var skipRestore = false;
        var cancelled = false;
        AppLog.Info("insert_start", $"id={snippetId} pid={target.ProcessId} allow_loss={options.AllowClipboardLoss}");
        try
        {
            var snippet = store.Snippets.FirstOrDefault(row => row.Snippet.Id == snippetId)?.Snippet;
            if (snippet is null)
            {
                outcome = InsertionOutcome.SnippetMissing;
            }
            else
            {
                var text = LineEndings.ToCrLf(snippet.Text);
                ct.ThrowIfCancellationRequested();
                var check = probe.Check(target);
                AppLog.Info("target_check", $"result={check}");
                if (check == TargetCheck.Closed && options.AllowClipboardLoss)
                    outcome = await PlaceForManualPaste(text, "Closed", ct);
                else if (check != TargetCheck.Ok)
                {
                    if (check == TargetCheck.OwnProcess) AppLog.Info("target_rejected", "reason=own_process");
                    outcome = check switch { TargetCheck.Closed => InsertionOutcome.TargetClosed, TargetCheck.Elevated => InsertionOutcome.TargetElevated, _ => InsertionOutcome.PasteFailed };
                }
                else
                {
                    ct.ThrowIfCancellationRequested();
                    var focus = await dispatcher.RestoreFocusAsync(target, ct);
                    AppLog.Info("focus_path", focus switch { FocusPath.Direct => "direct", FocusPath.AttachThreadInput => "attach_thread_input", _ => "failed" });
                    if (focus is null)
                        outcome = options.AllowClipboardLoss ? await PlaceForManualPaste(text, "FocusFailed", ct) : InsertionOutcome.FocusFailed;
                    else
                    {
                        dispatcher.ReleaseHeldModifiers();
                        ct.ThrowIfCancellationRequested();
                        ClipboardSwap swap = new(SwapStatus.Busy, null);
                        var attempts = 0;
                        var swapWatch = Stopwatch.StartNew();
                        for (; attempts < _timings.ClipboardAttempts;)
                        {
                            ct.ThrowIfCancellationRequested();
                            attempts++;
                            swap = dispatcher.TrySwapClipboard(text, options.AllowClipboardLoss);
                            snapshot = swap.Snapshot;
                            if (swap.Status != SwapStatus.Busy) break;
                            if (attempts < _timings.ClipboardAttempts) await _delay(_timings.ClipboardRetryDelay, ct);
                        }
                        AppLog.Info("clipboard_swap", $"attempts={attempts} status={swap.Status} saved={snapshot?.SavedCount ?? 0} skipped_by_design={snapshot?.SkippedByDesignCount ?? 0} unsaved={snapshot?.UnsavedCount ?? swap.UnsavedCount} cap_exceeded={snapshot?.CapExceeded ?? swap.CapExceeded} bytes={snapshot?.ByteCount ?? 0} ms={swapWatch.ElapsedMilliseconds}");
                        if (swap.Status != SwapStatus.Swapped)
                            outcome = swap.Status switch { SwapStatus.Busy => InsertionOutcome.ClipboardBusy, SwapStatus.NotPreserved => InsertionOutcome.ClipboardNotPreserved, _ => InsertionOutcome.PasteFailed };
                        else
                        {
                            ct.ThrowIfCancellationRequested();
                            // Keep the final foreground gate and SendPaste adjacent: no await or other call.
                            if (!probe.IsTargetForeground(target))
                            {
                                AppLog.Info("foreground_gate", "ok=False");
                                if (options.AllowClipboardLoss)
                                {
                                    if (snapshot is not null) dispatcher.DiscardClipboardSnapshot(snapshot);
                                    skipRestore = true;
                                    AppLog.Info("manual_paste_fallback", "reason=ForegroundLost status=Swapped");
                                    outcome = InsertionOutcome.CopiedForManualPaste;
                                }
                                else outcome = InsertionOutcome.FocusFailed;
                            }
                            else
                            {
                                DispatchState dispatch;
                                try { dispatch = dispatcher.SendPaste(); }
                                catch (Exception ex)
                                {
                                    AppLog.Info("paste_exception", $"kind={ex.GetType().Name}");
                                    dispatch = DispatchState.Uncertain;
                                }
                                AppLog.Info("foreground_gate", "ok=True");
                                if (dispatch == DispatchState.Uncertain)
                                {
                                    waitBeforeRestore = true;
                                    outcome = InsertionOutcome.MaybePasted;
                                    try { dispatcher.ReleasePasteKeys(); AppLog.Info("paste_keys_released"); }
                                    catch (Exception ex) { AppLog.Info("paste_key_release_failed", $"kind={ex.GetType().Name}"); }
                                }
                                else if (dispatch == DispatchState.NotDispatched) outcome = InsertionOutcome.PasteFailed;
                                else
                                {
                                    waitBeforeRestore = true;
                                    outcome = InsertionOutcome.Inserted;
                                    try
                                    {
                                        var usage = store.RecordUsage(snippetId);
                                        if (!usage.Success) AppLog.Info("usage_record_failed", $"id={snippetId} kind={usage.Error}");
                                    }
                                    catch (Exception ex) { AppLog.Info("usage_record_failed", $"id={snippetId} kind={(ex is StoreException storeException ? storeException.Failure.ToString() : ex.GetType().Name)}"); }
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            cancelled = true;
            throw;
        }
        catch (Exception ex)
        {
            AppLog.Info("insert_exception", $"kind={ex.GetType().Name}");
            outcome = InsertionOutcome.PasteFailed;
        }
        finally
        {
            try
            {
                if (snapshot is not null && !skipRestore)
                {
                    if (waitBeforeRestore)
                    {
                        try { await _delay(_timings.RestoreDelay, CancellationToken.None); }
                        catch (Exception ex) { AppLog.Info("restore_delay_failed", $"kind={ex.GetType().Name}"); }
                    }
                    var status = RestoreStatus.Busy;
                    var attempts = 0;
                    try
                    {
                        for (; attempts < _timings.ClipboardAttempts;)
                        {
                            attempts++;
                            status = dispatcher.TryRestoreClipboard(snapshot);
                            if (status != RestoreStatus.Busy) break;
                            if (attempts < _timings.ClipboardAttempts) await _delay(_timings.ClipboardRetryDelay, CancellationToken.None);
                        }
                    }
                    catch (Exception ex) { AppLog.Info("clipboard_restore_exception", $"kind={ex.GetType().Name}"); status = RestoreStatus.Failed; }
                    notice = status is RestoreStatus.Partial or RestoreStatus.Failed or RestoreStatus.Busy;
                    AppLog.Info("clipboard_restore", $"status={status} attempts={attempts}");
                }
                else if (skipRestore) AppLog.Info("clipboard_restore", "status=Skipped attempts=0");
            }
            finally
            {
                Interlocked.Exchange(ref _inserting, 0);
                AppLog.Info("insert_result", $"outcome={(cancelled ? "Cancelled" : outcome)} id={snippetId} notice={notice} ms={watch.ElapsedMilliseconds}");
            }
        }
        return Result(outcome, target, notice);
    }

    private async Task<InsertionOutcome> PlaceForManualPaste(string text, string reason, CancellationToken ct)
    {
        var status = SwapStatus.Busy;
        for (var attempt = 0; attempt < _timings.ClipboardAttempts; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            status = dispatcher.TryPlaceText(text);
            if (status != SwapStatus.Busy) break;
            if (attempt + 1 < _timings.ClipboardAttempts) await _delay(_timings.ClipboardRetryDelay, ct);
        }
        AppLog.Info("manual_paste_fallback", $"reason={reason} status={status}");
        return status switch { SwapStatus.Swapped => InsertionOutcome.CopiedForManualPaste, SwapStatus.Busy => InsertionOutcome.ClipboardBusy, _ => InsertionOutcome.PasteFailed };
    }

    private static InsertionResult Result(InsertionOutcome outcome, InsertionTarget target, bool notice) =>
        new(outcome, UiText.InsertionMessage(outcome, target.DisplayName, notice));
}
