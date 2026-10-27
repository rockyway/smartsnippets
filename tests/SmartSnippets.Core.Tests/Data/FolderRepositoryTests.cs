using SmartSnippets.Core.Data;

namespace SmartSnippets.Core.Tests.Data;

public class FolderRepositoryTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset T1 = T0.AddHours(1);

    private static TempDatabase Migrated()
    {
        var db = new TempDatabase();
        new Migrator(db.Factory).MigrateToLatest();
        return db;
    }

    [Fact]
    public void Delete_MovesSnippetsToUnfiled_BumpsTheirUpdatedAt()
    {
        using var db = Migrated();
        var folders = new FolderRepository(db.Factory);
        var snippets = new SnippetRepository(db.Factory);
        long inside1, inside2, other;
        long folderId;
        using (var c = db.Factory.Open())
        using (var tx = c.BeginTransaction())
        {
            folderId = folders.Insert(c, tx, "Work", "WORK", T0).Id;
            var keep = folders.Insert(c, tx, "Home", "HOME", T0).Id;
            inside1 = snippets.Insert(c, tx, "a", "a", false, folderId, [], T0).Id;
            inside2 = snippets.Insert(c, tx, "b", "b", false, folderId, [], T0).Id;
            other = snippets.Insert(c, tx, "c", "c", false, keep, [], T0).Id;
            tx.Commit();
        }

        int moved;
        using (var c = db.Factory.Open())
        using (var tx = c.BeginTransaction())
        {
            moved = folders.Delete(c, tx, folderId, T1);
            Assert.Equal(-1, folders.Delete(c, tx, folderId, T1));
            tx.Commit();
        }

        Assert.Equal(2, moved);
        Assert.Equal("Home", Assert.Single(folders.LoadAll()).Name);
        foreach (var id in new[] { inside1, inside2 })
        {
            var s = snippets.Get(id)!;
            Assert.Null(s.FolderId);
            Assert.Equal(T1, s.UpdatedAt);
            Assert.Equal(T0, s.CreatedAt);
        }

        var untouched = snippets.Get(other)!;
        Assert.NotNull(untouched.FolderId);
        Assert.Equal(T0, untouched.UpdatedAt);
    }

    [Fact]
    public void Rename_DoesNotTouchSnippets()
    {
        using var db = Migrated();
        var folders = new FolderRepository(db.Factory);
        var snippets = new SnippetRepository(db.Factory);
        long folderId, snippetId;
        using (var c = db.Factory.Open())
        using (var tx = c.BeginTransaction())
        {
            folderId = folders.Insert(c, tx, "Work", "WORK", T0).Id;
            snippetId = snippets.Insert(c, tx, "a", "a", false, folderId, [], T0).Id;
            tx.Commit();
        }

        using (var c = db.Factory.Open())
        using (var tx = c.BeginTransaction())
        {
            Assert.True(folders.Rename(c, tx, folderId, "Jobs", "JOBS", T1));
            Assert.False(folders.Rename(c, tx, folderId + 50, "X", "X", T1));
            tx.Commit();
        }

        var folder = Assert.Single(folders.LoadAll());
        Assert.Equal("Jobs", folder.Name);
        Assert.Equal(T0, folder.CreatedAt);
        Assert.Equal(T1, folder.UpdatedAt);
        var s = snippets.Get(snippetId)!;
        Assert.Equal(folderId, s.FolderId);
        Assert.Equal(T0, s.UpdatedAt);
    }

    [Fact]
    public void NameKeyExists_HonoursExceptId()
    {
        using var db = Migrated();
        var folders = new FolderRepository(db.Factory);
        using var c = db.Factory.Open();
        using var tx = c.BeginTransaction();
        var id = folders.Insert(c, tx, "Work", "WORK", T0).Id;

        Assert.True(folders.NameKeyExists(c, tx, "WORK", null));
        Assert.False(folders.NameKeyExists(c, tx, "WORK", id));
        Assert.False(folders.NameKeyExists(c, tx, "OTHER", null));
    }
}
