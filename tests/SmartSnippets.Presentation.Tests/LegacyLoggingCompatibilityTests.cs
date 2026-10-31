using Microsoft.Data.Sqlite;
using SmartSnippets.Core.Data;
using SmartSnippets.Core.Logging;
using SmartSnippets.Core.Store;
using SmartSnippets.Presentation.Tests.Fakes;
using SmartSnippets.Presentation.Text;
using SmartSnippets.Presentation.ViewModels;

namespace SmartSnippets.Presentation.Tests;

[Collection("AppLog")]
public sealed class LegacyLoggingCompatibilityTests
{
    [Fact]
    public async Task LegacyStartupAndEditorFailure_KeepEventsAndRecoveryWithoutPayload()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ss-legacy-log-" + Guid.NewGuid().ToString("N"));
        const string secret = "PRIVATE-BODY-LABEL-TAG-TITLE-QUERY-PATH";
        try
        {
            AppLog.Initialize(directory);
            // Same public API as legacy AppHost's startup/load catch blocks, including its stack detail.
            foreach (var name in new[] { "startup_failed", "migrations_failed", "store_load_failed", "data_load_failed" })
                AppLog.Error(name, new IOException(secret), "at Legacy.AppHost(" + secret + "):line 85");
            var factory = new SqliteConnectionFactory(Path.Combine(directory, "test.db"));
            new Migrator(factory).MigrateToLatest();
            var store = new SnippetStore(factory, TimeProvider.System); store.Load();
            var id = store.CreateSnippet(new(secret, secret, true, null, secret)).Value!.Id;
            using (var connection = factory.Open())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = $"CREATE TRIGGER refuse_update BEFORE UPDATE ON snippets BEGIN SELECT RAISE(ABORT,'{secret}'); END; CREATE TRIGGER refuse_delete BEFORE DELETE ON snippets BEGIN SELECT RAISE(ABORT,'{secret}'); END;";
                command.ExecuteNonQuery();
            }
            var dialogs = new ScriptedDialogService();
            using var page = new SnippetsPageViewModel(store, dialogs);
            await page.RequestSelectAsync(id); page.Editor!.Text = secret + " dirty";
            Assert.False(await page.Editor.SaveAsync());
            Assert.True(page.Editor.IsDirty); Assert.True(page.Editor.HasSaveFailure);
            Assert.Equal(UiText.SaveFailed, page.Editor.SaveFailureMessage);
            dialogs.ConfirmAnswers.Enqueue(true);
            Assert.False(await page.DeleteRowAsync(id)); Assert.NotEmpty(dialogs.Errors);
            var log = File.ReadAllText(Path.Combine(directory, "app.log"));
            foreach (var name in new[] { "startup_failed", "migrations_failed", "store_load_failed", "data_load_failed", "save_failed", "delete_failed" })
                Assert.Contains(name, log);
            Assert.DoesNotContain(secret, log); Assert.Contains(secret, log + secret);
            Assert.DoesNotContain("Legacy.AppHost", log);
        }
        finally { AppLog.Shutdown(); SqliteConnection.ClearAllPools(); Directory.Delete(directory, true); }
    }
}
