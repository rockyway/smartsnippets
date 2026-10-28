using System.Text;
using SmartSnippets.Core.Porting;

namespace SmartSnippets.Core.Tests.Porting;

public class ImportParserDetectionTests
{
    private static ImportParseResult Parse(string json) => ImportParser.Parse(Encoding.UTF8.GetBytes(json), DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
    private static void Format(string json, ImportFormat format) { var r = Parse(json); Assert.Null(r.Failure); Assert.Equal(format, r.Parsed!.Format); }
    private static void Refuse(string json, ImportRefusal refusal) { var r = Parse(json); Assert.Null(r.Parsed); Assert.Equal(refusal, r.Failure!.Refusal); }
    private static void Version(string json, string? found) { var r = Parse(json); Assert.Null(r.Parsed); Assert.Equal(ImportRefusal.UnsupportedVersion, r.Failure!.Refusal); Assert.Equal(found, r.Failure.FoundVersion); }
    private static string Entries(int count, int version = 1) => "{\"version\":" + version + ",\"snippets\":[" + string.Join(',', Enumerable.Repeat("42", count)) + "]}";

    [Fact] public void Detect_NativeMarker_Native() => Format("{\"format\":\"smart-snippets\",\"version\":1,\"snippets\":[]}", ImportFormat.Native);
    [Theory] [InlineData("\"other\"")] [InlineData("42")] [InlineData("null")] [InlineData("\"\\ud800\"")]
    public void Detect_FormatMemberOtherThanOurs_Unknown(string marker) => Refuse("{\"format\":" + marker + ",\"version\":1,\"snippets\":[]}", ImportRefusal.UnknownFormat);
    [Fact] public void Detect_SnippetsKey_TermFlow() => Format("{\"version\":1,\"snippets\":[]}", ImportFormat.TermFlow);
    [Fact] public void Detect_CommandsKey_Rephlo() => Format("{\"version\":\"1.3\",\"commands\":[]}", ImportFormat.Rephlo);
    [Fact] public void Detect_MappingsKey_InkSpoke() => Format("{\"SchemaVersion\":2,\"Mappings\":[]}", ImportFormat.InkSpoke);
    [Fact] public void Detect_LowercaseMappings_Unknown() => Refuse("{\"SchemaVersion\":2,\"mappings\":[]}", ImportRefusal.UnknownFormat);
    [Fact] public void Detect_NoKnownKey_Unknown() => Refuse("{}", ImportRefusal.UnknownFormat);
    [Fact] public void Detect_RootArray_Unknown() => Refuse("[]", ImportRefusal.UnknownFormat);
    [Fact] public void Detect_SnippetsAndCommands_TermFlowWins() => Format("{\"version\":1,\"snippets\":[],\"commands\":[]}", ImportFormat.TermFlow);
    [Fact] public void Version_TermFlow2_Unsupported_FoundVersion2() => Version("{\"version\":2,\"snippets\":[]}", "2");
    [Fact] public void Version_TermFlowString1_Unsupported() => Version("{\"version\":\"1\",\"snippets\":[]}", "\"1\"");
    [Fact] public void Version_TermFlow1Point0_Unsupported() => Version("{\"version\":1.0,\"snippets\":[]}", "1.0");
    [Fact] public void Version_Null_IsAbsent() => Version("{\"version\":null,\"snippets\":[]}", null);
    [Fact] public void Version_TermFlowMissing_UnsupportedNoVersion() => Version("{\"snippets\":[]}", null);
    [Fact] public void Version_Rephlo12_Unsupported() => Version("{\"version\":\"1.2\",\"commands\":[]}", "\"1.2\"");
    [Fact] public void Version_RephloNumber13_Unsupported() => Version("{\"version\":1.3,\"commands\":[]}", "1.3");
    [Fact] public void Version_InkSpoke3_Unsupported() => Version("{\"SchemaVersion\":3,\"Mappings\":[]}", "3");
    [Fact] public void Version_Native2_Unsupported() => Version("{\"format\":\"smart-snippets\",\"version\":2,\"snippets\":[]}", "2");
    [Theory] [InlineData("5")] [InlineData("[42]")]
    public void Version_CheckedBeforeEntries(string entries) => Version("{\"version\":2,\"snippets\":" + entries + "}", "2");
    [Fact] public void Entries_Missing_MissingEntries() => Refuse("{\"format\":\"smart-snippets\",\"version\":1}", ImportRefusal.MissingEntries);
    [Fact] public void Entries_NotArray_MissingEntries() => Refuse("{\"version\":1,\"snippets\":42}", ImportRefusal.MissingEntries);
    [Fact] public void Entries_AtLimit_Accepted() { var r = Parse(Entries(SnippetFileRules.MaxImportEntries)); Assert.Null(r.Failure); Assert.Equal(SnippetFileRules.MaxImportEntries, r.Parsed!.EntryCount); }
    [Fact] public void Entries_LimitPlusOne_TooManyEntries_WithCount() { var r = Parse(Entries(SnippetFileRules.MaxImportEntries + 1)); Assert.Null(r.Parsed); Assert.Equal(ImportRefusal.TooManyEntries, r.Failure!.Refusal); Assert.Equal(SnippetFileRules.MaxImportEntries + 1, r.Failure.EntryCount); }
    [Fact] public void Entries_OverLimit_RefusedBeforeAnyEntryRead() => Refuse(Entries(SnippetFileRules.MaxImportEntries + 1), ImportRefusal.TooManyEntries);
    [Fact] public void Entries_OverLimit_AfterVersionCheck() => Version(Entries(SnippetFileRules.MaxImportEntries + 1, 2), "2");
    private static string NativeFolders(int folders, int snippets = 0, bool malformedFolders = false, int version = 1) =>
        "{\"format\":\"smart-snippets\",\"version\":" + version + ",\"snippets\":[" +
        string.Join(',', Enumerable.Repeat("{\"text\":\"snippet\"}", snippets)) + "],\"folders\":[" +
        string.Join(',', Enumerable.Range(0, folders).Select(i => malformedFolders ? "42" : "{\"name\":\"f" + i + "\"}")) + "]}";

    [Fact]
    public void NativeFolders_ZeroSnippets_OverLimit_Refused_WithCombinedCount()
    {
        var result = Parse(NativeFolders(SnippetFileRules.MaxImportEntries + 1));
        Assert.Null(result.Parsed);
        Assert.Equal(ImportRefusal.TooManyEntries, result.Failure!.Refusal);
        Assert.Equal(SnippetFileRules.MaxImportEntries + 1, result.Failure.EntryCount);
    }
    [Theory] [InlineData(0)] [InlineData(1)]
    public void NativeFolders_AtCombinedLimit_Accepted(int snippets)
    {
        var result = Parse(NativeFolders(SnippetFileRules.MaxImportEntries - snippets, snippets));
        Assert.Null(result.Failure);
        Assert.Equal(snippets, result.Parsed!.EntryCount); // Summary balance counts snippet outcomes, not folders.
        Assert.Equal(SnippetFileRules.MaxImportEntries - snippets, result.Parsed.Batch.FolderNames.Count);
    }
    [Fact]
    public void NativeFolders_CombinedLimitPlusOne_Refused()
    {
        var result = Parse(NativeFolders(SnippetFileRules.MaxImportEntries, 1));
        Assert.Null(result.Parsed);
        Assert.Equal(ImportRefusal.TooManyEntries, result.Failure!.Refusal);
        Assert.Equal(SnippetFileRules.MaxImportEntries + 1, result.Failure.EntryCount);
    }
    [Fact] public void NativeFolders_MalformedElementsCountTowardLimit() =>
        Refuse(NativeFolders(SnippetFileRules.MaxImportEntries + 1, malformedFolders: true), ImportRefusal.TooManyEntries);
    [Fact] public void NativeFolders_VersionCheckedBeforeCombinedLimit() =>
        Version(NativeFolders(SnippetFileRules.MaxImportEntries + 1, version: 2), "2");

    [Fact] public void InvalidJson_Syntax() => Refuse("{", ImportRefusal.InvalidJson);
    [Fact] public void InvalidJson_TrailingComma() => Refuse("{\"version\":1,\"snippets\":[],}", ImportRefusal.InvalidJson);
    [Fact] public void InvalidJson_Comment() => Refuse("/*comment*/{}", ImportRefusal.InvalidJson);
    [Fact] public void InvalidJson_TrailingGarbage() => Refuse("{} true", ImportRefusal.InvalidJson);
    [Fact] public void InvalidJson_DepthOver64() => Refuse(new string('[', 65) + "0" + new string(']', 65), ImportRefusal.InvalidJson);
    [Fact] public void Parse_NeverThrows()
    {
        string[] inputs = ["", "null", "[]", "{}", "\"x\"", "true", "false", "0", "1e400", "-1e400", "[", "{", "}", "[1,]", "{}{}", "/*x*/{}", "NaN", "undefined", "\"\\ud800\"", "{\"format\":\"\\ud800\"}", "{\"version\":\"\\ud800\",\"commands\":[]}", "{\"snippets\":null}", "{\"version\":1e400,\"snippets\":[]}", "{\"version\":null,\"snippets\":[]}", "{\"version\":{},\"snippets\":[]}", "{\"version\":[],\"snippets\":[]}", "{\"Mappings\":0}", "{\"format\":false}", new string('[', 100) + "0" + new string(']', 100), "{\"version\":1,\"snippets\":[null]}"];
        Assert.Equal(30, inputs.Length);
        foreach (var input in inputs) { var r = Parse(input); Assert.NotEqual(r.Parsed is null, r.Failure is null); }
        Assert.NotNull(ImportParser.Parse(new byte[] { 0xff, 0xc3, 0x28 }, DateTimeOffset.UtcNow).Failure);
    }
}
