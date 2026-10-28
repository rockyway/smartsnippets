using SmartSnippets.Core.Porting;

namespace SmartSnippets.Core.Tests.Porting;

public class SnippetFileRulesTests
{
    [Fact] public void MaxImportBytes_Is32MiB() => Assert.Equal(33_554_432, SnippetFileRules.MaxImportBytes);
    [Fact] public void MaxImportEntries_IsCalibratedValue() => Assert.Equal(50_000, SnippetFileRules.MaxImportEntries);
    [Theory]
    [InlineData("a.json", true)] [InlineData("a.JSON", true)] [InlineData("a.json.txt", false)]
    [InlineData("a.txt", false)] [InlineData("a", false)] [InlineData("json", false)]
    public void HasJsonExtension(string path, bool expected) => Assert.Equal(expected, SnippetFileRules.HasJsonExtension(path));
    [Fact] public void ExportBaseName_IsInvariantDate() => Assert.Equal("smart-snippets-2026-10-28", SnippetFileRules.ExportBaseName(new DateTimeOffset(2026, 10, 28, 0, 0, 0, TimeSpan.Zero)));
}
