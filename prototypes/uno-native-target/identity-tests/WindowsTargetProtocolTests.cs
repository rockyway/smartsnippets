using SmartSnippets.Target018;
using Xunit;

public sealed class WindowsTargetProtocolTests
{
    [Theory]
    [InlineData(42, 100, 42, 100, true, true)]
    [InlineData(42, 100, 43, 100, true, false)]
    [InlineData(42, 100, 42, 101, true, false)]
    [InlineData(42, 100, 42, 100, false, false)]
    [InlineData(0, 100, 0, 100, true, false)]
    [InlineData(uint.MaxValue, 100, uint.MaxValue, 100, true, false)]
    public void GrantPinRejectsWrongOrStaleProcess(uint pid, long started, uint actualPid, long actualStarted, bool alive, bool expected) =>
        Assert.Equal(expected, Program.MatchesGrantee(pid, started, actualPid, actualStarted, alive));

    [Fact]
    public void NativeGrant_SourcePinsSingleCallToRetainedCandidatePid()
    {
        var directory = new System.IO.DirectoryInfo(System.AppContext.BaseDirectory);
        string? path = null;
        while (directory is not null)
        {
            var candidate = System.IO.Path.Combine(directory.FullName, "prototypes", "uno-native-target", "windows", "Program.cs");
            if (System.IO.File.Exists(candidate)) { path = candidate; break; }
            directory = directory.Parent;
        }
        Assert.NotNull(path);
        var source = System.IO.File.ReadAllText(path);
        var start = source.IndexOf("private static string GrantForeground(nint a)", System.StringComparison.Ordinal);
        var end = source.IndexOf("private static int _closed", start, System.StringComparison.Ordinal);
        var grant = source[start..end];
        Assert.Contains("grantee.HasExited", grant);
        Assert.Contains("VerifyOwner(a);", grant);
        Assert.Contains("AllowSetForegroundWindow((uint)grantee.Id)", grant);
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(grant, @"AllowSetForegroundWindow\s*\("));
        Assert.Contains("? \"granted\" : \"refused\"", grant);
        Assert.Contains("catch (Exception) { return \"failed\"; }", grant);
        Assert.DoesNotContain("uint.MaxValue", grant);
        Assert.DoesNotContain("ASFW_ANY", grant);
    }

    private const string Unicode = "AΩ中🙂e\u0301";

    [Theory]
    [InlineData("a")]
    [InlineData("b")]
    public void ExactUnicodeAndCrlfAreComparedWithoutNormalization(string window)
    {
        Assert.True(Program.VerifyFixture("verify_unicode_" + window, Unicode));
        Assert.True(Program.VerifyFixture("verify_multiline_" + window, Unicode + "\r\nsecond line\r\n"));
        Assert.False(Program.VerifyFixture("verify_unicode_" + window, Unicode.Normalize()));
        Assert.False(Program.VerifyFixture("verify_multiline_" + window, Unicode + "\nsecond line\n"));
        Assert.False(Program.VerifyFixture("verify_unicode_" + window, Unicode + Unicode));
        Assert.False(Program.VerifyFixture("verify_unicode_" + window, ""));
    }

    [Theory]
    [InlineData("verify_unicode_a payload")]
    [InlineData("verify_multiline_c")]
    [InlineData("focus_42")]
    [InlineData("foreground_grant 42")]
    [InlineData("foreground_grant ")]
    [InlineData("foreground_grant\nfocus_a")]
    [InlineData("focus_a\nfocus_b")]
    [InlineData("verify_unicode_a ")]
    [InlineData("empty_A")]
    public void UnknownCommandsAndPayloadsAreNotAccepted(string command)
    {
        Assert.False(Program.AllowedCommand(command));
        Assert.False(Program.VerifyFixture(command, Unicode));
    }

    [Theory]
    [InlineData("foreground_grant")]
    [InlineData("focus_a")]
    [InlineData("focus_b")]
    [InlineData("verify_unicode_a")]
    [InlineData("verify_unicode_b")]
    [InlineData("verify_multiline_a")]
    [InlineData("verify_multiline_b")]
    public void FixedOwnedCommandsAreAllowlisted(string command) => Assert.True(Program.AllowedCommand(command));
}
