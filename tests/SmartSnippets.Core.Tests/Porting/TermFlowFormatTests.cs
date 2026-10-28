using SmartSnippets.Core.Porting;

namespace SmartSnippets.Core.Tests.Porting;

public class TermFlowFormatTests
{
    [Fact] public void TermFlow_Fixture_PerEntryOutcomes()
    {
        var p = Fixture.Read("termflow-v1.json");
        Assert.Equal(10, p.EntryCount); Assert.Empty(p.Skipped);
        Assert.Equal(new[] { 0, 1, 2, 3, 4, 5, 9 }, p.Batch.Candidates.Select(c => c.EntryIndex));
        Assert.Equal(new[] { new RejectedEntry(6, RejectReason.WrongFieldType), new RejectedEntry(7, RejectReason.TextBlank), new RejectedEntry(8, RejectReason.NotAnObject) }, p.Rejected);
    }
    [Fact] public void TermFlow_Fixture_StatsKept() => Assert.Equal(new ImportStats(DateTimeOffset.FromUnixTimeMilliseconds(1700000000000), DateTimeOffset.FromUnixTimeMilliseconds(1700000500000), DateTimeOffset.FromUnixTimeMilliseconds(1700000900000), 7), Fixture.Read("termflow-v1.json").Batch.Candidates[0].Stats);
    [Fact] public void TermFlow_Fixture_StatsDefaulted() => Assert.Equal(new ImportStats(Fixture.Now, Fixture.Now, null, 0), Fixture.Read("termflow-v1.json").Batch.Candidates.Single(c => c.EntryIndex == 5).Stats);
    [Fact] public void TermFlow_IdIgnored_NotRequired() { Assert.Single(Fixture.TermFlow("{\"text\":\"ok\"}").Batch.Candidates); Assert.Single(Fixture.TermFlow("{\"id\":{},\"text\":\"ok\"}").Batch.Candidates); }
    [Fact] public void TermFlow_TextVerbatim_NoTrim_LineEndingsKept() => Assert.Equal("  a\rb\nc\r\nd\t ", Assert.Single(Fixture.TermFlow("{\"text\":\"  a\\rb\\nc\\r\\nd\\t \"}").Batch.Candidates).Text);
    [Fact] public void TermFlow_LabelWrongType_Rejected() => Assert.Equal(RejectReason.WrongFieldType, Assert.Single(Fixture.TermFlow("{\"text\":\"ok\",\"label\":3}").Rejected).Reason);
    [Fact] public void TermFlow_TagsElementNotString_Rejected() => Assert.Equal(RejectReason.WrongFieldType, Assert.Single(Fixture.TermFlow("{\"text\":\"ok\",\"tags\":[\"x\",null]}").Rejected).Reason);
    [Fact] public void TermFlow_ExtraField_Tolerated() => Assert.Single(Fixture.TermFlow("{\"text\":\"ok\",\"unknown\":{\"x\":[]}}").Batch.Candidates);
    [Fact] public void TermFlow_BatchDedupeIsTextOnly() => Assert.Equal(DedupeMode.TextOnly, Fixture.TermFlow("{\"text\":\"ok\"}").Batch.Dedupe);
}
