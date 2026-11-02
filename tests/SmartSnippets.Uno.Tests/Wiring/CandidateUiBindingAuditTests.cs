using System.Xml.Linq;
using SmartSnippets.Uno.Tests.Build;

namespace SmartSnippets.Uno.Tests.Wiring;

public sealed class CandidateUiBindingAuditTests
{
    private static string Read(string file) => File.ReadAllText(Path.Combine(CandidateBuildGraphTests.RepoRoot, "src/SmartSnippets.Uno", file));

    [Fact]
    public void IntentionallyDisabledButtons_HaveNoCommandThatCanReenableThem()
    {
        foreach (var file in new[] { "Pages/SettingsPage.xaml", "Popup/PopupWindow.xaml", "Controls/SnippetEditor.xaml" })
        {
            var xaml = XDocument.Parse(Read(file));
            foreach (var button in xaml.Descendants().Where(e => e.Name.LocalName == "Button" && (string?)e.Attribute("IsEnabled") == "False"))
                Assert.Null(button.Attribute("Command"));
        }
    }

    [Fact]
    public void FooterMenusDialogsAndPlacement_ConsumeLivePolicies()
    {
        Assert.Contains("HintLine(ViewModel.HintText, ViewModel.StatusText, ViewModel.IsPreview)", Read("Popup/PopupWindow.xaml"));
        Assert.Contains("IsEnabled = ViewModel.CanCopyRow(item)", Read("Popup/PopupWindow.xaml.cs"));
        Assert.Contains("IsEnabled = vm.CanCopyRow", Read("Pages/SnippetsPage.xaml.cs"));
        Assert.Contains("_lifecycle.Preview(ViewModel", Read("Popup/PopupWindow.xaml.cs"));
        Assert.Contains("_lifecycle.Reopen(ViewModel", Read("Popup/PopupWindow.xaml.cs"));
        Assert.Contains("WindowPlacement.CenterAtCursor(AppWindow, ViewModel.Target?.MonitorHandle ?? 0", Read("Popup/PopupWindow.xaml.cs"));
        Assert.Contains("if (ViewModel.IsPreview)", Read("Popup/PopupWindow.xaml.cs"));
        Assert.Contains("PopupPlacement.CenterIn(work, width, height)", Read("Services/WindowPlacement.cs"));
        Assert.Contains("PInvoke.GetCursorPos(&cursor)", Read("Services/WindowPlacement.cs"));
        Assert.Contains("PInvoke.MonitorFromPoint(cursor, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST)", Read("Services/WindowPlacement.cs"));
        Assert.Contains("WindowPlacement.ParkBottomLeft(AppWindow", Read("Popup/PopupWindow.xaml.cs"));
        Assert.DoesNotContain("_previewMonitor", Read("Popup/PopupWindow.xaml.cs"));
        Assert.DoesNotContain("MONITOR_DEFAULTTOPRIMARY", Read("Popup/PopupWindow.xaml.cs"));
        Assert.Contains("CandidateUiPolicy.DialogRoot", Read("Services/DialogService.cs"));
        Assert.Contains("textSize, _theme", Read("WindowsCandidateBoundary.cs"));
        Assert.Contains("_main?.BringToFront()", Read("WindowsCandidateBoundary.cs"));
        Assert.Contains("if (await NavigateGuardedAsync(kind) && show)", Read("MainWindow.xaml.cs"));
        Assert.Contains("Services.WindowPlacement.ParkBottomLeft(AppWindow", Read("MainWindow.xaml.cs"));
        Assert.Contains("CandidateUiPolicy.IsBetterBottomLeft(", Read("Services/WindowPlacement.cs"));
        Assert.Contains("EnumDisplayMonitors", Read("Services/WindowPlacement.cs"));
        Assert.DoesNotContain("DisplayArea.FindAll", Read("Services/WindowPlacement.cs"));
        var failure = Read("WindowsCandidateBoundary.cs");
        Assert.Contains("CandidateUiPolicy.ReportStartupFailure(", failure);
        Assert.Contains("Program.StartInBackground, Program.InitialOptions.HasRequest", failure);
        Assert.Equal(CandidateUiPolicy.PreviewHint, CandidateUiPolicy.HintLine("active hint", "", true));
        Assert.Equal("active hint", CandidateUiPolicy.HintLine("active hint", "", false));
        Assert.Equal("saved", CandidateUiPolicy.HintLine("saved", "saved", true));
        Assert.Contains("IsEnabled = CanInsert()", Read("Popup/PopupWindow.xaml.cs"));
        // Source supplement only; PositiveCopySceneTests drives this shared callback behaviorally.
        Assert.Contains("await PopupContextInsert.InvokeAsync", Read("Popup/PopupWindow.xaml.cs"));
    }
}
