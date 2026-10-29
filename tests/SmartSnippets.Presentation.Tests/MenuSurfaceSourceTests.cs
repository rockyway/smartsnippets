namespace SmartSnippets.Presentation.Tests;

/// <summary>Source regression guards only; do not prove WinUI focus, discovery or pixel opacity.</summary>
public sealed class MenuSurfaceSourceTests
{
    private static string Source(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "UiContracts", name));

    [Fact]
    public void PreviewSortMenuIsPlacedBelowButtonWithoutFocusCalls()
    {
        var source = Source("PopupWindow.xaml.cs");
        var start = source.IndexOf("private void QueuePreviewSortMenu()", StringComparison.Ordinal);
        var end = source.IndexOf("private void ReportReady()", start, StringComparison.Ordinal);
        var seam = source[start..end];
        Assert.Contains("SortFlyout.ShowAt(SortButton", seam);
        Assert.Contains("Placement = Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.Bottom", seam);
        Assert.Contains("ShowMode = Microsoft.UI.Xaml.Controls.Primitives.FlyoutShowMode.Transient", seam);
        Assert.DoesNotContain(".Focus(", seam);
        Assert.DoesNotContain("Activate(", seam);
        Assert.DoesNotContain("SetForegroundWindow(", seam);
    }

    [Fact]
    public void AllThreeMenusUseSharedSolidSurfaceHook()
    {
        var popup = Source("PopupWindow.xaml.cs");
        Assert.Contains("FlyoutSurface.KeepSolid(SortFlyout);", popup);
        Assert.Contains("FlyoutSurface.KeepSolid(menu);", popup);
        Assert.Contains("FlyoutSurface.KeepSolid(menu);", Source("SnippetsPage.xaml.cs"));
    }

    [Fact]
    public void SurfaceDiscoveryUsesHostedItemAncestorsAndKeepsReceipt()
    {
        var source = Source("FlyoutSurface.cs");
        Assert.Contains("FromItems(menu)", source);
        Assert.Contains("VisualTreeHelper.GetParent(node)", source);
        Assert.Contains("presenter.SystemBackdrop = null;", source);
        Assert.Contains("if (found is not null) MakeSolid(found);", source);
        Assert.Contains("LogSurface(found);", source);
        Assert.Contains("AppLog.Info(\"menu_surface\"", source);
    }
}
