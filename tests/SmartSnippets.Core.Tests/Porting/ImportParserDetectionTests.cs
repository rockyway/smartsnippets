using System.Text;
using SmartSnippets.Core.Porting;

namespace SmartSnippets.Core.Tests.Porting;

public class ImportParserDetectionTests
{
    private static ImportParseResult Parse(string json) => ImportParser.Parse(Encoding.UTF8.GetBytes(json), DateTimeOffset.UtcNow);
    [Theory]
    [InlineData("{\"format\":\"smart-snippets\",\"version\":1,\"snippets\":[]}", ImportFormat.Native)]
    [InlineData("{\"version\":1,\"snippets\":[]}", ImportFormat.TermFlow)]
    [InlineData("{\"version\":\"1.3\",\"commands\":[]}", ImportFormat.Rephlo)]
    [InlineData("{\"SchemaVersion\":2,\"Mappings\":[]}", ImportFormat.InkSpoke)]
    [InlineData("{\"version\":1,\"snippets\":[],\"commands\":[]}", ImportFormat.TermFlow)]
    public void Detect_KnownFormat(string json, ImportFormat format) => Assert.Equal(format, Parse(json).Parsed!.Format);
    [Theory]
    [InlineData("{\"format\":\"other\",\"snippets\":[]}")]
    [InlineData("{\"mappings\":[]}")]
    [InlineData("{}")]
    [InlineData("[]")]
    public void Detect_Unknown(string json) => Assert.Equal(ImportRefusal.UnknownFormat, Parse(json).Failure!.Refusal);
    [Theory]
    [InlineData("{\"version\":2,\"snippets\":5}", "2")]
    [InlineData("{\"version\":\"1\",\"snippets\":[]}", "\"1\"")]
    [InlineData("{\"version\":1.0,\"snippets\":[]}", "1.0")]
    [InlineData("{\"snippets\":[]}", null)]
    [InlineData("{\"version\":\"1.2\",\"commands\":[]}", "\"1.2\"")]
    [InlineData("{\"version\":1.3,\"commands\":[]}", "1.3")]
    [InlineData("{\"SchemaVersion\":3,\"Mappings\":[]}", "3")]
    [InlineData("{\"format\":\"smart-snippets\",\"version\":2,\"snippets\":[]}", "2")]
    public void Version_CheckedFirst(string json, string? found) { var f = Parse(json).Failure!; Assert.Equal(ImportRefusal.UnsupportedVersion, f.Refusal); Assert.Equal(found, f.FoundVersion); }
    [Theory] [InlineData("{\"format\":\"smart-snippets\",\"version\":1}")] [InlineData("{\"version\":1,\"snippets\":42}")]
    public void Entries_MissingOrNotArray(string json) => Assert.Equal(ImportRefusal.MissingEntries, Parse(json).Failure!.Refusal);
    [Theory] [InlineData("{")] [InlineData("[1,]")] [InlineData("/*comment*/{}")] [InlineData("{} true")]
    public void InvalidJson_Syntax(string json) => Assert.Equal(ImportRefusal.InvalidJson, Parse(json).Failure!.Refusal);
    [Fact] public void Entries_LimitPlusOne_TooManyEntries_WithCount() { var json = "{\"version\":1,\"snippets\":[" + string.Join(',', Enumerable.Repeat("42", SnippetFileRules.MaxImportEntries + 1)) + "]}"; var failure = Parse(json).Failure!; Assert.Equal(ImportRefusal.TooManyEntries, failure.Refusal); Assert.Equal(SnippetFileRules.MaxImportEntries + 1, failure.EntryCount); }
}
