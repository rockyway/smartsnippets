using SmartSnippets.Core.Porting;

namespace SmartSnippets.Core.Tests.Porting;

public class InkSpokeFormatTests
{
    private static ParsedImport Read(string entry, string? groups = null) => Fixture.Parse("{\"SchemaVersion\":2,\"Mappings\":[" + entry + "]" + (groups is null ? "" : ",\"Groups\":" + groups) + "}");
    private static string Mapping(string parameters, string extra = "") => "{\"ActionType\":\"SendKeys\",\"ActionParamsJson\":" + System.Text.Json.JsonSerializer.Serialize(parameters, TestJsonContext.Default.String) + extra + "}";
    [Fact] public void InkSpoke_Fixture_PerEntryOutcomes()
    {
        var p = Fixture.Read("inkspoke-v2.json"); Assert.Equal(9, p.EntryCount);
        Assert.Equal(new[] { 0, 1, 7, 8 }, p.Batch.Candidates.Select(c => c.EntryIndex));
        Assert.Equal(new[] { new SkippedEntry(2, SkipReason.Parameterized), new SkippedEntry(3, SkipReason.Secret), new SkippedEntry(4, SkipReason.NotSendKeys), new SkippedEntry(5, SkipReason.UnparsableParams) }, p.Skipped);
        Assert.Equal(new[] { new RejectedEntry(6, RejectReason.WrongFieldType) }, p.Rejected);
        Assert.Equal(DedupeMode.TextOnly, p.Batch.Dedupe);
        Assert.Equal("Prompts", p.Batch.Candidates[0].FolderName); Assert.All(p.Batch.Candidates.Skip(1), c => Assert.Null(c.FolderName is "" ? null : c.FolderName));
    }
    [Fact] public void InkSpoke_SecretInParams_Skipped() => Assert.Equal(SkipReason.Secret, Assert.Single(Read(Mapping("{\"Text\":\"secret\",\"IsSecret\":true}")).Skipped).Reason);
    [Fact] public void InkSpoke_SecretOnMapping_Skipped() => Assert.Equal(SkipReason.Secret, Assert.Single(Read(Mapping("{\"Text\":\"secret\"}", ",\"IsSecret\":true")).Skipped).Reason);
    [Fact] public void InkSpoke_LowercaseTextKey_Imported() => Assert.Equal("{last}", Assert.Single(Read(Mapping("{\"text\":\"{last}\"}")).Batch.Candidates).Text);
    [Fact] public void InkSpoke_UpperTextWinsOverLower() => Assert.Equal("upper", Assert.Single(Read(Mapping("{\"Text\":\"upper\",\"text\":\"lower\"}")).Batch.Candidates).Text);
    [Fact] public void InkSpoke_EmptyParams_Unparsable() => Assert.Equal(SkipReason.UnparsableParams, Assert.Single(Read(Mapping("")).Skipped).Reason);
    [Fact] public void InkSpoke_ParamsArray_Unparsable() => Assert.Equal(SkipReason.UnparsableParams, Assert.Single(Read(Mapping("[]")).Skipped).Reason);
    [Fact] public void InkSpoke_ParameterizedCheckedBeforeParams() => Assert.Equal(SkipReason.Parameterized, Assert.Single(Read(Mapping("", ",\"IsParameterized\":true")).Skipped).Reason);
    [Fact] public void InkSpoke_MissingGroupId_Unfiled() => Assert.Null(Assert.Single(Read(Mapping("{\"Text\":\"ok\"}")).Batch.Candidates).FolderName);
    [Fact] public void InkSpoke_UnknownGroupId_Unfiled() => Assert.Null(Assert.Single(Read(Mapping("{\"Text\":\"ok\"}", ",\"GroupId\":\"missing\"")).Batch.Candidates).FolderName);
    [Fact] public void InkSpoke_GroupsAbsent_AllUnfiled() => Assert.Null(Assert.Single(Read(Mapping("{\"Text\":\"ok\"}", ",\"GroupId\":\"g1\"")).Batch.Candidates).FolderName);
    [Fact] public void InkSpoke_FavoritesVocabularyIgnored() { var p = Fixture.Parse("{\"SchemaVersion\":2,\"Mappings\":[],\"Favorites\":[42],\"Vocabulary\":[null]}"); Assert.Equal(0, p.EntryCount); Assert.Empty(p.Batch.Candidates); Assert.Empty(p.Rejected); }
    [Fact] public void InkSpoke_ActionTypeMissing_Rejected() => Assert.Equal(RejectReason.WrongFieldType, Assert.Single(Read("{}").Rejected).Reason);
}

[System.Text.Json.Serialization.JsonSerializable(typeof(string))]
internal partial class TestJsonContext : System.Text.Json.Serialization.JsonSerializerContext;
