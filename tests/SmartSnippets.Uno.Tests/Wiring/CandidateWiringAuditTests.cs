using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using SmartSnippets.Presentation.Navigation;
using SmartSnippets.Uno.Pages;
using SmartSnippets.Uno.Tests.Build;
using SmartSnippets.Uno.Tests.Composition;

namespace SmartSnippets.Uno.Tests.Wiring;

/// <summary>Headless factory bridge and source supplements; native delivery and foreground behavior still need T7.</summary>
[Collection("Candidate XAML actions")]
public sealed class CandidateWiringAuditTests
{
    private static string Candidate => Path.Combine(CandidateBuildGraphTests.RepoRoot, "src", "SmartSnippets.Uno");
    private static IEnumerable<string> Sources(string root) => Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
        .Select(p => Path.GetRelativePath(root, p).Replace('\\', '/'))
        .Where(p => !p.Split('/').Any(part => part is "bin" or "obj"))
        .Where(p => Path.GetExtension(p) is ".cs" or ".xaml");

    [Fact]
    public Task PopupSurface_OpenMainForwardsThroughRealFactoryToGuardedManagement() => CandidateHostThread.RunAsync(async () =>
    {
        using var fixture = new HostFixture();
        await fixture.Start(background: true);
        var views = new WindowsCandidateBoundary();
        views.CreateTheme(fixture.Host.Settings);
        using var main = views.CreateMain(fixture.Host);
        using var popup = views.CreatePopup(fixture.Host);
        await main.NavigateAsync(PageKind.Folders, false);
        Assert.IsType<FoldersPage>(views.MainWindow!.AuditPage);
        Assert.True(views.MainWindow.IsHidden);
        // Exercise the real forwarding surface AND PopupWindow callback; the
        // action audit's VM uses HostFixture's separate external popup boundary.
        popup.OpenMainWindow();
        await Task.Yield();
        Assert.IsType<SnippetsPage>(views.MainWindow.AuditPage);
        Assert.False(views.MainWindow.IsHidden);
    });

    [Fact]
    public void CandidateSourceAndViews_RejectReflectionAndRuntimeBindings()
    {
        foreach (var relative in Sources(Candidate))
        {
            var source = File.ReadAllText(Path.Combine(Candidate, relative));
            if (relative.EndsWith(".cs", StringComparison.Ordinal))
            {
                foreach (var forbidden in new[] { "System.Reflection", "Activator.", "Type.GetType(",
                    ".GetMethod(", ".GetProperty(", ".GetField(", ".DynamicInvoke(", "Assembly.Load" })
                    Assert.DoesNotContain(forbidden, source);
            }
            else
            {
                var attributes = XDocument.Parse(source).Descendants().Attributes().ToArray();
                Assert.DoesNotContain(attributes, a => a.Value.StartsWith("{Binding", StringComparison.Ordinal));
                Assert.DoesNotContain(attributes, a => a.Name.LocalName == "DataContext");
            }
        }
    }

    [Fact]
    public void CandidateSources_OnlyComposeInsertionBehindGuardedBoundaryWithoutRuntimeUnlock()
    {
        var source = string.Join(Environment.NewLine, Directory.GetFiles(Candidate, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) && !p.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar))
            .Select(File.ReadAllText));
        // D80: ordinary Windows hotkey/capture/paste is now real. Keep all other unlocks absent.
        foreach (var forbidden in new[] { "GetEnvironmentVariable(", "--profile" })
            Assert.DoesNotContain(forbidden, source);
        var windows = File.ReadAllText(Path.Combine(Candidate, "WindowsCandidateBoundary.cs"));
        Assert.Contains("if (Program.InitialOptions.HasRequest) return null;", windows);
        Assert.Contains("new WindowsDesktopServices", windows);
        var desktop = File.ReadAllText(Path.Combine(Candidate, "Platforms/Windows/DailyNative/WindowsDesktopServices.cs"));
        Assert.Contains("new HotkeyService(message)", desktop);
        Assert.Contains("manager.Initialize(settings.Hotkey)", desktop);
        Assert.Contains("TargetCapture.CaptureForeground()", desktop);
        Assert.Contains("new StartupRegistration()", desktop);
        Assert.Contains("ClipboardService _copy = new()", desktop);
        var startup = File.ReadAllText(Path.Combine(Candidate, "Platforms/Windows/DailyNative/StartupRegistration.cs"));
        Assert.Contains("ValueName = \"SmartSnippets\"", startup);
        Assert.Contains("_write(ValueName, Command(exe))", startup);
        Assert.Contains("_delete(ValueName)", startup);
        Assert.Contains("key.SetValue(name, command, RegistryValueKind.String)", startup);
        Assert.Contains("string.Equals(stored, Command(exe)", startup);
        Assert.Contains("new ElevationService(mainHidden)", desktop);
        var gatePath = Path.Combine(Candidate, "Platforms", "Windows", "DesktopInsertionCoordinator.cs");
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
        Assert.Contains("Gate.CloseAdmission();", host);
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
        Assert.Equal("Smart Snippets", Services.CandidateTrayText.Tooltip(false));
        Assert.Equal("Smart Snippets — hotkey unavailable", Services.CandidateTrayText.Tooltip(true));
        Assert.Contains("private const uint IconId = 1;", tray);
        Assert.Contains("data.hWnd = _window.Handle;", tray);
        Assert.DoesNotContain("NIF_GUID", tray);
        Assert.DoesNotContain("guidItem", tray);
        Assert.Contains("_captureTargets ? (nint)PreviousAppWindow(previousForeground).Value : 0", tray);
        Assert.Contains("var previousForeground = _captureTargets ? PInvoke.GetForegroundWindow() : default;", tray);
        Assert.Contains("if (_captureTargets) PInvoke.SetForegroundWindow", tray);
    }
    [Fact]
    public void CandidateViews_KeepCompiledBindingsAndTruthfulNativeActions()
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
        Assert.Null(settings.Descendants().Single(e => (string?)e.Attribute(ns + "Name") == "StartupToggle").Attribute("IsEnabled")); // Actual registration controls the toggle state.
        Assert.Contains("Restart with administrator rights", settings.ToString());
    }
}
