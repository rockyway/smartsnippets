using System.Text.Json;
using Microsoft.Data.Sqlite;
using SmartSnippets.Core.Data;
using SmartSnippets.Core.Logging;
using SmartSnippets.Core.Porting;
using SmartSnippets.Core.Query;
using SmartSnippets.Core.Settings;
using SmartSnippets.Core.Store;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Presentation.Tests.Fakes;
using SmartSnippets.Presentation.ViewModels;

namespace SmartSnippets.Presentation.Tests;

[Collection("AppLog")]
public sealed class PasswordSnippetFlowTests : IDisposable
{
    private const string Secret = "  synthetic-only-secret 🔒\nsecond\rthird\r\nlast  ";
    private const string Label = "Private account label";
    private readonly string _root = Path.Combine(Path.GetTempPath(), "ss-password-flow-" + Guid.NewGuid().ToString("N"));
    private readonly SqliteConnectionFactory _factory;
    private readonly TestSecretProtector _protector = new();
    private readonly FakeClipboardService _clipboard = new();
    private readonly ScriptedDialogService _dialogs = new();

    public PasswordSnippetFlowTests()
    {
        _factory = new(Path.Combine(_root, "test.db"));
        new Migrator(_factory).MigrateToLatest();
    }

    private SnippetStore Open()
    {
        var store = new SnippetStore(_factory, TimeProvider.System, _protector);
        store.Load();
        return store;
    }

    private async Task<long> Save(SnippetStore store)
    {
        using var editor = new SnippetEditorViewModel(store, _dialogs, null, _clipboard);
        editor.Text = Secret;
        editor.IsPassword = true;
        editor.Label = Label;
        editor.TagsInput = "vault-tag";
        Assert.True(await editor.SaveAsync());
        return Assert.Single(store.Snippets).Snippet.Id;
    }

    private PopupViewModel Popup(SnippetStore store) => new(store, new MemorySettingsStore(), new(),
        new InsertionService(store, new FakeTargetProbe([]), new FakePasteDispatcher([]), delay: (_, _) => Task.CompletedTask),
        _clipboard, new FakeElevation(), new FakePopupHost(), _dialogs, new RecordingNotifier());

    private string RawText()
    {
        using var connection = _factory.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT text FROM snippets WHERE is_password = 1";
        return Assert.IsType<string>(command.ExecuteScalar());
    }

    [Fact]
    public async Task SaveReloadPreviewSearchExportPaste()
    {
        var id = await Save(Open());
        var envelope = RawText();
        Assert.StartsWith("dpapi:v1:", envelope);
        Assert.DoesNotContain("synthetic-only-secret", envelope);
        var fresh = Open();
        var view = Assert.Single(fresh.Snippets);
        Assert.True(view.Snippet.IsPassword);
        Assert.Equal(Secret, view.Snippet.Text);
        var main = SnippetRowViewModel.From(view);
        Assert.Equal("••••••••", main.Preview);
        Assert.Equal("••••••••", main.ContentText);
        using var popup = Popup(fresh);
        Assert.Equal("••••••••", Assert.Single(popup.Items).ContentText);
        var search = new SnippetSearch(fresh.Snippets);
        Assert.Single(search.Filter(Label));
        Assert.Single(search.Filter("vault-tag"));
        Assert.Empty(search.Filter("synthetic-only-secret"));
        popup.Query = "vault-tag";
        Assert.Single(popup.Items);
        popup.Query = "synthetic-only-secret";
        Assert.Empty(popup.Items);
        Assert.True(fresh.CreateSnippet(new("ordinary body", "Public label", true, null, "")).Success);
        var export = NativeExport.Write(fresh.Folders, fresh.Snippets, DateTimeOffset.UtcNow);
        Assert.Null(export.Refusal);
        Assert.Equal(1, export.SnippetCount);
        Assert.Equal(1, export.ExcludedPasswordCount);
        using var json = JsonDocument.Parse(export.Utf8Json!);
        Assert.Single(json.RootElement.GetProperty("snippets").EnumerateArray());
        var exported = System.Text.Encoding.UTF8.GetString(export.Utf8Json!);
        Assert.Contains("ordinary body", exported);
        Assert.DoesNotContain("synthetic-only-secret", exported);
        Assert.DoesNotContain(Label, exported);
        var dispatcher = new FakePasteDispatcher([]);
        var service = new InsertionService(fresh, new FakeTargetProbe([]), dispatcher, delay: (_, _) => Task.CompletedTask);
        Assert.Equal(InsertionOutcome.Inserted, (await service.InsertAsync(new(1, 2, 0, "target"), id)).Outcome);
        Assert.Equal("  synthetic-only-secret 🔒\r\nsecond\r\nthird\r\nlast  ", dispatcher.Text);
        Assert.Equal(1, Open().Snippets.Single(v => v.Snippet.Id == id).Snippet.UsageCount);
    }

