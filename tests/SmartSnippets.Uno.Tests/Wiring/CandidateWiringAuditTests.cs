using System.Xml.Linq;
using SmartSnippets.Uno.Tests.Build;

namespace SmartSnippets.Uno.Tests.Wiring;

/// <summary>Source supplements only: native callbacks, compiled view delivery and foreground behavior still need T7.</summary>
public sealed class CandidateWiringAuditTests
{
    private static string Candidate => Path.Combine(CandidateBuildGraphTests.RepoRoot, "src", "SmartSnippets.Uno");
    [Fact]
    public void CandidateSources_HaveNoNativeMutationAdaptersOrRuntimeUnlock()
    {
        var source = string.Join(Environment.NewLine, Directory.GetFiles(Candidate, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) && !p.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar))
            .Select(File.ReadAllText));
        foreach (var forbidden in new[] { "StartupMigrationCoordinator", "Registry.CurrentUser", "new ClipboardService(", "new InsertionService(",
            "PInvoke.RegisterHotKey(", "PInvoke.OpenClipboard(", "PInvoke.SetClipboardData(", "PInvoke.SendInput(", "PInvoke.OpenProcess(",
            "PInvoke.GetWindowText(", "TargetCapture.", "PreviousAppWindow(", "PInvoke.SetForegroundWindow(", "AttachThreadInput(", "GetEnvironmentVariable(", "--after-pid", "--profile" })
            Assert.DoesNotContain(forbidden, source);
        Assert.DoesNotContain("SmartSnippets.App", source);
        var host = File.ReadAllText(Path.Combine(Candidate, "UnoAppHost.cs"));
        Assert.Contains("new DeferredPopupHost(", host); Assert.Contains("new Popup.RecoverablePopupHost(", host);
        Assert.False(File.Exists(Path.Combine(Candidate, "Services", "DeferredPopupHost.cs")));
    }
    [Fact]
    public void CandidateTray_BothStatesBrandedAndDistinctOwnedIcon()
    {
        var tray = File.ReadAllText(Path.Combine(Candidate, "Services", "TrayIcon.cs"));
        Assert.Contains("Tooltip = \"Smart Snippets — Uno candidate\"", tray);
        Assert.Contains("TooltipHotkeyUnavailable = \"Smart Snippets — Uno candidate — hotkey unavailable\"", tray);
        Assert.Contains("d0351d61-c130-481c-8398-e0cb2d42e019", tray);
        Assert.DoesNotContain("PreviousAppWindow", tray);
        Assert.DoesNotContain("GetTopWindow", tray);
        Assert.Contains("_onShowPopup();", tray);
    }
    [Fact]
    public void CandidateViews_KeepCompiledBindingsAndUnavailableNativeActions()
    {
        var ns = XNamespace.Get("http://schemas.microsoft.com/winfx/2006/xaml");
        foreach (var path in Directory.GetFiles(Candidate, "*.xaml", SearchOption.AllDirectories)
            .Where(p => !p.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) && !p.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)))
        {
            var xml = XDocument.Load(path);
            Assert.StartsWith("SmartSnippets.Uno.", (string?)xml.Root!.Attribute(ns + "Class"));
        }
        var popup = File.ReadAllText(Path.Combine(Candidate, "Popup", "PopupWindow.xaml.cs"));
        Assert.Contains("new PopupShowCoordinator(", popup);
        Assert.Contains("_shows.Close();", popup);
        Assert.Contains("private void CancelReveal()", popup);
        Assert.Contains("CompositionTarget.Rendered -= handler", popup);
        Assert.Contains("if (_closed) return;", popup);
        Assert.Contains("FlyoutShowMode.Transient", popup);
        Assert.Contains("FlyoutPlacementMode.Bottom", popup);
        var settings = XDocument.Load(Path.Combine(Candidate, "Pages", "SettingsPage.xaml"));
        Assert.Equal("False", settings.Descendants().Single(e => (string?)e.Attribute(ns + "Name") == "StartupToggle").Attribute("IsEnabled")!.Value);
    }
}
