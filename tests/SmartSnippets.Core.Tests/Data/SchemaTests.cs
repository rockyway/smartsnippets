using Microsoft.Data.Sqlite;
using SmartSnippets.Core.Data;

namespace SmartSnippets.Core.Tests.Data;

public class SchemaTests
{
    private static TempDatabase Migrated()
    {
        var db = new TempDatabase();
        new Migrator(db.Factory).MigrateToLatest();
        return db;
    }

    [Fact]
    public void ForeignKeys_RejectUnknownFolder()
    {
        using var db = Migrated();

        var ex = Assert.Throws<SqliteException>(() => db.Exec(
            "INSERT INTO snippets (text, label, folder_id, created_at, updated_at) VALUES ('t', 'l', 999, 1, 1)"));

        Assert.Equal(19, ex.SqliteErrorCode);
    }

    [Fact]
    public void FolderNameKey_IsUnique()
    {
        using var db = Migrated();
        db.Exec("INSERT INTO folders (name, name_key, created_at, updated_at) VALUES ('Work', 'WORK', 1, 1)");

        var ex = Assert.Throws<SqliteException>(() => db.Exec(
            "INSERT INTO folders (name, name_key, created_at, updated_at) VALUES ('work', 'WORK', 1, 1)"));

        Assert.Equal(19, ex.SqliteErrorCode);
    }

    [Fact]
    public void DeleteFolderRow_Directly_IsRestrictedWhenSnippetsReference()
    {
        using var db = Migrated();
        db.Exec("INSERT INTO folders (id, name, name_key, created_at, updated_at) VALUES (1, 'Work', 'WORK', 1, 1)");
        db.Exec("INSERT INTO snippets (text, label, folder_id, created_at, updated_at) VALUES ('t', 'l', 1, 1, 1)");

        var ex = Assert.Throws<SqliteException>(() => db.Exec("DELETE FROM folders WHERE id = 1"));

        Assert.Equal(19, ex.SqliteErrorCode);
        Assert.Equal(1L, db.Scalar("SELECT COUNT(*) FROM folders"));
    }
}
