using System;
using System.Threading.Tasks;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Uno.Platforms.Windows.AcceptanceNative;
using Xunit;

namespace SmartSnippets.Uno.Tests.AcceptanceNative;

public sealed class GuardedAdaptersTests
{
    private static readonly InsertionTarget A = new(11, 21, 0, "");
    private static readonly InsertionTarget B = new(12, 21, 0, "");

    [Fact]
    public async Task DeniedOrExpired_AllEntryPathsRefuseBeforeClipboardOrInput()
    {
        var channel = new FakeGuardian(); var input = new FakeInput();
        var adapter = new GuardedPasteDispatcher(new Probe(), A, _ => false, channel, input);
        Assert.Null(await adapter.RestoreFocusAsync(A, default));
        Assert.False(adapter.ReleaseHeldModifiers());
        Assert.False(adapter.IsAvailable);
        Assert.False(adapter.TrySetText(GuardianClipboard.SyntheticText));
        Assert.Equal(SwapStatus.NotPreserved, adapter.TrySwapClipboard(GuardianClipboard.SyntheticText, false).Status);
        Assert.Equal(SwapStatus.NotPreserved, adapter.TryPlaceText(GuardianClipboard.SyntheticText));
        Assert.Equal(DispatchState.NotDispatched, adapter.SendPaste());
        Assert.Equal(0, channel.Publishes);
        Assert.Equal(0, input.Sends);
    }

    [Fact]
    public async Task ExactRetainedWindow_FocusOnlyObservesNeverTransfers()
    {
        var probe = new Probe(); var channel = new FakeGuardian(); var input = new FakeInput();
        var adapter = new GuardedPasteDispatcher(probe, A, _ => true, channel, input);
        Assert.Null(await adapter.RestoreFocusAsync(B, default));
        Assert.Equal(FocusPath.Direct, await adapter.RestoreFocusAsync(A, default));
        probe.Foreground = false;
        Assert.Null(await adapter.RestoreFocusAsync(A, default));
        Assert.Equal(0, input.Sends);
        Assert.Equal(0, channel.Publishes);
    }

    [Fact]
    public void UnsupportedPayloadOrLossConsent_RefusesBeforePublication()
    {
        var channel = new FakeGuardian();
        var adapter = new GuardedPasteDispatcher(new Probe(), A, _ => true, channel, new FakeInput());
        Assert.Equal(SwapStatus.NotPreserved, adapter.TrySwapClipboard("arbitrary content", false).Status);
        Assert.Equal(SwapStatus.NotPreserved, adapter.TrySwapClipboard(GuardianClipboard.SyntheticText, true).Status);
        Assert.False(adapter.TrySetText("arbitrary content"));
        Assert.Equal(0, channel.Publishes);
    }

    [Theory]
    [InlineData(DispatchState.Uncertain, true)]
    [InlineData(DispatchState.NotDispatched, false)]
    [InlineData(DispatchState.Dispatched, false)]
    public void RealDispatchResult_IsSingleAttemptAndNeverSendsGenericKeyUps(DispatchState state, bool pending)
    {
        var channel = new FakeGuardian(); var input = new FakeInput { Result = state };
        var adapter = new GuardedPasteDispatcher(new Probe(), A, _ => true, channel, input);
        var swap = adapter.TrySwapClipboard(GuardianClipboard.SyntheticText, false);
        Assert.NotNull(swap.Snapshot);
        Assert.Equal(state, adapter.SendPaste());
        Assert.Equal(pending, adapter.KeyboardRecoveryPending);
        Assert.Equal(!pending, adapter.ReleasePasteKeys());
        Assert.Equal(DispatchState.NotDispatched, adapter.SendPaste());
        Assert.Equal(1, input.Sends);
        Assert.Equal(RestoreStatus.Restored, adapter.TryRestoreClipboard(swap.Snapshot!));
        Assert.Equal(pending, adapter.RecoveryPending); // Clipboard settlement cannot erase uncertain keyboard state.
    }

