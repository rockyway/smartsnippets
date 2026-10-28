using SmartSnippets.Core.Domain;
using SmartSnippets.Core.Porting;
using SmartSnippets.Core.Store;

namespace SmartSnippets.Core.Tests.Porting;

public class RoundTripTests
{
    private static ParsedImport ThroughFile(byte[] bytes)
    {
        string path = Path.Combine(Path.GetTempPath(), "ss-roundtrip-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            File.WriteAllBytes(path, bytes);
            using var stream = File.OpenRead(path);
            // Core has no UI pump: block only this file boundary, keeping the real store on its owner thread.
            var read = CappedReader.ReadAsync(stream, SnippetFileRules.MaxImportBytes).GetAwaiter().GetResult();
            Assert.Null(read.Refusal); var p = ImportParser.Parse(read.Utf8Json!, Fixture.Now); Assert.Null(p.Failure); return p.Parsed!;
        }
        finally { File.Delete(path); }
    }
    private static void Seed(PortingStoreEnv e)
    {
        long work = e.Store.CreateFolder("Work").Value!.Id; long home = e.Store.CreateFolder("Home").Value!.Id; e.Store.CreateFolder("Empty");
        e.Store.CreateSnippet(new("hello", "Greeting", true, work, "mail")); e.Store.CreateSnippet(new("hello", "Greeting", true, home, "mail"));
        e.Store.CreateSnippet(new("hello", "Greeting", true, null, "x")); e.Store.CreateSnippet(new("hello", "Different", true, work, "mail"));
        e.Store.CreateSnippet(new("a\rb", "One", true, null, "")); e.Store.CreateSnippet(new("a\nb", "Two", true, null, ""));
        e.Store.CreateSnippet(new("😀 漢字\t  \r\nnext\nlast\rline  ", "", false, null, "tag, two"));
        e.Store.RecordUsage(e.Store.Snippets[0].Snippet.Id); e.Store.RecordUsage(e.Store.Snippets[0].Snippet.Id);
        e.Db.Exec("UPDATE snippets SET created_at=1700000000000,updated_at=1700000500000,last_used_at=CASE WHEN usage_count>0 THEN 1700000900000 ELSE NULL END;"); e.Store.Load();
    }
    private sealed record Row(string Text, string Label, bool IsLabelCustom, string? FolderName, IReadOnlyList<string> Tags, int UsageCount, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, DateTimeOffset? LastUsedAt);
    private sealed class RowComparer : IEqualityComparer<Row>
    {
        public bool Equals(Row? a, Row? b) => a is not null && b is not null && a with { Tags = b.Tags } == b && a.Tags.SequenceEqual(b.Tags, StringComparer.Ordinal);
        public int GetHashCode(Row r) => HashCode.Combine(r.Text, r.Label, r.FolderName);
    }
    private static Dictionary<Row, int> Projection(ISnippetStore store)
    {
        var rows = new Dictionary<Row, int>(new RowComparer());
        foreach (var v in store.Snippets)
        {
            var s = v.Snippet; var row = new Row(s.Text, s.Label, s.IsLabelCustom, v.FolderName, s.Tags, s.UsageCount, s.CreatedAt, s.UpdatedAt, s.LastUsedAt);
            rows[row] = rows.GetValueOrDefault(row) + 1;
        }
        return rows;
    }
    private static void EqualLibrary(ISnippetStore source, ISnippetStore target)
    {
        var a = Projection(source); var b = Projection(target); Assert.Equal(a.Count, b.Count);
        foreach (var (row, count) in a) { Assert.True(b.TryGetValue(row, out var actual)); Assert.Equal(count, actual); }
        Assert.Equal(source.Folders.Select(f => f.Name), target.Folders.Select(f => f.Name));
    }
    [Fact] public void RoundTrip_ExportThenImportIntoEmpty_ReproducesEverything()
    {
        using var source = new PortingStoreEnv(); Seed(source); var parsed = ThroughFile(NativeExport.Write(source.Store.Folders, source.Store.Snippets, Fixture.Now).Utf8Json!);
        using var target = new PortingStoreEnv(); var s = SnippetImport.Commit(parsed, target.Store); Assert.Equal(7, s.Imported); Assert.True(s.IsBalanced); EqualLibrary(source.Store, target.Store);
    }
    [Fact] public void RoundTrip_FolderIdsDiffer_ProjectionStillEqual() { using var source = new PortingStoreEnv(); Seed(source); using var target = new PortingStoreEnv(); SnippetImport.Commit(ThroughFile(NativeExport.Write(source.Store.Folders, source.Store.Snippets, Fixture.Now).Utf8Json!), target.Store); Assert.NotEqual(source.Store.Snippets[0].Snippet.FolderId, target.Store.Snippets[0].Snippet.FolderId); EqualLibrary(source.Store, target.Store); }
    [Fact] public void RoundTrip_FullKeyRepeat_CollapsesToFirst()
    {
        using var source = new PortingStoreEnv(); foreach (string text in new[] { "a\rb", "a\nb", "same", "same" }) source.Store.CreateSnippet(new(text, "Label", true, null, "x")); source.Store.RecordUsage(source.Store.Snippets[3].Snippet.Id);
        using var target = new PortingStoreEnv(); var s = SnippetImport.Commit(ThroughFile(NativeExport.Write(source.Store.Folders, source.Store.Snippets, Fixture.Now).Utf8Json!), target.Store); Assert.Equal(2, s.Imported); Assert.Equal(2, s.SkippedDuplicates); Assert.True(s.IsBalanced); Assert.Equal("a\rb", target.Store.Snippets[0].Snippet.Text); Assert.Equal(0, target.Store.Snippets[1].Snippet.UsageCount);
    }
    [Fact] public void UsageChain_Export_Reimport_Exact() { using var source = new PortingStoreEnv(); source.Store.ImportSnippets(new([new(0, "one", "One", null, [], new(Fixture.Now, Fixture.Now, null, 1000000003)), new(1, "two", "Two", null, [], new(Fixture.Now, Fixture.Now, null, int.MaxValue))], [], DedupeMode.FullRow)); using var target = new PortingStoreEnv(); SnippetImport.Commit(ThroughFile(NativeExport.Write(source.Store.Folders, source.Store.Snippets, Fixture.Now).Utf8Json!), target.Store); Assert.Equal(new[] { 1000000003, int.MaxValue }, target.Store.Snippets.Select(v => v.Snippet.UsageCount)); }
    [Fact] public void RoundTrip_ReimportIntoSameLibrary_ZeroNew_NoEvent() { using var e = new PortingStoreEnv(); Seed(e); var p = ThroughFile(NativeExport.Write(e.Store.Folders, e.Store.Snippets, Fixture.Now).Utf8Json!); e.Events.Clear(); var s = SnippetImport.Commit(p, e.Store); Assert.Equal(0, s.Imported); Assert.Equal(7, s.SkippedDuplicates); Assert.Empty(e.Events); }
    [Fact] public void RoundTrip_ReimportIntoTarget_ZeroNew() { using var e = new PortingStoreEnv(); using var target = new PortingStoreEnv(); Seed(e); var p = ThroughFile(NativeExport.Write(e.Store.Folders, e.Store.Snippets, Fixture.Now).Utf8Json!); SnippetImport.Commit(p, target.Store); target.Events.Clear(); Assert.Equal(0, SnippetImport.Commit(p, target.Store).Imported); Assert.Empty(target.Events); }
    [Fact] public void RoundTrip_ParserDetectsOwnOutputAsNative() => Assert.Equal(ImportFormat.Native, ThroughFile(NativeExportTests.Export().Utf8Json!).Format);
    private static int Overhead => checked((int)NativeExportTests.Export("x").Bytes - 1);
    [Fact] public void ExportImport_ExactlyAtCap_ExportsAndImports() { var r = NativeExportTests.Export(new string('x', SnippetFileRules.MaxImportBytes - Overhead)); Assert.Null(r.Refusal); Assert.Equal(SnippetFileRules.MaxImportBytes, r.Utf8Json!.Length); using var target = new PortingStoreEnv(); Assert.Equal(1, SnippetImport.Commit(ThroughFile(r.Utf8Json), target.Store).Imported); }
    [Fact] public void ExportImport_OneByteOver_RefusedAtExport_NoFile() { var path = Path.Combine(Path.GetTempPath(), "ss-no-export-" + Guid.NewGuid().ToString("N") + ".json"); var r = NativeExportTests.Export(new string('x', SnippetFileRules.MaxImportBytes - Overhead + 1)); Assert.Equal(ExportRefusal.TooLarge, r.Refusal); Assert.Null(r.Utf8Json); Assert.False(File.Exists(path)); Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(path)!, Path.GetFileName(path) + ".*.tmp")); }
    [Fact] public void ExportImport_EscapingExpansion_RefusedAtExport() { var text = new string('é', 6 * 1024 * 1024); Assert.True(System.Text.Encoding.UTF8.GetByteCount(text) < SnippetFileRules.MaxImportBytes); Assert.Equal(ExportRefusal.TooLarge, NativeExportTests.Export(text).Refusal); }
    [Fact] public void ExportImport_EscapingNearCap_ImportsBack() { var length = (SnippetFileRules.MaxImportBytes - Overhead - 100) / 6; var r = NativeExportTests.Export(new string('é', length) + "ascii"); Assert.Null(r.Refusal); Assert.InRange(r.Bytes, SnippetFileRules.MaxImportBytes - 200L, SnippetFileRules.MaxImportBytes); using var target = new PortingStoreEnv(); var s = SnippetImport.Commit(ThroughFile(r.Utf8Json!), target.Store); Assert.Equal(1, s.Imported); Assert.Equal(new string('é', length) + "ascii", target.Store.Snippets[0].Snippet.Text); }
}
