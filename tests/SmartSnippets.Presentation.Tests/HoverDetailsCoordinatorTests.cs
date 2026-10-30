using SmartSnippets.Presentation.Services;
namespace SmartSnippets.Presentation.Tests;

public sealed class HoverDetailsCoordinatorTests
{
    private readonly List<(long Due, Action Run)> _work = [];
    private long _now;
    private int _shown, _closed;
    private HoverDetailsCoordinator Create() => new((delay, run) => _work.Add((_now + (long)delay.TotalMilliseconds, run)), () => _shown++, () => _closed++);
    private void Advance(int ms)
    {
        _now += ms;
        while (_work.Any(w => w.Due <= _now))
        {
            var next = _work.Where(w => w.Due <= _now).MinBy(w => w.Due);
            _work.Remove(next); next.Run();
        }
    }
    [Fact] public void OpensOnlyAfter500ms()
    {
        using var c = Create(); c.Enter(new object()); Advance(499); Assert.Equal(0, _shown);
        Advance(1); Assert.Equal(1, _shown);
    }
    [Fact] public void SwitchingTargetRestartsAndRejectsOldTick()
    {
        using var c = Create(); c.Enter(new object()); Advance(400); c.Enter(new object());
        Advance(100); Assert.Equal(0, _shown); Advance(400); Assert.Equal(1, _shown);
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public void OwnerHideOrRebindCannotReopen(bool opened)
    {
        using var c = Create(); c.Enter(new object()); if (opened) Advance(500);
        c.Cancel(); Advance(1000); Assert.False(c.IsOpen); Assert.Equal(opened ? 1 : 0, _shown);
    }
    [Fact] public void ShortHoverAndEmptyContentNeverOpen()
    {
        using var c = Create(); c.Enter(new object()); Advance(499); c.OwnerExited(); Advance(1);
        c.Enter(new object(), false); Advance(500); Assert.Equal(0, _shown);
    }
    [Fact] public void HandoffAndNeighborTransitPreserveSurface()
    {
        using var c = Create(); c.Enter(new object()); Advance(500); c.OwnerExited();
        Advance(60); c.SurfaceEntered(); Advance(1000);
        Assert.Equal(1, _shown); Assert.True(c.IsOpen);
        c.SurfaceExited(); Advance(119); Assert.True(c.IsOpen); Advance(1); Assert.False(c.IsOpen);
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public void ScrollbarPressSuppressesDismissalUntilReleaseOrCaptureLoss(bool captureLoss)
    {
        using var c = Create(); c.Enter(new object()); Advance(500); c.SurfaceEntered(); c.OwnerExited();
        c.Pressed(); c.SurfaceExited(); Advance(1000); Assert.True(c.IsOpen);
        if (captureLoss) c.CaptureLost(); else c.Released(); Advance(120);
        Assert.False(c.IsOpen); Assert.Equal(1, _closed);
    }
    [Fact] public void CaptureReleaseRechecksActualRegionsEvenIfExitWasNotDelivered()
    {
        using var c = Create(); c.Enter(new object()); Advance(500); c.SurfaceEntered(); c.Pressed();
        c.UpdateRegions(false, false); c.Released(); Advance(120); Assert.False(c.IsOpen);
    }
    [Fact] public void NeighborClockRunsInParallelWithOldHandoff()
    {
        using var old = Create(); old.Enter(new object()); Advance(500); old.OwnerExited();
        using var next = Create(); next.Enter(new object()); Advance(120);
        Assert.False(old.IsOpen); Assert.False(next.IsOpen);
        Advance(379); Assert.False(next.IsOpen); Advance(1); Assert.True(next.IsOpen);
    }
    [Fact] public void CrossingMetadataKeepsExternalContentButSettlingReplacesItAfterFullDelay()
    {
        using var content = Create(); content.Enter(new object()); Advance(500);
        content.OwnerExited(); Advance(60); content.OwnerEntered(); // Root-owned horizontal transit region.
        using var metadata = new HoverDetailsCoordinator(
            (delay, run) => _work.Add((_now + (long)delay.TotalMilliseconds, run)),
            () => { content.Cancel(); _shown++; }, () => _closed++);
        metadata.Enter(new object()); Advance(499);
        Assert.True(content.IsOpen); Assert.False(metadata.IsOpen);
        Advance(1);
        Assert.False(content.IsOpen); Assert.True(metadata.IsOpen);
    }
    [Fact] public void PassingThroughMetadataCancelsOnlyItsPendingClock()
    {
        using var content = Create(); content.Enter(new object()); Advance(500);
        using var metadata = Create(); metadata.Enter(new object()); Advance(100); metadata.TargetExited();
        content.OwnerExited(); Advance(60); content.SurfaceEntered(); Advance(1000);
        Assert.True(content.IsOpen); Assert.False(metadata.IsOpen); Assert.Equal(1, _shown);
    }
    [Fact] public void SwitchingTargetsOnOpenRowRestartsFullDelay()
    {
        using var c = Create(); c.Enter(new object()); Advance(500); c.TargetExited();
        c.Enter(new object()); Assert.False(c.IsOpen); Advance(499); Assert.Equal(1, _shown);
        Advance(1); Assert.Equal(2, _shown);
    }
    [Fact] public void TargetExitBeforeOpenCancelsEvenWhenRowRemainsInside()
    {
        using var c = Create(); c.Enter(new object()); Advance(400); c.TargetExited(); c.OwnerEntered();
        Advance(1000); Assert.Equal(0, _shown);
    }
    [Fact] public void OpenSurfaceStaysWhilePointerIsInsideRow()
    {
        using var c = Create(); c.Enter(new object()); Advance(500); c.TargetExited(); Advance(1000);
        Assert.True(c.IsOpen); c.OwnerExited(); Advance(120); Assert.False(c.IsOpen);
    }
    [Fact] public void PlacementPrefersRightFlipsLeftAndClamps()
    {
        var right = HoverDetailsPlacement.Place(1000, 700, 100, 400, 420, 480, 320);
        Assert.Equal(112, right.X); Assert.Equal(74, right.Y);
        var left = HoverDetailsPlacement.Place(1000, 700, 900, 640, 660, 480, 320);
        Assert.Equal(408, left.X); Assert.Equal(314, left.Y);
        Assert.True(left.Y + left.Height < 640);
        var small = HoverDetailsPlacement.Place(200, 100, 190, 85, 95, 480, 320);
        Assert.Equal(184, small.Width); Assert.Equal(79, small.Height);
        Assert.InRange(small.X, 0, 16); Assert.Equal(0, small.Y);
    }
    [Fact] public void PlacementKeepsNarrowMetadataRightOfCursorAndAboveTarget()
    {
        var result = HoverDetailsPlacement.Place(1000, 700, 350, 100, 120, 96, 40);
        Assert.Equal(362, result.X);
        Assert.Equal(54, result.Y);
        Assert.Equal(96, result.Width);
        Assert.Equal(40, result.Height);
    }
    [Fact] public void PlacementShrinksToAvailableSpaceWithoutCoveringTarget()
    {
        var result = HoverDetailsPlacement.Place(500, 200, 100, 70, 100, 240, 320);
        Assert.Equal(106, result.Y);
        Assert.Equal(94, result.Height);
    }
    [Fact] public void KeyboardOpenIsImmediateAndRejectsOldHoverTicks()
    {
        using var c = Create(); c.Enter(new object()); Advance(400);
        c.OpenPinned(new object()); Assert.Equal(1, _shown); Assert.True(c.IsOpen);
        Advance(1000); Assert.Equal(1, _shown);
    }
    [Fact] public void KeyboardPinSurvivesPointerExitCaptureAndSelectionUpdate()
    {
        using var c = Create(); c.OpenPinned(new object()); c.TargetExited(); c.OwnerExited(); c.SurfaceExited();
        c.Pressed(); c.Released(); c.CaptureLost(); Advance(1000);
        Assert.True(c.IsOpen); Assert.Equal(0, _closed);
        c.OpenPinned(new object()); Assert.Equal(2, _shown); Assert.Equal(0, _closed);
    }
    [Fact] public void PointerTakeoverReturnsToDelayedMode()
    {
        using var c = Create(); c.OpenPinned(new object()); c.Enter(new object());
        Assert.False(c.IsOpen); Assert.Equal(1, _closed);
        Advance(499); Assert.Equal(1, _shown); Advance(1); Assert.Equal(2, _shown);
        c.OwnerExited(); Advance(120); Assert.False(c.IsOpen);
    }
    [Fact] public void HostCloseAndEmptyContentCancelKeyboardPin()
    {
        using var c = Create(); c.OpenPinned(new object()); c.Cancel(); Advance(1000);
        Assert.False(c.IsOpen); Assert.Equal(1, _closed);
        c.OpenPinned(new object(), false); Assert.False(c.IsOpen); Assert.Equal(1, _shown);
        c.Dispose(); c.OpenPinned(new object()); Assert.Equal(1, _shown);
    }
    [Fact] public void DisposeRejectsPendingAndFutureRequests()
    {
        var c = Create(); c.Enter(new object()); c.Dispose(); c.Enter(new object()); Advance(2000); Assert.Equal(0, _shown);
    }
}
