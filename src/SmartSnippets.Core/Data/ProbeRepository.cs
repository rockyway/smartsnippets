using Microsoft.Data.Sqlite;
using SmartSnippets.Core.Logging;

namespace SmartSnippets.Core.Data;

public sealed record ProbeResult(bool Success, string Message);

public sealed class ProbeRepository
{
    private readonly SqliteConnectionFactory _factory;

    public ProbeRepository(SqliteConnectionFactory factory)
    {
        _factory = factory;
    }

    public ProbeResult RunCrud(string text)
    {
        try
        {
            using var connection = _factory.Open();
            using var transaction = connection.BeginTransaction();

            var id = Insert(connection, transaction, text);
            if (!string.Equals(Read(connection, transaction, id), text, StringComparison.Ordinal))
            {
                return Fail("read-back after insert differs");
            }

            var updated = text + "✓";
            Execute(connection, transaction, "UPDATE probe SET text = $text WHERE id = $id;", id, updated);
            if (!string.Equals(Read(connection, transaction, id), updated, StringComparison.Ordinal))
            {
                return Fail("read-back after update differs");
            }

            Execute(connection, transaction, "DELETE FROM probe WHERE id = $id;", id, null);
            if (Read(connection, transaction, id) is not null)
            {
                return Fail("row still present after delete");
            }

            transaction.Commit();
            AppLog.Info("sqlite_probe", "ok");
            return new ProbeResult(true, "CRUD OK");
        }
        catch (Exception ex)
        {
            AppLog.Error("sqlite_probe", ex);
            return new ProbeResult(false, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static ProbeResult Fail(string message)
    {
        AppLog.Error("sqlite_probe", null, message);
        return new ProbeResult(false, message);
    }

    private static long Insert(SqliteConnection connection, SqliteTransaction transaction, string text)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT INTO probe (text) VALUES ($text); SELECT last_insert_rowid();";
        command.Parameters.AddWithValue("$text", text);
        return (long)command.ExecuteScalar()!;
    }

    private static string? Read(SqliteConnection connection, SqliteTransaction transaction, long id)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT text FROM probe WHERE id = $id;";
        command.Parameters.AddWithValue("$id", id);
        return command.ExecuteScalar() as string;
    }

    private static void Execute(SqliteConnection connection, SqliteTransaction transaction, string sql, long id, string? text)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.Parameters.AddWithValue("$id", id);
        if (text is not null)
        {
            command.Parameters.AddWithValue("$text", text);
        }

        command.ExecuteNonQuery();
    }
}
