using System.Xml.Linq;

namespace SmartSnippets.Uno.Tests.Build;

public sealed class MeasurementIsolationTests
{
    private static string Read(string path) => File.ReadAllText(Path.Combine(BuildGraphTests.RepoRoot, path));

    [Fact]
    public void NormalBuildHasNoMeasurementSourcesOrHooks()
    {
        var project = XDocument.Parse(Read("src/SmartSnippets.Uno/SmartSnippets.Uno.csproj"));
        var constants = project.Descendants("DefineConstants").Single(e => e.Value.Contains("SMARTSNIPPETS_MEASURE"));
        Assert.Equal("'$(SmartSnippetsMeasure)' == 'true'", (string?)constants.Parent!.Attribute("Condition"));
        var exclude = project.Descendants("Compile").Single(e => (string?)e.Attribute("Remove") == "Measurement/**/*.cs");
        Assert.Equal("'$(SmartSnippetsMeasure)' != 'true'", (string?)exclude.Parent!.Attribute("Condition"));
        foreach (var path in new[] { "AppProfile.cs", "Program.cs", "App.xaml.cs", "UnoAppHost.cs",
            "Services/WindowPlacement.cs", "Services/MessageWindowLifetime.cs", "Platforms/Windows/DailyNative/WindowsDesktopServices.cs" })
        {
            var normal = WithoutMeasurement(Read("src/SmartSnippets.Uno/" + path));
            Assert.DoesNotContain("Measurement", normal);
            Assert.DoesNotContain("SMARTSNIPPETS_MEASURE_ROOT", normal);
            Assert.DoesNotContain("SmartSnippets.Measure", normal);
        }
        // These are the actual linked production types compiled without the switch.
        Assert.Equal("SmartSnippets.Main", AppProfile.Identity);
        Assert.Equal("SmartSnippets.MessageWindow", Services.MessageWindowLifetime.ClassName);
    }

    [Fact]
    public void MeasurementStartupCannotConstructRegistrationOrRunMigration()
    {
        var source = Read("src/SmartSnippets.Uno/Platforms/Windows/DailyNative/WindowsDesktopServices.cs");
        var measure = SelectMeasurement(source, true);
        Assert.Contains("new UnavailableStartupRegistration()", measure);
        Assert.DoesNotContain("new StartupRegistration()", measure);
        Assert.DoesNotContain("StartupMigrationCoordinator.Start", measure);
        Assert.DoesNotContain("WindowsStartupMigrationRegistry", measure);
    }

    [Fact]
    public void ScriptRequiresGrantAndNeverUsesInputOrClipboardApis()
    {
        var script = Read("scripts/measure-memory.ps1");
        Assert.Contains("[Parameter(Mandatory)][switch]$ScreenGranted", script);
        Assert.Contains("DISPLAY29", script);
        Assert.Contains("measurement-build.txt", script);
        Assert.Contains("$start.Environment['SMARTSNIPPETS_MEASURE_ROOT'] = $root", script);
        Assert.DoesNotContain("SendKeys", script);
        Assert.DoesNotContain("SendInput", script);
        Assert.DoesNotContain("SetForegroundWindow", script);
        Assert.DoesNotContain("Get-Clipboard", script);
        Assert.DoesNotContain("Set-Clipboard", script);
        Assert.Contains("$owned.Kill()", script);
    }

    private static string WithoutMeasurement(string source) => SelectMeasurement(source, false);
    private static string SelectMeasurement(string source, bool measurement)
    {
        // Pin the small compile-time guard surface, including both branches. Fail on unexpected directives.
        var output = new List<string>();
        var enabled = true;
        var inGuard = false;
        foreach (var line in source.Split('\n'))
        {
            switch (line.Trim())
            {
                case "#if SMARTSNIPPETS_MEASURE": Assert.False(inGuard); inGuard = true; enabled = measurement; break;
                case "#if !SMARTSNIPPETS_MEASURE": Assert.False(inGuard); inGuard = true; enabled = !measurement; break;
                case "#else": Assert.True(inGuard); enabled = !enabled; break;
                case "#endif": Assert.True(inGuard); inGuard = false; enabled = true; break;
                default: if (enabled) output.Add(line); break;
            }
        }
        Assert.False(inGuard);
        return string.Join('\n', output);
    }
}
