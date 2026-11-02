using SmartSnippets.Presentation.Services;
using SmartSnippets.Uno.Platforms.Windows;
using SmartSnippets.Uno.Tests.Composition;

namespace SmartSnippets.Uno.Tests.Flows;

// All paths use UnoAppHost's real SQLite store, shared VMs and real InsertionService.
public sealed class DesktopHostFlowTests
{
    private static async Task<(HostFixture Host, NativeOperations Native, AdmissionControl Clock, long Id)> Start()
    {
        var f = new HostFixture(); var native = new NativeOperations();
        var clock = new AdmissionControl(() => f.Host.Gate.SuspendAdmission());
        f.Boundary.DesktopFactory = (_, _) => new TestDesktop(native);
        await f.Start();
        var id = f.Host.Store.CreateSnippet(new("first\nsecond", "synthetic", true, null, "")).Value!.Id;
        return (f, native, clock, id);
    }

    [Fact]
    public Task DesktopPopupAndMain_ShareSingleFlightCaptureBeforeShowAndExactTarget() => HostThread.RunAsync(async () =>
    {
        var (f, native, _, id) = await Start(); using var fixture = f;
        f.Boundary.Popups[0].CapturedShown = target =>
        {
            Assert.Equal(1, native.Captures);
            Assert.Same(native.Target, target);
            native.Trace.Add("show");
        };
        Assert.True(f.Host.ShowCapturedPopup());
        Assert.Same(native.Target, f.Host.Popup.Target);
        Assert.Equal(new[] { "capture", "show" }, native.Trace);
        native.FocusPause = new();
        var popupInsert = f.Host.Popup.InsertSelectedAsync();
        Assert.True(f.Host.Popup.IsInserting);
        Assert.False(f.Host.Snippets.CanInsertRow);
        await f.Host.Snippets.InsertRowAsync(id);
        Assert.Equal(InsertionOutcome.Busy, (await f.Host.Gate.InsertAsync(native.Target, id)).Outcome);
        Assert.Equal(InsertionOutcome.PasteFailed,
            (await f.Host.Gate.InsertAsync(native.Target with { WindowHandle = 124 }, id)).Outcome);
        Assert.False(f.Host.ShowCapturedPopup());
        native.FocusPause.SetResult(FocusPath.Direct);
        await popupInsert;
        Assert.Equal(1, native.Sends); Assert.Equal(1, native.Restores);
        Assert.Equal("first\r\nsecond", native.SwappedText);
        Assert.Equal(1, Assert.Single(f.Host.Store.Snippets).Snippet.UsageCount);
        Assert.True(f.Host.Snippets.CanInsertRow);
        await f.Host.Snippets.InsertRowAsync(id);
        Assert.Equal(2, native.Sends); Assert.Equal(2, native.Restores);
        Assert.Equal(1, native.Captures); // Main never recaptures the foreground window.
        Assert.Equal(2, Assert.Single(f.Host.Store.Snippets).Snippet.UsageCount);
        Assert.All(native.Targets, target => Assert.Same(native.Target, target));
        Assert.True(native.Trace.IndexOf("capture") < native.Trace.IndexOf("show"));
    });

    [Fact]
    public Task DesktopCopy_IsSeparateFromInsertionAndNeverCreditsUsage() => HostThread.RunAsync(async () =>
    {
        var (f, native, _, id) = await Start(); using var fixture = f;
        Assert.True(f.Host.Snippets.CanCopyRow); f.Host.Snippets.CopyRow(id);
        Assert.True(f.Host.ShowCapturedPopup());
        var row = f.Host.Popup.Items.First(r => r.SnippetId == id);
        Assert.True(f.Host.Popup.CanCopyRow(row)); f.Host.Popup.CopyRow(row);
        Assert.Equal(2, native.Copies); Assert.Equal(0, native.Sends); Assert.Equal(0, native.Swaps);
        Assert.Equal(0, Assert.Single(f.Host.Store.Snippets).Snippet.UsageCount);
        native.CopyResult = false;
        Assert.True(await f.Host.Snippets.RequestEditAsync(id));
        var editor = f.Host.Snippets.Editor!; editor.Text = "unsaved synthetic draft";
        editor.CopyTextCommand.Execute(null);
        Assert.True(editor.IsDirty); Assert.Equal("unsaved synthetic draft", editor.Text);
        Assert.Equal(3, native.Copies);
    });

    [Fact]
    public Task Preview_IgnoresEvenDesktopServicesAcrossRowFooterEditorAndMain() => HostThread.RunAsync(async () =>
    {
        var (f, native, _, id) = await Start(); using var fixture = f;
        f.Host.ShowPreview();
        var row = f.Host.Popup.Items.First(r => r.SnippetId == id);
        Assert.False(f.Host.ShowCapturedPopup());
        f.Host.Snippets.CopyRow(id); f.Host.Popup.CopyRow(row);
        await f.Host.Snippets.InsertRowAsync(id);
        f.Host.Popup.CopyFailedTextCommand.Execute(null);
        f.Host.Popup.EditRow(row);
        f.Host.Popup.Editor!.Text = "preview draft";
        Assert.False(f.Host.Popup.Editor.CopyTextCommand.CanExecute(null));
        f.Host.Popup.Editor.CopyTextCommand.Execute(null);
        Assert.True(await f.Host.Snippets.RequestEditAsync(id));
        f.Host.Snippets.Editor!.CopyTextCommand.Execute(null);
        Assert.False(f.Host.Gate.TrySetText("preview"));
        Assert.Equal(InsertionOutcome.PasteFailed, (await f.Host.Gate.InsertAsync(native.Target, id, new(true))).Outcome);
        await f.Host.Popup.CancelEditorAsync(false);
        await f.Host.Popup.ActivateAsync(row); // Selection only hides; it never inserts.
        Assert.Equal(0, native.SideEffects); Assert.Equal(0, native.Captures);
        Assert.Equal(0, Assert.Single(f.Host.Store.Snippets).Snippet.UsageCount);
    });

