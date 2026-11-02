using System.Text;
using Microsoft.Data.Sqlite;

namespace SmartSnippets.Core.Data;

/// <summary>Multi-row INSERT in fixed-size prepared chunks; rows bind in input order.</summary>
internal static class BulkInsert
{
    private const int ParametersPerStatement = 64;

    /// <summary>The id SQLite would assign next to an INTEGER PRIMARY KEY table without AUTOINCREMENT.</summary>
    public static long NextId(SqliteConnection connection, SqliteTransaction transaction, string table)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT COALESCE(MAX(id), 0) + 1 FROM {table};";
        return (long)command.ExecuteScalar()!;
    }

    public static void Run(SqliteConnection connection, SqliteTransaction transaction, string insertPrefix, string rowFormat,
        string[] rowParameters, int count, Action<SqliteCommand>? addShared, Action<SqliteParameter[], int> bind)
    {
        // SQLite resolves parameter names linearly, so keep each statement's parameter count small.
        var rowsPerStatement = Math.Max(1, ParametersPerStatement / rowParameters.Length);
        var full = count / rowsPerStatement;
        if (full > 0) Chunk(rowsPerStatement, full, 0);
        if (count % rowsPerStatement is var tail and > 0) Chunk(tail, 1, full * rowsPerStatement);

        void Chunk(int rows, int repeats, int start)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            var sql = new StringBuilder(insertPrefix);
            var parameters = new SqliteParameter[rows][];
            for (var r = 0; r < rows; r++)
            {
                if (r > 0) sql.Append(", ");
                sql.AppendFormat(rowFormat, r);
                parameters[r] = new SqliteParameter[rowParameters.Length];
                for (var p = 0; p < rowParameters.Length; p++)
                    parameters[r][p] = command.Parameters.Add(new SqliteParameter(rowParameters[p] + r, null));
            }

            command.CommandText = sql.Append(';').ToString();
            addShared?.Invoke(command);
            command.Prepare();
            for (var k = 0; k < repeats; k++)
            {
                for (var r = 0; r < rows; r++) bind(parameters[r], start + k * rows + r);
                command.ExecuteNonQuery();
            }
        }
    }
}
