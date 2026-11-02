using SmartSnippets.Core.Layout;
using SmartSnippets.Uno.Platforms.Windows;
using SmartSnippets.Uno.Tests.Build;

namespace SmartSnippets.Uno.Tests.Flows;

public sealed class PopupMonitorPlacementTests
{
    [Fact]
    public void CursorMonitorWinsOverCapturedTargetAndPrimary()
    {
        var centered = new List<nint>();
        Assert.True(PopupMonitorPlacement.Center(() => 11, 22,
            () => throw new InvalidOperationException("Primary must not be sampled"),
            monitor => { centered.Add(monitor); return true; }));
        Assert.Equal(new nint[] { 11 }, centered);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void CursorSamplingOrPlacementFailure_FallsBackToCapturedMonitor(int cursor)
    {
        var centered = new List<nint>();
        Assert.True(PopupMonitorPlacement.Center(() => cursor, 22,
            () => throw new InvalidOperationException("Captured monitor must win"),
            monitor => { centered.Add(monitor); return monitor == 22; }));
        Assert.Equal(cursor == 0 ? new nint[] { 22 } : new nint[] { 11, 22 }, centered);
    }

    [Fact]
    public void InvalidCursorAndCapturedMonitor_UsesPrimary_NotBottomLeft()
    {
        var centered = new List<nint>();
        Assert.True(PopupMonitorPlacement.Center(() => 11, 22, () => 33,
            monitor => { centered.Add(monitor); return monitor == 33; }));
        Assert.Equal(new nint[] { 11, 22, 33 }, centered);
        Assert.False(PopupMonitorPlacement.Center(() => 0, 0, () => 0,
            _ => throw new InvalidOperationException("No valid monitor")));
    }

    [Fact]
    public void ReshowSamplesCurrentCursorAgain_WithoutCachingCapturedMonitor()
    {
        nint cursor = 11; var centered = new List<nint>();
        bool Show() => PopupMonitorPlacement.Center(() => cursor, 22, () => 33,
            monitor => { centered.Add(monitor); return true; });
        Assert.True(Show()); cursor = 44; Assert.True(Show());
        Assert.Equal(new nint[] { 11, 44 }, centered);
    }

    [Fact]
    public void DpiScaledCenterHandlesNegativeMonitorOrigin()
    {
        var work = new PixelRect(-1920, -1080, 0, 0);
        var width = (int)Math.Round(800 * 1.5);
        var height = (int)Math.Round(600 * 1.5);
        Assert.Equal(new PixelPoint(-1560, -990), PopupPlacement.CenterIn(work, width, height));
    }

    [Fact]
    public void CapturedAndReshowUseSameCursorPlacement()
    {
        string Read(string head, string file) => File.ReadAllText(Path.Combine(BuildGraphTests.RepoRoot, "src", head, file)).Replace("\r\n", "\n");
        var placement = Read("SmartSnippets.Uno", "Services/WindowPlacement.cs");
        Assert.Contains("PInvoke.GetCursorPos(&cursor)", placement);
        Assert.Contains("PInvoke.MonitorFromPoint(cursor, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST)", placement);
        Assert.Contains("PInvoke.MonitorFromWindow(default, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTOPRIMARY)", placement);
        var popup = Read("SmartSnippets.Uno", "Popup/PopupWindow.xaml.cs");
        var centerStart = popup.IndexOf("    private void Center()", StringComparison.Ordinal);
        var centerEnd = popup.IndexOf("    public void Hide()", centerStart, StringComparison.Ordinal);
        var center = popup[centerStart..centerEnd];
        Assert.Contains("if (ViewModel.IsPreview)", center);
        Assert.Contains("WindowPlacement.CenterAtCursor(AppWindow, ViewModel.Target?.MonitorHandle ?? 0", center);
        var ordinary = center[center.IndexOf("if (!WindowPlacement.CenterAtCursor", StringComparison.Ordinal)..];
        Assert.DoesNotContain("ParkBottomLeft", ordinary);
        var reshow = popup[popup.IndexOf("    private PopupShowResult ShowAgainReady()", StringComparison.Ordinal)..];
        Assert.Contains("Center(); ShowSurface(false)", reshow);
    }
}
