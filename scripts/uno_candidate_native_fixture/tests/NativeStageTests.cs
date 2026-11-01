using System.Text.RegularExpressions;
using SmartSnippets.Core.Logging;
using Xunit;

namespace SmartSnippets.Uno;

[Collection("Fixture host")] // AppLog is process-global; serialize with the actual-host tests.
public sealed class NativeStageTests
{
    [Fact]
    public void Tokens_AreFixedUniqueLowercaseAndStartAtStart()
    {
        Assert.Equal("start", NativeStage.Tokens[0]);
        Assert.Equal(NativeStage.Tokens.Length, NativeStage.Tokens.Distinct().Count());
        Assert.All(NativeStage.Tokens, token => Assert.Matches("^[a-z_]+$", token));
        Assert.Equal("target_foreground_grant", NativeStage.Tokens[^1]);
    }

    [Fact]
    public void Reach_RecordsOnlyAllowlistedTokens()
    {
        NativeStage.Reset();
        Assert.Equal("start", NativeStage.Last);
        NativeStage.Reach("host_ready");
        Assert.Equal("host_ready", NativeStage.Last);
        foreach (var rejected in new[] { "C:\\secret\\path", "Host_Ready", "host_ready ", "title=secret", "", "guardian_ready\nforged" })
        {
            NativeStage.Reach(rejected);
            Assert.Equal("host_ready", NativeStage.Last);
        }
        Assert.Equal("native_positive_failed stage=host_ready", NativeStage.FailureEvent);
        NativeStage.Reset();
        Assert.Equal("native_positive_failed stage=start", NativeStage.FailureEvent);
    }

    [Fact]
    public void BeforeHostLog_IsTrueUntilTheHostHasStarted()
    {
        NativeStage.Reset();
        Assert.True(NativeStage.BeforeHostLog);
        NativeStage.Reach("host_start");
        Assert.True(NativeStage.BeforeHostLog);
        NativeStage.Reach("host_ready");
        Assert.False(NativeStage.BeforeHostLog);
        NativeStage.Reset();
    }

    [Theory]
    [InlineData("review_gate")] [InlineData("fixture_start")] [InlineData("host_ready")]
    [InlineData("target_foreground_grant")]
    [InlineData("session_create")] [InlineData("guardian_bootstrap")] [InlineData("guardian_dup")]
    public void FailureEvent_LogsStageTypeAndHResultOnly(string stage)
    {
        var directory = Path.Combine(Path.GetTempPath(), "ss-stage-" + Guid.NewGuid().ToString("N"));
        try
        {
            AppLog.Initialize(directory);
            NativeStage.Reset();
            NativeStage.Reach(stage);
            AppLog.Error(NativeStage.FailureEvent, new InvalidOperationException("secret message C:\\private\\title.txt"));
            AppLog.Shutdown();
            var line = Assert.Single(File.ReadAllLines(Path.Combine(directory, "app.log")));
            Assert.Matches(new Regex("^\\S+ ERROR native_positive_failed stage=" + stage + " InvalidOperationException \\(HResult=0x[0-9A-F]{8}\\)$"), line);
            Assert.DoesNotContain("secret", line);
            Assert.DoesNotContain("private", line);
        }
        finally
        {
            AppLog.Shutdown();
            NativeStage.Reset();
            try { Directory.Delete(directory, true); } catch (IOException) { }
        }
    }

    [Fact]
    public void FailurePath_UsesTheFixedEventNameAndNeverTheExceptionMessage()
    {
        var app = File.ReadAllText(Path.Combine(SourceRoot(), "App.xaml.cs"));
        Assert.Contains("AppLog.Error(NativeStage.FailureEvent, ex);", app);
        Assert.DoesNotContain("ex.Message", app);
        Assert.DoesNotContain("ex.ToString", app);
        // Every stage the bootstrap names, in order, appears before the guardian_ready write completes.
        var order = Regex.Matches(app, "NativeStage\\.Reach\\(\"([a-z_]+)\"\\)").Select(m => m.Groups[1].Value).ToArray();
        Assert.Equal(new[] { "review_gate", "fixture_start", "fixture_owned", "host_start", "host_ready", "session_create",
            "broker_bind", "outside_job", "guardian_dup", "guardian_ready" }, order);
        Assert.All(order, token => Assert.Contains(token, NativeStage.Tokens));
    }

    private static string SourceRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "App.xaml.cs"))) return directory.FullName;
            else if (File.Exists(Path.Combine(directory.FullName, "scripts", "uno_candidate_native_fixture", "App.xaml.cs")))
                return Path.Combine(directory.FullName, "scripts", "uno_candidate_native_fixture");
        throw new InvalidOperationException("source root not found");
    }
}
