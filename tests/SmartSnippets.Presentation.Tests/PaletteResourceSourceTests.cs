using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace SmartSnippets.Presentation.Tests;

/// <summary>Source wiring guards only. These do not prove WinUI rendering or system notification delivery.</summary>
public sealed class PaletteResourceSourceTests
{
    private static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "UiContracts", file));

    [Fact]
    public void EveryBaselineSourceHasAnExplicitThemedConsumer()
    {
        var xaml = XDocument.Parse(Source(Path.Combine("Controls", "PaletteBaseline.xaml")));
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        var names = xaml.Descendants().Select(element => (string?)element.Attribute(x + "Name")).ToHashSet();
        foreach (Match source in Regex.Matches(Source("PaletteResources.cs"), "Add\\(\"(Palette[^\"]+)\""))
            Assert.Contains(source.Groups[1].Value, names);
        var code = Source("PaletteBaseline.xaml.cs");
        Assert.True(code.IndexOf("RequestedTheme = dark", StringComparison.Ordinal) < code.IndexOf("InitializeComponent();", StringComparison.Ordinal));
        Assert.Contains("SolidColorBrush.ColorProperty", code);
        Assert.Contains("Border.BackgroundProperty", code);
        // Baselines consume primitive Colors, not the overridden Brush keys.
        Assert.DoesNotContain("FillColorBaseBrush", xaml.ToString());
        Assert.Contains("settings.GetColorValue(type)", Source("PaletteResources.cs"));
    }

    [Fact]
    public void RealizedControlAliasesAndOpenSurfacesAreIncluded()
    {
        var resources = Source("PaletteResources.cs");
        foreach (var alias in new[] { "ButtonBackground", "TextControlBackground", "ComboBoxBackground", "MenuFlyoutItemBackgroundPointerOver",
            "ContentDialogBackground", "NavigationViewContentBackground", "AccentButtonBackground", "ListViewItemBackgroundSelected",
            "TextControlBorderBrushFocused", "TextControlSelectionHighlightColor" })
            Assert.Contains('"' + alias + '"', resources);
        Assert.Contains("entry.Live.Color =", resources);
        Assert.Contains("dictionary.Remove(key);", resources);
        Assert.Contains("root.Resources.ThemeDictionaries", resources);
        var highContrastStart = resources.IndexOf("var highContrast = new ResourceDictionary", StringComparison.Ordinal);
        var highContrastEnd = resources.IndexOf("root.Resources.ThemeDictionaries[\"HighContrast\"]", highContrastStart, StringComparison.Ordinal);
        var highContrastBlock = resources[highContrastStart..highContrastEnd];
        Assert.Contains("SolidBackgroundFillColorBaseBrush", highContrastBlock);
        Assert.DoesNotContain("foreach", highContrastBlock);
        Assert.DoesNotContain("MenuFlyoutItemBackgroundPointerOver", highContrastBlock);
        Assert.Contains("if (root is Grid grid && !_baselinesAttached)", resources);
        Assert.Contains("popup.SetPaletteBackground(_paletteResources.BackgroundBrush(root.ActualTheme))", Source("ThemeService.cs"));
        Assert.Contains("entry.Baseline.Opacity", resources);
        var dialog = Source("DialogService.cs");
        Assert.Contains("_theme?.RegisterTransient(dialog);", dialog);
        Assert.Contains("finally { _theme?.UnregisterTransient(dialog); }", dialog);
        var theme = Source("ThemeService.cs");
        Assert.Contains("surface.RequestedTheme = RequestedTheme", theme);
        Assert.Contains("ColorValuesChanged += OnSystemColorsChanged", theme);
        Assert.Contains("ColorValuesChanged -= OnSystemColorsChanged", theme);
        Assert.Contains("MessageReceived -= OnSystemMessage", theme);
        Assert.Contains("0x0015 or 0x001A or 0x031A", theme);
    }

    [Fact]
    public void StartupAndRecoveryUseTheCurrentPaletteBeforePopupPrewarm()
    {
        var host = Source("AppHost.cs");
        Assert.Contains("new ThemeService(settings.Theme, settings.ColorPalette)", host);
        Assert.Contains("_paletteResources.InstallRootResources(content)", Source("ThemeService.cs"));
        Assert.True(host.IndexOf("theme.Register(popup);", StringComparison.Ordinal) < host.IndexOf("popup.Prewarm();", StringComparison.Ordinal));
        var recovery = host[host.IndexOf("private PopupWindow EnsurePopup()", StringComparison.Ordinal)..];
        Assert.True(recovery.IndexOf("_theme!.Register(popup);", StringComparison.Ordinal) < recovery.IndexOf("popup.Prewarm();", StringComparison.Ordinal));
    }
}
