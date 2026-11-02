using System.Xml.Linq;

namespace SmartSnippets.Uno.Tests.Build;

public sealed class OfficialIdentityTests
{
    private static string Read(string relative) => File.ReadAllText(Path.Combine(CandidateBuildGraphTests.RepoRoot, "src/SmartSnippets.Uno", relative));

    [Fact]
    public void RuntimeIdentityMatchesLegacyWithoutOpeningAnyProfile()
    {
        Assert.Equal("SmartSnippets.Main", CandidateProfile.Identity);
        var profile = Read("CandidateProfile.cs");
        Assert.Contains("OpenOwnedRoot(new AppPaths().Root)", profile);
        Assert.DoesNotContain(".candidate-owner", profile);
        Assert.DoesNotContain("RefuseOverlap", profile);
        Assert.Equal("SmartSnippets.MessageWindow", Services.CandidateMessageWindowLifetime.ClassName);
        Assert.Contains("private const uint IconId = 1;", Read("Services/TrayIcon.cs"));
        var manifest = XDocument.Parse(Read("app.manifest"));
        Assert.Equal("SmartSnippets.App.app", (string?)manifest.Descendants().Single(e => e.Name.LocalName == "assemblyIdentity").Attribute("name"));
        Assert.Equal("SmartSnippets", Services.StartupRegistration.ValueName);
    }

    [Fact]
    public void ElevatedChildWaitsBeforeInstanceRegistrationAndProfileOpen()
    {
        var program = Read("Program.cs");
        var wait = program.IndexOf("WaitForPreviousInstance();", StringComparison.Ordinal);
        var claim = program.IndexOf("AppInstance.FindOrRegisterForKey(InstanceKey)", StringComparison.Ordinal);
        var app = program.IndexOf("new App()", StringComparison.Ordinal);
        Assert.True(wait >= 0 && claim > wait && app > claim);
        Assert.Contains("previous.WaitForExit(TimeSpan.FromSeconds(5))", program);
        Assert.Contains("InstanceKey = CandidateProfile.Identity", program);
        var elevation = Read("Services/ElevationService.cs");
        Assert.Contains("AdministratorRestart.Arguments(Environment.ProcessId, _isMainWindowHidden())", elevation);
        Assert.Contains("PInvoke.ShellExecuteEx(&info)", elevation);
        Assert.Contains("ErrorCancelled = 1223", elevation);
        Assert.Contains("fixed (char* verb = \"runas\")", elevation);
    }
}
