using System.Xml.Linq;
using SmartSnippets.Uno.Tests.Build;

namespace SmartSnippets.Uno.Tests.Wiring;

public sealed class PasswordEditorSourceTests
{
    private static string EditorPath => Path.Combine(BuildGraphTests.RepoRoot, "src/SmartSnippets.Uno/Controls/SnippetEditor.xaml");
    private static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

    [Fact]
    public void NoRevealButtonOrPlaintextPasswordBinding()
    {
        var document = XDocument.Load(EditorPath);
        var password = Assert.Single(document.Descendants(), e => e.Name.LocalName == "PasswordBox");
        Assert.Equal("Hidden", (string?)password.Attribute("PasswordRevealMode"));
        Assert.Null(password.Attribute("Password"));
        Assert.Null(password.Attribute("IsSpellCheckEnabled"));
        Assert.Null(password.Attribute("IsTextPredictionEnabled"));
        var text = document.Descendants().Single(e => (string?)e.Attribute(Xaml + "Name") == "SnippetTextBox");
        Assert.Null(text.Attribute("Text"));
        Assert.All(document.Descendants().Attributes(), a => Assert.DoesNotContain("x:Bind ViewModel.Text,", a.Value));
        Assert.DoesNotContain(document.Descendants().Where(e => e.Name.LocalName is "Button" or "ToggleButton"),
            e => e.Attributes().Any(a => a.Value.Contains("reveal", StringComparison.OrdinalIgnoreCase)));
        var mask = document.Descendants().Single(e => (string?)e.Attribute(Xaml + "Name") == "MultilinePasswordMask");
        Assert.Equal("True", (string?)mask.Attribute("IsReadOnly"));
        Assert.Equal("••••••••", (string?)mask.Attribute("Text"));
        var labelToggle = Assert.Single(document.Descendants(), e => e.Name.LocalName == "ToggleSwitch");
        Assert.Equal("{x:Bind ViewModel.CanChangeLabelMode, Mode=OneWay}", (string?)labelToggle.Attribute("IsEnabled"));
    }

    [Fact]
    public void BothHostsReuseTheSharedEditor()
    {
        foreach (var path in new[] { "Pages/SnippetsPage.xaml", "Popup/PopupWindow.xaml" })
        {
            var document = XDocument.Load(Path.Combine(BuildGraphTests.RepoRoot, "src/SmartSnippets.Uno", path));
            Assert.Single(document.Descendants(), e => e.Name.LocalName == "SnippetEditor");
        }
    }
}
