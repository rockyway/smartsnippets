using Microsoft.Data.Sqlite;
using SmartSnippets.Core.Data;
using SmartSnippets.Core.Settings;
using SmartSnippets.Core.Store;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Presentation.Tests.Fakes;
using SmartSnippets.Presentation.ViewModels;

namespace SmartSnippets.Presentation.Tests;

public sealed class PopupViewModelIntegrationTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "ss-popup-" + Guid.NewGuid().ToString("N"));
    private readonly SqliteConnectionFactory _factory;
    public PopupViewModelIntegrationTests()
    {
        _factory = new SqliteConnectionFactory(Path.Combine(_directory, "test.db"));
        new Migrator(_factory).MigrateToLatest();
    }
    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try { Directory.Delete(_directory, true); } catch (IOException) { }
    }
    private SnippetStore OpenStore() { var store = new SnippetStore(_factory, TimeProvider.System); store.Load(); return store; }
    private PopupViewModel Create(SnippetStore store, ISettingsStore settings, FakeInsertionService insertion) =>
        new(store, settings, AppSettings.Load(settings), insertion, new FakeClipboardService(), new FakeElevation(), new FakePopupHost(), new ScriptedDialogService(), new RecordingNotifier());

    [Fact]
    public async Task AddThenInsert_RealStore_SelectsAndInsertsSavedId()
    {
        var store = OpenStore(); var settings = new SettingsRepository(_factory); var insertion = new FakeInsertionService();
        using var vm = Create(store, settings, insertion);
        var target = new InsertionTarget(12, 34, 56, "Notepad"); vm.Open(target);
        vm.AddSnippetCommand.Execute(null); vm.Editor!.Text = "saved text"; await vm.SaveEditorAsync();
        var saved = Assert.Single(OpenStore().Snippets).Snippet;
        Assert.Equal(saved.Id, vm.Items[vm.SelectedIndex].SnippetId);
        await vm.InsertSelectedAsync();
        var call = Assert.Single(insertion.Calls); Assert.Equal(saved.Id, call.Id); Assert.Equal(target, call.Target);
    }

    [Fact]
    public void LayoutAndSort_SurviveAppSettingsReload()
    {
        var settings = new SettingsRepository(_factory);
        using var vm = Create(OpenStore(), settings, new FakeInsertionService());
        vm.ToggleLayoutCommand.Execute(null); vm.SetSort(SortField.Updated);
        var loaded = AppSettings.Load(new SettingsRepository(_factory));
        Assert.Equal(PopupLayout.Folder, loaded.PopupLayout); Assert.Equal(SortField.Updated, loaded.PopupSort);
    }
}
