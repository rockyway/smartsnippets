using Microsoft.Data.Sqlite;
using SmartSnippets.Core.Domain;

namespace SmartSnippets.Core.Data;

public sealed class FolderRepository
{
    private readonly SqliteConnectionFactory _factory;

    public FolderRepository(SqliteConnectionFactory factory)
    {
        _factory = factory;
    }

    public IReadOnlyList<Folder> LoadAll(SqliteConnection? connection = null)
    {
        if (connection is null)
        {
            using var own = _factory.Open();
            return LoadAll(own);
        }

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, name, created_at, updated_at FROM folders ORDER BY id;";
        using var reader = command.ExecuteReader();
        var result = new List<Folder>();
        while (reader.Read())
        {
            result.Add(new Folder(
                reader.GetInt64(0),
                reader.GetString(1),
                DbTime.FromDb(reader.GetInt64(2)),
                DbTime.FromDb(reader.GetInt64(3))));
        }

        return result;
    }

    public Folder Insert(SqliteConnection connection, SqliteTransaction transaction, string name, string nameKey, DateTimeOffset now)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            "INSERT INTO folders (name, name_key, created_at, updated_at) VALUES ($name, $key, $now, $now); SELECT last_insert_rowid();";
        command.Parameters.AddWithValue("$name", name);
        command.Parameters.AddWithValue("$key", nameKey);
        command.Parameters.AddWithValue("$now", DbTime.ToDb(now));
        var id = (long)command.ExecuteScalar()!;
        var stored = DbTime.FromDb(DbTime.ToDb(now));
        return new Folder(id, name, stored, stored);
    }

    /// <summary>Returns false when no folder has the id. Never touches snippets.</summary>
    public bool Rename(SqliteConnection connection, SqliteTransaction transaction, long id, string name, string nameKey, DateTimeOffset now)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "UPDATE folders SET name = $name, name_key = $key, updated_at = $now WHERE id = $id;";
        command.Parameters.AddWithValue("$name", name);
        command.Parameters.AddWithValue("$key", nameKey);
        command.Parameters.AddWithValue("$now", DbTime.ToDb(now));
        command.Parameters.AddWithValue("$id", id);
        return command.ExecuteNonQuery() > 0;
    }

    /// <summary>
    /// Moves the folder's snippets to Unfiled (bumping their updated_at) and deletes the folder.
    /// Returns the number of snippets moved, or -1 when the folder does not exist.
    /// </summary>
    public int Delete(SqliteConnection connection, SqliteTransaction transaction, long id, DateTimeOffset now)
    {
        int moved;
        using (var move = connection.CreateCommand())
        {
            move.Transaction = transaction;
            move.CommandText = "UPDATE snippets SET folder_id = NULL, updated_at = $now WHERE folder_id = $id;";
            move.Parameters.AddWithValue("$now", DbTime.ToDb(now));
            move.Parameters.AddWithValue("$id", id);
            moved = move.ExecuteNonQuery();
        }

        using var delete = connection.CreateCommand();
        delete.Transaction = transaction;
        delete.CommandText = "DELETE FROM folders WHERE id = $id;";
        delete.Parameters.AddWithValue("$id", id);
        return delete.ExecuteNonQuery() > 0 ? moved : -1;
    }

    public bool NameKeyExists(SqliteConnection connection, SqliteTransaction? transaction, string key, long? exceptId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT 1 FROM folders WHERE name_key = $key AND ($except IS NULL OR id <> $except) LIMIT 1;";
        command.Parameters.AddWithValue("$key", key);
        command.Parameters.AddWithValue("$except", exceptId is null ? DBNull.Value : exceptId.Value);
        return command.ExecuteScalar() is not null;
    }
}
