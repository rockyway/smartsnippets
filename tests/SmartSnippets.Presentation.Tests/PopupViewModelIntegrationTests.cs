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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BodyOnlyEditBeyondPreviewBoundaryRefreshesBothListsAndReload(bool visible)
    {
        var store = OpenStore();
        var original = new string('x', 100) + "\nOLD_END";
        var updated = new string('x', 100) + "\nNEW_END";
        var saved = store.CreateSnippet(new(original, "Same label", true, null, "one,two,three")).Value!;
        using var main = new SnippetListViewModel(store);
        var settings = new SettingsRepository(_factory);
        using var popup = Create(store, settings, new FakeInsertionService());
        if (visible) popup.OpenPreview();
        var beforeMain = Assert.Single(main.Items);
        var beforePopup = Assert.Single(popup.Items);
        Assert.Same(store.Snippets.Single().Snippet.Text, beforeMain.ContentText);
        Assert.Same(store.Snippets.Single().Snippet.Text, beforePopup.ContentText);
        store.UpdateSnippet(saved.Id, new(updated, "Same label", true, null, "one,two,three"));
        popup.EnsureCurrent();
        var afterMain = Assert.Single(main.Items);
        var afterPopup = Assert.Single(popup.Items);
        Assert.Equal(beforeMain.Preview, afterMain.Preview);
        Assert.NotSame(beforeMain, afterMain); Assert.NotSame(beforePopup, afterPopup);
        Assert.Equal(updated, afterMain.ContentText); Assert.Equal(updated, afterPopup.ContentText);
        Assert.Empty(afterPopup.Preview);
        Assert.DoesNotContain("NEW_END", afterMain.ToString());
        Assert.DoesNotContain("NEW_END", afterPopup.ToString());
        store.RecordUsage(saved.Id); popup.EnsureCurrent();
        Assert.Same(afterMain, Assert.Single(main.Items));
        Assert.Same(afterPopup, Assert.Single(popup.Items));
        var equalBody = new string(updated.ToCharArray());
        store.UpdateSnippet(saved.Id, new(equalBody, "Same label", true, null, "one,two,three"));
        popup.EnsureCurrent();
        Assert.Same(afterMain, Assert.Single(main.Items)); Assert.Same(afterPopup, Assert.Single(popup.Items));
        var reloaded = OpenStore();
        using var reloadedMain = new SnippetListViewModel(reloaded);
        using var reloadedPopup = Create(reloaded, settings, new FakeInsertionService());
        Assert.Equal(updated, Assert.Single(reloadedMain.Items).ContentText);
        Assert.Equal(updated, Assert.Single(reloadedPopup.Items).ContentText);
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
