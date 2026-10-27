using Microsoft.Data.Sqlite;

namespace SmartSnippets.Core.Data;

public sealed class SqliteConnectionFactory
{
    private readonly int? _busyTimeoutSeconds;

    /// <param name="busyTimeout">
    /// Test hook: how long a locked database is retried before SQLITE_BUSY surfaces, rounded up to whole seconds
    /// (the driver's command timeout, minimum 1 s). Null keeps the driver default.
    /// </param>
    public SqliteConnectionFactory(string databasePath, TimeSpan? busyTimeout = null)
    {
        DatabasePath = databasePath;
        _busyTimeoutSeconds = busyTimeout is { } timeout ? Math.Max(1, (int)Math.Ceiling(timeout.TotalSeconds)) : null;
    }

    public string DatabasePath { get; }

    public SqliteConnection Open()
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(DatabasePath));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
        };
        if (_busyTimeoutSeconds is { } seconds)
        {
            builder.DefaultTimeout = seconds;
        }

        var connection = new SqliteConnection(builder.ToString());

        try
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON;";
            command.ExecuteNonQuery();
            return connection;
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }
}