    [Theory]
    [InlineData("capture")]
    [InlineData("check")]
    [InlineData("focus")]
    [InlineData("modifiers")]
    [InlineData("swap")]
    [InlineData("foreground")]
    public Task SuspensionAtEveryBoundary_StopsFollowingMutationButRestoresOwnedClipboard(string expireAt) => HostThread.RunAsync(async () =>
    {
        var (f, native, clock, id) = await Start(); using var fixture = f;
        native.After = name => { if (name == expireAt) clock.Advance(TimeSpan.FromMinutes(1)); };
        var shown = f.Host.ShowCapturedPopup();
        if (shown) await f.Host.Popup.InsertSelectedAsync();
        Assert.Equal(0, native.Sends); Assert.Equal(0, native.Placements);
        Assert.Equal(native.Swaps, native.Restores);
        Assert.Equal(0, Assert.Single(f.Host.Store.Snippets).Snippet.UsageCount);
    });

    [Theory]
    [InlineData(DispatchState.Uncertain)]
    [InlineData(DispatchState.NotDispatched)]
    public Task UncertainOrFailedDispatch_NeverCreditsUsageAndDoesNotRetry(DispatchState state) => HostThread.RunAsync(async () =>
    {
        var (f, native, _, _) = await Start(); using var fixture = f;
        native.Dispatch = state; Assert.True(f.Host.ShowCapturedPopup());
        await f.Host.Popup.InsertSelectedAsync();
        Assert.Equal(1, native.Sends); Assert.Equal(1, native.Restores);
        Assert.Equal(state == DispatchState.Uncertain ? 1 : 0, native.KeyCleanups);
        Assert.Equal(0, Assert.Single(f.Host.Store.Snippets).Snippet.UsageCount);
    });

    [Fact]
    public Task SuspensionWhileSwapThrows_RestoresTransferredSnapshotWithoutSending() => HostThread.RunAsync(async () =>
    {
        var (f, native, clock, _) = await Start(); using var fixture = f;
        Assert.True(f.Host.ShowCapturedPopup()); native.ThrowAfterSwap = true;
        native.After = name => { if (name == "swap") clock.Advance(TimeSpan.FromMinutes(1)); };
        await f.Host.Popup.InsertSelectedAsync();
        Assert.Equal(1, native.Restores); Assert.Equal(0, native.Sends);
        Assert.Equal(0, Assert.Single(f.Host.Store.Snippets).Snippet.UsageCount);
    });

    [Fact]
    public Task Shutdown_DrainsOwnedRecoveryAfterSuspensionAndVmContinuationBeforeDisposal() => HostThread.RunAsync(async () =>
    {
        var (f, native, clock, id) = await Start(); using var fixture = f;
        Assert.True(f.Host.ShowCapturedPopup()); native.Restore = RestoreStatus.Busy;
        native.RecoveryPause = new();
        native.After = name => { if (name == "send") clock.Advance(TimeSpan.FromMinutes(1)); };
        var insertion = f.Host.RunOperationAsync(() => f.Host.Popup.InsertSelectedAsync());
        var exit = f.Host.ExitAsync();
        await native.RecoveryEntered.Task;
        f.Host.Dispose(); // Reentrant shutdown must share, not duplicate, owned recovery.
        Assert.Equal(1, native.Drains);
        Assert.False(exit.IsCompleted); Assert.Equal(0, f.Boundary.Main.Disposals);
        Assert.False(f.Host.Gate.TrySetText("late")); Assert.False(f.Host.ShowCapturedPopup());
        Assert.Equal(InsertionOutcome.PasteFailed, (await f.Host.Gate.InsertAsync(native.Target, id)).Outcome);
        native.RecoveryPause.SetResult(true);
        await insertion; await exit; await UiTurn();
        Assert.False(f.Host.Popup.IsInserting);
        Assert.Equal(1, native.Drains); Assert.Equal(1, native.Sends);
        Assert.Equal(1, f.Boundary.Main.Disposals); Assert.Equal(1, f.Boundary.TrayDisposals);
        Assert.Equal(0, f.Boundary.Exits); // Direct Dispose supersedes the pending Exit continuation.
    });

    [Fact]
    public Task UnsettledRecovery_BlocksExitAndNewMutationsRetainsOwnedHostForRetry() => HostThread.RunAsync(async () =>
    {
        var (f, native, clock, id) = await Start(); using var fixture = f;
        Assert.True(f.Host.ShowCapturedPopup()); native.Restore = RestoreStatus.Failed;
        native.RecoveryResult = false;
        await f.Host.Popup.InsertSelectedAsync(); clock.Advance(TimeSpan.FromMinutes(1));
        await f.Host.ExitAsync();
        Assert.Equal(0, f.Boundary.Exits); Assert.Equal(0, f.Boundary.Main.Disposals);
        Assert.False(f.Host.Gate.TrySetText("late"));
        Assert.Equal(InsertionOutcome.PasteFailed, (await f.Host.Gate.InsertAsync(native.Target, id)).Outcome);
        Assert.Single(f.Host.Store.Snippets);
        native.RecoveryResult = true;
        await f.Host.ExitAsync(); Assert.Equal(1, f.Boundary.Exits); Assert.Equal(2, native.Drains);
    });

