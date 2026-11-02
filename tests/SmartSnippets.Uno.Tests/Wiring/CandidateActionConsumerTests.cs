using System.Xml.Linq;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using SmartSnippets.Presentation.Navigation;
using SmartSnippets.Uno.Controls;
using SmartSnippets.Uno.Pages;
using SmartSnippets.Uno.Platforms.Windows;
using SmartSnippets.Uno.Popup;
using SmartSnippets.Uno.Tests.Build;
using SmartSnippets.Uno.Tests.Composition;

namespace SmartSnippets.Uno.Tests.Wiring;

[CollectionDefinition("Candidate XAML actions", DisableParallelization = true)]
public sealed class CandidateActionCollection;

/// <summary>
/// Executes the linked, unmodified candidate code-behind against UnoAppHost's actual store/VMs/gate.
/// HeadlessXaml supplies only rendering, scheduling, window, pointer/keyboard state and native primitives.
/// The reviewed manifest is deliberately NOT generated during builds: new/changed/deleted actions fail.
/// No reflection, source Contains checks, desktop execution or native-acceptance claims.
/// </summary>
[Collection("Candidate XAML actions")]
public sealed class CandidateActionConsumerTests
{
    private static string Candidate => Path.Combine(CandidateBuildGraphTests.RepoRoot, "src/SmartSnippets.Uno");
    private static string Manifest => Path.Combine(CandidateBuildGraphTests.RepoRoot, "tests/SmartSnippets.Uno.Tests/Wiring/CandidateActions.txt");
    private static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2006/xaml";
    // Known property bindings are not actions. Any new x:Bind attribute outside this
    // reviewed property vocabulary is treated as an action until explicitly classified.
    private static readonly HashSet<string> BoundProperties = new(StringComparer.Ordinal)
    {
        "Content", "ContentText", "DisplayText", "FolderName", "FontSize", "Glyph", "IsChecked", "IsEnabled", "IsOn", "IsOpen",
        "Item", "ItemsSource", "Label", "Message", "OffContent", "OnContent", "PlaceholderText", "SelectedIndex", "SelectedItem",
        "Severity", "TagsDisplay", "Text", "TextSize", "ToolTipService.ToolTip", "Value", "ViewModel", "Visibility"
    };
    private static readonly HashSet<string> EventAttributes = new(StringComparer.Ordinal)
    {
        "Click", "Command", "Invoked", "KeyDown", "KeyUp", "PreviewKeyDown", "PreviewKeyUp", "Loaded", "Unloaded",
        "SelectionChanged", "TextChanged", "ValueChanged", "Toggled", "GotFocus", "LostFocus", "SizeChanged",
        "ItemClick", "ContainerContentChanging", "PointerEntered", "PointerExited", "PointerMoved", "PointerPressed",
        "PointerReleased", "Tapped", "DoubleTapped", "RightTapped", "EscapePressed", "Canceled", "Cleared", "GestureCaptured"
    };