    [Fact]
    public void NativeThrowAndShutdown_PreserveKeyboardPendingAndDrainClipboardDespiteExpiry()
    {
        var allowed = true; var channel = new FakeGuardian(); var input = new FakeInput { ThrowSend = true };
        var adapter = new GuardedPasteDispatcher(new Probe(), A, _ => allowed, channel, input);
        adapter.TrySwapClipboard(GuardianClipboard.SyntheticText, false);
        Assert.Throws<InvalidOperationException>(() => adapter.SendPaste());
        allowed = false;
        Assert.Equal(RestoreStatus.Restored, adapter.CloseAdmissionAndRestore());
        Assert.True(channel.Closed);
        Assert.Equal(1, channel.Restores);
        Assert.True(adapter.KeyboardRecoveryPending);
        Assert.False(adapter.ReleasePasteKeys());
        Assert.False(adapter.IsAvailable);
    }

    [Fact]
    public void CopyAndInsert_PermissionsAreIndependent()
    {
        var channel = new FakeGuardian();
        var adapter = new GuardedPasteDispatcher(new Probe(), A, op => op == AcceptanceOperation.Copy, channel, new FakeInput());
        Assert.True(adapter.IsAvailable);
        Assert.Equal(SwapStatus.NotPreserved, adapter.TrySwapClipboard(GuardianClipboard.SyntheticText, false).Status);
        Assert.True(adapter.TrySetText(GuardianClipboard.SyntheticText));
        Assert.Equal(AcceptanceOperation.Copy, channel.LastOperation);
        Assert.Equal(1, channel.Commits);
        Assert.Equal(0, channel.Restores);
    }

    [Fact]
    public void CopyExpiryAfterPublication_RefusesCommitButStillRestores()
    {
        var allowed = true;
        var channel = new FakeGuardian { AfterPublish = () => allowed = false };
        var adapter = new GuardedPasteDispatcher(new Probe(), A, _ => allowed, channel, new FakeInput());
        Assert.False(adapter.TrySetText(GuardianClipboard.SyntheticText));
        Assert.Equal(0, channel.Commits);
        Assert.Equal(1, channel.Restores);
    }

    [Fact]
    public void CopyCleanupFailure_DoesNotThrowOrInventSettlement()
    {
        var channel = new FakeGuardian { CommitResult = false, ThrowRestore = true };
        var adapter = new GuardedPasteDispatcher(new Probe(), A, _ => true, channel, new FakeInput());
        Assert.False(adapter.TrySetText(GuardianClipboard.SyntheticText));
        Assert.True(adapter.RecoveryPending);
        Assert.False(adapter.IsAvailable);
    }

    [Fact]
    public void FinalForegroundAndHeldInputGate_RejectBeforeNativeSend()
    {
        var probe = new Probe(); var channel = new FakeGuardian(); var input = new FakeInput();
        var adapter = new GuardedPasteDispatcher(probe, A, _ => true, channel, input);
        adapter.TrySwapClipboard(GuardianClipboard.SyntheticText, false);
        probe.Foreground = false;
        Assert.Equal(DispatchState.NotDispatched, adapter.SendPaste());
        probe.Foreground = true; input.IsQuiet = false;
        Assert.Equal(DispatchState.NotDispatched, adapter.SendPaste());
        Assert.Equal(0, input.Sends);
    }

    [Fact]
    public void LostPublicationAcknowledgement_TransfersRecoveryToken()
    {
        var channel = new FakeGuardian { ThrowPublish = true };
        var adapter = new GuardedPasteDispatcher(new Probe(), A, _ => true, channel, new FakeInput());
        var error = Assert.Throws<ClipboardSwapException>(() => adapter.TrySwapClipboard(GuardianClipboard.SyntheticText, false));
        Assert.NotNull(error.Snapshot);
        Assert.Equal(1, error.Snapshot.UnsavedCount); // Unknown acknowledgement is not preservation proof.
        Assert.True(adapter.RecoveryPending);
        Assert.Equal(RestoreStatus.Restored, adapter.TryRestoreClipboard(error.Snapshot));
    }

    [Theory]
    [InlineData(0u, DispatchState.NotDispatched)]
    [InlineData(1u, DispatchState.Uncertain)]
    [InlineData(2u, DispatchState.Uncertain)]
    [InlineData(3u, DispatchState.Uncertain)]
    [InlineData(4u, DispatchState.Dispatched)]
    public void NativeSendCount_ClassifiesPartialAsUncertain(uint count, DispatchState expected) =>
        Assert.Equal(expected, NativeAcceptanceInput.ClassifySendCount(count));

