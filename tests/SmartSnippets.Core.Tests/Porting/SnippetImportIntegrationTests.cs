using SmartSnippets.Core.Porting;

namespace SmartSnippets.Core.Tests.Porting;

public class SnippetImportIntegrationTests
{
    internal static async Task<ParsedImport> ParseFile(string name)
    {
        using var stream = File.OpenRead(Fixture.PathFor(name));
        var bytes = await CappedReader.ReadAsync(stream, SnippetFileRules.MaxImportBytes);
        Assert.Null(bytes.Refusal);
        var result = ImportParser.Parse(bytes.Utf8Json!, Fixture.Now); Assert.Null(result.Failure); return result.Parsed!;
    }
    [Theory]
    [InlineData("termflow-v1.json", 10, 6, 1, 0, 3, 1)]
    [InlineData("rephlo-1.3.json", 6, 3, 1, 1, 1, 1)]
    [InlineData("inkspoke-v2.json", 9, 4, 0, 4, 1, 1)]
    [InlineData("native-v1.json", 7, 6, 1, 0, 0, 3)]
    public async Task Pipeline_EachFixture_CountsMatchIndependentTable_AndBalanced(string name, int entries, int imported, int duplicates, int unsupported, int rejected, int folders)
    {
        var parsed = await ParseFile(name);
        using var e = new PortingStoreEnv(); var s = SnippetImport.Commit(parsed, e.Store);
        Assert.Equal(new ImportSummary(parsed.Format, entries, imported, duplicates, unsupported, rejected, folders), s); Assert.True(s.IsBalanced);
        Assert.Equal(imported, e.NewStore().Snippets.Count); Assert.Equal(folders, e.NewStore().Folders.Count); Assert.Single(e.Events);
        Assert.DoesNotContain(e.Store.Folders, f => f.Name is "Zulu" or "Secure");
    }
    [Theory] [InlineData("termflow-v1.json")] [InlineData("native-v1.json")] [InlineData("rephlo-1.3.json")] [InlineData("inkspoke-v2.json")]
    public async Task Pipeline_SameFileTwice_SecondImportsZero_NoEvent(string name)
    {
        var parsed = await ParseFile(name);
        using var e = new PortingStoreEnv(); SnippetImport.Commit(parsed, e.Store); e.Events.Clear(); var before = e.Store.Snippets;
        var summary = SnippetImport.Commit(parsed, e.Store); Assert.Equal(0, summary.Imported); Assert.True(summary.IsBalanced); Assert.Empty(e.Events); Assert.Same(before, e.Store.Snippets);
    }
}
