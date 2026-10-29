using System.Globalization;
using System.Xml.Linq;

namespace SmartSnippets.Presentation.Tests;

/// <summary>Layout-source regression checks; these do not measure rendered controls.</summary>
public sealed class ButtonSpacingSourceTests
{
    private static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2006/xaml";
    private static readonly XNamespace Automation = "using:Microsoft.UI.Xaml.Automation";
    private static string SourceRoot => Path.Combine(AppContext.BaseDirectory, "UiContracts");

    [Fact]
    public void PopupEditorActionsAndSectionsHaveEightDipGaps()
    {
        var document = XDocument.Load(Path.Combine(SourceRoot, "Popup", "PopupWindow.xaml"));
        var cancel = ById(document, "PopupEditorCancel");
        var save = ById(document, "PopupEditorSave");
        Assert.Same(cancel.Parent, save.Parent);
        Assert.Equal(8, Gap(cancel.Parent!, "ColumnSpacing"));
        var overlay = Assert.Single(document.Descendants().Where(e => (string?)e.Attribute(Xaml + "Name") == "EditorOverlay"));
        Assert.Equal(8, Gap(overlay, "RowSpacing"));
        var error = ById(document, "PopupFooterError");
        Assert.Equal(8, Gap(error.Parent!, "Spacing"));
        Assert.Equal(8, Gap(ById(document, "PopupPasteCancel").Parent!, "Spacing"));
    }

    [Fact]
    public void AllCustomHorizontalButtonRowsDeclarePositiveSpacing()
    {
        var files = Directory.GetFiles(SourceRoot, "*.xaml", SearchOption.AllDirectories);
        Assert.NotEmpty(files);
        foreach (var path in files)
        {
            var document = XDocument.Load(path);
            foreach (var panel in document.Descendants())
            {
                var buttons = panel.Elements().Where(e => e.Name.LocalName is "Button" or "ToggleButton").ToArray();
                if (buttons.Length < 2) continue;
                if (panel.Name.LocalName == "StackPanel" && (string?)panel.Attribute("Orientation") == "Horizontal")
                    Assert.True(Gap(panel, "Spacing") > 0, $"Missing button Spacing in {path}");
                if (panel.Name.LocalName == "Grid" && buttons.GroupBy(e => (string?)e.Attribute("Grid.Row") ?? "0").Any(g => g.Count() > 1))
                    Assert.True(Gap(panel, "ColumnSpacing") > 0, $"Missing button ColumnSpacing in {path}");
            }
        }
    }

    private static XElement ById(XDocument document, string id)
        => Assert.Single(document.Descendants().Where(e => (string?)e.Attribute(Automation + "AutomationProperties.AutomationId") == id));

    private static double Gap(XElement element, string attribute)
        => double.TryParse((string?)element.Attribute(attribute), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : 0;
}
