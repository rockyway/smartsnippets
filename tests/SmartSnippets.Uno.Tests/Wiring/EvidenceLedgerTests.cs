using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using SmartSnippets.Uno.Evidence;

namespace SmartSnippets.Uno.Tests.Wiring;

public sealed class EvidenceLedgerTests
{
    private static string Root
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "SmartSnippets.slnx")))
            {
                if (Directory.Exists(Path.Combine(dir.FullName, "docs", "plan"))) return dir.FullName;
                dir = dir.Parent;
            }
            return dir?.FullName ?? throw new InvalidOperationException("Repository not found");
        }
    }

    private static EvidenceLedger Load() => JsonSerializer.Deserialize(
        File.ReadAllText(Path.Combine(Root, "src/SmartSnippets.Uno/Evidence/ac-matrix.json")),
        EvidenceLedgerJsonContext.Default.EvidenceLedger)!;

    [Fact]
    public void MissingNativeEvidence_CannotPromoteCandidateOr018ToComplete()
    {
        var ledger = Load();
        Assert.NotEmpty(ledger.Rows);
        Assert.Equal(ledger.Rows.Count, ledger.Rows.Select(r => r.Id).Distinct().Count());
        foreach (var id in Enumerable.Range(1, 7).Select(i => $"AC{i}")
            .Concat(Enumerable.Range(1, 14).Select(i => $"S{i:00}")))
            Assert.Contains(ledger.Rows, r => r.Id == id);
        foreach (var id in new[] { "R039-registered-enter", "R040-preview", "R040-expired", "R040-copy",
            "R033-snippets", "R033-folders", "R033-settings" })
            Assert.Contains(ledger.Rows, r => r.Id == id);
        foreach (var row in ledger.Rows)
        {
            Assert.Contains(row.Status, new[] { "PASS", "PARTIAL", "MISSING", "BLOCKED-NATIVE" });
            if (row.Id.StartsWith("R039-") || row.Id.StartsWith("R040-")) Assert.Equal("PARTIAL", row.Status);
            if (row.ResearchDoc.Length != 0)
            {
                var receipt = File.ReadAllText(Path.Combine(Root, row.ResearchDoc));
                Assert.False(string.IsNullOrWhiteSpace(row.SessionId));
                Assert.False(string.IsNullOrWhiteSpace(row.SourceFingerprint));
                Assert.Contains(row.SessionId, receipt);
                Assert.Contains(row.SourceFingerprint, receipt);
            }
            if (row.Status != "PASS") continue;
            Assert.False(string.IsNullOrWhiteSpace(row.SessionId));
            Assert.False(string.IsNullOrWhiteSpace(row.ResearchDoc));
            Assert.Equal(ledger.SourceFingerprint, row.SourceFingerprint);
            Assert.Contains(row.SessionId, File.ReadAllText(Path.Combine(Root, row.ResearchDoc)));
            Assert.Contains(row.SourceFingerprint, File.ReadAllText(Path.Combine(Root, row.ResearchDoc)));
        }
        var source = Path.Combine(Root, "src/SmartSnippets.Uno");
        var entries = Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories)
            .Select(p => (Path: p, Relative: Path.GetRelativePath(source, p).Replace('\\', '/')))
            .Where(p => !p.Relative.StartsWith("bin/") && !p.Relative.StartsWith("obj/") && !p.Relative.StartsWith("Evidence/"))
            .Where(p => new[] { ".cs", ".xaml", ".csproj" }.Contains(Path.GetExtension(p.Path)))
            .OrderBy(p => p.Relative, StringComparer.Ordinal)
            .Select(p => p.Relative + ":" + Convert.ToHexString(SHA256.HashData(
                Encoding.UTF8.GetBytes(File.ReadAllText(p.Path).Replace("\r\n", "\n")))).ToLowerInvariant());
        var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n", entries)))).ToLowerInvariant();
        // The checkpoint binds only once a row claims PASS; until then source changes need no ledger edit.
        if (ledger.Rows.Any(r => r.Status == "PASS")) Assert.Equal(digest, ledger.SourceFingerprint);
        foreach (var plan in Directory.GetFiles(Path.Combine(Root, "docs/plan"), "0*.md")
            .Where(p => Path.GetFileName(p).StartsWith("018-") || Path.GetFileName(p).StartsWith("019-")))
        {
            var status = Regex.Match(File.ReadAllText(plan), @"(?im)^\*\*Status:\*\*\s*(.+)$");
            Assert.True(status.Success);
            if (Regex.IsMatch(status.Groups[1].Value, @"(?i)\bcomplete(?:d)?\b")) Assert.True(ledger.CanComplete);
        }
    }

    [Fact]
    public void PromotionPolicy_RejectsMissingReceiptSupersededSourceAndIncompleteRows()
    {
        var ledger = new EvidenceLedger { SourceFingerprint = "current", Rows = [new EvidenceRow
            { Status = "PASS", ResearchDoc = "receipt", SessionId = "session", SourceFingerprint = "current" }] };
        Assert.True(ledger.CanComplete);
        ledger.Rows[0].SessionId = "";
        Assert.False(ledger.CanComplete);
        ledger.Rows[0].SessionId = "session";
        ledger.Rows[0].SourceFingerprint = "superseded";
        Assert.False(ledger.CanComplete);
        ledger.Rows[0].SourceFingerprint = "current";
        foreach (var status in new[] { "PARTIAL", "MISSING", "BLOCKED-NATIVE" })
        {
            ledger.Rows[0].Status = status;
            Assert.False(ledger.CanComplete);
        }
    }

    [Fact]
    public void ReadmeMatrix_MatchesLedger()
    {
        var readme = File.ReadAllText(Path.Combine(Root, "src/SmartSnippets.Uno/README.md"));
        var matrix = readme.Split("<!-- evidence-matrix:start -->")[1].Split("<!-- evidence-matrix:end -->")[0].Trim();
        Assert.Equal(RenderMatrix(Load()).Replace("\r", ""), matrix.Replace("\r", ""));
    }

    private static string RenderMatrix(EvidenceLedger ledger) =>
        "| ID | AC | Surface / scene | Status | Receipt / session |\n|---|---|---|---|---|\n" +
        string.Join("\n", ledger.Rows.Select(r => $"| {r.Id} | {r.Ac} | {r.Surface} / {r.Scene} | {r.Status} | " +
            (r.ResearchDoc.Length == 0 ? "MISSING" : $"{r.ResearchDoc} / {r.SessionId}") + " |"));
}