    [Fact]
    public Task RecoveryBoundary_RefusesUnownedSnapshotsAndUnadmittedInputOrManualFallback() => HostThread.RunAsync(async () =>
    {
        var (f, native, _, _) = await Start(); using var fixture = f;
        var dispatcher = (IPasteDispatcher)f.Host.Gate;
        Assert.Equal(RestoreStatus.Failed, dispatcher.TryRestoreClipboard(new Snapshot()));
        Assert.Throws<InvalidOperationException>(() => dispatcher.DiscardClipboardSnapshot(new Snapshot()));
        Assert.Equal(DispatchState.NotDispatched, dispatcher.SendPaste());
        Assert.False(dispatcher.ReleasePasteKeys()); Assert.False(dispatcher.ReleaseHeldModifiers());
        Assert.Equal(SwapStatus.Failed, dispatcher.TryPlaceText("synthetic"));
        Assert.Equal(SwapStatus.Failed, dispatcher.TrySwapClipboard("synthetic", true).Status);
        Assert.Null(await dispatcher.RestoreFocusAsync(native.Target, default));
        Assert.Equal(0, native.SideEffects);
    });

    [Fact]
    public Task DirectDispose_DrainsActiveOwnedRecoveryAndVmBeforeReleasingProfile() => HostThread.RunAsync(async () =>
    {
        var (f, native, clock, _) = await Start(); using var fixture = f;
        Assert.True(f.Host.ShowCapturedPopup()); native.Restore = RestoreStatus.Failed; native.RecoveryPause = new();
        native.After = name => { if (name == "send") clock.Advance(TimeSpan.FromMinutes(1)); };
        var insertion = f.Host.Popup.InsertSelectedAsync();
        f.Host.Dispose();
        await native.RecoveryEntered.Task;
        Assert.Equal(0, f.Boundary.Main.Disposals); Assert.Equal(0, f.Boundary.TrayDisposals);
        Assert.False(f.Host.Gate.TrySetText("late"));
        var disposed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        // The dispatcher provides a UI turn after the asynchronous drain and VM continuations.
        native.RecoveryPause.SetResult(true);
        await insertion;
        SynchronizationContext.Current!.Post(_ => disposed.SetResult(), null);
        await disposed.Task;
        Assert.False(f.Host.Popup.IsInserting); Assert.Equal(1, f.Boundary.Main.Disposals);
        using var reopened = SmartSnippets.Uno.AppProfile.OpenOwnedRoot(f.Root);
    });

    [Fact]
    public Task WarmupSuspension_DoesNotRevealCapturedTargetAfterAdmissionCloses() => HostThread.RunAsync(async () =>
    {
        var (f, native, clock, _) = await Start(); using var fixture = f;
        f.Boundary.Popups[0].Dispose(); f.Boundary.PopupAutoReady = false;
        Assert.True(f.Host.ShowCapturedPopup());
        var surface = f.Boundary.Popups[^1];
        clock.Advance(TimeSpan.FromMinutes(1)); surface.Ready();
        Assert.Equal(0, surface.Shows); Assert.Null(f.Host.Popup.Target);
        Assert.Equal(1, native.Captures); Assert.Equal(0, native.SideEffects);
    });

    [Fact]
    public Task AdmissionSuspensionAndShutdownReceipts_AreContentFreeFixedTokens() => HostThread.RunAsync(async () =>
    {
        var (f, native, clock, id) = await Start(); using var fixture = f;
        Assert.True(f.Host.ShowCapturedPopup()); f.Host.Snippets.CopyRow(id);
        clock.Advance(TimeSpan.FromMinutes(1)); Assert.False(f.Host.Gate.IsAvailable);
        await f.Host.ExitAsync();
        var log = File.ReadAllText(Path.Combine(f.Root, "logs", "app.log"));
        foreach (var token in new[] { "candidate_started", "candidate_native_admission", "state=desktop",
            "state=closed", "candidate_shutdown_started", "candidate_shutdown_complete" })
            Assert.Contains(token, log);
        Assert.DoesNotContain(native.Target.DisplayName, log);
        Assert.DoesNotContain("first", log); Assert.DoesNotContain("second", log);
        Assert.DoesNotContain(f.Root, log);
    });

