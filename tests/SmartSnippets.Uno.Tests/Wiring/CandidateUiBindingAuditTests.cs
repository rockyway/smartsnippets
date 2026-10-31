using System.Xml.Linq;
using SmartSnippets.Uno.Tests.Build;

namespace SmartSnippets.Uno.Tests.Wiring;

public sealed class CandidateUiBindingAuditTests
{
    private static string Read(string file) => File.ReadAllText(Path.Combine(CandidateBuildGraphTests.RepoRoot, "src/SmartSnippets.Uno", file));

    [Fact]
    public void IntentionallyDisabledButtons_HaveNoCommandThatCanReenableThem()
    {
        foreach (var file in new[] { "Pages/SettingsPage.xaml", "Popup/PopupWindow.xaml" })
        {
            var xaml = XDocument.Parse(Read(file));
            foreach (var button in xaml.Descendants().Where(e => e.Name.LocalName == "Button" && (string?)e.Attribute("IsEnabled") == "False"))
                Assert.Null(button.Attribute("Command"));
        }
    }

    [Fact]
    public void FooterMenusDialogsAndPlacement_ConsumeLivePolicies()
    {
        Assert.Contains("HintLine(ViewModel.HintText, ViewModel.StatusText)", Read("Popup/PopupWindow.xaml"));
        Assert.Contains("IsEnabled = ViewModel.CanCopyRow(item)", Read("Popup/PopupWindow.xaml.cs"));
        Assert.Contains("IsEnabled = vm.CanCopyRow", Read("Pages/SnippetsPage.xaml.cs"));
        Assert.Contains("_lifecycle.Preview(ViewModel", Read("Popup/PopupWindow.xaml.cs"));
        Assert.Contains("_lifecycle.Reopen(ViewModel", Read("Popup/PopupWindow.xaml.cs"));
        Assert.Contains("new HMONITOR(target.MonitorHandle) : _previewMonitor", Read("Popup/PopupWindow.xaml.cs"));
        Assert.Contains("WindowPlacement.CenterOn(AppWindow, monitor", Read("Popup/PopupWindow.xaml.cs"));
        Assert.DoesNotContain("MONITOR_DEFAULTTOPRIMARY", Read("Popup/PopupWindow.xaml.cs"));
        Assert.Contains("CandidateUiPolicy.DialogRoot", Read("Services/DialogService.cs"));
        Assert.Contains("textSize, _theme", Read("WindowsCandidateBoundary.cs"));
        Assert.Contains("_main?.BringToFront()", Read("WindowsCandidateBoundary.cs"));
        Assert.Contains("if (await NavigateGuardedAsync(kind) && show)", Read("MainWindow.xaml.cs"));
        Assert.Equal(CandidateUiPolicy.PreviewHint, CandidateUiPolicy.HintLine("legacy active hint", ""));
        Assert.Equal("saved", CandidateUiPolicy.HintLine("saved", "saved"));
    }
}
