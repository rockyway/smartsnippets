using System.Diagnostics;
using System.Text.Json;
using System.Xml.Linq;

namespace SmartSnippets.Uno.Tests.Build;

public sealed class BuildGraphTests
{
    internal static string RepoRoot
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SmartSnippets.slnx"))) directory = directory.Parent;
            return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
        }
    }
    private static string App => Path.Combine(RepoRoot, "src", "SmartSnippets.Uno");
    [Fact]
    public void RejectsHelperGraphMacOverlayOrIlSuppression()
    {
        var project = XDocument.Load(Path.Combine(App, "SmartSnippets.Uno.csproj"));
        Assert.All(project.Descendants("Import").Where(e => e.Attribute("Sdk") is not null),
            e => { Assert.Equal("Uno.Sdk", (string?)e.Attribute("Sdk")); Assert.Equal("6.7.30", (string?)e.Attribute("Version")); });
        var properties = Evaluate("SmartSnippets.Uno.csproj");
        Assert.Equal("", properties["NoWarn"]);
        Assert.Equal("true", properties["DisableImplicitUnoPackages"]);
        foreach (var name in new[] { "PublishAot", "PublishTrimmed", "IsAotCompatible", "EnableAotAnalyzer", "EnableTrimAnalyzer", "TreatWarningsAsErrors" })
            Assert.Equal("true", properties[name]);
        Assert.Equal("false", properties["SuppressTrimAnalysisWarnings"]);
        Assert.Equal("false", properties["TrimmerSingleWarn"]);
        Assert.Equal("net10.0-windows10.0.26100", properties["TargetFramework"]);
        Assert.Equal("10.0.403", properties["NETCoreSdkVersion"]);
        var lockText = File.ReadAllText(Path.Combine(App, "packages.lock.json"));
        Assert.DoesNotContain("Uno.WinUI", lockText); Assert.DoesNotContain("Skia", lockText);
        using var closure = JsonDocument.Parse(lockText);
        var deps = closure.RootElement.GetProperty("dependencies").GetProperty("net10.0-windows10.0.26100");
        Assert.Equal("1.8.260921001", deps.GetProperty("Microsoft.WindowsAppSDK").GetProperty("resolved").GetString());
        Assert.Equal("10.0.28000.2705", deps.GetProperty("Microsoft.Windows.SDK.BuildTools").GetProperty("resolved").GetString());
        Assert.Equal("2.1.13", deps.GetProperty("SQLitePCLRaw.core").GetProperty("resolved").GetString());
    }
    [Fact]
    public void LockProperties_DoNotPropagateToProjectReferences()
    {
        var app = Evaluate("SmartSnippets.Uno.csproj");
        Assert.Equal("win-x64", app["RuntimeIdentifier"]);
        Assert.Equal("true", app["RestorePackagesWithLockFile"]);
        Assert.Equal("true", app["RestoreLockedMode"]);
        foreach (var name in new[] { "Core", "Presentation" })
        {
            var reference = Evaluate($"../SmartSnippets.{name}/SmartSnippets.{name}.csproj");
            Assert.Equal("", reference["RuntimeIdentifier"]);
            Assert.Equal("", reference["RestorePackagesWithLockFile"]);
            Assert.Equal("", reference["RestoreLockedMode"]);
        }
    }
    [Fact]
    public void OfficialIdentityPreservesPinnedNativeGraph()
    {
        var values = Evaluate("SmartSnippets.Uno.csproj");
        Assert.Equal("SmartSnippets", values["AssemblyName"]);
        Assert.Equal("10.0.17763.0", values["SupportedOSPlatformVersion"]);
        Assert.Equal("10.0.17763.0", values["TargetPlatformMinVersion"]);
        var profile = XDocument.Load(Path.Combine(App, "Properties", "PublishProfiles", "win-x64.pubxml"));
        Assert.Equal(values["RuntimeIdentifier"], profile.Descendants("RuntimeIdentifier").Single().Value);
        var rootSdk = File.ReadAllText(Path.Combine(RepoRoot, "global.json")); Assert.Contains("10.0.100", rootSdk);
        var rootPins = File.ReadAllText(Path.Combine(RepoRoot, "Directory.Packages.props")); Assert.Contains("1.8.260921001", rootPins);
    }
    private static Dictionary<string, string> Evaluate(string project)
    {
        const string names = "NoWarn,DisableImplicitUnoPackages,PublishAot,PublishTrimmed,IsAotCompatible,EnableAotAnalyzer,EnableTrimAnalyzer,TreatWarningsAsErrors,SuppressTrimAnalysisWarnings,TrimmerSingleWarn,RuntimeIdentifier,RestorePackagesWithLockFile,RestoreLockedMode,TargetFramework,NETCoreSdkVersion,AssemblyName,SupportedOSPlatformVersion,TargetPlatformMinVersion";
        var info = new ProcessStartInfo("dotnet") { WorkingDirectory = App, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        foreach (var arg in new[] { "msbuild", project, "-p:Configuration=Release", "-p:ContinuousIntegrationBuild=true", "-p:_IsPublishing=true", "-getProperty:" + names }) info.ArgumentList.Add(arg);
        using var process = Process.Start(info)!;
        var output = process.StandardOutput.ReadToEnd(); var error = process.StandardError.ReadToEnd();
        Assert.True(process.WaitForExit(30000), "MSBuild evaluation timed out."); Assert.True(process.ExitCode == 0, output + error);
        using var json = JsonDocument.Parse(output);
        return json.RootElement.GetProperty("Properties").EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString()!);
    }
}
