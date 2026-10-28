using Microsoft.Data.Sqlite;
using SmartSnippets.Core.Data;
using SmartSnippets.Core.Logging;
using SmartSnippets.Core.Settings;
using SmartSnippets.Core.Store;
using SmartSnippets.Presentation.Tests.Fakes;
using SmartSnippets.Presentation.ViewModels;

namespace SmartSnippets.Presentation.Tests;

[Collection("AppLog")]
public class SnippetPortingIntegrationTests
{
    private static Task Run(string fixture, Func<SnippetStore, SnippetPortingViewModel, ScriptedDialogService, SqliteConnectionFactory, Task> action) => QueueSynchronizationContext.RunOnOwnerThread(async () =>
    {
        var directory = Path.Combine(Path.GetTempPath(), "ss-integration-" + Guid.NewGuid().ToString("N"));
        try
        {
            var factory = new SqliteConnectionFactory(Path.Combine(directory, "db.sqlite")); new Migrator(factory).MigrateToLatest();
            var store = new SnippetStore(factory, new FixedTime()); store.Load(); var dialogs = new ScriptedDialogService();
            var files = new FakeSnippetFileService { Suspend = true, Bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Porting", "Fixtures", fixture)) };
            await action(store, new(store, dialogs, files, new FixedTime()), dialogs, factory);
        }
        finally { AppLog.Shutdown(); SqliteConnection.ClearAllPools(); Directory.Delete(directory, true); }
    });
    [Fact] public Task Integration_ImportTermFlow_ListFindsImportedTextFolderAndTag() => Run("termflow-v1.json", async (store, vm, _, _) =>
    {
        using var list = new SnippetListViewModel(store); await vm.ImportAsync(); list.Query = "Alpha line"; Assert.Single(list.Items);
        list.Query = ""; list.SelectedBrowse = list.BrowseItems.Single(b => b.Name == "Work"); Assert.Equal(2, list.Items.Count);
        list.SelectedBrowse = list.BrowseItems[0]; list.Query = "Gamma"; Assert.Single(list.Items);
    });
    internal static PopupViewModel Popup(SnippetStore store, SqliteConnectionFactory factory)
    {
        var settings = new SettingsRepository(factory);
        return new(store, settings, AppSettings.Load(settings), new FakeInsertionService(), new FakeClipboardService(), new FakeElevation(), new FakePopupHost(), new ScriptedDialogService(), new RecordingNotifier());
    }
    [Fact] public Task Integration_ImportTermFlow_PopupSearchAndSortsReflectImport() => Run("termflow-v1.json", async (store, vm, _, factory) =>
    {
        using var popup = Popup(store, factory); await vm.ImportAsync(); popup.Query = "Gamma"; Assert.Single(popup.Items); popup.Query = "Alpha line"; Assert.Single(popup.Items); popup.Query = "";
        popup.SetSort(SortField.LastUsed); Assert.Equal(new[] { "Golf", "Alpha", "Charlie", "Beta", "Bravo", "Echo" }, popup.Items.Select(i => i.Label));
        popup.SetSort(SortField.UsageCount); Assert.Equal(new[] { "Alpha", "Charlie", "Beta", "Bravo", "Echo", "Golf" }, popup.Items.Select(i => i.Label));
    });
    [Fact] public Task Integration_ImportNative_FoldersPageShowsNewFolderWithCount() => Run("native-v1.json", async (store, vm, dialogs, _) =>
    {
        using var folders = new FoldersPageViewModel(store, dialogs); await vm.ImportAsync(); Assert.Equal(2, folders.Rows.Single(r => r.Name == "Work").SnippetCount); Assert.Equal(1, folders.Rows.Single(r => r.Name == "Home").SnippetCount); Assert.Equal(0, folders.Rows.Single(r => r.Name == "Empty folder").SnippetCount);
    });
    [Fact] public Task Integration_InjectedFailure_NoInfoDialog_LibraryUnchanged() => Run("termflow-v1.json", async (store, vm, dialogs, factory) =>
    {
        using (var c = factory.Open()) { using var cmd = c.CreateCommand(); cmd.CommandText = "CREATE TRIGGER fail BEFORE INSERT ON snippets WHEN NEW.text='Charlie' BEGIN SELECT RAISE(ABORT,'private'); END;"; cmd.ExecuteNonQuery(); }
        var before = store.Snippets; await vm.ImportAsync(); Assert.Same(before, store.Snippets); Assert.Empty(store.Folders); Assert.Empty(dialogs.Infos); Assert.Single(dialogs.Errors);
        var fresh = new SnippetStore(factory, TimeProvider.System); fresh.Load(); Assert.Empty(fresh.Snippets); Assert.Empty(fresh.Folders);
    });
    [Fact] public Task Integration_ThrowingSubscriber_RowsAcknowledged_OtherViewsRefreshed() => Run("termflow-v1.json", async (store, vm, dialogs, _) =>
    {
        store.Changed += (_, _) => throw new InvalidOperationException("private"); using var list = new SnippetListViewModel(store); var result = await vm.ImportAsync(); Assert.Equal(6, result!.Summary!.Imported); Assert.Single(dialogs.Infos); list.Query = "Alpha line"; Assert.Single(list.Items);
    });
    internal sealed class FixedTime : TimeProvider { public override DateTimeOffset GetUtcNow() => DateTimeOffset.Parse("2026-01-01T00:00:00Z"); }
}
