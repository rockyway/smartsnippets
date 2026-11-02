using SmartSnippets.Uno.Platforms.Windows;
using Xunit;

namespace SmartSnippets.Uno;

public sealed class PositiveSceneChecksTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PreparationWaitsForReadySurfaceOrRefusesWithoutGrant(bool becomesReady)
    {
        long elapsed = 0;
        var task = PositiveSceneChecks.WaitForWarmupAsync(() => becomesReady && elapsed == 100,
            () => elapsed, () => { elapsed += 100; return Task.CompletedTask; });
        if (becomesReady) { await task; Assert.Equal(100, elapsed); }
        else Assert.Equal("popup_warmup_timeout", (await Assert.ThrowsAsync<PositiveSceneFailure>(() => task)).Token);
    }

    [Theory]
    [InlineData(false, true, true, "callback_duplicate")]
    [InlineData(true, false, true, "shortcut_foreground_not_ready")]
    [InlineData(true, true, false, "popup_show_refused")]
    public async Task ShortcutReportsExactThrowSite(bool accept, bool ready, bool show, string reason)
    {
        var failure = await Assert.ThrowsAsync<PositiveSceneFailure>(() => PositiveSceneChecks.ShortcutAsync(
            () => accept, () => Task.FromResult(ready), () => show));
        Assert.Equal(reason, failure.Token);
    }

    [Theory]
    [InlineData(true, "shortcut_readiness_failed")]
    [InlineData(false, "popup_show_failed")]
    public async Task PropagatedErrorsHaveFixedReasonWithoutExceptionPayload(bool duringReadiness, string token)
    {
        var failure = await Assert.ThrowsAsync<PositiveSceneFailure>(() => PositiveSceneChecks.ShortcutAsync(() => true,
            () => duringReadiness ? Task.FromException<bool>(new InvalidOperationException("private payload")) : Task.FromResult(true),
            () => throw new InvalidOperationException("private payload")));
        Assert.Equal(token, failure.Token);
        Assert.DoesNotContain("private payload", failure.Message);
        Assert.Null(failure.InnerException);
    }

    [Theory]
    [InlineData(500, true, true)]
    [InlineData(500, false, false)]
    [InlineData(501, true, false)]
    public void NullGapNeverProvesReadiness(int duration, bool allowedEnd, bool accepted)
    {
        var gap = new ForegroundNullGap();
        Assert.True(gap.Observe(true, false, 0));
        Assert.True(gap.Pending);
        Assert.Equal(accepted, gap.Observe(false, allowedEnd, duration));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CopyCloseRestoresBeforeHidingAndNeverHidesOnRefusal(bool accepted)
    {
        var restored = new TaskCompletionSource<bool>();
        var hidden = false;
        var task = PositiveSceneChecks.RestoreBeforeHideAsync(() => restored.Task, () => hidden = true);
        Assert.False(hidden);
        restored.SetResult(accepted);
        if (accepted) await task;
        else Assert.Equal("copy_restore_refused", (await Assert.ThrowsAsync<PositiveSceneFailure>(() => task)).Token);
        Assert.Equal(accepted, hidden);
    }

    [Fact]
    public async Task NullAtEndTimesOutRatherThanCountingAsPopup()
    {
        long elapsed = 0;
        var failure = await Assert.ThrowsAsync<PositiveSceneFailure>(() => PositiveSceneChecks.PopupReadyAsync(
            () => 0, 10, 30, () => true, () => elapsed, () => { elapsed += 50; return Task.CompletedTask; }));
        Assert.Equal("popup_foreground_refused", failure.Token);
        Assert.Equal(550, elapsed);
    }

    [Theory]
    [InlineData(10, 500, true)]
    [InlineData(30, 500, true)]
    [InlineData(99, 100, false)]
    [InlineData(10, 501, false)]
    public async Task PopupWaitUsesBoundedNullGap(int end, int duration, bool accepted)
    {
        long elapsed = 0;
        nint foreground = 0;
        var task = PositiveSceneChecks.PopupReadyAsync(() => foreground, 10, 30, () => true,
            () => elapsed, () => { elapsed += duration; foreground = elapsed == duration ? end : 30; return Task.CompletedTask; });
        if (accepted) await task;
        else Assert.Equal("popup_foreground_refused", (await Assert.ThrowsAsync<PositiveSceneFailure>(() => task)).Token);
    }
}
