using System;
using System.Threading.Tasks;
using SmartSnippets.Uno.Platforms.Windows.AcceptanceNative;
using Xunit;

namespace SmartSnippets.Uno.Tests.AcceptanceNative;

public sealed class ClipboardControlReadinessTests
{
    [Theory]
    [InlineData((int)ClipboardControlAdmission.Ready)]
    [InlineData((int)ClipboardControlAdmission.Closed)]
    [InlineData((int)ClipboardControlAdmission.Permission)]
    [InlineData((int)ClipboardControlAdmission.GuardianUnavailable)]
    [InlineData((int)ClipboardControlAdmission.GuardianUnpinned)]
    [InlineData((int)ClipboardControlAdmission.PopupUnpinned)]
    [InlineData((int)ClipboardControlAdmission.TargetIdentity)]
    [InlineData((int)ClipboardControlAdmission.Desktop)]
    public async Task NonUnquiet_ReturnsImmediatelyWithoutFallthrough(int value)
    {
        var reason = (ClipboardControlAdmission)value;
        var calls = 0;
        Assert.Equal(reason, await ClipboardControlReadiness.WaitAsync(() => { calls++; return reason; }, () => true, default));
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Unquiet_TimesOutAtFourSecondsWithoutAdmission()
    {
        var clock = new Clock();
        Assert.Equal(ClipboardControlAdmission.TimedOut, await ClipboardControlReadiness.WaitAsync(
            () => { clock.Ticks = 4; return ClipboardControlAdmission.Unquiet; }, () => true, default, clock));
    }

    [Fact]
    public async Task ChangedForeground_RefusesBeforeClassifier()
    {
        Assert.Equal(ClipboardControlAdmission.ForegroundChanged, await ClipboardControlReadiness.WaitAsync(
            () => throw new InvalidOperationException(), () => false, default));
    }

    private sealed class Clock : TimeProvider
    {
        internal long Ticks;
        public override long TimestampFrequency => 1;
        public override long GetTimestamp() => Ticks;
    }
}
