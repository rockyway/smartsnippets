using Microsoft.Data.Sqlite;
using SmartSnippets.Core.Data;
using SmartSnippets.Core.Logging;
using SmartSnippets.Core.Settings;
using SmartSnippets.Core.Store;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Presentation.Tests.Fakes;
using SmartSnippets.Presentation.Text;
using SmartSnippets.Presentation.ViewModels;

namespace SmartSnippets.Presentation.Tests;

[Collection("AppLog")]
public sealed class ClipboardAvailabilityTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "ss-copy-" + Guid.NewGuid().ToString("N"));
    private readonly SqliteConnectionFactory _factory;
    private readonly SnippetStore _store;
    private readonly ScriptedDialogService _dialogs = new();
    private readonly ClipboardBoundary _clipboard = new();
    public ClipboardAvailabilityTests()
    {
        AppLog.Initialize(_directory);
        _factory = new(Path.Combine(_directory, "test.db"));
        new Migrator(_factory).MigrateToLatest();
        _store = new(_factory, TimeProvider.System); _store.Load();
        _store.CreateSnippet(new("PRIVATE-BODY", "PRIVATE-LABEL", true, null, "PRIVATE-TAG"));
    }
    public void Dispose() { AppLog.Shutdown(); SqliteConnection.ClearAllPools(); Directory.Delete(_directory, true); }
    private long Id => _store.Snippets[0].Snippet.Id;
    private string Log => File.ReadAllText(Path.Combine(_directory, "app.log"));
    private PopupViewModel Popup(FakeInsertionService? insertion = null) => new(_store, new MemorySettingsStore(), new AppSettings(),
        insertion ?? new(), _clipboard, new FakeElevation(), new FakePopupHost(), _dialogs, new RecordingNotifier());

    [Fact]
    public void AvailabilityRefresh_NotifiesBothCopyAndInsertPredicates()
    {
        var canInsert = true;
        using var page = new SnippetsPageViewModel(_store, _dialogs, _clipboard,
            insertRow: _ => Task.CompletedTask, canInsertRow: () => canInsert);
        var changed = new List<string?>();
        page.PropertyChanged += (_, args) => changed.Add(args.PropertyName);
        Assert.True(page.CanInsertRow);
        canInsert = false;
        page.RefreshClipboardAvailability();
        Assert.False(page.CanInsertRow);
        Assert.Contains(nameof(page.CanInsertRow), changed);
        Assert.Contains(nameof(page.CanCopyRow), changed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AllCopyConsumers_UnavailableOrRefused_NoCrashSuccessLogOrUsage(bool available)
    {
        _clipboard.IsAvailable = available;
        using var page = new SnippetsPageViewModel(_store, _dialogs, _clipboard);
        await page.RequestSelectAsync(Id); page.Editor!.Text = "PRIVATE-DRAFT";
        page.CopyRow(Id);
        Assert.Contains(_dialogs.Errors, e => e.Message == UiText.CopyFailed);
        using var popup = Popup(new FakeInsertionService { });
        popup.Open(new(11, 22, 33, "PRIVATE-TITLE"));
        popup.CopyRow(popup.Items[0]);
        Assert.Equal(UiText.CopyFailed, popup.StatusText);
        // Generate a genuine save failure via SQLite; the recovery draft must remain dirty.
        using (var connection = _factory.Open())
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "CREATE TRIGGER refuse_save BEFORE UPDATE ON snippets BEGIN SELECT RAISE(ABORT,'PRIVATE-QUERY'); END;";
            command.ExecuteNonQuery();
        }
        Assert.False(await page.Editor.SaveAsync());
        page.Editor.CopyTextCommand.Execute(null);
        Assert.True(page.Editor.HasSaveFailure);
        Assert.True(page.Editor.IsDirty);
        Assert.Equal("PRIVATE-DRAFT", page.Editor.Text);
        Assert.Equal(UiText.CopyFailed, page.Editor.GeneralError);
        Assert.Equal(available ? 3 : 0, _clipboard.Attempts);
        Assert.DoesNotContain("_copied", Log); Assert.DoesNotContain("popup_copy_text", Log);
        Assert.Equal(0, _store.Snippets[0].Snippet.UsageCount);
        foreach (var token in new[] { "PRIVATE-BODY", "PRIVATE-LABEL", "PRIVATE-TAG", "PRIVATE-DRAFT", "PRIVATE-TITLE", "PRIVATE-QUERY" })
        {
            Assert.DoesNotContain(token, Log);
            Assert.Contains(token, Log + token); // planted scanner negative control
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FooterCopy_RefusalPreservesRecoveryAndRefreshesCommand(bool available)
    {
        var insertion = new FakeInsertionService();
        insertion.Results.Enqueue(new(InsertionOutcome.PasteFailed, "Try again"));
        using var popup = Popup(insertion); popup.Open(new(11, 22, 33, "Target"));
        await popup.InsertSelectedAsync();
        Assert.True(popup.CopyFailedTextCommand.CanExecute(null));
        var changes = new List<string?>(); popup.PropertyChanged += (_, e) => changes.Add(e.PropertyName);
        int commandChanges = 0; popup.CopyFailedTextCommand.CanExecuteChanged += (_, _) => commandChanges++;
        _clipboard.IsAvailable = available;
        popup.RefreshClipboardAvailability();
        Assert.Equal(available, popup.CanCopyFailedText);
        Assert.Equal(available, popup.CopyFailedTextCommand.CanExecute(null));
        Assert.Contains(nameof(PopupViewModel.CanCopyFailedText), changes); Assert.True(commandChanges > 0);
        popup.CopyFailedTextCommand.Execute(null);
        Assert.Equal(PopupFooterState.Error, popup.FooterState);
        Assert.Equal(UiText.CopyFailed, popup.FooterMessage);
        Assert.DoesNotContain("popup_copy_text", Log);
        Assert.Equal(available ? 1 : 0, _clipboard.Attempts);
    }

    [Fact]
    public async Task AvailabilityRefresh_DisablesCommandsAndRechecksAtExecution()
    {
        using var page = new SnippetsPageViewModel(_store, _dialogs, _clipboard);
        await page.RequestSelectAsync(Id);
        Assert.True(page.CanCopyRow); Assert.True(page.Editor!.CopyTextCommand.CanExecute(null));
        int commandChanges = 0; page.Editor.CopyTextCommand.CanExecuteChanged += (_, _) => commandChanges++;
        var changes = new List<string?>(); page.PropertyChanged += (_, e) => changes.Add(e.PropertyName);
        _clipboard.IsAvailable = false; page.RefreshClipboardAvailability();
        Assert.False(page.CanCopyRow); Assert.False(page.Editor.CopyTextCommand.CanExecute(null));
        Assert.True(commandChanges > 0); Assert.Contains(nameof(SnippetsPageViewModel.CanCopyRow), changes);
        page.CopyRow(Id); page.Editor.CopyTextCommand.Execute(null); Assert.Equal(0, _clipboard.Attempts);
        _clipboard.IsAvailable = true; page.RefreshClipboardAvailability();
        _clipboard.Succeeds = true;
        page.CopyRow(Id);
        using var popup = Popup(); popup.Open(new(11, 22, 33, "Target")); popup.CopyRow(popup.Items[0]);
        Assert.Contains("snippet_row_copied", Log); Assert.Contains("popup_row_copied", Log);
        Assert.Equal(0, _store.Snippets[0].Snippet.UsageCount);
    }

    [Fact]
    public async Task SuccessfulCopyRetry_ClearsOnlyCopyErrorAndPreservesDirtyDraft()
    {
        using var popup = Popup(); popup.Open(new(11, 22, 33, "Target"));
        popup.CopyRow(popup.Items[0]); Assert.Equal(UiText.CopyFailed, popup.StatusText);
        _clipboard.Succeeds = true;
        popup.CopyRow(popup.Items[0]); Assert.Equal(string.Empty, popup.StatusText);
        using var page = new SnippetsPageViewModel(_store, _dialogs, _clipboard);
        await page.RequestSelectAsync(Id); page.Editor!.Text = "PRIVATE-DRAFT";
        _clipboard.Succeeds = false; page.Editor.CopyTextCommand.Execute(null);
        Assert.Equal(UiText.CopyFailed, page.Editor.GeneralError);
        _clipboard.Succeeds = true; page.Editor.CopyTextCommand.Execute(null);
        Assert.Equal(string.Empty, page.Editor.GeneralError);
        Assert.True(page.Editor.IsDirty); Assert.Equal("PRIVATE-DRAFT", page.Editor.Text);
        Assert.Equal(0, _store.Snippets[0].Snippet.UsageCount);
    }

    [Fact]
    public void PopupAvailabilityRefresh_ReachesActiveEditorAndRowPredicate()
    {
        using var popup = Popup(); popup.Open(new(11, 22, 33, "Target"));
        var row = popup.Items[0]; Assert.True(popup.CanCopyRow(row));
        popup.EditRow(row);
        int editorChanges = 0;
        popup.Editor!.CopyTextCommand.CanExecuteChanged += (_, _) => editorChanges++;
        var changes = new List<string?>(); popup.PropertyChanged += (_, e) => changes.Add(e.PropertyName);
        _clipboard.IsAvailable = false; popup.RefreshClipboardAvailability();
        Assert.False(popup.Editor.CopyTextCommand.CanExecute(null));
        Assert.False(popup.CanCopyRow(row));
        Assert.True(editorChanges > 0); Assert.Contains(nameof(PopupViewModel.CanCopyRow), changes);
    }

    [Fact]
    public async Task FooterCopy_TrueResultAloneLogsSuccessWithoutUsage()
    {
        var insertion = new FakeInsertionService(); insertion.Results.Enqueue(new(InsertionOutcome.PasteFailed, "Retry"));
        using var popup = Popup(insertion); popup.Open(new(11, 22, 33, "Target"));
        await popup.InsertSelectedAsync(); _clipboard.Succeeds = true;
        popup.CopyFailedTextCommand.Execute(null);
        Assert.Contains("popup_copy_text", Log); Assert.Equal(1, _clipboard.Attempts);
        Assert.Equal(0, _store.Snippets[0].Snippet.UsageCount);
    }

    [Fact]
    public async Task Preview_IgnoresEvenAvailableClipboardIncludingEditor()
    {
        _clipboard.Succeeds = true;
        using var popup = Popup(); popup.OpenPreview();
        Assert.False(popup.CanCopyRow(popup.Items[0]));
        popup.CopyRow(popup.Items[0]); popup.CopyFailedTextCommand.Execute(null);
        popup.EditRow(popup.Items[0]);
        Assert.False(popup.Editor!.CopyTextCommand.CanExecute(null));
        popup.Editor.CopyTextCommand.Execute(null);
        popup.RefreshClipboardAvailability();
        await popup.CancelEditorAsync(false); await popup.InsertSelectedAsync();
        Assert.Equal(0, _clipboard.Attempts);
        Assert.Equal(0, _store.Snippets[0].Snippet.UsageCount);
    }

    private sealed class ClipboardBoundary : IClipboardService
    {
        public bool IsAvailable { get; set; } = true;
        public bool Succeeds { get; set; }
        public int Attempts { get; private set; }
        public void SetText(string text) => throw new InvalidOperationException("VM bypassed result boundary");
        public bool TrySetText(string text) { Attempts++; return Succeeds; }
    }
}