    [Theory]
    [InlineData(RestoreStatus.ClipboardChanged)]
    [InlineData(RestoreStatus.Partial)]
    [InlineData(RestoreStatus.Failed)]
    public Task UnsettledSnapshot_BlocksAllAdmissionAndRefreshesCommandsUntilOwnedDrainSettles(RestoreStatus restore) => HostThread.RunAsync(async () =>
    {
        var (f, native, _, id) = await Start(); using var fixture = f;
        Assert.True(f.Host.ShowCapturedPopup());
        Assert.True(await f.Host.Snippets.RequestEditAsync(id));
        var editor = f.Host.Snippets.Editor!; editor.Text = "retained recovery draft";
        var changes = 0; editor.CopyTextCommand.CanExecuteChanged += (_, _) => changes++;
        native.Restore = restore; native.RecoveryResult = false;
        await f.Host.Popup.InsertSelectedAsync();
        Assert.True(changes > 0); Assert.False(editor.CopyTextCommand.CanExecute(null));
        Assert.False(f.Host.Snippets.CanCopyRow); Assert.False(f.Host.Snippets.CanInsertRow);
        Assert.False(f.Host.Gate.IsIdle); Assert.False(f.Host.Gate.CanInsert);
        var before = native.SideEffects; var captures = native.Captures;
        Assert.Null(f.Host.Gate.Capture()); Assert.False(f.Host.ShowCapturedPopup());
        f.Host.Snippets.CopyRow(id); editor.CopyTextCommand.Execute(null);
        Assert.Equal(InsertionOutcome.PasteFailed, (await f.Host.Gate.InsertAsync(native.Target, id)).Outcome);
        Assert.Equal(before, native.SideEffects); Assert.Equal(captures, native.Captures);
        await f.Host.ExitAsync();
        Assert.Equal(1, native.Drains); Assert.Equal(0, f.Boundary.Main.Disposals); Assert.Equal(0, f.Boundary.Exits);
        Assert.True(editor.IsDirty); Assert.False(f.Host.Gate.IsAvailable);
        // A successful owned drain settles recovery but does not rearm the old capture.
        var pendingChanges = changes;
        f.Host.Gate.SuspendAdmission(); native.RecoveryResult = true;
        Assert.True(await f.Host.Gate.DrainAsync()); f.Host.Gate.ResumeAdmission();
        Assert.True(changes > pendingChanges);
        Assert.True(f.Host.Snippets.CanCopyRow); Assert.True(editor.CopyTextCommand.CanExecute(null));
        Assert.False(f.Host.Snippets.CanInsertRow); Assert.False(f.Host.Gate.CanInsert);
        f.Boundary.Dialogs.Choices.Enqueue(SaveChoice.Discard);
        await f.Host.ExitAsync(); Assert.Equal(1, f.Boundary.Exits);
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task MissingTransferredSnapshot_FailsWithoutDispatchAndRetainsRecoveryAdmissionBlock(bool throws) => HostThread.RunAsync(async () =>
    {
        var (f, native, _, id) = await Start(); using var fixture = f;
        Assert.True(f.Host.ShowCapturedPopup());
        native.NullSnapshot = true; native.ThrowWithoutSnapshot = throws; native.RecoveryResult = false;
        await f.Host.Popup.InsertSelectedAsync();
        Assert.Equal(0, native.Sends); Assert.Equal(0, native.Restores);
        Assert.Equal(0, Assert.Single(f.Host.Store.Snippets).Snippet.UsageCount);
        Assert.False(f.Host.Popup.IsConfirmingClipboardLoss); Assert.False(f.Host.Snippets.CanCopyRow);
        Assert.False(f.Host.Gate.IsIdle); Assert.False(f.Host.ShowCapturedPopup());
        await f.Host.ExitAsync();
        Assert.Equal(1, native.Drains); Assert.Equal(0, f.Boundary.Main.Disposals);
        Assert.False(f.Host.Gate.TrySetText("blocked"));
        Assert.Equal(InsertionOutcome.PasteFailed, (await f.Host.Gate.InsertAsync(native.Target, id)).Outcome);
        native.RecoveryResult = true; await f.Host.ExitAsync();
        Assert.Equal(2, native.Drains); Assert.Equal(1, f.Boundary.Exits);
    });

    [Fact]
    public Task RefusedRecapture_CannotRearmPreviousTargetAfterSafetyRecovers() => HostThread.RunAsync(async () =>
    {
        var (f, native, _, id) = await Start(); using var fixture = f;
        Assert.True(f.Host.ShowCapturedPopup()); native.Safe = false;
        Assert.False(f.Host.ShowCapturedPopup()); native.Safe = true;
        Assert.False(f.Host.Gate.CanInsert);
        Assert.Equal(InsertionOutcome.PasteFailed, (await f.Host.Gate.InsertAsync(native.Target, id)).Outcome);
        Assert.Equal(0, native.Sends);
        Assert.True(f.Host.ShowCapturedPopup());
        await f.Host.Popup.InsertSelectedAsync(); Assert.Equal(1, native.Sends);
    });

    [Fact]
    public Task ForegroundExceptionBeforeNativeDispatch_IsFailedNotMaybePasted() => HostThread.RunAsync(async () =>
    {
        var (f, native, _, _) = await Start(); using var fixture = f;
        native.ThrowForegroundAt = 2;
        Assert.True(f.Host.ShowCapturedPopup()); await f.Host.Popup.InsertSelectedAsync();
        Assert.Equal(2, native.ForegroundChecks); Assert.Equal(0, native.Sends);
        Assert.Equal(0, native.KeyCleanups); Assert.Equal(1, native.Restores);
        Assert.Equal(Presentation.ViewModels.PopupFooterState.Error, f.Host.Popup.FooterState);
        Assert.Equal(0, Assert.Single(f.Host.Store.Snippets).Snippet.UsageCount);
    });

    [Fact]
    public Task ActivePreviewClose_EndsMutationLockButKeepsVmPreviewGuardsUntilNewCapture() => HostThread.RunAsync(async () =>
    {
        var (f, native, _, id) = await Start(); using var fixture = f;
        Assert.True(f.Host.ShowCapturedPopup());
        Assert.True(await f.Host.Snippets.RequestEditAsync(id));
        var editor = f.Host.Snippets.Editor!; var changes = 0;
        editor.CopyTextCommand.CanExecuteChanged += (_, _) => changes++;
        f.Host.ShowPreview();
        Assert.True(changes > 0); Assert.False(editor.CopyTextCommand.CanExecute(null));
        Assert.False(f.Host.Gate.CanInsert); Assert.False(f.Host.ShowCapturedPopup());
        f.Host.Snippets.CopyRow(id); Assert.Equal(0, native.Copies);
        var activeChanges = changes;
        f.Host.Popup.Close();
        Assert.True(changes > activeChanges);
        Assert.True(f.Host.Popup.IsPreview); // Shared VM keeps its independent nonmutating row/editor guards.
        Assert.True(f.Host.Snippets.CanCopyRow); Assert.True(editor.CopyTextCommand.CanExecute(null));
        f.Host.Snippets.CopyRow(id); Assert.Equal(1, native.Copies);
        f.Host.Popup.CopyRow(f.Host.Popup.Items.First(row => row.SnippetId == id)); Assert.Equal(1, native.Copies);
        Assert.True(f.Host.ShowCapturedPopup()); Assert.False(f.Host.Popup.IsPreview);
        await f.Host.Popup.InsertSelectedAsync(); Assert.Equal(1, native.Sends);
        Assert.Equal(1, Assert.Single(f.Host.Store.Snippets).Snippet.UsageCount);
    });

    [Fact]
    public Task HiddenPreviewEditor_KeepsMutationLockUntilEditorClosesThenAllowsFreshCapture() => HostThread.RunAsync(async () =>
    {
        var (f, native, _, id) = await Start(); using var fixture = f;
        f.Host.ShowPreview(); f.Host.Popup.EditRow(f.Host.Popup.Items.First(row => row.SnippetId == id));
        f.Boundary.Popups[0].Hide();
        Assert.False(f.Host.Snippets.CanCopyRow); Assert.False(f.Host.ShowCapturedPopup());
        f.Host.Popup.Editor!.CopyTextCommand.Execute(null); Assert.Equal(0, native.Copies);
        await f.Host.Popup.CancelEditorAsync(false);
        Assert.True(f.Host.Snippets.CanCopyRow); Assert.True(f.Host.ShowCapturedPopup());
        await f.Host.Popup.InsertSelectedAsync(); Assert.Equal(1, native.Sends);
    });

    [Fact]
    public Task WarmupTransitions_RefreshActualCommandsAndCanceledOrRejectedCaptureCannotRearm() => HostThread.RunAsync(async () =>
    {
        var (f, native, _, id) = await Start(); using var fixture = f;
        f.Host.ShowPreview(); f.Host.Popup.Close();
        f.Boundary.Popups[0].Dispose(); f.Boundary.PopupAutoReady = false;
        Assert.True(f.Host.ShowCapturedPopup()); var surface = f.Boundary.Popups[^1];
        Assert.False(f.Host.Snippets.CanInsertRow); // VM still carries closed preview mode until ready.
        var refreshed = 0; f.Host.Snippets.PropertyChanged += (_, args) => { if (args.PropertyName == "CanCopyRow") refreshed++; };
        surface.Ready();
        Assert.True(refreshed > 0); Assert.True(f.Host.Snippets.CanInsertRow);
        surface.Dispose();
        Assert.True(f.Host.ShowCapturedPopup()); surface = f.Boundary.Popups[^1];
        f.Host.Gate.SuspendAdmission(); surface.Ready(); f.Host.Gate.ResumeAdmission();
        Assert.Equal(0, surface.Shows); Assert.False(f.Host.Gate.CanInsert);
        Assert.Equal(InsertionOutcome.PasteFailed, (await f.Host.Gate.InsertAsync(native.Target, id)).Outcome);
        surface.Dispose(); Assert.True(f.Host.ShowCapturedPopup()); surface = f.Boundary.Popups[^1];
        surface.Hide(); surface.Ready();
        Assert.Equal(0, surface.Shows); Assert.False(f.Host.Gate.CanInsert);
        Assert.Equal(0, native.Sends);
    });

    [Fact]
    public Task QueuedPreview_HideBeforeReadyRestoresMainCopyAndDoesNotOpenStalePreview() => HostThread.RunAsync(async () =>
    {
        var (f, native, _, id) = await Start(); using var fixture = f;
        f.Boundary.Popups[0].Dispose(); f.Boundary.PopupAutoReady = false;
        f.Host.ShowPreview(); var surface = f.Boundary.Popups[^1];
        Assert.False(f.Host.Snippets.CanCopyRow);
        surface.Hide(); surface.Ready();
        Assert.Equal(0, surface.Shows); Assert.True(f.Host.Snippets.CanCopyRow);
        f.Host.Snippets.CopyRow(id); Assert.Equal(1, native.Copies);
        Assert.True(f.Host.ShowCapturedPopup()); surface.Ready();
        Assert.False(f.Host.Popup.IsPreview); Assert.Equal(1, surface.Shows);
    });

    [Theory]
    [InlineData("cancel")]
    [InlineData("success")]
    [InlineData("throw")]
    public Task DisposeSupersedesMainExitGuard_NoResumeOrExitAfterAwait(string result) => HostThread.RunAsync(async () =>
    {
        var (f, native, _, _) = await Start(); using var fixture = f;
        var guard = new TaskCompletionSource<bool>(); f.Boundary.Main.ExitGuard = () => guard.Task;
        var exit = f.Host.ExitAsync(); Assert.False(exit.IsCompleted);
        f.Host.Dispose(); Assert.Equal(1, f.Boundary.Main.Disposals);
        if (result == "throw") guard.SetException(new InvalidOperationException("synthetic guard"));
        else guard.SetResult(result == "success");
        await exit; Assert.False(f.Boundary.Main.InteractionEnabled); Assert.Equal(0, f.Boundary.Exits);
        Assert.False(f.Host.Gate.TrySetText("late")); Assert.Equal(0, native.SideEffects);
    });

    [Theory]
    [InlineData(SaveChoice.Save)]
    [InlineData(SaveChoice.Discard)]
    [InlineData(SaveChoice.Cancel)]
    public Task DisposeSupersedesPopupExitDialog_NoLateSaveDiscardOrRecoveryShow(SaveChoice choice) => HostThread.RunAsync(async () =>
    {
        var (f, native, _, id) = await Start(); using var fixture = f;
        Assert.True(f.Host.ShowCapturedPopup()); f.Host.Popup.EditRow(f.Host.Popup.Items.First(row => row.SnippetId == id));
        var editor = f.Host.Popup.Editor!; editor.Text = "unsaved guard draft";
        var dialog = new TaskCompletionSource<SaveChoice>(); f.Boundary.Dialogs.PendingChoice = dialog.Task;
        var exit = f.Host.ExitAsync(); Assert.False(exit.IsCompleted);
        var shows = f.Boundary.Popups[0].Shows;
        f.Host.Dispose(); dialog.SetResult(choice); await exit;
        Assert.Equal("first\nsecond", Assert.Single(f.Host.Store.Snippets).Snippet.Text);
        Assert.Equal("unsaved guard draft", editor.Text);
        Assert.False(f.Boundary.Main.InteractionEnabled); Assert.Equal(0, f.Boundary.Exits);
        Assert.Equal(shows, f.Boundary.Popups[0].Shows); Assert.False(f.Host.Gate.TrySetText("late"));
        Assert.Equal(0, native.Sends);
    });

    [Fact]
    public Task DisposeDuringFailedOwnedDrain_CannotCancelShutdownOrReenableAfterBothContinuations() => HostThread.RunAsync(async () =>
    {
        var (f, native, _, _) = await Start(); using var fixture = f;
        Assert.True(f.Host.ShowCapturedPopup()); native.Restore = RestoreStatus.ClipboardChanged;
        await f.Host.Popup.InsertSelectedAsync(); native.RecoveryPause = new();
        var exit = f.Host.ExitAsync(); await native.RecoveryEntered.Task;
        f.Host.Dispose(); native.RecoveryPause.SetResult(false);
        await exit; await UiTurn();
        Assert.Equal(1, native.Drains); Assert.Equal(0, f.Boundary.Main.Disposals);
        Assert.False(f.Boundary.Main.InteractionEnabled); Assert.False(f.Host.Gate.TrySetText("late"));
        Assert.False(f.Host.CanPresentPopup); Assert.Equal(0, f.Boundary.Exits);
        native.RecoveryPause = null; native.RecoveryResult = true; f.Host.Dispose();
        Assert.Equal(1, f.Boundary.Main.Disposals);
    });

    [Fact]
    public Task DisposeSupersedesManagementDrain_NoLateExitOrInteractionResume() => HostThread.RunAsync(async () =>
    {
        var (f, native, _, _) = await Start(); using var fixture = f;
        var pause = new TaskCompletionSource(); var management = f.Host.RunOperationAsync(() => pause.Task);
        var exit = f.Host.ExitAsync(); Assert.False(exit.IsCompleted);
        f.Host.Dispose(); Assert.Equal(0, f.Boundary.Main.Disposals);
        pause.SetResult(); await management; await exit; await UiTurn();
        Assert.Equal(1, f.Boundary.Main.Disposals); Assert.False(f.Boundary.Main.InteractionEnabled);
        Assert.Equal(0, f.Boundary.Exits); Assert.False(f.Host.Gate.TrySetText("late")); Assert.Equal(0, native.SideEffects);
    });

    [Fact]
    public Task FailedWarmup_CancelsPendingPreviewAndRefreshesMainCommandsWithoutLateReveal() => HostThread.RunAsync(async () =>
    {
        var (f, native, _, id) = await Start(); using var fixture = f;
        f.Boundary.Popups[0].Dispose(); f.Boundary.PopupAutoReady = false;
        f.Host.ShowPreview(); var surface = f.Boundary.Popups[^1];
        Assert.False(f.Host.Snippets.CanCopyRow);
        surface.Lifecycle.Shows.WarmupTimedOut(surface.Lifecycle.Shows.WarmupGeneration);
        surface.Lifecycle.Shows.WarmupTimedOut(surface.Lifecycle.Shows.WarmupGeneration);
        Assert.True(f.Host.Snippets.CanCopyRow); surface.Prewarm(); surface.Ready();
        Assert.Equal(0, surface.Shows); f.Host.Snippets.CopyRow(id); Assert.Equal(1, native.Copies);
        Assert.True(f.Host.ShowCapturedPopup()); Assert.False(f.Host.Popup.IsPreview);
    });

    [Fact]
    public Task ReadySurfaceFocus_RequiresCapturedLiveNonPreviewIdleServices() => HostThread.RunAsync(async () =>
    {
        var (f, native, clock, _) = await Start(); using var owner = f;
        Assert.False(f.Host.Gate.TryFocusOwnedPopup());
        Assert.Equal(0, native.PopupFocuses);
        Assert.True(f.Host.ShowCapturedPopup());
        Assert.True(f.Host.Gate.TryFocusOwnedPopup()); Assert.Equal(1, native.PopupFocuses);
        f.Host.ShowPreview();
        Assert.False(f.Host.Gate.TryFocusOwnedPopup()); Assert.Equal(1, native.PopupFocuses);
        f.Boundary.Popups.Last().Hide();
        Assert.True(f.Host.ShowCapturedPopup());
        clock.Advance(TimeSpan.FromMinutes(2));
        Assert.False(f.Host.Gate.TryFocusOwnedPopup()); Assert.Equal(1, native.PopupFocuses);
    });

    [Theory]
    [InlineData(true)]
    public Task FailedCopy_RetainsUnknownRecoveryBlocksAdmissionAndAlwaysDrains(bool throws) => HostThread.RunAsync(async () =>
    {
        var (f, native, _, id) = await Start(); using var owner = f;
        native.CopyResult = false; native.ThrowCopy = throws; native.RecoveryResult = false;
        f.Host.Snippets.CopyRow(id);
        Assert.Equal(1, native.Copies); Assert.False(f.Host.Snippets.CanCopyRow);
        Assert.False(f.Host.Gate.IsIdle); Assert.False(f.Host.ShowCapturedPopup());
        await f.Host.ExitAsync();
        Assert.Equal(1, native.Drains); Assert.Equal(0, f.Boundary.Exits);
        native.RecoveryResult = true; await f.Host.ExitAsync();
        Assert.Equal(2, native.Drains); Assert.Equal(1, f.Boundary.Exits);
    });

    [Fact]
    public Task AdapterRecoveryWithoutLocalSnapshot_StillBlocksShutdownUntilSettled() => HostThread.RunAsync(async () =>
    {
        var (f, native, _, _) = await Start(); using var owner = f;
        native.RecoveryResult = false;
        await f.Host.ExitAsync();
        Assert.Equal(1, native.Drains); Assert.Equal(0, f.Boundary.Exits);
        native.RecoveryResult = true; await f.Host.ExitAsync();
        Assert.Equal(1, f.Boundary.Exits);
    });

    [Fact]
    public Task UiCopy_WaitsForQuietAdmissionThenUsesRealVmWithoutUsage() => HostThread.RunAsync(async () =>
    {
        var (f, native, _, id) = await Start(); using var owner = f;
        native.CopyPause = new();
        var copy = f.Host.RunCopyOperationAsync(() => f.Host.Snippets.CopyRow(id));
        Assert.False(copy.IsCompleted); Assert.Equal(0, native.Copies);
        native.CopyPause.SetResult(true); await copy;
        Assert.Equal(1, native.CopyWaits); Assert.Equal(1, native.Copies);
        Assert.Equal(0, f.Host.Store.Snippets.Single().Snippet.UsageCount);
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task UiCopy_RechecksSuspensionOrPreviewAfterQuietWait(bool preview) => HostThread.RunAsync(async () =>
    {
        var (f, native, clock, id) = await Start(); using var owner = f;
        native.CopyPause = new();
        var copy = f.Host.RunCopyOperationAsync(() => f.Host.Snippets.CopyRow(id));
        if (preview) f.Host.ShowPreview(); else clock.Advance(TimeSpan.FromMinutes(2));
        native.CopyPause.SetResult(true); await copy;
        Assert.Equal(0, native.Copies);
        Assert.Equal(0, f.Host.Store.Snippets.Single().Snippet.UsageCount);
    });

    [Fact]
    public Task UiCopy_ShutdownDrainsWaitAndRefusesLateMutation() => HostThread.RunAsync(async () =>
    {
        var (f, native, _, id) = await Start(); using var owner = f;
        native.CopyPause = new();
        var copy = f.Host.RunCopyOperationAsync(() => f.Host.Snippets.CopyRow(id));
        var exit = f.Host.ExitAsync();
        await UiTurn(); Assert.False(exit.IsCompleted);
        native.CopyPause.SetResult(true); await Task.WhenAll(copy, exit);
        Assert.Equal(0, native.Copies); Assert.Equal(1, f.Boundary.Exits);
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task QaLaunchPreview_NeverCallsNativeCaptureOrProbe(bool redirected) => HostThread.RunAsync(async () =>
    {
        using var f = new HostFixture(); var native = new NativeOperations();
        f.Boundary.DesktopFactory = (_, _) => new TestDesktop(native);
        var options = new SmartSnippets.Presentation.Navigation.LaunchOptions(null, true, "synthetic", true);
        if (!redirected) await f.Start(options: options);
        else
        {
            await f.Start(background: true);
            var queue = new ActivationQueue();
            queue.Attach(f.Boundary.Dispatcher, o => f.Host.ApplyLaunchOptionsAsync(o));
            queue.Enqueue(options); f.Boundary.Drain();
        }
        Assert.True(f.Host.Popup.IsPreview);
        Assert.Equal(0, native.Captures); Assert.Empty(native.Targets);
        Assert.Equal(0, native.SideEffects); Assert.Equal(0, native.PopupFocuses);
        Assert.False(f.Boundary.Main.Visible);
    });

    private static Task UiTurn()
    {
        var turn = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        SynchronizationContext.Current!.Post(_ => turn.SetResult(), null);
        return turn.Task;
    }

    private sealed class AdmissionControl(Action suspend)
    {
        internal void Advance(TimeSpan _) => suspend();
    }

    private sealed class TestDesktop(NativeOperations operations) : IDesktopServices
    {
        public IDailyOperations Operations => operations;
        public IHotkeyManager Hotkeys { get; } = new UnavailableHotkeyManager(SmartSnippets.Core.Settings.HotkeyGesture.Default);
        public IStartupRegistration Startup { get; } = new UnavailableStartupRegistration();
        public IElevation Elevation { get; } = new UnavailableElevation();
        public void Dispose() { }
    }

    private sealed class Snapshot : IClipboardSnapshot
    {
        public int SavedCount => 1;
        public int SkippedByDesignCount => 0;
        public int UnsavedCount => 0;
        public bool CapExceeded => false;
        public long ByteCount => 8;
    }

    private sealed class NativeOperations : IDailyOperations
    {
        internal InsertionTarget Target { get; } = new(123, 456, 789, "sensitive synthetic target");
        internal readonly List<string> Trace = [];
        internal readonly List<InsertionTarget> Targets = [];
        internal int Captures, Copies, Sends, Swaps, Restores, Placements, KeyCleanups, Drains, Focuses, Modifiers;
        internal int SideEffects => Copies + Sends + Swaps + Restores + Placements + KeyCleanups + Focuses + Modifiers;
        internal bool Safe = true, CopyResult = true, RecoveryResult = true, ThrowAfterSwap;
        internal bool NullSnapshot, ThrowWithoutSnapshot, ThrowCopy;
        internal int ForegroundChecks, ThrowForegroundAt;
        internal SwapStatus Swap = SwapStatus.Swapped;
        internal string? SwappedText;
        internal Action<string>? After;
        internal TaskCompletionSource<FocusPath?>? FocusPause;
        internal TaskCompletionSource<bool>? RecoveryPause;
        internal TaskCompletionSource<bool>? CopyPause;
        internal int CopyWaits, PopupFocuses;
        public bool TryFocusOwnedPopup() { PopupFocuses++; return Safe; }
        public Task<bool> WaitForCopyAdmissionAsync(CancellationToken ct)
        { CopyWaits++; return CopyPause?.Task ?? Task.FromResult(true); }
        internal TaskCompletionSource RecoveryEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal DispatchState Dispatch = DispatchState.Dispatched;
        internal RestoreStatus Restore = RestoreStatus.Restored;
        public InsertionTarget? CaptureForeground()
        { Captures++; Trace.Add("capture"); After?.Invoke("capture"); return Safe ? Target : null; }
        public InsertionTarget? CaptureFrom(nint window) => CaptureForeground();
        public bool IsAvailable => true;
        public bool TrySetText(string text)
        { Copies++; if (ThrowCopy) throw new InvalidOperationException("synthetic copy failure"); return CopyResult; }
        public void SetText(string text) => throw new InvalidOperationException("The result-aware path is required.");
        public TargetCheck Check(InsertionTarget target)
        { Targets.Add(target); After?.Invoke("check"); return TargetCheck.Ok; }
        public bool IsTargetForeground(InsertionTarget target)
        {
            Targets.Add(target); ForegroundChecks++;
            if (ForegroundChecks == ThrowForegroundAt) throw new InvalidOperationException("synthetic foreground check");
            After?.Invoke("foreground"); return true;
        }
        public Task<FocusPath?> RestoreFocusAsync(InsertionTarget target, CancellationToken ct)
        { Targets.Add(target); Focuses++; After?.Invoke("focus"); return FocusPause?.Task ?? Task.FromResult<FocusPath?>(FocusPath.Direct); }
        public bool ReleaseHeldModifiers() { Modifiers++; After?.Invoke("modifiers"); return true; }
        public ClipboardSwap TrySwapClipboard(string text, bool allowIncomplete)
        {
            Swaps++; SwappedText = text; var snapshot = new Snapshot(); After?.Invoke("swap");
            if (ThrowAfterSwap) throw new ClipboardSwapException(snapshot, new InvalidOperationException("synthetic"));
            if (ThrowWithoutSnapshot) throw new InvalidOperationException("synthetic untransferred snapshot");
            return new(Swap, NullSnapshot || Swap != SwapStatus.Swapped ? null : snapshot);
        }

        public SwapStatus TryPlaceText(string text) { Placements++; return SwapStatus.Swapped; }
        public DispatchState SendPaste() { Sends++; After?.Invoke("send"); return Dispatch; }
        public bool ReleasePasteKeys() { KeyCleanups++; return true; }
        public RestoreStatus TryRestoreClipboard(IClipboardSnapshot snapshot) { Restores++; return Restore; }
        public void DiscardClipboardSnapshot(IClipboardSnapshot snapshot) => throw new InvalidOperationException("Unexpected manual fallback.");
        public Task<bool> DrainOwnedRecoveryAsync()
        { Drains++; RecoveryEntered.TrySetResult(); return RecoveryPause?.Task ?? Task.FromResult(RecoveryResult); }
    }
}