    [Fact]
    public async Task ExplicitCopiesUseOriginalText()
    {
        var id = await Save(Open());
        var store = Open();
        using var page = new SnippetsPageViewModel(store, _dialogs, _clipboard);
        Assert.True(page.CanCopyRow);
        page.CopyRow(id);
        using var popup = Popup(store);
        var row = Assert.Single(popup.Items);
        Assert.True(popup.CanCopyRow(row));
        popup.CopyRow(row);
        using var editor = new SnippetEditorViewModel(store, _dialogs, id, _clipboard);
        editor.TagsInput = "changed";
        _protector.Fail = true;
        Assert.False(await editor.SaveAsync());
        Assert.True(editor.HasSaveFailure);
        Assert.True(editor.CopyTextCommand.CanExecute(null));
        editor.CopyTextCommand.Execute(null);
        Assert.Equal(new[] { Secret, Secret, Secret }, _clipboard.Texts);
        Assert.Equal(0, Assert.Single(store.Snippets).Snippet.UsageCount);
    }

    [Fact]
    public async Task PreviewAndToggleNeverUseClipboard()
    {
        var id = await Save(Open());
        var store = Open();
        using var editor = new SnippetEditorViewModel(store, _dialogs, id, _clipboard);
        Assert.Equal(Secret, editor.Text);
        editor.IsPassword = false;
        Assert.False(editor.IsPassword);
        editor.IsPassword = true;
        Assert.True(editor.IsPassword);
        editor.ResetToSaved();
        Assert.Equal(Secret, editor.Text);
        using var page = new SnippetsPageViewModel(store, _dialogs, _clipboard);
        Assert.True(await page.RequestSelectAsync(id));
        Assert.Equal(Secret, page.Editor!.Text);
        using var popup = Popup(store);
        Assert.Equal("••••••••", Assert.Single(popup.Items).ContentText);
        Assert.Equal("••••••••", SnippetRowViewModel.From(Assert.Single(store.Snippets)).ContentText);
        store.Load();
        Assert.True(editor.IsPassword);
        Assert.Equal(Secret, editor.Text);
        Assert.Empty(_clipboard.Texts);
    }

    [Fact]
    public async Task FailureLogsContainOnlySafeTokens()
    {
        var store = Open();
        var id = await Save(store);
        var envelope = RawText();
        Assert.StartsWith("dpapi:v1:", envelope);
        AppLog.Initialize(Path.Combine(_root, "logs"));
        _protector.Fail = true;
        using (var editor = new SnippetEditorViewModel(store, _dialogs, id, _clipboard))
        {
            editor.TagsInput = "changed";
            Assert.False(await editor.SaveAsync());
            Assert.True(editor.HasSaveFailure);
        }
        _protector.Fail = false;
        using (var connection = _factory.Open())
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "UPDATE snippets SET text = $text WHERE id = $id";
            command.Parameters.AddWithValue("$text", "dpapi:v1:AQIDBA==");
            command.Parameters.AddWithValue("$id", id);
            Assert.Equal(1, command.ExecuteNonQuery());
        }
        Assert.Equal(StoreFailure.SecretProtection, Assert.Throws<StoreException>(() => Open()).Failure);
        AppLog.Shutdown();
        var log = File.ReadAllText(Path.Combine(_root, "logs", "app.log"));
        Assert.Contains("store_write_failed", log);
        Assert.Contains("op=update_snippet", log);
        Assert.Contains("op=load", log);
        Assert.Contains("error=SecretProtectionException", log);
        Assert.DoesNotContain(Secret, log);
        Assert.DoesNotContain("synthetic-only-secret", log);
        Assert.DoesNotContain(Label, log);
        Assert.DoesNotContain(envelope, log);
        Assert.DoesNotContain("dpapi:v1:AQIDBA==", log);
    }

    public void Dispose()
    {
        AppLog.Shutdown();
        SqliteConnection.ClearAllPools();
        Directory.Delete(_root, true);
    }
}
