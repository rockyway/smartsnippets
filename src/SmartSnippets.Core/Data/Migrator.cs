using System.Globalization;
using Microsoft.Data.Sqlite;
using SmartSnippets.Core.Logging;

namespace SmartSnippets.Core.Data;

public sealed record Migration(int Version, string Sql);

public sealed class Migrator
{
    private readonly SqliteConnectionFactory _factory;
    private readonly IReadOnlyList<Migration> _migrations;

    public Migrator(SqliteConnectionFactory factory)
        : this(factory, Migrations.All)
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

        var highest = _migrations.Count == 0 ? 0 : _migrations[^1].Version;
        if (start > highest)
        {
            AppLog.Error("migration_failed", null, $"reason=newer stored={start} known={highest}");
            throw new MigrationException("The database was created by a newer version of Smart Snippets.");
        }

        var pending = _migrations.Where(m => m.Version > start).ToList();
        if (start > 0 && pending.Count > 0)
        {
            Backup(connection, start);
        }

        foreach (var migration in pending)
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

    private void Backup(SqliteConnection connection, int from)
    {
        try
        {
            var target = $"{_factory.DatabasePath}.v{from}.bak";
            if (File.Exists(target))
            {
                target += "." + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
            }

            using var command = connection.CreateCommand();
            command.CommandText = "VACUUM INTO $path;";
            command.Parameters.AddWithValue("$path", target);
            command.ExecuteNonQuery();
            AppLog.Info("migration_backup", $"path={Path.GetFileName(target)}");
        }
        catch (Exception ex)
        {
            AppLog.Error("migration_failed", ex, "reason=backup");
            throw new MigrationException("Could not back up the database before upgrading: " + ex.Message, ex);
        }
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
