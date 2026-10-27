namespace SmartSnippets.Core.Data;

public static class Migrations
{
    public static IReadOnlyList<Migration> All { get; } =
    [
        new Migration(1, "CREATE TABLE IF NOT EXISTS probe (id INTEGER PRIMARY KEY, text TEXT NOT NULL);"),
        new Migration(2, """
            CREATE TABLE folders (
              id          INTEGER PRIMARY KEY,
              name        TEXT    NOT NULL,
              name_key    TEXT    NOT NULL UNIQUE,
              created_at  INTEGER NOT NULL,
              updated_at  INTEGER NOT NULL
            );
            CREATE TABLE snippets (
              id              INTEGER PRIMARY KEY,
              text            TEXT    NOT NULL,
              label           TEXT    NOT NULL,
              label_is_custom INTEGER NOT NULL DEFAULT 0,
              folder_id       INTEGER NULL REFERENCES folders(id) ON DELETE RESTRICT,
              tags            TEXT    NOT NULL DEFAULT '',
              created_at      INTEGER NOT NULL,
              updated_at      INTEGER NOT NULL,
              last_used_at    INTEGER NULL,
              usage_count     INTEGER NOT NULL DEFAULT 0
            );
            CREATE INDEX ix_snippets_folder ON snippets(folder_id);
            CREATE TABLE settings (key TEXT PRIMARY KEY, value TEXT NOT NULL);
            """),
    ];
}
