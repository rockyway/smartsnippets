using Microsoft.Data.Sqlite;
using SmartSnippets.Core.Data;

namespace SmartSnippets.Core.Tests.Data;

internal sealed class TempDatabase : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "ss-test-" + Guid.NewGuid().ToString("N"));

    public TempDatabase()
    {
        Factory = new SqliteConnectionFactory(Path.Combine(_directory, "nested", "test.db"));
    }

    public SqliteConnectionFactory Factory { get; }

    public object? Scalar(string sql)
    {
        using var connection = Factory.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return command.ExecuteScalar();
    }

    public void Exec(string sql)
    {
        using var connection = Factory.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
        }
    }
}
