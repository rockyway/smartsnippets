using SmartSnippets.Core.Data;

namespace SmartSnippets.Core.Tests.Data;

public class SnippetRepositoryTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset T1 = T0.AddHours(1);
    private static readonly DateTimeOffset T2 = T0.AddHours(2);

    private static TempDatabase Migrated()
    {
        var db = new TempDatabase();
        new Migrator(db.Factory).MigrateToLatest();
        return db;
    }

    [Fact]
    public void Insert_RoundTripsUnicodeMultilineExactly()
    {
        using var db = Migrated();
        var repo = new SnippetRepository(db.Factory);
        const string text = "Héllo 👋 世界\r\nline2\rlone-cr\ttab  trailing spaces  \n ";

        using (var c = db.Factory.Open())
        using (var tx = c.BeginTransaction())
        {
            var inserted = repo.Insert(c, tx, text, "lbl", false, null, [], T0);
            Assert.Equal(text, inserted.Text);
            tx.Commit();
        }

        var loaded = Assert.Single(repo.LoadAll());
        Assert.Equal(text, loaded.Text);
        Assert.Equal("lbl", loaded.Label);
        Assert.Equal(T0, loaded.CreatedAt);
        Assert.Equal(T0, loaded.UpdatedAt);
        Assert.Null(loaded.LastUsedAt);
        Assert.Equal(0, loaded.UsageCount);
        Assert.Equal(text, repo.Get(loaded.Id)!.Text);
    }

    [Fact]
    public void TagsRoundTrip()
    {
        using var db = Migrated();
        var repo = new SnippetRepository(db.Factory);
        using var c = db.Factory.Open();
        using var tx = c.BeginTransaction();

        repo.Insert(c, tx, "a", "a", false, null, ["Work", "été", "x y"], T0);
        repo.Insert(c, tx, "b", "b", false, null, [], T0);
        tx.Commit();

        var all = repo.LoadAll();
        Assert.Equal(["Work", "été", "x y"], all[0].Tags);
        Assert.Empty(all[1].Tags);
    }

    [Fact]
    public void Update_KeepsCreatedAndUsage()
    {
        using var db = Migrated();
        var repo = new SnippetRepository(db.Factory);
        long id;
        using (var c = db.Factory.Open())
        using (var tx = c.BeginTransaction())
        {
            id = repo.Insert(c, tx, "old", "old", false, null, ["a"], T0).Id;
            repo.RecordUsage(c, tx, id, T1);
            Assert.True(repo.Update(c, tx, id, "new", "custom", true, null, ["b", "c"], T2));
            Assert.False(repo.Update(c, tx, id + 100, "x", "x", false, null, [], T2));
            tx.Commit();
        }

        var s = repo.Get(id)!;
        Assert.Equal("new", s.Text);
        Assert.Equal("custom", s.Label);
        Assert.True(s.IsLabelCustom);
        Assert.Equal(["b", "c"], s.Tags);
        Assert.Equal(T0, s.CreatedAt);
        Assert.Equal(T2, s.UpdatedAt);
        Assert.Equal(1, s.UsageCount);
        Assert.Equal(T1, s.LastUsedAt);
    }

    [Fact]
    public void RecordUsage_IncrementsAndSetsLastUsed_KeepsUpdatedAt()
    {
        using var db = Migrated();
        var repo = new SnippetRepository(db.Factory);
        using var c = db.Factory.Open();
        using var tx = c.BeginTransaction();
        var id = repo.Insert(c, tx, "t", "t", false, null, [], T0).Id;

        Assert.True(repo.RecordUsage(c, tx, id, T1));
        Assert.True(repo.RecordUsage(c, tx, id, T2));
        Assert.False(repo.RecordUsage(c, tx, id + 1, T2));
        tx.Commit();

        var s = repo.Get(id)!;
        Assert.Equal(2, s.UsageCount);
        Assert.Equal(T2, s.LastUsedAt);
        Assert.Equal(T0, s.UpdatedAt);
    }

    [Fact]
    public void Delete_RemovesRow_AndReportsMissing()
    {
        using var db = Migrated();
        var repo = new SnippetRepository(db.Factory);
        using var c = db.Factory.Open();
        using var tx = c.BeginTransaction();
        var id = repo.Insert(c, tx, "t", "t", false, null, [], T0).Id;

        Assert.True(repo.Delete(c, tx, id));
        Assert.False(repo.Delete(c, tx, id));
        tx.Commit();

        Assert.Empty(repo.LoadAll());
        Assert.Null(repo.Get(id));
    }
}
