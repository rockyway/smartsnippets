using SmartSnippets.Core.Porting;

namespace SmartSnippets.Core.Tests.Porting;

public class RephloFormatTests
{
    private static ParsedImport Read(string entry) => Fixture.Parse("{\"version\":\"1.3\",\"commands\":[" + entry + "]}");
    [Fact] public void Rephlo_Fixture_PerEntryOutcomes()
    {
        var p = Fixture.Read("rephlo-1.3.json"); Assert.Equal(6, p.EntryCount);
        Assert.Equal(new[] { 0, 1, 4, 5 }, p.Batch.Candidates.Select(c => c.EntryIndex));
        Assert.Equal(new[] { new SkippedEntry(2, SkipReason.Archived) }, p.Skipped);
        Assert.Equal(new[] { new RejectedEntry(3, RejectReason.TextMissing) }, p.Rejected);
        Assert.Equal(DedupeMode.TextOnly, p.Batch.Dedupe);
    }
    [Fact] public void Rephlo_FixedContentAppend_NeverAppended() => Assert.Equal("Refactor the code.\r\nKeep tests.\nDone.", Fixture.Read("rephlo-1.3.json").Batch.Candidates[0].Text);
    [Fact] public void Rephlo_Archived_SkippedBeforeTextCheck() { var p = Read("{\"isArchived\":true}"); Assert.Equal(SkipReason.Archived, Assert.Single(p.Skipped).Reason); Assert.Empty(p.Rejected); }
    [Fact] public void Rephlo_StatsNull_Fresh() => Assert.All(Fixture.Read("rephlo-1.3.json").Batch.Candidates, c => { Assert.Null(c.Stats); Assert.Empty(c.Tags); });
    [Theory] [InlineData("{\"isArchived\":42}", RejectReason.WrongFieldType)] [InlineData("{\"instruction\":\"ok\",\"name\":42}", RejectReason.WrongFieldType)] [InlineData("42", RejectReason.NotAnObject)]
    public void Rephlo_InvalidFields_Rejected(string entry, RejectReason reason) => Assert.Equal(reason, Assert.Single(Read(entry).Rejected).Reason);
}
