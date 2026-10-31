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

    [Fact]
    public async Task PublicationRefusal_NoDispatchNoUsageAndRecoveryRemainsOwned()
    {
        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "publication-" + Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(directory);
        try
        {
            var factory = new SmartSnippets.Core.Data.SqliteConnectionFactory(System.IO.Path.Combine(directory, "test.db"));
            new SmartSnippets.Core.Data.Migrator(factory).MigrateToLatest();
            var store = new SmartSnippets.Core.Store.SnippetStore(factory, TimeProvider.System);
            store.Load();
            var id = store.CreateSnippet(new(GuardianClipboard.SyntheticText, "Synthetic", true, null, "")).Value!.Id;
            var channel = new FakeGuardian { PublicationValid = false, ThrowRestore = true };
            var input = new FakeInput(); var probe = new Probe();
            var adapter = new GuardedPasteDispatcher(probe, A, _ => true, channel, input);
            var service = new InsertionService(store, probe, adapter, delay: (_, _) => Task.CompletedTask);
            var result = await service.InsertAsync(A, id);
            Assert.Equal(InsertionOutcome.PasteFailed, result.Outcome);
            Assert.Equal(0, input.Sends);
            Assert.Equal(0, Assert.Single(store.Snippets).Snippet.UsageCount);
            Assert.False(adapter.KeyboardRecoveryPending);
            Assert.True(adapter.RecoveryPending);
            Assert.Equal(1, channel.Verifications);
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            System.IO.Directory.Delete(directory, recursive: true);
        }
    }

    [Theory]
    [InlineData(0)] // Permission revoked during RPC.
    [InlineData(1)] // Input arrives during RPC.
    [InlineData(2)] // Foreground changes during RPC.
    public void VerificationReturn_RechecksFinalLocalGatesBeforeSend(int change)
    {
        var permitted = true; var probe = new Probe(); var input = new FakeInput();
        var channel = new FakeGuardian { AfterVerify = () =>
        {
            if (change == 0) permitted = false;
            if (change == 1) input.IsQuiet = false;
            if (change == 2) probe.Foreground = false;
        } };
        var adapter = new GuardedPasteDispatcher(probe, A, _ => permitted, channel, input);
        adapter.TrySwapClipboard(GuardianClipboard.SyntheticText, false);
        Assert.Equal(DispatchState.NotDispatched, adapter.SendPaste());
        Assert.Equal(0, input.Sends);
        Assert.False(adapter.KeyboardRecoveryPending);
        Assert.True(adapter.RecoveryPending);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task VerificationResponsePipe_EofTruncationOrMalformedReplyFails(int failure)
    {
        var response = failure switch { 0 => "", 1 => "published", _ => ((char)0).ToString() + Environment.NewLine };
        using var stream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(response));
        using var reader = new System.IO.StreamReader(stream);
        await Assert.ThrowsAnyAsync<Exception>(() => SmartSnippets.WindowsInsertion018.FixedPipe.ReadAsync(
            reader, TimeSpan.FromMilliseconds(50), default));
    }

    [Fact]
    public void GuardianClient_VerificationProtocolFailureIsClosedAndRetainsPending_SourceSupplement()
    {
        var directory = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !System.IO.File.Exists(System.IO.Path.Combine(directory.FullName, "SmartSnippets.slnx")))
            directory = directory.Parent;
        var root = directory!.FullName;
        var source = System.IO.File.ReadAllText(System.IO.Path.Combine(root,
            "src/SmartSnippets.Uno/Platforms/Windows/AcceptanceNative/GuardianClient.cs"));
        var method = source.Split("public bool VerifyPublication()", StringSplitOptions.None)[1]
            .Split("public bool CommitCopy()", StringSplitOptions.None)[0];
        Assert.Contains("closed || !pending || !allowInsert || !IsPinned", method);
        Assert.Contains("Exchange(\"verify-publication\")", method);
        Assert.Contains("if (answer == \"published\") return true;", method);
        Assert.Contains("catch (Exception) { _ = Break(); return false; }", method);
        Assert.DoesNotContain("pending =", method);
        Assert.Contains("private IOException Break() { broken = true;", source);
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
        internal Action? AfterPublish, AfterVerify;
        internal bool PublicationValid = true;
        internal int Verifications;
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
        public bool VerifyPublication() { Verifications++; AfterVerify?.Invoke(); return PublicationValid; }
        public bool CommitCopy() { Commits++; if (CommitResult) RecoveryPending = false; return CommitResult; }
        public RestoreStatus Restore() { Restores++; if (ThrowRestore) throw new InvalidOperationException(); RecoveryPending = false; return RestoreStatus.Restored; }
        public void CloseAdmission() => Closed = true;
    }
}
