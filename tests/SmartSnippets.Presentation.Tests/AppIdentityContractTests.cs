namespace SmartSnippets.Presentation.Tests;

public sealed class AppIdentityContractTests
{
    private static string Read(string path) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "UiContracts", path));

    [Fact]
    public void TitleAssetAndOfficialAssemblyAreWired()
    {
        var xaml = Read("MainWindow.xaml");
        var project = Read("SmartSnippets.Uno.csproj");
        Assert.Contains("Assets/TitleIcon.png", xaml);
        Assert.Contains("Width=\"16\" Height=\"16\"", xaml);
        Assert.Contains("Assets/TitleIcon.png", project);
        Assert.Contains("CopyToPublishDirectory=\"PreserveNewest\"", project);
        Assert.True(File.Exists(Path.Combine(AppContext.BaseDirectory, "UiContracts", "TitleIcon.png")));
        var text = Read("CandidateTrayText.cs");
        Assert.Contains("Smart Snippets", text);
        Assert.Contains("Smart Snippets — hotkey unavailable", text);
        Assert.Contains("<AssemblyName>SmartSnippets</AssemblyName>", project);
        Assert.Contains("<ApplicationId>SmartSnippets</ApplicationId>", project);
        Assert.Contains("private const uint IconId = 1;", Read("TrayIcon.cs"));
    }

    [Fact]
    public void LaunchAndNavigationRetainNonactivatingShow()
    {
        var host = Read("UnoAppHost.cs");
        var window = Read("MainWindow.xaml.cs");
        Assert.Contains("await ApplyLaunchOptionsAsync(options, !background)", host);
        Assert.Contains("ShowPageWithoutActivationAsync", window);
        Assert.Contains("AppWindow.Show(false)", window);
    }

    [Fact]
    public void StartupMigrationPrecedesMainAndFailureHasNotificationFallback()
    {
        var desktop = Read("WindowsDesktopServices.cs");
        Assert.True(desktop.IndexOf("StartupMigrationCoordinator.Start(", StringComparison.Ordinal)
            < desktop.IndexOf("new HotkeyService(message)", StringComparison.Ordinal));
        var host = Read("UnoAppHost.cs");
        Assert.True(host.IndexOf("boundary.CreateDesktopServices(", StringComparison.Ordinal)
            < host.IndexOf("boundary.CreateMain(this)", StringComparison.Ordinal));
        Assert.Contains("StartupMigrationFailed == true", host);
        Assert.Contains("_tray.TryNotify(", host);
        Assert.Contains("await Dialogs.ShowInfoAsync(", host);
    }
}
