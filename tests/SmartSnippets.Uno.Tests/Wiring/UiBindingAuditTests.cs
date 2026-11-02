using System.Xml.Linq;
using SmartSnippets.Uno.Tests.Build;

namespace SmartSnippets.Uno.Tests.Wiring;

public sealed class UiBindingAuditTests
{
    private static string Read(string file) => File.ReadAllText(Path.Combine(BuildGraphTests.RepoRoot, "src/SmartSnippets.Uno", file));

    [Fact]
    public void AdministratorRestartAndExplicitConsentAreWiredToGuardedCommands()
    {
        var settings = XDocument.Parse(Read("Pages/SettingsPage.xaml"));
        var restart = settings.Descendants().Single(e => (string?)e.Attribute("Content") == "Restart as administrator");
        Assert.Equal("{x:Bind ViewModel.RestartAsAdministratorCommand}", (string?)restart.Attribute("Command"));
        Assert.Equal("{x:Bind ViewModel.CanRestartAsAdmin, Mode=OneTime}", (string?)restart.Attribute("IsEnabled"));
        var popup = XDocument.Parse(Read("Popup/PopupWindow.xaml"));
        foreach (var command in new[] { "RestartAsAdminCommand", "PasteAnywayCommand" })
            Assert.Single(popup.Descendants(), e => (string?)e.Attribute("Command") == "{x:Bind ViewModel." + command + ", Mode=OneWay}");
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
        Assert.Contains("WindowPlacement.PlaceManagement(AppWindow, LogicalWidth, LogicalHeight, UseQaPlacement)", Read("Popup/PopupWindow.xaml.cs"));
        Assert.DoesNotContain("_previewMonitor", Read("Popup/PopupWindow.xaml.cs"));
        Assert.DoesNotContain("MONITOR_DEFAULTTOPRIMARY", Read("Popup/PopupWindow.xaml.cs"));
        Assert.Contains("UiPolicy.DialogRoot", Read("Services/DialogService.cs"));
        Assert.Contains("textSize, _theme", Read("WindowsHostBoundary.cs"));
        Assert.Contains("_main?.BringToFront()", Read("WindowsHostBoundary.cs"));
        Assert.Contains("if (await NavigateGuardedAsync(kind) && show)", Read("MainWindow.xaml.cs"));
        Assert.Contains("Services.WindowPlacement.PlaceManagement(AppWindow, LogicalWidth, LogicalHeight, qaPlacement)", Read("MainWindow.xaml.cs"));
        Assert.Contains("Program.InitialOptions.HasRequest", Read("WindowsHostBoundary.cs"));
        Assert.Contains("if (selected.Handle == (nint)primary.Value) return false;", Read("Services/WindowPlacement.cs"));
        Assert.Contains("UiPolicy.IsBetterBottomLeft(", Read("Services/WindowPlacement.cs"));
        Assert.Contains("EnumDisplayMonitors", Read("Services/WindowPlacement.cs"));
        Assert.DoesNotContain("DisplayArea.FindAll", Read("Services/WindowPlacement.cs"));
        var failure = Read("WindowsHostBoundary.cs");
        Assert.Contains("UiPolicy.ReportStartupFailure(", failure);
        Assert.Contains("ReportStartupFailure(string message)", failure);
        Assert.Contains("message,", failure);
        Assert.Contains("ex is StoreException or MigrationException ? ex.Message", Read("UnoAppHost.cs"));
        Assert.Contains("Program.StartInBackground, Program.InitialOptions.HasRequest", failure);
        Assert.Equal(UiPolicy.PreviewHint, UiPolicy.HintLine("active hint", "", true));
        Assert.Equal("active hint", UiPolicy.HintLine("active hint", "", false));
        Assert.Equal("saved", UiPolicy.HintLine("saved", "saved", true));
        Assert.Contains("IsEnabled = CanInsert()", Read("Popup/PopupWindow.xaml.cs"));
        // Source supplement only; PositiveCopySceneTests drives this shared callback behaviorally.
        Assert.Contains("await PopupContextInsert.InvokeAsync", Read("Popup/PopupWindow.xaml.cs"));
    }
}
