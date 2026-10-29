namespace SmartSnippets.Presentation.Tests;

/// <summary>Binding regression guard; rendered appearance still requires Windows visual verification.</summary>
public sealed class PopupRowFontSourceTests
{
    [Fact]
    public void MetadataAndIconScaleWithLabelWhileLabelAndHeadingRemainUnchanged()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "UiContracts");
        var xaml = File.ReadAllText(Path.Combine(root, "Controls", "PopupRow.xaml"));
        var code = File.ReadAllText(Path.Combine(root, "PopupRow.xaml.cs"));
        Assert.Equal(2, xaml.Split("MetadataFontSize(Item.TextSize.FontSize)", StringSplitOptions.None).Length - 1);
        Assert.Equal(2, xaml.Split("Item.TextSize.FontSize, Mode=OneWay, FallbackValue=14", StringSplitOptions.None).Length - 1);
        Assert.Contains("labelFontSize * 0.85", code);
    }
}
