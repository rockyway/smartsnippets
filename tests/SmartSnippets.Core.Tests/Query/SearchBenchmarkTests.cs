using System.Text;
using SmartSnippets.Core.Domain;
using SmartSnippets.Core.Query;
using SmartSnippets.Core.Settings;
using Xunit.Abstractions;

namespace SmartSnippets.Core.Tests.Query;

[Trait("Category", "Benchmark")]
public class SearchBenchmarkTests
{
    private const int SnippetCount = 10_000;
    private const int Runs = 50;
    private const double BudgetMs = 150;

    private static readonly string[] Words =
    [
        "invoice", "meeting", "regards", "thanks", "tomorrow", "address", "payment", "support", "report", "schedule",
        "привет", "спасибо", "договор", "встреча", "оплата", "отчёт",
        "你好", "谢谢", "会议", "发票", "地址", "日本語", "ありがとう", "お願いします",
        "مرحبا", "شكرا", "عنوان", "naïve", "café", "Straße", "İstanbul", "👋", "✅", "🚀",
    ];

    private static readonly DateTimeOffset Epoch = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly ITestOutputHelper _output;

    public SearchBenchmarkTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void TenThousandSnippets_WithinBudget()
    {
        var views = Generate();
        Assert.Equal(SnippetCount, views.Count);

        // Warm-up so JIT cost is not attributed to the first measurement.
        _ = new SnippetSearch(views.Take(100).ToList()).Filter("invoice meeting");
        _ = SnippetSort.Apply(views.Take(100), SortField.LastUsed);

        var buildMs = Time(() => _ = new SnippetSearch(views));
        var search = new SnippetSearch(views);
        var sortMs = Time(() => _ = SnippetSort.Apply(views, SortField.LastUsed));

        // Common terms match ~95% of items (early exit per term); the "qzxv" terms never match, so every
        // field of every item is scanned: the worst case for a substring search.
        var oneTerm = Measure(() => search.Filter("invoice"));
        var threeTerms = Measure(() => search.Filter("invoice meeting 你好"));
        var oneTermMiss = Measure(() => search.Filter("qzxv"));
        var threeTermsMiss = Measure(() => search.Filter("invoice meeting qzxv"));
        var hits1 = search.Filter("invoice").Count;
        var hits3 = search.Filter("invoice meeting 你好").Count;

        _output.WriteLine($"Config: {SnippetCount} snippets, seed 20261027, {Runs} runs/query, {DebugOrRelease()}");
        _output.WriteLine($"Index build:            {buildMs:F1} ms");
        _output.WriteLine($"Sort (LastUsed, 10k):   {sortMs:F1} ms");
        _output.WriteLine($"Filter 1 term  ({hits1} hits):  median {oneTerm.Median:F1} ms, p95 {oneTerm.P95:F1} ms");
        _output.WriteLine($"Filter 3 terms ({hits3} hits):  median {threeTerms.Median:F1} ms, p95 {threeTerms.P95:F1} ms");
        _output.WriteLine($"Filter 1 term  (0 hits, worst case):  median {oneTermMiss.Median:F1} ms, p95 {oneTermMiss.P95:F1} ms");
        _output.WriteLine($"Filter 3 terms (0 hits, worst case):  median {threeTermsMiss.Median:F1} ms, p95 {threeTermsMiss.P95:F1} ms");

        Assert.True(oneTermMiss.Median <= BudgetMs, $"1-term miss median {oneTermMiss.Median:F1} ms > {BudgetMs} ms");
        Assert.True(threeTermsMiss.Median <= BudgetMs, $"3-term miss median {threeTermsMiss.Median:F1} ms > {BudgetMs} ms");
        Assert.True(oneTerm.Median <= BudgetMs, $"1-term median {oneTerm.Median:F1} ms > {BudgetMs} ms");
        Assert.True(threeTerms.Median <= BudgetMs, $"3-term median {threeTerms.Median:F1} ms > {BudgetMs} ms");
        Assert.True(buildMs + sortMs <= BudgetMs, $"build+sort {buildMs + sortMs:F1} ms > {BudgetMs} ms");
    }

    private static string DebugOrRelease() =>
#if DEBUG
        "Debug";
#else
        "Release";
#endif

    private static double Time(Action action)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        action();
        return sw.Elapsed.TotalMilliseconds;
    }

    private static (double Median, double P95) Measure(Func<IReadOnlyList<SnippetView>> filter)
    {
        _ = filter();
        var samples = new double[Runs];
        for (var i = 0; i < Runs; i++)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            _ = filter();
            samples[i] = sw.Elapsed.TotalMilliseconds;
        }

        Array.Sort(samples);
        return ((samples[(Runs / 2) - 1] + samples[Runs / 2]) / 2, samples[(int)Math.Ceiling(Runs * 0.95) - 1]);
    }

    private static List<SnippetView> Generate()
    {
        var random = new Random(20261027);
        var folders = Enumerable.Range(1, 50).Select(i => $"Folder {i:00} {Words[i % Words.Length]}").ToArray();
        var views = new List<SnippetView>(SnippetCount);
        for (var i = 1; i <= SnippetCount; i++)
        {
            var text = MakeText(random, random.Next(200, 2001));
            var tags = Enumerable.Range(0, random.Next(0, 6)).Select(_ => Words[random.Next(Words.Length)]).ToList();
            var folderIndex = random.Next(-1, folders.Length);
            var tagList = TagList.Parse(TagList.Format(tags));
            var snippet = new Snippet(
                i,
                text,
                LabelSuggester.Suggest(text, tagList),
                false,
                folderIndex < 0 ? null : folderIndex + 1,
                tagList,
                Epoch.AddMinutes(i),
                Epoch.AddMinutes(i),
                random.Next(3) == 0 ? null : Epoch.AddMinutes(random.Next(100_000)),
                random.Next(50));
            views.Add(new SnippetView(snippet, folderIndex < 0 ? null : folders[folderIndex]));
        }

        return views;
    }

    private static string MakeText(Random random, int length)
    {
        var sb = new StringBuilder(length + 32);
        while (sb.Length < length)
        {
            sb.Append(Words[random.Next(Words.Length)]);
            sb.Append(random.Next(12) == 0 ? "\n" : " ");
        }

        return sb.ToString(0, length);
    }
}
