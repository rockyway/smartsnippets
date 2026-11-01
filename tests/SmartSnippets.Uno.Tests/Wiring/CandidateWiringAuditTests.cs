using System.Xml.Linq;
using SmartSnippets.Uno.Tests.Build;

namespace SmartSnippets.Uno.Tests.Wiring;

/// <summary>Source supplements only: native callbacks, compiled view delivery and foreground behavior still need T7.</summary>
public sealed class CandidateWiringAuditTests
{
    private static string Candidate => Path.Combine(CandidateBuildGraphTests.RepoRoot, "src", "SmartSnippets.Uno");
    [Fact]
    public void CandidateSources_OnlyComposeInsertionBehindGuardedBoundaryWithoutRuntimeUnlock()
    {
        var source = string.Join(Environment.NewLine, Directory.GetFiles(Candidate, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) && !p.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar))
            .Select(File.ReadAllText));
        foreach (var forbidden in new[] { "StartupMigrationCoordinator", "Registry.CurrentUser", "new ClipboardService(",
            "PInvoke.RegisterHotKey(", "PInvoke.OpenClipboard(", "PInvoke.SetClipboardData(", "PInvoke.SendInput(", "PInvoke.OpenProcess(",
            "PInvoke.GetWindowText(", "TargetCapture.", "PreviousAppWindow(", "PInvoke.SetForegroundWindow(", "AttachThreadInput(", "GetEnvironmentVariable(", "--after-pid", "--profile" })
            Assert.DoesNotContain(forbidden, source);
        Assert.DoesNotContain("SmartSnippets.App", source);
        var gatePath = Path.Combine(Candidate, "Platforms", "Windows", "CandidateNativeGate.cs");
        Assert.Contains("new InsertionService(store, this, this)", File.ReadAllText(gatePath));
        foreach (var path in Directory.GetFiles(Candidate, "*.cs", SearchOption.AllDirectories)
            .Where(path => path != gatePath))
            Assert.DoesNotContain("new InsertionService(", File.ReadAllText(path));
        var host = File.ReadAllText(Path.Combine(Candidate, "UnoAppHost.cs"));
        Assert.Contains("new DeferredPopupHost(", host); Assert.Contains("new Popup.RecoverablePopupHost(", host);
        Assert.False(File.Exists(Path.Combine(Candidate, "Services", "DeferredPopupHost.cs")));
    }
    [Fact]
    public void PopupNativeEventSources_ForwardHiddenAndQueuedPresentationCompletionToActualHost()
    {
        var boundary = File.ReadAllText(Path.Combine(Candidate, "WindowsCandidateBoundary.cs"));
        Assert.Contains("host.CanPresentPopup", boundary);
        Assert.Contains("window.VisibleChanged +=", boundary);
        Assert.Contains("Hidden?.Invoke(this, EventArgs.Empty)", boundary);
        Assert.Contains("window.PresentationChanged +=", boundary);
        var window = File.ReadAllText(Path.Combine(Candidate, "Popup", "PopupWindow.xaml.cs"));
        Assert.Contains("add => _lifecycle.PresentationChanged += value", window);
        Assert.Contains("_lifecycle.Hidden();", window);
        Assert.Contains("ShowPreviewReady(openSortMenu), () => CanShow?.Invoke() != false", window);
        var host = File.ReadAllText(Path.Combine(Candidate, "UnoAppHost.cs"));
        Assert.Contains("popup.Hidden += OnPopupHidden", host);
        Assert.Contains("popup.PresentationChanged += OnPopupPresentationChanged", host);
        Assert.Contains("capability.Revoke();", host);
    }

    [Fact]
    public void MainWindow_TeardownDoesNotReadClosedNativeContentOrRunQueuedDpiWork()
    {
        var source = File.ReadAllText(Path.Combine(Candidate, "MainWindow.xaml.cs"));
        Assert.Contains("Closed += (_, _) => _closed = true;", source);
        Assert.DoesNotContain("Closed += (_, _) => Controls.HoverDetailsSurface.CancelActive(ContentXamlRoot)", source);
        Assert.Contains("ContentXamlRoot => _closingForExit || _closed ? null : Content?.XamlRoot;", source);
        Assert.Contains("if (_closingForExit || _closed) return;\n                var currentDpi", source.Replace("\r\n", "\n"));
        Assert.Contains("if (!_closingForExit && !_closed && args.WindowActivationState", source);
    }

    [Fact]
    public void CandidateTray_BothStatesBrandedAndDistinctOwnedIcon()
    {
        var tray = File.ReadAllText(Path.Combine(Candidate, "Services", "TrayIcon.cs"));
        Assert.Equal("Smart Snippets — Uno candidate", Services.CandidateTrayText.Tooltip(false));
        Assert.Equal("Smart Snippets — Uno candidate — hotkey unavailable", Services.CandidateTrayText.Tooltip(true));
        Assert.Contains("private const uint IconId = 2019;", tray);
        Assert.Contains("data.hWnd = _window.Handle;", tray);
        Assert.DoesNotContain("NIF_GUID", tray);
        Assert.DoesNotContain("guidItem", tray);
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
        Assert.Contains("new CandidatePopupLifecycle(", popup);
        var lifecycle = File.ReadAllText(Path.Combine(Candidate, "CandidateUiPolicy.cs"));
        Assert.Contains("internal PopupShowCoordinator Shows", lifecycle);
        Assert.Contains("Shows.RequestShow(", lifecycle);
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
