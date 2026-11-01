using SmartSnippets.Presentation.Services;
using Xunit;

namespace SmartSnippets.Uno;

public sealed class PositiveNativeMutationTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void NativeMutationRequiresTheSeparatePostPermitGrant(bool needsMutation)
    {
        Assert.Equal(!needsMutation, PositiveNativeMutation.Permits(false, true, true, false, needsMutation, false));
        Assert.True(PositiveNativeMutation.Permits(false, true, true, false, needsMutation, true));
        Assert.False(PositiveNativeMutation.Permits(false, false, true, false, needsMutation, true));
    }

    [Fact]
    public void CaptureAndClipboardOnlyControlsRetainTheirExistingAdmission()
    {
        Assert.True(PositiveNativeMutation.Permits(true, false, false, false, true, false));
        Assert.True(PositiveNativeMutation.Permits(false, true, false, true, false, false));
        Assert.False(PositiveNativeMutation.Permits(false, false, false, true, false, true));
        Assert.False(PositiveNativeMutation.Permits(false, true, false, false, true, true));
    }

    [Theory]
    [InlineData(false, true, false, true)]
    [InlineData(true, false, false, true)]
    [InlineData(true, true, true, true)]
    [InlineData(true, true, false, false)]
    public void UnexpectedPopupWorkDuringPermitWaitCannotArmNativeMutation(bool keyboard, bool shown, bool inserting, bool idle)
    {
        var mutation = new PositiveNativeMutation();
        Assert.False(mutation.TryArm(keyboard, shown, inserting, idle));
        Assert.False(mutation.Armed);
        Assert.False(mutation.TryArm(true, true, false, true)); // Failed admission is permanently closed.
    }

    [Fact]
    public void NativeGrantIsOneShotAndCloseCannotBeReversed()
    {
        var mutation = new PositiveNativeMutation();
        Assert.True(mutation.TryArm(true, true, false, true));
        Assert.True(mutation.Armed);
        mutation.Close();
        Assert.False(mutation.Armed);
        Assert.False(mutation.TryArm(true, true, false, true));
    }

    [Theory]
    [InlineData(true, true, false)]
    [InlineData(false, false, false)]
    [InlineData(false, true, true)]
    public async Task UndispatchedEnterCannotSettleAcrossActiveVmGateOrRecovery(bool inserting, bool idle, bool recovery)
    {
        var mutation = new PositiveNativeMutation();
        var keyboard = new PositiveKeyboardLifetime(); keyboard.Arm();
        Assert.True(mutation.TryArm(keyboard.Armed, true, false, true));
        var receipts = 0;
        Assert.False(await mutation.TrySettleUndispatchedAsync(DispatchState.NotDispatched, inserting, idle, recovery,
            keyboard, () => { receipts++; return Task.CompletedTask; }));
        Assert.True(keyboard.Armed); Assert.Equal(0, receipts);
    }

    [Theory]
    [InlineData(DispatchState.Dispatched)] [InlineData(DispatchState.Uncertain)]
    public async Task AcceptedOrUncertainInputCannotUseZeroEventSettlement(DispatchState state)
    {
        var mutation = new PositiveNativeMutation();
        var keyboard = new PositiveKeyboardLifetime(); keyboard.Arm();
        Assert.True(mutation.TryArm(keyboard.Armed, true, false, true));
        Assert.False(await mutation.TrySettleUndispatchedAsync(state, false, true, false,
            keyboard, () => throw new Exception("must retain keyboard")));
        Assert.True(keyboard.Armed);
    }

    [Fact]
    public async Task MutationRevokesBeforeSettlementAwaitAndKeyboardRetainsUntilAcknowledged()
    {
        var mutation = new PositiveNativeMutation();
        var keyboard = new PositiveKeyboardLifetime(); keyboard.Arm();
        Assert.True(mutation.TryArm(keyboard.Armed, true, false, true));
        var receipt = new TaskCompletionSource();
        var settling = mutation.TrySettleUndispatchedAsync(DispatchState.NotDispatched, false, true, false,
            keyboard, () => receipt.Task);
        Assert.False(settling.IsCompleted);
        Assert.False(mutation.Armed);
        Assert.True(keyboard.Armed);
        Assert.False(PositiveNativeMutation.Permits(false, true, true, false, true, mutation.Armed));
        Assert.False(mutation.TryArm(true, true, false, true));
        receipt.SetResult();
        Assert.True(await settling);
        Assert.False(keyboard.Armed);
    }

    [Fact]
    public async Task LostSettlementReceiptRetainsKeyboardButRevokesNativeMutation()
    {
        var mutation = new PositiveNativeMutation();
        var keyboard = new PositiveKeyboardLifetime(); keyboard.Arm();
        Assert.True(mutation.TryArm(keyboard.Armed, true, false, true));
        await Assert.ThrowsAsync<IOException>(() => mutation.TrySettleUndispatchedAsync(
            DispatchState.NotDispatched, false, true, false, keyboard, () => throw new IOException()));
        Assert.False(mutation.Armed); Assert.True(keyboard.Armed);
    }

    [Fact]
    public async Task UnarmedOrClosedNativeGrantCannotEmitASettlementReceipt()
    {
        var mutation = new PositiveNativeMutation();
        var keyboard = new PositiveKeyboardLifetime();
        Assert.False(await mutation.TrySettleUndispatchedAsync(DispatchState.NotDispatched, false, true, false,
            keyboard, () => throw new Exception()));
        keyboard.Arm();
        Assert.True(mutation.TryArm(true, true, false, true));
        mutation.Close();
        Assert.False(await mutation.TrySettleUndispatchedAsync(DispatchState.NotDispatched, false, true, false,
            keyboard, () => throw new Exception()));
        Assert.True(keyboard.Armed);
    }
}
