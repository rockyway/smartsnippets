using SmartSnippets.UnoProbe;
using SmartSnippets.Presentation.Services;
using Xunit;

namespace CompanionPointer.Tests;

public sealed class CompanionPointerRouteTests
{
    private readonly List<(TimeSpan Delay, Action Callback)> _scheduled = [];
    private readonly List<string> _receipts = [];
    private int _shows, _hides;
    private CompanionPointerRoute Create() => new((delay, callback) => _scheduled.Add((delay, callback)),
        () => _shows++, () => _hides++, _receipts.Add);
    private void Fire(int index) => _scheduled[index].Callback();

    [Fact]
    public void ActualRouteUsesDelayedPresentationPolicyAndHoldsAcrossExpiredLeave()
    {
        using var route = Create();
        route.RowEntered();
        Assert.Equal(0, _shows);
        Assert.Equal(HoverDetailsCoordinator.OpenDelay, _scheduled[0].Delay);
        Fire(0);
        Assert.True(route.IsOpen);
        Assert.Equal(1, _shows);
        route.RowExited();
        Assert.True(route.IsHandingOff);
        Assert.Equal(HoverDetailsCoordinator.HandoffDelay, _scheduled[1].Delay);
        route.SurfaceEntered();
        Fire(1); // A stale row-leave callback must not close the native surface.
        Assert.True(route.IsOpen);
        Assert.False(route.IsHandingOff);
        Assert.Equal(0, _hides);
        route.SurfaceExited();
        Assert.False(route.Complete);
        Fire(2);
        Assert.True(route.Complete);
        Assert.Equal(1, _hides);
        Assert.Equal(new[] { "ui_companion_pointer_row_entered", "ui_companion_pointer_row_exited",
            "ui_companion_pointer_surface_entered", "ui_companion_pointer_surface_exited" }, _receipts);
    }

    [Fact]
    public void MissedHandoffClosesButCannotClaimCompletion()
    {
        using var route = Create();
        route.RowEntered(); Fire(0); route.RowExited(); Fire(1);
        Assert.False(route.IsOpen);
        Assert.False(route.Complete);
        Assert.Equal(1, _hides);
        Assert.Throws<InvalidOperationException>(route.SurfaceEntered);
    }

    [Fact]
    public void NoCallbackCannotOpenAndSurfaceCannotFabricateRowEntry()
    {
        using var route = Create();
        Assert.Empty(_scheduled);
        Assert.Equal(0, _shows);
        Assert.False(route.Complete);
        Assert.Throws<InvalidOperationException>(route.SurfaceEntered);
    }

    [Fact]
    public void DuplicateRowCallbackFailsClosedRatherThanCountingAsNativeEvidence()
    {
        using var route = Create();
        route.RowEntered();
        Assert.Throws<InvalidOperationException>(route.RowEntered);
        Assert.Single(_scheduled);
        Assert.False(route.Complete);
    }

    [Fact]
    public void DisposeInvalidatesPendingNativeOpen()
    {
        var route = Create();
        route.RowEntered(); route.Dispose(); Fire(0);
        Assert.Equal(0, _shows);
        Assert.Equal(0, _hides);
        Assert.False(route.Complete);
    }
}