    private static IEnumerable<(string Key, XElement Element, XAttribute Attribute)> Inventory(string root)
    {
        foreach (var file in Directory.GetFiles(root, "*.xaml", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
            if (relative.Split('/').Any(part => part is "obj" or "bin")) continue;
            var index = 0;
            foreach (var element in XDocument.Load(file).Descendants())
            {
                foreach (var attribute in element.Attributes())
                {
                    if (attribute.Name.Namespace != XNamespace.None) continue;
                    // Also reject custom event handlers outside the built-in event vocabulary.
                    if (!EventAttributes.Contains(attribute.Name.LocalName)
                        && !(attribute.Value.StartsWith("{x:Bind", StringComparison.Ordinal) && !BoundProperties.Contains(attribute.Name.LocalName))
                        && !(attribute.Value.StartsWith("On", StringComparison.Ordinal)
                            && attribute.Value.Length > 2 && char.IsUpper(attribute.Value[2]) && attribute.Value != "Once")) continue;
                    yield return ($"{relative}|{index}|{attribute.Name.LocalName}|{attribute.Value}", element, attribute);
                }
                index++;
            }
        }
    }

    [Fact]
    public void InventoryIncludesBoundEventsAndCommandsButNotPropertyBindingsOrComments()
    {
        var root = Path.Combine(Path.GetTempPath(), "SmartSnippets.ActionInventory", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(Path.Combine(root, "Actions.xaml"), """
                <Page xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
                  <!-- <Button Click="CommentOnly" /> -->
                  <Button Click="ExistingHandler" Command="{x:Bind ViewModel.SaveCommand}" />
                  <Grid Tapped="{x:Bind HandleTap}" NewCustomEvent="{x:Bind HandleCustom}" />
                  <TextBlock Text="{x:Bind ViewModel.StatusMessage, Mode=OneWay}" />
                </Page>
                """);
            var entries = Inventory(root).ToArray();
            Assert.Equal(new[] { "Click", "Command", "Tapped", "NewCustomEvent" }, entries.Select(e => e.Attribute.Name.LocalName));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public Task EveryCandidateAction_ReachesRegisteredConsumerOrTruthfulRefusal() => CandidateHostThread.RunAsync(async () =>
    {
        var actions = Inventory(Candidate).ToArray();
        Assert.Equal(File.ReadAllLines(Manifest).Where(line => line.Length > 0).Order(StringComparer.Ordinal), actions.Select(a => a.Key).Order(StringComparer.Ordinal));
        Assert.NotEmpty(actions);
        foreach (var entry in actions)
        {
            using var f = new HostFixture();
            var native = new ActionAuditNative();
            var file = entry.Key.Split('|')[0];
            var action = entry.Attribute.Value;
            if (entry.Attribute.Name.LocalName == "Command")
            {
                Assert.StartsWith("{x:Bind ", action);
                action = action[8..].Split(',', '}')[0];
            }
            if (file == "Popup/PopupWindow.xaml" || action == "CopyAfterAdmissionCommand")
                f.Boundary.DesktopFactory = (_, _) => new ActionAuditSupport.Desktop(native);
            await f.Start();
            var views = new WindowsCandidateBoundary();
            views.CreateTheme(f.Host.Settings);
            using var mainSurface = views.CreateMain(f.Host);
            var main = MainWindow.LastCreated;
            try
            {
                switch (file)
                {
                    case "Pages/FoldersPage.xaml":
                        await mainSurface.NavigateAsync(PageKind.Folders, false);
                        await Assert.IsType<FoldersPage>(main.AuditPage).Audit(action, f); break;
                    case "Pages/SettingsPage.xaml":
                        await mainSurface.NavigateAsync(PageKind.Settings, false);
                        await Assert.IsType<SettingsPage>(main.AuditPage).Audit(action, f); break;
                    case "Pages/SnippetsPage.xaml": await Assert.IsType<SnippetsPage>(main.AuditPage).Audit(action, f); break;
                    case "Controls/SnippetEditor.xaml":
                        await Assert.IsType<SnippetsPage>(main.AuditPage).EditorControl.Audit(action, f, native); break;
                    case "Popup/PopupWindow.xaml":
                        using (var popupSurface = views.CreatePopup(f.Host))
                            await PopupWindow.LastCreated.Audit(action, f, native);
                        break;
                    case "Controls/SnippetRowDetails.xaml": new SnippetRowDetails().Audit(action, f, (string?)entry.Element.Attribute(Xaml + "Name")); break;
                    case "Controls/HoverDetailsSurface.xaml":
                        using (var surface = new HoverDetailsSurface(new Grid())) surface.Audit(action);
                        Assert.Empty(f.Host.Store.Snippets); Assert.Equal(0, native.Copies + native.Sends); break;
                    case "MainWindow.xaml":
                        await main.Audit(f); break;
                    default: Assert.Fail("Unmapped candidate action surface: " + file); break;
                }
            }
            catch (Exception ex)
            {
                // Only the reviewed action identity is added; do not attach exception payloads or user data.
                throw new Xunit.Sdk.XunitException($"Action {entry.Key} failed ({ex.GetType().Name}); inspect the consumer assertion.");
            }
            finally
            {
                HoverDetailsSurface.CancelActive(); DispatcherQueue.Timers.Clear(); FocusManager.Focused = null;
            }
        }
    });
}
