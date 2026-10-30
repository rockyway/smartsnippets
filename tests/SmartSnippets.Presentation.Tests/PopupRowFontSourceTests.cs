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
    public void HoverHasIndependentContentTargetsAndScrollableNonFocusSurface()
    {
        var row = Source("SnippetRowDetails.xaml.cs");
        Assert.Contains("? ContentText", row);
        Assert.Contains("? FolderName : TagsDisplay", row);
        var surface = Source("Controls/HoverDetailsSurface.xaml");
        Assert.Contains("ScrollViewer", surface);
        Assert.Contains("AllowFocusOnInteraction=\"False\"", surface);
        var code = Source("HoverDetailsSurface.xaml.cs");
        Assert.Contains("Math.Min(480", code); Assert.Contains("Math.Min(320", code);
        Assert.Contains("hover_surface_failed", code);
        Assert.DoesNotContain("ToolTipService", code);
        Assert.Contains("KeyboardAcceleratorPlacementMode=\"Hidden\"", Source("Pages/SnippetsPage.xaml"));
        Assert.Contains("New snippet (Ctrl+N)", Source("Pages/SnippetsPage.xaml"));
    }
}
