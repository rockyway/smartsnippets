using SmartSnippets.Core.Data;

namespace SmartSnippets.Core.Tests.Data;

public class SqliteProbeTests
{
    private const string Sample = "Héllo – naïve 👋 世界\r\n\tline 2  (trailing spaces)  \r\nline 3";

    [Fact]
    public void Crud_RoundTripsUnicodeMultiline()
    {
        using var db = new TempDatabase();
        new Migrator(db.Factory).MigrateToLatest();

        var result = new ProbeRepository(db.Factory).RunCrud(Sample);

        Assert.True(result.Success, result.Message);
        Assert.DoesNotContain("Héllo", result.Message, StringComparison.Ordinal);
        Assert.Equal(0L, db.Scalar("SELECT COUNT(*) FROM probe"));
    }

    [Fact]
    public void DirectInsert_ReadsBackOrdinalEqual()
    {
        using var db = new TempDatabase();
        new Migrator(db.Factory).MigrateToLatest();

        using (var connection = db.Factory.Open())
        {
            using var insert = connection.CreateCommand();
            insert.CommandText = "INSERT INTO probe (text) VALUES ($t);";
            insert.Parameters.AddWithValue("$t", Sample);
            insert.ExecuteNonQuery();
        }

        Assert.Equal(Sample, (string?)db.Scalar("SELECT text FROM probe"));
    }

    [Fact]
    public void RunCrud_WithoutSchema_ReturnsFailure()
    {
        using var db = new TempDatabase();

        var result = new ProbeRepository(db.Factory).RunCrud(Sample);

        Assert.False(result.Success);
    }

    [Fact]
    public void Open_SetsWalAndForeignKeys()
    {
        using var db = new TempDatabase();

        Assert.Equal("wal", (string?)db.Scalar("PRAGMA journal_mode"));
        Assert.Equal(1L, db.Scalar("PRAGMA foreign_keys"));
    }
}
