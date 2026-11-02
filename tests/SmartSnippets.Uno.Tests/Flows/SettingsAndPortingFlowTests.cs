using Microsoft.Data.Sqlite;
using SmartSnippets.Core.Data;
using SmartSnippets.Core.Domain;
using SmartSnippets.Core.Settings;
using SmartSnippets.Core.Store;
using SmartSnippets.Uno.Tests.Composition;

namespace SmartSnippets.Uno.Tests.Flows;

public sealed class SettingsAndPortingFlowTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task OfflineV1V2Fixture_PreservesSharedDataSettingsAndLocalBackup(int version)
    {
        using var f = new HostFixture();
        var source = Path.Combine(f.Root, "offline-source"); Directory.CreateDirectory(source);
        var sourceFactory = new SqliteConnectionFactory(Path.Combine(source, "prepared.db"));
        new Migrator(sourceFactory, Migrations.All.Take(version).ToList()).MigrateToLatest();
        using (var connection = sourceFactory.Open())
        using (var command = connection.CreateCommand())
        { command.CommandText = "INSERT INTO probe(text) VALUES ('synthetic fixture');"; command.ExecuteNonQuery(); }
        const string v2Text = "synthetic\n😀";
        string[] v2Tags = ["tag", "日本語"];
        if (version == 2)
        {
            // Seed with raw v2-schema SQL: the current repository reads columns added by later migrations.
            var now = DbTime.ToDb(DateTimeOffset.UtcNow);
            using (var connection = sourceFactory.Open())
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    "INSERT INTO folders (id, name, name_key, created_at, updated_at) VALUES (1, $name, $key, $now, $now);" +
                    "INSERT INTO snippets (id, text, label, label_is_custom, folder_id, tags, created_at, updated_at, last_used_at, usage_count) " +
                    "VALUES (1, $text, 'Label', 1, 1, $tags, $now, $now, $now, 1);";
                command.Parameters.AddWithValue("$name", "Synthetic 日本語");
                command.Parameters.AddWithValue("$key", FolderNameRules.Key("Synthetic 日本語"));
                command.Parameters.AddWithValue("$text", v2Text);
                command.Parameters.AddWithValue("$tags", TagList.ToStorage(v2Tags));
                command.Parameters.AddWithValue("$now", now);
                command.ExecuteNonQuery();
            }
            new AppSettings { Theme = ThemeMode.Dark, ColorPalette = ColorPalette.Plum, PopupSort = SortField.UsageCount }.Save(new SettingsRepository(sourceFactory));
        }
        using (var connection = sourceFactory.Open())
        using (var command = connection.CreateCommand())
        { command.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);"; command.ExecuteNonQuery(); }
        SqliteConnection.ClearAllPools();
        var target = Path.Combine(f.Root, "smartsnippets.db");
        File.Copy(sourceFactory.DatabasePath, target);
        var original = System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(sourceFactory.DatabasePath));
        await f.Start();
        Assert.Equal(0, f.Boundary.Reports);
        if (version == 1)
        {
            Assert.Empty(f.Host.Store.Snippets);
            Assert.True(File.Exists(target + ".v1.bak"));
            Assert.False(File.Exists(sourceFactory.DatabasePath + ".v1.bak"));
        }
        else
        {
            var actual = Assert.Single(f.Host.Store.Snippets).Snippet;
            Assert.Equal(1, actual.Id); Assert.Equal(v2Text, actual.Text);
            Assert.Equal(v2Tags, actual.Tags); Assert.Equal(1, actual.UsageCount);
            Assert.True(actual.IsLabelCustom); Assert.Equal(1, actual.FolderId);
            Assert.False(actual.IsPassword);
            Assert.Equal(AppSettings.Load(new SettingsRepository(sourceFactory)), f.Host.Settings);
        }
        SqliteConnection.ClearAllPools();
        Assert.Equal(original, System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(sourceFactory.DatabasePath)));
    }
    [Fact]
    public async Task NewerDatabase_LeavesVersionAndExitsBeforeWindowConstruction()
    {
        using var f = new HostFixture(); Directory.CreateDirectory(f.Root);
        var factory = new SqliteConnectionFactory(Path.Combine(f.Root, "smartsnippets.db"));
        new Migrator(factory).MigrateToLatest();
        using (var connection = factory.Open()) using (var command = connection.CreateCommand())
        { command.CommandText = "UPDATE schema_version SET version = 99"; command.ExecuteNonQuery(); }
        await f.Start();
        Assert.Equal(1, f.Boundary.Reports); Assert.Equal(0, f.Boundary.Messages);
        using var verify = factory.Open(); using var check = verify.CreateCommand(); check.CommandText = "SELECT version FROM schema_version";
        Assert.Equal(99L, check.ExecuteScalar());
    }
}
