namespace SmartSnippets.Presentation.Tests;

public sealed class AppIdentityContractTests
{
    private static string Read(string path) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "UiContracts", path));

    [Fact]
    public void TitleAssetAndTrayTooltipsAreWired()
    {
        var xaml = Read("MainWindow.xaml");
        var project = Read("SmartSnippets.App.csproj");
        var tray = Read("TrayIcon.cs");
        Assert.Contains("Assets/TitleIcon.png", xaml);
        Assert.Contains("Width=\"16\" Height=\"16\"", xaml);
        Assert.Contains("Assets\\TitleIcon.png", project);
        Assert.Contains("CopyToPublishDirectory=\"PreserveNewest\"", project);
        Assert.True(File.Exists(Path.Combine(AppContext.BaseDirectory, "UiContracts", "TitleIcon.png")));
        Assert.Contains("Tooltip = \"Smart Snippets\"", tray);
        Assert.Contains("Smart Snippets — hotkey unavailable", tray);
        Assert.Contains("<AssemblyName>SmartSnippets</AssemblyName>", project);
    }

    [Fact]
    public void InitialLaunchSuppressesExplicitFlashButLaterOpenKeepsFallback()
    {
        var host = Read("AppHost.cs");
        var window = Read("MainWindow.xaml.cs");
        Assert.Contains("mainWindow.ShowPage(PageKind.Snippets, flashOnFocusDenied: false)", host);
        Assert.Contains("ShowPage(PageKind kind, bool flashOnFocusDenied = true)", window);
        var startupStart = window.IndexOf("if (!flashOnFocusDenied)", StringComparison.Ordinal);
        var startupEnd = window.IndexOf("return;", startupStart, StringComparison.Ordinal);
        var startup = window[startupStart..startupEnd];
        Assert.Contains("AppWindow.Show(false)", startup);
        Assert.DoesNotContain("SetForegroundWindow", startup.Replace("// turn a denied request into taskbar attention even without FlashWindow.", ""));
        Assert.DoesNotContain("Activate()", startup);
        Assert.Contains("PInvoke.FlashWindow(hwnd, true)", window);
        Assert.Contains("startup_no_activation", startup);
    }

    [Fact]
    public void PrimaryStartupInvokesMigrationBeforePageConstructionAndDeliversFailures()
    {
        var host = Read("AppHost.cs");
        var migration = host.IndexOf("StartupMigrationCoordinator.Start(", StringComparison.Ordinal);
        var pageFactory = host.IndexOf("UIElement CreatePage(", StringComparison.Ordinal);
        var tray = host.IndexOf("tray.Add()", StringComparison.Ordinal);
        var notice = host.IndexOf("startupMigration == StartupMigrationResult.Failed", StringComparison.Ordinal);
        Assert.True(migration >= 0 && pageFactory > migration && tray > pageFactory && notice > tray);
        Assert.Contains("_ui!.Post(() => StartupMigrationCoordinator.Deliver", host);
        Assert.Contains("tray.ShowBalloon", host);
        Assert.Contains("PInvoke.MessageBox", host);
    }
}
