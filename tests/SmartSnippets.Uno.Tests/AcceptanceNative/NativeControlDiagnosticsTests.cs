using System;
using System.Threading.Tasks;
using SmartSnippets.Uno.Platforms.Windows.AcceptanceNative;
using Xunit;

namespace SmartSnippets.Uno.Tests.AcceptanceNative;

public sealed class NativeControlDiagnosticsTests
{
    [Theory]
    [InlineData(0, 0, 0, 0, 0, "none")]
    [InlineData(10, 0, 0, 0, 0, "foreign")]
    [InlineData(10, 7, 7, 7, 7, "owned_a")]
    [InlineData(20, 7, 7, 7, 7, "owned_b")]
    [InlineData(30, 7, 7, 7, 7, "guardian")]
    [InlineData(30, 7, 0, 7, 7, "candidate")]
    [InlineData(30, 7, 0, 0, 7, "same_pid_other")]
    [InlineData(30, 7, 0, 0, 0, "foreign")]
    [InlineData(30, 7, 8, 9, 10, "foreign")]
    public void RelationsHaveFixedPrecedence(int fg, uint pid, uint guardian, uint candidate, uint owned, string expected)
    {
        Assert.Equal(expected, NativeControlDiagnostics.Relation(fg, 10, 20, pid, guardian, candidate, owned));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task ThrowingDiagnosticCannotChangeForegroundRefusal(int failAt)
    {
        var samples = 0; var classified = 0; var reports = 0; var reportedOrder = 0;
        var admission = await ClipboardControlReadiness.WaitAsync(
            () => { classified++; return ClipboardControlAdmission.Unquiet; },
            () => ++samples < failAt, default, refused: (_, order) =>
            { reports++; reportedOrder = order; throw new InvalidOperationException(); });
        Assert.Equal(ClipboardControlAdmission.ForegroundChanged, admission);
        Assert.Equal(failAt, reportedOrder);
        Assert.Equal(failAt - 1, classified);
        Assert.Equal(1, reports);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IdleIsOneOrderedSampleAndThrowingDiagnosticCannotChangeResult(bool unchanged)
    {
        var order = 4; var samples = 0; var reports = 0; var reportedOrder = 0; string? reportedCheck = null;
        Assert.Equal(unchanged, NativeControlDiagnostics.CheckIdle(() => { samples++; return unchanged; },
            ref order, (check, sample) =>
            { reports++; reportedCheck = check; reportedOrder = sample; throw new InvalidOperationException(); }));
        Assert.Equal(5, order);
        Assert.Equal(1, samples);
        Assert.Equal(unchanged ? 0 : 1, reports);
        Assert.Equal(unchanged ? 0 : 5, reportedOrder);
        Assert.Equal(unchanged ? null : "idle", reportedCheck);
    }

    [Theory]
    [InlineData(10, 7, "owned_a")] [InlineData(20, 7, "owned_b")]
    [InlineData(30, 7, "same_pid_other")] [InlineData(40, 8, "foreign")]
    [InlineData(10, 8, "foreign")]
    public void NormalizedChildRecordKeepsExactRootRelationAndRawChildFact(int root, uint pid, string relation)
    {
        var normalized = SmartSnippets.TargetIdentity018.ForegroundIdentity.Normalize(101,
            _ => 0x40000000, _ => root, _ => (1u, pid), out var child);
        var record = NativeControlDiagnostics.Record(normalized, 10, 20, pid, 9, 10, 7,
            true, true, 1, 1, "changed", 1, child);
        Assert.Contains("relation=" + relation + " foreground_was_child=True ", record);
    }

    [Fact]
    public void RecordHasOnlyTheFixedFieldsAndNoNativeIdentity()
    {
        Assert.Equal("relation=guardian foreground_was_child=False fgVisible=True fgEnabled=False fgSameThreadAsOwned=True check=idle sampleOrder=5",
            NativeControlDiagnostics.Record(12345, 10, 20, 54321, 54321, 9, 8, true, false, 7, 7, "idle", 5));
        Assert.Equal("relation=foreign foreground_was_child=False fgVisible=False fgEnabled=True fgSameThreadAsOwned=False check=initial sampleOrder=1",
            NativeControlDiagnostics.Record(12345, 10, 20, 0, 0, 0, 0, false, true, 0, 0, "initial", 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => NativeControlDiagnostics.Record(0, 0, 0, 0, 0, 0, 0,
            false, false, 0, 0, "free text", 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => NativeControlDiagnostics.Record(0, 0, 0, 0, 0, 0, 0,
            false, false, 0, 0, "changed", 0));
    }
}
