using System.Text;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Uno.Platforms.Windows;
using Xunit;

namespace SmartSnippets.Uno;

public sealed class BootstrapProtocolTests
{
    [Theory]
    [InlineData("restore")] [InlineData("abort")]
    public void NativeSceneState_RequiresGuardianBeforeJobBeforePublication(string name)
    {
        var scene = name == "restore" ? NativeScene.Restore : NativeScene.Abort;
        var state = new NativeSceneState(scene);
        Assert.Throws<InvalidOperationException>(state.Assign);
        Assert.Throws<InvalidOperationException>(state.Publish);
        state.Ready(); state.Assign(); state.Publish();
        Assert.False(state.Settled);
        Assert.Throws<InvalidOperationException>(state.Publish);
        if (scene == NativeScene.Restore) state.Restored(); else state.GuardianStopped(0);
        Assert.True(state.Settled);
    }
    [Fact]
    public void NativeSceneState_AbortRequiresAcknowledgedPublicationAndKnownSuccessfulGuardianExit()
    {
        var state = new NativeSceneState(NativeScene.Abort);
        Assert.Throws<InvalidOperationException>(() => state.GuardianStopped(0));
        state.Ready(); state.Assign(); state.Publish();
        Assert.Throws<InvalidOperationException>(() => state.GuardianStopped(2));
        Assert.False(state.Settled);
        Assert.Throws<InvalidOperationException>(state.Restored);
    }
    [Theory]
    [InlineData("")] [InlineData("guardian_ready 1 2 other")]
    [InlineData("guardian_ready -1 2 ss-uno019-recovery-00000000000000000000000000000000")]
    [InlineData("guardian_ready 1 2 ss-uno019-recovery-00000000000000000000000000000000 extra")]
    public void BootstrapProtocol_RejectsForeignOrMalformedReady(string receipt) => Assert.Throws<IOException>(() => BootstrapProtocol.Ready(receipt));
    [Fact]
    public void BootstrapProtocol_AcceptsOnlyBoundedMetadataReady() => Assert.Equal(4, BootstrapProtocol.Ready("guardian_ready 1 2 ss-uno019-recovery-00000000000000000000000000000000").Length);
    [Fact]
    public async Task BootstrapProtocol_ReadAndWriteRoundtrip()
    {
        using var stream = new MemoryStream();
        await BootstrapProtocol.WriteAsync(stream, "published recovery_pending", CancellationToken.None);
        stream.Position = 0;
        Assert.Equal("published recovery_pending", await BootstrapProtocol.ReadAsync(stream, CancellationToken.None));
    }
    [Theory]
    [InlineData("unfinished")] [InlineData("bad\r\n")]
    public async Task BootstrapProtocol_EofAndControlBytesFailClosed(string line)
    {
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes(line));
        await Assert.ThrowsAsync<IOException>(() => BootstrapProtocol.ReadAsync(stream, CancellationToken.None));
    }
    [Fact]
    public async Task BootstrapProtocol_OversizeFailsClosed()
    {
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes(new string('a', 513) + "\n"));
        await Assert.ThrowsAsync<IOException>(() => BootstrapProtocol.ReadAsync(stream, CancellationToken.None));
        await Assert.ThrowsAsync<IOException>(() => BootstrapProtocol.WriteAsync(stream, "payload\n", CancellationToken.None));
    }
    [Fact]
    public async Task LateNativeOperations_BindDoesNotAdmitAndRecoveryRemainsReachableAfterClose()
    {
        var broker = new LateNativeOperations();
        Assert.False(broker.IsAvailable);
        Assert.False(broker.Validate(CandidateNativeOperation.Input, Guid.NewGuid(), 1, 2));
        Assert.Equal(DispatchState.NotDispatched, broker.SendPaste());
        var operations = new Operations();
        var id = Guid.NewGuid();
        broker.Bind(operations, id);
        Assert.False(broker.TrySetText("synthetic"));
        Assert.Equal(0, operations.Calls);
        broker.Admit();
        Assert.True(broker.Validate(CandidateNativeOperation.Copy, Guid.NewGuid(), 1, 2));
        Assert.Equal(id, operations.Session);
        broker.Close();
        Assert.False(broker.IsAvailable);
        Assert.True(await broker.DrainOwnedRecoveryAsync());
        Assert.Equal(1, operations.Drains);
        Assert.Throws<InvalidOperationException>(() => broker.Bind(operations, id));
        Assert.Throws<InvalidOperationException>(() => broker.DiscardClipboardSnapshot(null!));
    }
    [Fact]
    public void LateNativeOperations_ZeroPermissionCapabilityCannotBecomePositiveAfterBinding()
    {
        var broker = new LateNativeOperations();
        var operations = new Operations();
        var id = Guid.NewGuid();
        var capability = new CandidateNativeCapability(id, 1, 2, (CandidateNativeOperation)0, TimeSpan.FromSeconds(20), TimeProvider.System, broker);
        broker.Bind(operations, id); broker.Admit();
        foreach (var permission in Enum.GetValues<CandidateNativeOperation>()) Assert.False(capability.Allows(permission));
        Assert.Equal(0, operations.Calls);
    }
    [Fact]
    public async Task NativeRecoveryLifetime_CleanupExceptionStillRetainsGuardian()
    {
        var pending = 0;
        var retained = false;
        await NativeRecoveryLifetime.CleanupAsync(() => throw new IOException(), () => { retained = true; return Task.CompletedTask; }, () => pending++);
        Assert.True(retained); Assert.Equal(1, pending);
    }
    [Fact]
    public async Task NativeRecoveryLifetime_PendingDoesNotEndRetentionOrForceCleanup()
    {
        var checks = 0;
        var pending = 0;
        var result = await NativeRecoveryLifetime.RetainAsync(() => ++checks == 6, () => 0u, () => Task.CompletedTask, () => pending++, 2);
        Assert.Equal(0u, result); Assert.Equal(6, checks); Assert.Equal(1, pending);
    }
    private sealed class Operations : ICandidateNativeOperations
    {
        internal int Calls, Drains;
        internal Guid Session;
        public bool Validate(CandidateNativeOperation operation, Guid id, nint window, uint pid) { Calls++; Session = id; return true; }
        public InsertionTarget? CaptureOwnedTarget(nint window, uint pid) => null;
        public TargetCheck Check(InsertionTarget target) => TargetCheck.Closed;
        public bool IsTargetForeground(InsertionTarget target) => false;
        public Task<FocusPath?> RestoreFocusAsync(InsertionTarget target, CancellationToken ct) => Task.FromResult<FocusPath?>(null);
        public bool ReleaseHeldModifiers() => false;
        public ClipboardSwap TrySwapClipboard(string text, bool allowIncomplete) => new(SwapStatus.NotPreserved, null);
        public SwapStatus TryPlaceText(string text) => SwapStatus.NotPreserved;
        public DispatchState SendPaste() => DispatchState.NotDispatched;
        public bool ReleasePasteKeys() => false;
        public RestoreStatus TryRestoreClipboard(IClipboardSnapshot snapshot) => RestoreStatus.Failed;
        public void DiscardClipboardSnapshot(IClipboardSnapshot snapshot) => throw new InvalidOperationException();
        public bool IsAvailable => true;
        public bool TrySetText(string text) { Calls++; return true; }
        public void SetText(string text) => _ = TrySetText(text);
        public Task<bool> DrainOwnedRecoveryAsync() { Drains++; return Task.FromResult(true); }
    }
}
