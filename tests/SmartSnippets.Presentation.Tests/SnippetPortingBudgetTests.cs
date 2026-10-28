using System.Diagnostics;
using System.Text;
using Microsoft.Data.Sqlite;
using SmartSnippets.Core.Data;
using SmartSnippets.Core.Logging;
using SmartSnippets.Core.Porting;
using SmartSnippets.Core.Store;
using SmartSnippets.Presentation.Tests.Fakes;
using SmartSnippets.Presentation.ViewModels;
using Xunit.Abstractions;

namespace SmartSnippets.Presentation.Tests;

[Collection("AppLog")]
public class SnippetPortingBudgetTests(ITestOutputHelper output)
{
#if DEBUG
    private const int Threshold = 10000;
#else
    private const int Threshold = 2000;
#endif
    private static string Entry(int i, string kind) => kind switch
    {
        "empty-folders" => $"{{\"name\":\"F{i:D7}\"}}",
        "folders" => $"{{\"text\":\"s{i:D7}\",\"folder\":\"F{i:D7}\"}}",
        "typical" => $"{{\"text\":\"s{i:D7}{new string('x', 1024)}\",\"folder\":\"F{i % 100:D3}\",\"tags\":[\"one\",\"two\"]}}",
        _ => $"{{\"text\":\"s{i:D7}\"}}"
    };
    private static int Fits(string kind) => (SnippetFileRules.MaxImportBytes - 30) / (Encoding.UTF8.GetByteCount(Entry(0, kind)) + 1);
    private double Measure(int count, string kind, bool calibration)
    {
        var directory = Path.Combine(Path.GetTempPath(), "ss-budget-" + Guid.NewGuid().ToString("N"));
        try
        {
            var factory = new SqliteConnectionFactory(Path.Combine(directory, "db.sqlite")); new Migrator(factory).MigrateToLatest();
            var store = new SnippetStore(factory, TimeProvider.System); store.Load();
            store.ImportSnippets(new(Enumerable.Range(0, 20000).Select(i => new ImportCandidate(i, "existing-" + i, null, null, [], null)).ToArray(), [], DedupeMode.TextOnly));
            var dialogs = new ScriptedDialogService(); using var list = new SnippetListViewModel(store); using var popup = SnippetPortingIntegrationTests.Popup(store, factory); using var folders = new FoldersPageViewModel(store, dialogs); using var editor = new SnippetEditorViewModel(store, dialogs, null);
            AppLog.Shutdown(); AppLog.Initialize(directory);
            ParsedImport parsed; var parseWatch = Stopwatch.StartNew();
            if (calibration)
            {
                // Calibration must measure above the current guard to choose the next guard. It bypasses ONLY
                // parsing/refusal and supplies exactly the candidates the synthetic JSON would produce.
                parsed = new(ImportFormat.TermFlow, count, new(Enumerable.Range(0, count).Select(i => new ImportCandidate(i,
                    $"s{i:D7}" + (kind == "typical" ? new string('x', 1024) : ""), null,
                    kind == "folders" ? $"F{i:D7}" : kind == "typical" ? $"F{i % 100:D3}" : null,
                    kind == "typical" ? ["one", "two"] : [], null)).ToArray(), [], DedupeMode.TextOnly), [], []);
            }
            else
            {
                var prefix = kind == "empty-folders" ? "{\"format\":\"smart-snippets\",\"version\":1,\"snippets\":[],\"folders\":[" : "{\"version\":1,\"snippets\":[";
                var json = prefix + string.Join(',', Enumerable.Range(0, count).Select(i => Entry(i, kind))) + "]}";
                var bytes = Encoding.UTF8.GetBytes(json); Assert.True(bytes.Length <= SnippetFileRules.MaxImportBytes);
                var result = ImportParser.Parse(bytes, DateTimeOffset.UtcNow); Assert.Null(result.Failure); parsed = result.Parsed!;
            }
            var parseMs = parseWatch.Elapsed.TotalMilliseconds;
            var watch = Stopwatch.StartNew(); var summary = SnippetImport.Commit(parsed, store); watch.Stop();
            int imported = kind == "empty-folders" ? 0 : count;
            Assert.Equal(imported, summary.Imported); Assert.True(summary.IsBalanced);
            var fresh = new SnippetStore(factory, TimeProvider.System); fresh.Load(); Assert.Equal(20000 + imported, fresh.Snippets.Count);
            if (kind == "empty-folders")
            {
                Assert.Equal(count, summary.FoldersCreated);
                Assert.Equal(count, fresh.Folders.Count);
                Assert.Equal(0, folders.Rows.Single(r => r.Name == $"F{count - 1:D7}").SnippetCount);
            }
            else { list.Query = $"s{count - 1:D7}"; Assert.Single(list.Items); }
            if (kind is "folders" or "empty-folders") Assert.Equal(count + 1, folders.Rows.Count);
            Assert.DoesNotContain("store_subscriber_failed", File.ReadAllText(Path.Combine(directory, "app.log")));
            output.WriteLine($"BUDGET kind={kind} entries={count} parse_ms={parseMs:F2} commit_ms={watch.Elapsed.TotalMilliseconds:F2}");
            return watch.Elapsed.TotalMilliseconds;
        }
        finally { AppLog.Shutdown(); SqliteConnection.ClearAllPools(); if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
    [Fact] [Trait("Category", "Budget")] public void Budget_AtLimit_Dense_CommitUnderThreshold() => Assert.True(Measure(Math.Min(SnippetFileRules.MaxImportEntries, Fits("dense")), "dense", false) <= Threshold);
    [Fact] [Trait("Category", "Budget")] public void Budget_AtLimit_FolderDense_CommitUnderThreshold() => Assert.True(Measure(Math.Min(SnippetFileRules.MaxImportEntries, Fits("folders")), "folders", false) <= Threshold);
    [Fact] [Trait("Category", "Budget")] public void Budget_TypicalAtCap_CommitUnderThreshold() => Assert.True(Measure(Math.Min(SnippetFileRules.MaxImportEntries, Fits("typical")), "typical", false) <= Threshold);
    [Fact] [Trait("Category", "Budget")] public void Budget_AtLimit_EmptyNativeFolders_CommitUnderThreshold() => Assert.True(Measure(SnippetFileRules.MaxImportEntries, "empty-folders", false) <= Threshold);
    [Fact] [Trait("Category", "Calibration")] public void Budget_Calibrate()
    {
        if (Environment.GetEnvironmentVariable("SMARTSNIPPETS_CALIBRATE") != "1") return;
        foreach (var kind in new[] { "dense", "folders", "typical" })
            foreach (int count in new[] { 10000, 20000, 25000, 50000, 100000, 200000, Fits(kind) }.Distinct().Where(n => n <= Fits(kind)))
                Measure(count, kind, true);
    }
}