    [Fact]
    public void OrdinaryFactory_IsSourceReviewClosedWithoutNativeCalls()
    {
        Assert.False(NativeAcceptanceFactory.SourceReviewed);
        Assert.Throws<InvalidOperationException>(NativeAcceptanceFactory.RequireReviewed);
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r")]
    [InlineData("\r\n")]
    public void Copy_NormalizesStoredAndEditorLineEndings(string newline)
    {
        var channel = new FakeGuardian();
        var adapter = new GuardedPasteDispatcher(new Probe(), A, op => op == AcceptanceOperation.Copy, channel, new FakeInput());
        Assert.True(adapter.TrySetText(GuardianClipboard.SyntheticText.Replace("\r\n", newline)));
        Assert.Equal(1, channel.Commits);
    }

    [Fact]
    public void CopyAvailability_DoesNotRequireIdle_ButMutationStillDoes()
    {
        var channel = new FakeGuardian(); var input = new FakeInput { IsQuiet = false };
        var adapter = new GuardedPasteDispatcher(new Probe(), A, _ => true, channel, input);
        Assert.True(adapter.IsAvailable);
        Assert.False(adapter.TrySetText(GuardianClipboard.SyntheticText));
        Assert.Equal(0, channel.Publishes);
        input.IsQuiet = true;
        Assert.True(adapter.TrySetText(GuardianClipboard.SyntheticText));
    }

    [Fact]
    public void FactoryPermissions_DefaultDenied_CaptureRequiresInsert_NotControlOrCopy()
    {
        foreach (var op in Enum.GetValues<AcceptanceOperation>())
            Assert.False(NativeAcceptanceFactory.AllowsOperation(op, false, false, false));
        Assert.False(NativeAcceptanceFactory.AllowsOperation((AcceptanceOperation)999, true, true, true));
        Assert.True(NativeAcceptanceFactory.AllowsOperation(AcceptanceOperation.Copy, false, true, false));
        Assert.True(NativeAcceptanceFactory.AllowsOperation(AcceptanceOperation.ClipboardControl, false, false, true));
        Assert.False(NativeAcceptanceFactory.AllowsOperation(AcceptanceOperation.Capture, false, true, true));
        Assert.True(NativeAcceptanceFactory.AllowsOperation(AcceptanceOperation.MetadataCapture, false, false, true));
        Assert.True(NativeAcceptanceFactory.AllowsOperation(AcceptanceOperation.MetadataCapture, false, true, false));
        Assert.True(NativeAcceptanceFactory.AllowsOperation(AcceptanceOperation.Capture, true, false, false));
    }

    private sealed class Probe : ITargetProbe
    {
        internal bool Foreground = true;
        public TargetCheck Check(InsertionTarget target) => target == A ? TargetCheck.Ok : TargetCheck.Closed;
        public bool IsTargetForeground(InsertionTarget target) => target == A && Foreground;
    }
    private sealed class FakeInput : IAcceptanceInput
    {
        internal int Sends;
        internal bool IsQuiet = true, ThrowSend;
        internal DispatchState Result = DispatchState.Dispatched;
        public bool Quiet(ITargetProbe probe, InsertionTarget target) => IsQuiet && probe.IsTargetForeground(target);
        public DispatchState SendPaste() { Sends++; if (ThrowSend) throw new InvalidOperationException(); return Result; }
    }
    private sealed class FakeGuardian : IGuardianChannel
    {
        internal int Publishes, Restores, Commits;
        internal bool Closed, ThrowPublish, ThrowRestore, CommitResult = true;
        internal Action? AfterPublish;
        internal AcceptanceOperation LastOperation;
        public string RecoveryControl => "fake-owned-recovery";
        public void Dispose() { }
        public bool IsPinned => true;
        public bool CanPublish => !RecoveryPending;
        public bool SnapshotAcknowledged => !ThrowPublish;
        public int SavedCount => 2;
        public long ByteCount => 64;
        public bool RecoveryPending { get; private set; }
        public SwapStatus Publish(AcceptanceOperation operation)
        {
            Publishes++; LastOperation = operation; RecoveryPending = true;
            if (ThrowPublish) throw new InvalidOperationException();
            AfterPublish?.Invoke(); return SwapStatus.Swapped;
        }
        public bool CommitCopy() { Commits++; if (CommitResult) RecoveryPending = false; return CommitResult; }
        public RestoreStatus Restore() { Restores++; if (ThrowRestore) throw new InvalidOperationException(); RecoveryPending = false; return RestoreStatus.Restored; }
        public void CloseAdmission() => Closed = true;
    }
}
