using Xunit;

namespace SmartSnippets.Uno;

public sealed class NativeStartupDiagnosticsTests
{
    [Theory]
    [InlineData(0, "none", false)]
    [InlineData(1, "owned_a", false)]
    [InlineData(2, "owned_b", false)]
    [InlineData(3, "launcher", true)]
    [InlineData(4, "foreign", false)]
    public void SamplesUseOneForegroundReadAndOrderedFixedRelations(int window, string relation, bool launcher)
    {
        var reads = 0;
        var diagnostics = new NativeStartupDiagnostics(() => { reads++; return window; },
            h => h == 3 ? 10u : 20u, 10, () => 1, () => 2);
        Assert.Equal($"phase=target_launch relation={relation} launcher_foreground={launcher} sample_order=1",
            diagnostics.Sample("target_launch"));
        Assert.Equal($"phase=preparation_start relation={relation} launcher_foreground={launcher} sample_order=2",
            diagnostics.Sample("preparation_start"));
        Assert.Equal(2, reads);
        Assert.Throws<ArgumentException>(() => diagnostics.Sample("private title"));
        Assert.Equal(2, reads);
    }

    [Theory]
    [InlineData(0, "popup_missing")]
    [InlineData(1, "popup_handle_missing")]
    [InlineData(2, "main_missing")]
    [InlineData(3, "main_handle_missing")]
    [InlineData(4, "store_missing")]
    [InlineData(5, "popup_model_missing")]
    [InlineData(6, "insert_admitted")]
    [InlineData(7, "foreground_changed")]
    [InlineData(8, "ready")]
    public void HostReportsFirstRefusalAndDoesNotEvaluateLaterChecks(int refusal, string expected)
    {
        var checks = new List<int>();
        var receipts = new List<string>();
        bool Check(int index) { checks.Add(index); return index != refusal; }
        Assert.Equal(refusal == 8, NativeHostReadiness.Check(() => Check(0), () => Check(1),
            () => Check(2), () => Check(3), () => Check(4), () => Check(5), () => Check(6),
            () => Check(7), receipts.Add));
        Assert.Equal(new[] { expected }, receipts);
        Assert.Equal(Enumerable.Range(0, Math.Min(refusal + 1, 8)), checks);
    }
}
