using System.Globalization;
using Microsoft.Data.Sqlite;
using SmartSnippets.Core.Logging;

namespace SmartSnippets.Core.Data;

public sealed record Migration(int Version, string Sql);

public sealed class Migrator
{
    private static readonly IReadOnlyList<Migration> DefaultMigrations =
    [
        new Migration(1, "CREATE TABLE IF NOT EXISTS probe (id INTEGER PRIMARY KEY, text TEXT NOT NULL);"),
    ];

    private readonly SqliteConnectionFactory _factory;
    private readonly IReadOnlyList<Migration> _migrations;

    public Migrator(SqliteConnectionFactory factory)
        : this(factory, DefaultMigrations)
    {
    }

    public Migrator(SqliteConnectionFactory factory, IReadOnlyList<Migration> migrations)
    {
        _factory = factory;
        _migrations = migrations.OrderBy(m => m.Version).ToList();
    }

    public int MigrateToLatest()
    {
        using var connection = _factory.Open();

        using (var create = connection.CreateCommand())
        {
            create.CommandText = "CREATE TABLE IF NOT EXISTS schema_version (version INTEGER NOT NULL);";
            create.ExecuteNonQuery();
        }

        var start = ReadVersion(connection);
        var current = start;

        foreach (var migration in _migrations.Where(m => m.Version > start))
        {
            try
            {
                Apply(connection, migration);
            }
            catch (Exception ex)
            {
                AppLog.Error("migration_failed", ex, $"version={migration.Version}");
                throw;
            }

            current = migration.Version;
        }

        AppLog.Info("migrations_applied", $"from={start} to={current}");
        return current;
    }

    private static int ReadVersion(SqliteConnection connection)
    {
        using var select = connection.CreateCommand();
        select.CommandText = "SELECT version FROM schema_version LIMIT 1;";
        var value = select.ExecuteScalar();
        if (value is not null and not DBNull)
        {
            return Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }

        using var insert = connection.CreateCommand();
        insert.CommandText = "INSERT INTO schema_version (version) VALUES (0);";
        insert.ExecuteNonQuery();
        return 0;
    }

    private static void Apply(SqliteConnection connection, Migration migration)
    {
        using var transaction = connection.BeginTransaction();
        try
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = migration.Sql;
                command.ExecuteNonQuery();
            }

            using (var update = connection.CreateCommand())
            {
                update.Transaction = transaction;
                update.CommandText = "UPDATE schema_version SET version = $version;";
                update.Parameters.AddWithValue("$version", migration.Version);
                update.ExecuteNonQuery();
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
