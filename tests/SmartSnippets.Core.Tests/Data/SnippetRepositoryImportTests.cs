using SmartSnippets.Core.Domain;
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
    // Bulk inserts assign ids themselves; they must match what row-by-row inserts would get, across chunk boundaries,
    // after a deleted top id (SQLite reuses max(id)+1), and the rows must round-trip exactly.
    [Theory] [InlineData(1)] [InlineData(6)] [InlineData(7)] [InlineData(50)]
    public void BulkInserts_MatchRowByRowIdsAndRows(int count)
    {
        static (List<long> Folders, List<long> Snippets, List<string> Rows, List<(long, string, string)> FolderRows) Run(int count, bool bulk)
        {
            using var e = new PortingStoreEnv(); var folders = new FolderRepository(e.Db.Factory); var snippets = new SnippetRepository(e.Db.Factory);
            var a = e.Store.CreateSnippet(new("keep", "keep", true, null, "")).Value!; e.Store.CreateSnippet(new("drop", "drop", true, null, ""));
            var f = e.Store.CreateFolder("Keep").Value!; e.Store.CreateFolder("Drop");
            e.Db.Exec($"DELETE FROM snippets WHERE id > {a.Id}; DELETE FROM folders WHERE id > {f.Id};");
            var names = Enumerable.Range(0, count).Select(i => ($"F{i}", $"f{i}")).ToArray();
            using var c = e.Db.Factory.Open(); using var tx = c.BeginTransaction();
            var newFolders = bulk ? folders.InsertMany(c, tx, names, Fixture.Now) : [.. names.Select(n => folders.Insert(c, tx, n.Item1, n.Item2, Fixture.Now))];
            var rows = Enumerable.Range(0, count).Select(i => new ImportedSnippetRow($"t{i}", $"l{i}", i % 2 == 0, i % 3 == 0 ? null : newFolders[i].Id,
                i % 2 == 0 ? ["x", "y"] : [], Fixture.Now.AddDays(-i), Fixture.Now, i % 4 == 0 ? Fixture.Now.AddHours(-i) : null, i)).ToArray();
            var newSnippets = bulk ? snippets.InsertImportedMany(c, tx, rows) : [.. rows.Select(r => snippets.InsertImported(c, tx, r.Text, r.Label, r.IsLabelCustom, r.FolderId, r.Tags, r.CreatedAt, r.UpdatedAt, r.LastUsedAt, r.UsageCount))];
            tx.Commit();
            var loaded = snippets.LoadAll();
            static string Key(Snippet s) => $"{s.Id}|{s.Text}|{s.Label}|{s.IsLabelCustom}|{s.FolderId}|{string.Join(',', s.Tags)}|{s.CreatedAt:O}|{s.UpdatedAt:O}|{s.LastUsedAt:O}|{s.UsageCount}";
            Assert.Equal(newSnippets.Select(Key), loaded.Where(s => s.Id > a.Id).Select(Key));
            Assert.Equal(newFolders, folders.LoadAll().Where(x => x.Id > f.Id));
            return ([.. newFolders.Select(x => x.Id)], [.. newSnippets.Select(s => s.Id)],
                [.. loaded.Select(Key)], [.. folders.LoadAll().Select(x => (x.Id, x.Name, x.Name.ToLowerInvariant()))]);
        }
        var bulk = Run(count, true); var single = Run(count, false);
        Assert.Equal(single.Folders, bulk.Folders); Assert.Equal(single.Snippets, bulk.Snippets);
        Assert.Equal(single.Rows, bulk.Rows); Assert.Equal(single.FolderRows, bulk.FolderRows);
    }
}
