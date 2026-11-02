using SmartSnippets.Uno.Services;

namespace SmartSnippets.Uno.Tests.Build;

public sealed class IdleMemoryTrimmerTests
{
    [Fact]
    public void RealServiceDelaysCancelsAndTrimsOncePerIdleEpoch()
    {
        long now = 0;
        var idle = true;
        var calls = 0;
        using var trimmer = new IdleMemoryTrimmer(() => idle, () => { calls++; return true; }, () => now);
        trimmer.Poll(); now = 2999; trimmer.Poll(); Assert.Equal(0, calls);
        idle = false; trimmer.Poll(); idle = true; now = 3000; trimmer.Poll();
        now = 5999; trimmer.Poll(); Assert.Equal(0, calls);
        now = 6000; trimmer.Poll(); now = 9000; trimmer.Poll(); Assert.Equal(1, calls);
        idle = false; trimmer.Poll(); idle = true; now = 10000; trimmer.Poll();
        now = 13000; trimmer.Poll(); Assert.Equal(2, calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FailedOrThrowingNativeTrimIsBestEffortAndDoesNotRetryWhileIdle(bool throws)
    {
        long now = 0;
        var calls = 0;
        using var trimmer = new IdleMemoryTrimmer(() => true, () =>
        {
            calls++;
            if (throws) throw new InvalidOperationException("synthetic failure");
            return false;
        }, () => now);
        trimmer.Poll(); now = 3000; trimmer.Poll(); now = 10000; trimmer.Poll();
        Assert.Equal(1, calls);
    }

    [Fact]
    public void DisposalCancelsPendingWorkAndDoesNotCallIdlePredicateAgain()
    {
        long now = 0;
        var admissions = 0;
        var calls = 0;
        var trimmer = new IdleMemoryTrimmer(() => { admissions++; return true; }, () => { calls++; return true; }, () => now);
        trimmer.Poll(); trimmer.Dispose(); trimmer.Dispose();
        now = 10000; trimmer.Poll();
        Assert.Equal(1, admissions); Assert.Equal(0, calls);
    }

    [Fact]
    public void SettleRequestTrimsOnceWhileMainWindowStaysOpen()
    {
        long now = 0;
        var calls = 0;
        using var trimmer = new IdleMemoryTrimmer(() => false, () => { calls++; return true; }, () => now, settled: () => true);
        trimmer.Poll(); now = 10000; trimmer.Poll(); Assert.Equal(0, calls);
        trimmer.RequestSettleTrim();
        now = 12999; trimmer.Poll(); Assert.Equal(0, calls);
        now = 13000; trimmer.Poll(); now = 30000; trimmer.Poll(); Assert.Equal(1, calls);
        trimmer.RequestSettleTrim(); now = 33000; trimmer.Poll(); Assert.Equal(2, calls);
    }

    [Fact]
    public void DraggingOrBusyWorkPostponesButDoesNotDropTheSettleTrim()
    {
        long now = 0;
        var settled = true;
        var calls = 0;
        using var trimmer = new IdleMemoryTrimmer(() => false, () => { calls++; return true; }, () => now, settled: () => settled);
        trimmer.PostponeSettleTrim(); now = 5000; trimmer.Poll(); Assert.Equal(0, calls);
        trimmer.RequestSettleTrim();
        now = 7000; trimmer.PostponeSettleTrim(); now = 9999; trimmer.Poll(); Assert.Equal(0, calls);
        settled = false; trimmer.Poll(); settled = true;
        now = 12998; trimmer.Poll(); Assert.Equal(0, calls);
        now = 12999; trimmer.Poll(); Assert.Equal(1, calls);
    }

    [Fact]
    public void TrayTrimSatisfiesAPendingSettleRequest()
    {
        long now = 0;
        var calls = 0;
        using var trimmer = new IdleMemoryTrimmer(() => true, () => { calls++; return true; }, () => now, settled: () => true);
        trimmer.RequestSettleTrim(); trimmer.Poll();
        now = 3000; trimmer.Poll(); now = 10000; trimmer.Poll();
        Assert.Equal(1, calls);
    }

    [Fact]
    public void DisposedTrimmerIgnoresSettleRequests()
    {
        long now = 0;
        var calls = 0;
        var trimmer = new IdleMemoryTrimmer(() => false, () => { calls++; return true; }, () => now, settled: () => true);
        trimmer.Dispose(); trimmer.RequestSettleTrim();
        now = 10000; trimmer.Poll();
        Assert.Equal(0, calls);
    }
}
