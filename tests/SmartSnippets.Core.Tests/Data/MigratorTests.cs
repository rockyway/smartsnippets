using SmartSnippets.Core.Data;

namespace SmartSnippets.Core.Tests.Data;

public class MigratorTests
{
    [Fact]
    public void Bootstrap_IsIdempotent()
    {
        using var db = new TempDatabase();
        var migrator = new Migrator(db.Factory);

        Assert.Equal(1, migrator.MigrateToLatest());
        Assert.Equal(1, migrator.MigrateToLatest());

        Assert.Equal(1L, db.Scalar("SELECT COUNT(*) FROM schema_version"));
        Assert.Equal(1L, db.Scalar("SELECT version FROM schema_version"));
        Assert.Equal(1L, db.Scalar("SELECT COUNT(*) FROM sqlite_master WHERE name = 'probe'"));
    }

    [Fact]
    public void FailedMigration_RollsBack()
    {
        using var db = new TempDatabase();
        new Migrator(db.Factory).MigrateToLatest();

        var failing = new Migrator(db.Factory,
        [
            new Migration(1, "CREATE TABLE IF NOT EXISTS probe (id INTEGER PRIMARY KEY, text TEXT NOT NULL);"),
            new Migration(2, "CREATE TABLE partial (id INTEGER); INSERT INTO missing_table VALUES (1);"),
        ]);

        Assert.ThrowsAny<Exception>(() => failing.MigrateToLatest());

        Assert.Equal(1L, db.Scalar("SELECT version FROM schema_version"));
        Assert.Equal(0L, db.Scalar("SELECT COUNT(*) FROM sqlite_master WHERE name = 'partial'"));
    }

    [Fact]
    public void PendingMigrations_AppliedInOrder()
    {
        using var db = new TempDatabase();
        var migrator = new Migrator(db.Factory,
        [
            new Migration(2, "INSERT INTO t VALUES (2);"),
            new Migration(1, "CREATE TABLE t (n INTEGER);"),
        ]);

        Assert.Equal(2, migrator.MigrateToLatest());
        Assert.Equal(2L, db.Scalar("SELECT n FROM t"));
    }
}
