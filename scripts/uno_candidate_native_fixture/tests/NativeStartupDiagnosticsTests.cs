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
    [InlineData(0, 0, "none", "not_applicable")]
    [InlineData(1, 10, "owned_a", "not_applicable")]
    [InlineData(2, 10, "a_other_window", "owned_by_a")]
    [InlineData(2, 10, "a_other_window", "ime")]
    [InlineData(2, 10, "a_other_window", "tooltip")]
    [InlineData(2, 10, "a_other_window", "toplevel_other")]
    [InlineData(2, 10, "a_other_window", "private title")]
    [InlineData(3, 20, "worker", "not_applicable")]
    [InlineData(4, 30, "guardian", "not_applicable")]
    [InlineData(5, 40, "coordinator", "not_applicable")]
    [InlineData(6, 50, "launcher", "not_applicable")]
    [InlineData(7, 60, "foreign", "not_applicable")]
    [InlineData(1, 60, "foreign", "not_applicable")]
    public void HighRateRelationsUsePidAndAllowlistedKind(int window, uint pid, string relation, string kind)
    {
        var reads = 0;
        var trace = new OwnedForegroundTrace(() => { reads++; return window; }, _ => pid,
            1, 10, 20, 30, 40, 50, 7, _ => kind, () => 0, () => Task.CompletedTask);
        var safeKind = kind == "private title" ? "toplevel_other" : kind;
        Assert.Equal($"relation={relation} kind={safeKind} initial_foreground={window == 7} sample_order=1 elapsed_ms=0",
            trace.Sample(1, 0));
        Assert.Equal(1, reads);
    }

    [Theory]
    [InlineData(5, 51)] [InlineData(130, 2)] [InlineData(0, 51)]
    public async Task HighRateTraceIsTimeAndCountBounded(int step, int count)
    {
        long time = 0;
        var window = 1;
        var trace = new OwnedForegroundTrace(() => window, _ => 10, 1, 10, 20, 30, 40, 50, 7,
            _ => "ime", () => time, () => { time += step; window = 2; return Task.CompletedTask; });
        var records = await trace.RecordAsync();
        Assert.Equal(count, records.Count);
        Assert.Contains("relation=owned_a", records[0]);
        Assert.All(records.Skip(1), record => Assert.Contains("relation=a_other_window kind=ime", record));
        Assert.DoesNotContain(records, record => record.Contains("elapsed_ms=260"));
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
