using SmartSnippets.Core.Data;
using SmartSnippets.Core.Tests.Porting;

namespace SmartSnippets.Core.Tests.Data;

public class SnippetRepositoryImportTests
{
    [Fact] public void InsertImported_PersistsAllStats_Reload()
    {
        using var e = new PortingStoreEnv(); var repo = new SnippetRepository(e.Db.Factory); long id;
        using (var c = e.Db.Factory.Open()) { using var tx = c.BeginTransaction(); id = repo.InsertImported(c, tx, "body", "label", true, null, ["a", "b"], Fixture.Now.AddDays(-3), Fixture.Now.AddDays(-2), Fixture.Now.AddDays(-1), 1000000003).Id; tx.Commit(); }
        var s = repo.Get(id)!; Assert.Equal(Fixture.Now.AddDays(-3), s.CreatedAt); Assert.Equal(Fixture.Now.AddDays(-2), s.UpdatedAt); Assert.Equal(Fixture.Now.AddDays(-1), s.LastUsedAt); Assert.Equal(1000000003, s.UsageCount); Assert.Equal(new[] { "a", "b" }, s.Tags);
    }
    [Fact] public void Insert_StillFreshStats() { using var e = new PortingStoreEnv(); var s = e.Store.CreateSnippet(new("body", "label", true, null, "")).Value!; var reloaded = new SnippetRepository(e.Db.Factory).Get(s.Id)!; Assert.Equal(Fixture.Now, reloaded.CreatedAt); Assert.Equal(Fixture.Now, reloaded.UpdatedAt); Assert.Null(reloaded.LastUsedAt); Assert.Equal(0, reloaded.UsageCount); }
    [Fact] public void RecordUsage_AtIntMax_Saturates()
    {
        using var e = new PortingStoreEnv(); var id = e.Store.CreateSnippet(new("body", "label", true, null, "")).Value!.Id;
        e.Db.Exec($"UPDATE snippets SET usage_count=2147483647 WHERE id={id};");
        var repo = new SnippetRepository(e.Db.Factory);
        using (var c = e.Db.Factory.Open()) { using var tx = c.BeginTransaction(); Assert.True(repo.RecordUsage(c, tx, id, Fixture.Now)); tx.Commit(); }
        Assert.Equal(2147483647L, e.Db.Scalar("SELECT usage_count FROM snippets;")); Assert.Equal(int.MaxValue, repo.Get(id)!.UsageCount);
    }
    [Theory] [InlineData(3000000000L, int.MaxValue)] [InlineData(-1L, 0)]
    public void Read_UsageAboveIntMax_ClampedToIntMax(long raw, int expected) { using var e = new PortingStoreEnv(); var id = e.Store.CreateSnippet(new("body", "label", true, null, "")).Value!.Id; e.Db.Exec($"UPDATE snippets SET usage_count={raw} WHERE id={id};"); Assert.Equal(expected, new SnippetRepository(e.Db.Factory).Get(id)!.UsageCount); }
}
