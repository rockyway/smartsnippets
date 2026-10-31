using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace SmartSnippets.UnoFlow.Tests;

/// <summary>STATIC XAML/native-edge audit only. Actual thin page handlers are also
/// compiled and executed against test-only UI primitives in FlowPageHandlerTests.</summary>
public sealed class HostCallbackRoutingTests
{
    private static string Source(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "HostContracts", name));
    internal static string StripComments(string source) => Regex.Replace(source,
        "@\"(?:\"\"|[^\"])*\"|\"(?:\\\\.|[^\"\\\\])*\"|'(?:\\\\.|[^'\\\\])*'|//[^\\r\\n]*|/\\*[\\s\\S]*?\\*/",
        match => match.Value.StartsWith('/') ? "" : match.Value);
    [Fact]
    public void CommentedOutCallsCannotSatisfyStaticAudit()
    {
        Assert.DoesNotContain("Flow.OnShortcut();", StripComments("// Flow.OnShortcut();\n/* Flow.OnShortcut(); */"));
        Assert.Contains("\"// not a comment\"", StripComments("\"// not a comment\""));
    }
    [Fact]
    public void ActualUnoHostConsumesSharedDriverAndClearsNativeRefsBeforeClosedPolicy()
    {
        var app = StripComments(Source("App.xaml.cs"));
        Assert.Contains("_uiFlow = new UiFlowProbe(_window);", app); // No native adapter argument.
        var host = StripComments(Source("UiFlowProbe.cs"));
        Assert.Contains("_driver = new ProbePopupDriver(", host);
        Assert.Contains("_flow = ProbeFlowComposition.Create(_driver,", host);
        Assert.Contains("_driver.Attach(_flow);", host);
        Assert.Contains("main.Content = new FlowProbePage(_flow);", host);
        Assert.Contains("_popup.Closed += PopupClosed;", host);
        var closed = host[host.IndexOf("private void PopupClosed", StringComparison.Ordinal)..];
        Assert.True(closed.IndexOf("_surface = null; _popup = null;", StringComparison.Ordinal) < closed.IndexOf("Closed?.Invoke();", StringComparison.Ordinal));
        Assert.Contains("_page?.InvalidateMeasure();", host);
        Assert.Contains("await _flow.ShutdownAsync();", host);
    }
    [Fact]
    public void XamlPopupClickIsEnabledAndMainInsertIsInsideTypedMainRowTemplate()
    {
        var doc = XDocument.Parse(Source("FlowProbePage.xaml"));
        var popup = Assert.Single(doc.Descendants(), e => e.Name.LocalName == "ListView" && (string?)e.Attribute("ItemClick") == "RowClicked");
        Assert.Equal("True", (string?)popup.Attribute("IsItemClickEnabled"));
        var button = Assert.Single(doc.Descendants(), e => (string?)e.Attribute("Click") == "MainInsertClicked");
        Assert.Equal("{x:Bind Id}", (string?)button.Attribute("Tag"));
        var template = Assert.Single(button.Ancestors(), e => e.Name.LocalName == "DataTemplate");
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        Assert.Equal("vm:SnippetRowViewModel", (string?)template.Attribute(x + "DataType"));
        Assert.Equal("{x:Bind Flow.Main.List.Items}", (string?)template.Ancestors().Single(e => e.Name.LocalName == "ListView").Attribute("ItemsSource"));
    }
    [Theory]
    [InlineData("Click", "ShortcutClicked", "Flow.OnShortcut();")]
    [InlineData("Click", "MenuClicked", "Flow.OnMenu();")]
    [InlineData("Click", "PreviewClicked", "Flow.OnPreview();")]
    [InlineData("Click", "OpenMainClicked", "Flow.OnOpenMain();")]
    [InlineData("Click", "MainInsertClicked", "await Flow.OnMainInsertAsync(id);")]
    [InlineData("Click", "CopyClicked", "Flow.OnMainCopy(Flow.SnippetId);")]
    [InlineData("Click", "HideClicked", "Flow.OnHide();")]
    [InlineData("Click", "FooterDismissClicked", "Flow.OnFooterDismiss();")]
    [InlineData("Click", "PopupCopyClicked", "Flow.OnPopupCopy(Flow.Popup.Items[Flow.Popup.SelectedIndex]);")]
    [InlineData("KeyDown", "SearchKeyDown", "await Flow.OnEnterAsync();")]
    [InlineData("ItemClick", "RowClicked", "await Flow.OnRowAsync(row);")]
    public void StaticXamlEventAudit(string attribute, string handler, string call)
    {
        var xaml = XDocument.Parse(Source("FlowProbePage.xaml"));
        Assert.Single(xaml.Descendants().Attributes(attribute), a => a.Value == handler);
        var code = StripComments(Source("FlowProbePage.xaml.cs"));
        var start = code.IndexOf("void " + handler + "(", StringComparison.Ordinal);
        Assert.True(start >= 0);
        var end = code.IndexOf("private ", start, StringComparison.Ordinal);
        Assert.Contains(call, end < 0 ? code[start..] : code[start..end]);
    }
}
