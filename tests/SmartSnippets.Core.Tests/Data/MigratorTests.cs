using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using SmartSnippets.Core.Data;

namespace SmartSnippets.Core.Tests.Data;

public class MigratorTests
{
    private static Migrator V1Only(TempDatabase db) => new(db.Factory, Migrations.All.Take(1).ToList());

    [Fact]
    public void Bootstrap_IsIdempotent()
    {
        using var db = new TempDatabase();
        var migrator = new Migrator(db.Factory);

        Assert.Equal(2, migrator.MigrateToLatest());
        Assert.Equal(2, migrator.MigrateToLatest());

        Assert.Equal(1L, db.Scalar("SELECT COUNT(*) FROM schema_version"));
        Assert.Equal(2L, db.Scalar("SELECT version FROM schema_version"));
        Assert.Equal(1L, db.Scalar("SELECT COUNT(*) FROM sqlite_master WHERE name = 'probe'"));
    }

    [Fact]
    public void EmptyDb_MigratesTo2()
    {
        using var db = new TempDatabase();

        Assert.Equal(2, new Migrator(db.Factory).MigrateToLatest());

        Assert.Equal(2L, db.Scalar("SELECT version FROM schema_version"));
        foreach (var table in new[] { "probe", "folders", "snippets", "settings" })
        {
            Assert.Equal(1L, db.Scalar($"SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = '{table}'"));
        }

        Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(db.Factory.DatabasePath)!, "*.bak*"));
    }

    [Fact]
    public void V1Db_UpgradesTo2_PreservesProbeRows_WritesBackup()
    {
        using var db = new TempDatabase();
        V1Only(db).MigrateToLatest();
        db.Exec("INSERT INTO probe (text) VALUES ('keep me')");

        Assert.Equal(2, new Migrator(db.Factory).MigrateToLatest());

        Assert.Equal("keep me", (string?)db.Scalar("SELECT text FROM probe"));
        Assert.Equal(1L, db.Scalar("SELECT COUNT(*) FROM sqlite_master WHERE name = 'snippets'"));

        var backup = db.Factory.DatabasePath + ".v1.bak";
        Assert.True(File.Exists(backup));
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = backup,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false,
        }.ToString());
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT (SELECT version FROM schema_version), (SELECT text FROM probe), " +
            "(SELECT COUNT(*) FROM sqlite_master WHERE name = 'snippets')";
        using var reader = command.ExecuteReader();
        Assert.True(reader.Read());
        Assert.Equal(1L, reader.GetInt64(0));
        Assert.Equal("keep me", reader.GetString(1));
        Assert.Equal(0L, reader.GetInt64(2));
    }

    [Fact]
    public void V1Db_ExistingBackup_GetsTimestampSuffix()
    {
        using var db = new TempDatabase();
        V1Only(db).MigrateToLatest();
        var existing = db.Factory.DatabasePath + ".v1.bak";
        File.WriteAllText(existing, "old");

        new Migrator(db.Factory).MigrateToLatest();

        Assert.Equal("old", File.ReadAllText(existing));
        Assert.Equal(2, Directory.GetFiles(Path.GetDirectoryName(existing)!, "*.bak*").Length);
    }

    [Fact]
    public void NewerDb_ThrowsAndLeavesFileUntouched()
    {
        using var db = new TempDatabase();
        new Migrator(db.Factory).MigrateToLatest();
        db.Exec("UPDATE schema_version SET version = 99");
        db.Exec("PRAGMA wal_checkpoint(TRUNCATE)");
        SqliteConnection.ClearAllPools();
        var before = SHA256.HashData(File.ReadAllBytes(db.Factory.DatabasePath));

        var ex = Assert.Throws<MigrationException>(() => new Migrator(db.Factory).MigrateToLatest());

        Assert.Equal("The database was created by a newer version of Smart Snippets.", ex.Message);
        SqliteConnection.ClearAllPools();
        Assert.Equal(before, SHA256.HashData(File.ReadAllBytes(db.Factory.DatabasePath)));
        Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(db.Factory.DatabasePath)!, "*.bak*"));
    }

    [Fact]
    public void FailedMigration_RollsBack()
    {
        using var db = new TempDatabase();
        V1Only(db).MigrateToLatest();

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
