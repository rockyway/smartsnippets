using Microsoft.Data.Sqlite;
using SmartSnippets.Core.Data;
using SmartSnippets.Core.Store;
using SmartSnippets.Presentation.Tests.Fakes;
using SmartSnippets.Presentation.ViewModels;

namespace SmartSnippets.Presentation.Tests;

/// <summary>The editor against the real <see cref="SnippetStore"/> on a temp SQLite database.</summary>
public sealed class SnippetEditorViewModelIntegrationTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "ss-ptest-" + Guid.NewGuid().ToString("N"));
    private readonly ScriptedDialogService _dialogs = new();
    private readonly SqliteConnectionFactory _factory;

    public SnippetEditorViewModelIntegrationTests()
    {
        _factory = new SqliteConnectionFactory(Path.Combine(_directory, "test.db"));
        new Migrator(_factory).MigrateToLatest();
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

    private SnippetStore OpenStore()
    {
        var store = new SnippetStore(_factory, TimeProvider.System);
        store.Load();
        return store;
    }

    [Fact]
    public async Task SaveUnicodeAndCrlf_ReloadedStoreIsIdentical()
    {
        const string text = "  Héllo wörld \U0001F600 日本語\r\n\tline two  \r\n\n  trailing  \r\n";
        var store = OpenStore();
        var vm = new SnippetEditorViewModel(store, _dialogs, null);
        vm.Text = text;
        vm.TagsInput = " work, Work ,, mail ";
        vm.NewFolderName = "Clients";
        Assert.True(await vm.CreateFolderAsync());

        Assert.True(await vm.SaveAsync());

        var fresh = OpenStore();
        var reloaded = Assert.Single(fresh.Snippets);
        Assert.Equal(text, reloaded.Snippet.Text);
        Assert.Equal("work", reloaded.Snippet.Label);
        Assert.False(reloaded.Snippet.IsLabelCustom);
        Assert.Equal(["work", "mail"], reloaded.Snippet.Tags);
        Assert.Equal("Clients", reloaded.FolderName);
        Assert.Equal(vm.SavedSnippetId, reloaded.Snippet.Id);
    }

    [Fact]
    public async Task EditExisting_RoundTripsThroughNewStoreInstance()
    {
        var store = OpenStore();
        var first = new SnippetEditorViewModel(store, _dialogs, null);
        first.Text = "Hello\r\n  world  ";
        Assert.True(await first.SaveAsync());

        var editor = new SnippetEditorViewModel(OpenStore(), _dialogs, first.SnippetId);
        Assert.Equal("Hello\r\n  world  ", editor.Text);
        Assert.Equal("Hello", editor.Label);
        Assert.False(editor.IsDirty);

        editor.Label = "Greeting";
        editor.Text = "Changed\r\n";
        Assert.True(await editor.SaveAsync());

        var reloaded = Assert.Single(OpenStore().Snippets).Snippet;
        Assert.Equal("Greeting", reloaded.Label);
        Assert.True(reloaded.IsLabelCustom);
        Assert.Equal("Changed\r\n", reloaded.Text);
    }
}
