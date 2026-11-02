using SmartSnippets.TargetIdentity018;
using Xunit;

namespace SmartSnippets.Uno;

public sealed class ForegroundIdentityTests
{
    [Theory]
    [InlineData(101, 10, 7, "owned_a", true)]
    [InlineData(102, 20, 7, "owned_b", true)]
    [InlineData(103, 30, 7, "a_other_window", true)]
    [InlineData(104, 40, 8, "foreign", true)]
    [InlineData(10, 10, 7, "owned_a", false)]
    public void DiagnosticComparisonsUseExactRootAndKeepRawFacts(int raw, int root, uint pid, string expected, bool child)
    {
        nint Normalize(nint h) => ForegroundIdentity.Normalize(h, _ => child ? 0x40000000 : 0,
            _ => root, _ => (1u, pid), out _);
        Assert.Equal((nint)root, Normalize(raw));
        ForegroundIdentity.Normalize(raw, _ => child ? 0x40000000 : 0, _ => root, _ => (1u, pid), out var wasChild);
        Assert.Equal(child, wasChild);
        WindowDiagnosticFacts Facts(nint h)
        {
            Assert.Equal((nint)raw, h);
            return new WindowDiagnosticFacts().WithWindowDetails(child ? 0x40000000 : 0, 0, false, null, null, null, null);
        }
        var trace = new OwnedForegroundTrace(() => raw, _ => pid, 10, 7, 11, 12, 13, 14,
            10, _ => "toplevel_other", () => 0, () => Task.CompletedTask, 20, Facts, Normalize);
        var sample = trace.Sample(1, 0);
        Assert.Contains("relation=" + expected + " ", sample);
        Assert.Contains("foreground_was_child=" + child, sample);
        var startup = new NativeStartupDiagnostics(() => raw, _ => pid, 14, () => 10, () => 20, Facts, Normalize);
        Assert.Contains("relation=" + (root == 10 ? "owned_a" : root == 20 ? "owned_b" : "foreign") + " ", startup.Sample("after_focus"));
    }

    [Fact]
    public void StartupCannotCallWrongPidRootOwnedA()
    {
        var startup = new NativeStartupDiagnostics(() => 101, _ => 8, 14, () => 10, () => 20,
            normalize: h => ForegroundIdentity.Normalize(h, _ => 0x40000000, _ => 10, _ => (1u, 8u), out _),
            targetPid: () => 7);
        Assert.Contains("relation=foreign ", startup.Sample("after_focus"));
    }

    [Theory]
    [InlineData(0, 7)] [InlineData(2, 7)] [InlineData(1, 8)]
    public void ChildCannotBorrowRootWithChangedThreadOrPid(uint thread, uint pid)
    {
        Assert.Equal(0, ForegroundIdentity.Normalize(101, _ => 0x40000000, _ => 10,
            h => h == 101 ? (1u, 7u) : (thread, pid), out var child));
        Assert.True(child);
    }
}
