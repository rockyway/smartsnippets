using SmartSnippets.Uno.Services;

namespace SmartSnippets.Uno.Tests.Build;

public sealed class IdleTrimScheduleTests
{
    [Fact]
    public void TrimsOnceAfterThreeSecondsOfContinuousIdle()
    {
        var schedule = new IdleTrimSchedule();
        Assert.False(schedule.Poll(0, true));
        Assert.False(schedule.Poll(2999, true));
        Assert.True(schedule.Poll(3000, true));
        Assert.False(schedule.Poll(60000, true));
    }

    [Fact]
    public void ShowingOrBecomingBusyCancelsPendingTrim()
    {
        var schedule = new IdleTrimSchedule();
        Assert.False(schedule.Poll(100, true));
        Assert.False(schedule.Poll(3000, false));
        Assert.False(schedule.Poll(3100, true));
        Assert.False(schedule.Poll(6099, true));
        Assert.True(schedule.Poll(6100, true));
    }

    [Fact]
    public void EachNewHiddenIdleEpochGetsOneTrim()
    {
        var schedule = new IdleTrimSchedule();
        Assert.False(schedule.Poll(0, true));
        Assert.True(schedule.Poll(3000, true));
        Assert.False(schedule.Poll(4000, false));
        Assert.False(schedule.Poll(5000, true));
        Assert.True(schedule.Poll(8000, true));
        Assert.False(schedule.Poll(9000, true));
    }

    [Fact]
    public void BusyStartupDoesNotStartTheIdleDelay()
    {
        var schedule = new IdleTrimSchedule();
        Assert.False(schedule.Poll(0, false));
        Assert.False(schedule.Poll(10000, false));
        Assert.False(schedule.Poll(20000, true));
        Assert.True(schedule.Poll(23000, true));
    }
}
