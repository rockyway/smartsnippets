using Microsoft.Data.Sqlite;
using SmartSnippets.Core.Domain;

namespace SmartSnippets.Core.Data;

public sealed class SnippetRepository
{
    private const string SelectColumns =
        "SELECT id, text, label, label_is_custom, folder_id, tags, created_at, updated_at, last_used_at, usage_count FROM snippets";

    private readonly SqliteConnectionFactory _factory;

    public SnippetRepository(SqliteConnectionFactory factory)
    {
        _factory = factory;
    }

    public IReadOnlyList<Snippet> LoadAll(SqliteConnection? connection = null)
    {
        if (connection is null)
        {
            using var own = _factory.Open();
            return LoadAll(own);
        }

        using var command = connection.CreateCommand();
        command.CommandText = SelectColumns + " ORDER BY id;";
        using var reader = command.ExecuteReader();
        var result = new List<Snippet>();
        while (reader.Read())
        {
            result.Add(Read(reader));
        }

        return result;
    }

    public Snippet? Get(long id, SqliteConnection? connection = null)
    {
        if (connection is null)
        {
            using var own = _factory.Open();
            return Get(id, own);
        }

        using var command = connection.CreateCommand();
        command.CommandText = SelectColumns + " WHERE id = $id;";
        command.Parameters.AddWithValue("$id", id);
        using var reader = command.ExecuteReader();
        return reader.Read() ? Read(reader) : null;
    }

    public Snippet Insert(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string text,
        string label,
        bool isLabelCustom,
        long? folderId,
        IReadOnlyList<string> tags,
        DateTimeOffset now) => InsertImported(connection, transaction, text, label, isLabelCustom, folderId, tags, now, now, null, 0);

    public Snippet InsertImported(
        SqliteConnection connection, SqliteTransaction transaction,
        string text, string label, bool isLabelCustom, long? folderId, IReadOnlyList<string> tags,
        DateTimeOffset createdAt, DateTimeOffset updatedAt, DateTimeOffset? lastUsedAt, int usageCount)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            "INSERT INTO snippets (text, label, label_is_custom, folder_id, tags, created_at, updated_at, last_used_at, usage_count) " +
            "VALUES ($text, $label, $custom, $folder, $tags, $created, $updated, $used, $usage); SELECT last_insert_rowid();";
        command.Parameters.AddWithValue("$text", text);
        command.Parameters.AddWithValue("$label", label);
        command.Parameters.AddWithValue("$custom", isLabelCustom ? 1 : 0);
        command.Parameters.AddWithValue("$folder", folderId is null ? DBNull.Value : folderId.Value);
        command.Parameters.AddWithValue("$tags", TagList.ToStorage(tags));
        command.Parameters.AddWithValue("$created", DbTime.ToDb(createdAt));
        command.Parameters.AddWithValue("$updated", DbTime.ToDb(updatedAt));
        command.Parameters.AddWithValue("$used", lastUsedAt is { } used ? DbTime.ToDb(used) : DBNull.Value);
        command.Parameters.AddWithValue("$usage", usageCount);
        var id = (long)command.ExecuteScalar()!;
        return new Snippet(id, text, label, isLabelCustom, folderId, TagList.FromStorage(TagList.ToStorage(tags)),
            DbTime.FromDb(DbTime.ToDb(createdAt)), DbTime.FromDb(DbTime.ToDb(updatedAt)),
            lastUsedAt is { } value ? DbTime.FromDb(DbTime.ToDb(value)) : null, usageCount);
    }

    /// <summary>Sets updated_at; never touches created_at, usage_count or last_used_at. False when not found.</summary>
    public bool Update(
        SqliteConnection connection,
        SqliteTransaction transaction,
        long id,
        string text,
        string label,
        bool isLabelCustom,
        long? folderId,
        IReadOnlyList<string> tags,
        DateTimeOffset now)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            "UPDATE snippets SET text = $text, label = $label, label_is_custom = $custom, folder_id = $folder, " +
            "tags = $tags, updated_at = $now WHERE id = $id;";
        command.Parameters.AddWithValue("$text", text);
        command.Parameters.AddWithValue("$label", label);
        command.Parameters.AddWithValue("$custom", isLabelCustom ? 1 : 0);
        command.Parameters.AddWithValue("$folder", folderId is null ? DBNull.Value : folderId.Value);
        command.Parameters.AddWithValue("$tags", TagList.ToStorage(tags));
        command.Parameters.AddWithValue("$now", DbTime.ToDb(now));
        command.Parameters.AddWithValue("$id", id);
        return command.ExecuteNonQuery() > 0;
    }

    public bool Delete(SqliteConnection connection, SqliteTransaction transaction, long id)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "DELETE FROM snippets WHERE id = $id;";
        command.Parameters.AddWithValue("$id", id);
        return command.ExecuteNonQuery() > 0;
    }

    /// <summary>Increments usage_count and sets last_used_at; never changes updated_at.</summary>
    public bool RecordUsage(SqliteConnection connection, SqliteTransaction transaction, long id, DateTimeOffset now)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "UPDATE snippets SET usage_count = MIN(usage_count + 1, 2147483647), last_used_at = $now WHERE id = $id;";
        command.Parameters.AddWithValue("$now", DbTime.ToDb(now));
        command.Parameters.AddWithValue("$id", id);
        return command.ExecuteNonQuery() > 0;
    }

    private static Snippet Read(SqliteDataReader reader) => new(
        reader.GetInt64(0),
        reader.GetString(1),
        reader.GetString(2),
        reader.GetInt64(3) != 0,
        reader.IsDBNull(4) ? null : reader.GetInt64(4),
        TagList.FromStorage(reader.GetString(5)),
        DbTime.FromDb(reader.GetInt64(6)),
        DbTime.FromDb(reader.GetInt64(7)),
        reader.IsDBNull(8) ? null : DbTime.FromDb(reader.GetInt64(8)),
        (int)Math.Clamp(reader.GetInt64(9), 0, int.MaxValue));
}
