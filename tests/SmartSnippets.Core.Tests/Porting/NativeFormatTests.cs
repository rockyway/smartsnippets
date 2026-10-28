using SmartSnippets.Core.Porting;

namespace SmartSnippets.Core.Tests.Porting;

public class NativeFormatTests
{
    [Fact] public void Native_Fixture_PerEntryOutcomes() { var p = Fixture.Read("native-v1.json"); Assert.Equal(7, p.EntryCount); Assert.Equal(Enumerable.Range(0, 7), p.Batch.Candidates.Select(c => c.EntryIndex)); Assert.Empty(p.Rejected); Assert.Empty(p.Skipped); }
    [Fact] public void Native_IsLabelCustomFalse_LabelNull() => Assert.Null(Assert.Single(Fixture.Native("{\"text\":\"Note\",\"label\":\"stale\",\"isLabelCustom\":false}").Batch.Candidates).Label);
    [Fact] public void Native_BatchDedupeIsFullRow() => Assert.Equal(DedupeMode.FullRow, Fixture.Read("native-v1.json").Batch.Dedupe);
    [Fact] public void Native_FoldersList_IncludesEmptyFolder() => Assert.Equal(new[] { "Work", "Home", "Empty folder" }, Fixture.Read("native-v1.json").Batch.FolderNames);
    [Fact] public void Native_FolderElementMalformed_IgnoredNotCounted() { var p = Fixture.Native("", "[42,null,{}, {\"name\":42},{\"name\":\"\\ud800\"},{\"name\":\"good\"}]"); Assert.Equal(0, p.EntryCount); Assert.Empty(p.Rejected); Assert.Equal(new[] { "good" }, p.Batch.FolderNames); }
    [Theory] [InlineData("{\"text\":null}", RejectReason.TextMissing)] [InlineData("{\"text\":42}", RejectReason.WrongFieldType)] [InlineData("{\"text\":\"ok\",\"isLabelCustom\":42}", RejectReason.WrongFieldType)] [InlineData("{\"text\":\"ok\",\"folder\":false}", RejectReason.WrongFieldType)]
    public void Native_InvalidFields_Rejected(string entry, RejectReason reason) => Assert.Equal(reason, Assert.Single(Fixture.Native(entry).Rejected).Reason);
}
