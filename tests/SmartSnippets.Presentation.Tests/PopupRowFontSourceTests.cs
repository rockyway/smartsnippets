namespace SmartSnippets.Presentation.Tests;

/// <summary>Supplementary source guards, not rendered or focus evidence.</summary>
public sealed class PopupRowFontSourceTests
{
    private static string Source(string path) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "UiContracts", path));
    [Fact]
    public void BothListsUseSharedSmallerSubduedMetadata()
    {
        var xaml = Source("Controls/SnippetRowDetails.xaml");
        Assert.Equal(3, xaml.Split("MetadataFontSize(TextSize.FontSize)", StringSplitOptions.None).Length - 1);
        Assert.Contains("size * 0.85", Source("SnippetRowDetails.xaml.cs"));
        Assert.Contains("controls:SnippetRowDetails", Source("Controls/PopupRow.xaml"));
        Assert.Contains("controls:SnippetRowDetails", Source("Pages/SnippetsPage.xaml"));
        Assert.Contains("x:Key=\"PopupMetadataOpacity\">1</x:Double>", Source("App.xaml"));
    }
    [Fact]
    public void ExternalHoverIsScopedToOverlayContentAndCleanedUp()
    {
        var surface = Source("HoverDetailsSurface.xaml.cs");
        Assert.Contains("!_isMetadata && ExternalOwners.ContainsKey(root)", surface);
        Assert.Contains("ShouldConstrainToRootBounds = !external", surface);
        Assert.Contains("_popup.Child = null", surface);
        Assert.Contains("external && _popup.IsConstrainedToRootBounds", surface);
        Assert.Contains("!_active._isExternal", surface); // Not an overlay caption/passthrough rectangle.
        Assert.Contains("externalTransit", surface);
        Assert.Contains("Dictionary<TextBlock, HoverDetailsSurface>", Source("SnippetRowDetails.xaml.cs"));
        var overlay = Source("PopupWindow.xaml.cs");
        Assert.Contains("RegisterExternalOwner(root", overlay);
        Assert.Contains("UnregisterExternalOwner(root)", overlay);
        Assert.Contains("AppWindow.Changed -= OnHoverWindowChanged", overlay);
        Assert.Contains("args.DidPositionChange || args.DidSizeChange", overlay);
        Assert.Contains("root.RasterizationScale", overlay);
        Assert.Contains("TrackExternalPointer", overlay);
        Assert.DoesNotContain("RegisterExternalOwner", Source("SnippetsPage.xaml.cs"));
        Assert.DoesNotContain("SetForegroundWindow", surface);
        Assert.DoesNotContain(".Focus(", surface);
    }

    [Fact]
    public void CompanionPanelHasFixedOverlayHeightAndNeutralAppearanceOnlyForExternalContent()
    {
        var surface = Source("HoverDetailsSurface.xaml.cs");
        Assert.Contains("screen.Window.Top, Width, screen.Window.Height", surface);
        Assert.Contains("Height = placement.Height", surface);
        Assert.Contains("Height = double.NaN", surface);
        Assert.Contains("ResolveAppearance?.Invoke(Surface, Body, RequestedTheme, external)", surface);
        var theme = Source("ThemeService.cs");
        Assert.Contains("theme, companion", theme);
        Assert.Contains("Color.FromArgb(255, 32, 32, 32)", theme);
        Assert.Contains("Color.FromArgb(255, 243, 243, 243)", theme);
        Assert.Contains("highContrast ? 1 : companion ? 0.18 : 0.35", theme);
        Assert.Contains("WindowSurface.SystemColor(5)", theme);
    }

    [Fact]
    public void HoverHasIndependentContentTargetsAndScrollableNonFocusSurface()
    {
        var row = Source("SnippetRowDetails.xaml.cs");
        Assert.Contains("? ContentText", row);
        Assert.Contains("? FolderName : TagsDisplay", row);
        var surface = Source("Controls/HoverDetailsSurface.xaml");
        Assert.Contains("ScrollViewer", surface);
        Assert.Contains("AllowFocusOnInteraction=\"False\"", surface);
        var code = Source("HoverDetailsSurface.xaml.cs");
        Assert.Contains("_isMetadata ? 240 : 480", code); Assert.Contains("Math.Min(320", code);
        Assert.Contains("isMetadata: !ReferenceEquals(target, LabelText)", row);
        Assert.Contains("targetBounds.Top, targetBounds.Bottom", code);
        Assert.Contains("hover_surface_failed", code);
        Assert.DoesNotContain("ToolTipService", code);
        Assert.Contains("KeyboardAcceleratorPlacementMode=\"Hidden\"", Source("Pages/SnippetsPage.xaml"));
        Assert.Contains("New snippet (Ctrl+N)", Source("Pages/SnippetsPage.xaml"));
    }
}
