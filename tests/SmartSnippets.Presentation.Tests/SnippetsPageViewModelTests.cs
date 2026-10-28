using SmartSnippets.Core.Store;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Presentation.Tests.Fakes;
using SmartSnippets.Presentation.Text;
using SmartSnippets.Presentation.ViewModels;

namespace SmartSnippets.Presentation.Tests;

public sealed class SnippetsPageViewModelTests
{
    private readonly FakeSnippetStore _store = new();
    private readonly ScriptedDialogService _dialogs = new();

    private (SnippetsPageViewModel Page, long A, long B) OpenDirtyOnA()
    {
        var a = _store.SeedSnippet("alpha", label: "Alpha").Id;
        var b = _store.SeedSnippet("bravo", label: "Bravo").Id;
        var page = new SnippetsPageViewModel(_store, _dialogs);
        page.List.SelectedId = a;
        page.PendingSelection.GetAwaiter().GetResult();
        Assert.Equal(a, page.Editor?.SnippetId);
        page.Editor!.Text = "alpha edited";
        return (page, a, b);
    }

    [Fact]
    public async Task RowActions_CopyInsertEditAndCancelDelete()
    {
        var row = _store.SeedSnippet("synthetic body", "Label");
        var clipboard = new FakeClipboardService();
        var inserted = new List<long>();
        using var page = new SnippetsPageViewModel(_store, _dialogs, clipboard,
            insertRow: id => { inserted.Add(id); return Task.CompletedTask; }, canInsertRow: () => true);
        page.CopyRow(row.Id);
        Assert.Equal("synthetic body", Assert.Single(clipboard.Texts));
        Assert.True(page.CanInsertRow);
        await page.InsertRowAsync(row.Id);
        Assert.Equal(row.Id, Assert.Single(inserted));
        Assert.True(await page.RequestSelectAsync(row.Id));
        _dialogs.ConfirmAnswers.Enqueue(false);
        Assert.False(await page.DeleteRowAsync(row.Id));
        Assert.Single(_store.Snippets);
    }

    [Fact]
    public async Task RowInsertWithoutTargetIsDisabled()
    {
        var row = _store.SeedSnippet("body");
        using var page = new SnippetsPageViewModel(_store, _dialogs);
        Assert.False(page.CanInsertRow);
        await page.InsertRowAsync(row.Id);
        Assert.Single(_store.Snippets);
    }

    [Fact]
    public async Task SwitchWhileDirty_Save_SavesThenSwitches()
    {
        var (page, a, b) = OpenDirtyOnA();
        _dialogs.ChoiceAnswers.Enqueue(SaveChoice.Save);

        Assert.True(await page.RequestSelectAsync(b));

        Assert.Equal(b, page.Editor!.SnippetId);
        Assert.Equal("alpha edited", _store.Snippets.Single(v => v.Snippet.Id == a).Snippet.Text);
        Assert.Equal(b, page.List.SelectedId);
    }

    [Fact]
    public async Task SwitchWhileDirty_SaveFails_StaysOnCurrent()
    {
        var (page, a, b) = OpenDirtyOnA();
        page.Editor!.Text = "  ";
        _dialogs.ChoiceAnswers.Enqueue(SaveChoice.Save);

        Assert.False(await page.RequestSelectAsync(b));

        Assert.Equal(a, page.Editor!.SnippetId);
        Assert.Equal("  ", page.Editor.Text);
        Assert.True(page.Editor.IsDirty);
        Assert.Equal(a, page.List.SelectedId);
    }

    [Fact]
    public async Task SwitchWhileDirty_Discard_Switches()
    {
        var (page, a, b) = OpenDirtyOnA();
        _dialogs.ChoiceAnswers.Enqueue(SaveChoice.Discard);

        Assert.True(await page.RequestSelectAsync(b));

        Assert.Equal(b, page.Editor!.SnippetId);
        Assert.Equal("alpha", _store.Snippets.Single(v => v.Snippet.Id == a).Snippet.Text);
    }

    [Fact]
    public async Task SwitchWhileDirty_Cancel_RestoresSelection()
    {
        var (page, a, b) = OpenDirtyOnA();
        var restored = new List<long?>();
        page.SelectionRestored += (_, id) => restored.Add(id);
        _dialogs.ChoiceAnswers.Enqueue(SaveChoice.Cancel);

        page.List.SelectedId = b;
        await page.PendingSelection;

        Assert.Equal(a, page.Editor!.SnippetId);
        Assert.Equal("alpha edited", page.Editor.Text);
        Assert.Equal(a, page.List.SelectedId);
        Assert.Equal([a], restored);
    }

    [Fact]
    public async Task CanLeave_Dirty_AsksOnce()
    {
        var (page, _, _) = OpenDirtyOnA();
        _dialogs.ChoiceAnswers.Enqueue(SaveChoice.Discard);

        Assert.True(await page.CanLeaveAsync());
        Assert.True(await page.CanLeaveAsync());

        Assert.Equal((UiText.DiscardTitle, UiText.DiscardBody), Assert.Single(_dialogs.SaveDiscardPrompts));
    }

    [Fact]
    public async Task EditorDeleteCommand_UsesSameConfirmation_AndDeletes()
    {
        var a = _store.SeedSnippet("a", label: "Alpha").Id;
        var b = _store.SeedSnippet("b", label: "Bravo").Id;
        var page = new SnippetsPageViewModel(_store, _dialogs);
        await page.RequestSelectAsync(a);
        _dialogs.ConfirmAnswers.Enqueue(true);

        page.Editor!.DeleteCommand.Execute(null);
        await page.PendingDelete;

        Assert.Equal(
            (UiText.DeleteSnippetTitle("Alpha"), UiText.DeleteSnippetBody, UiText.DeleteSnippetConfirm),
            _dialogs.Confirms.Single());
        Assert.DoesNotContain(_store.Snippets, v => v.Snippet.Id == a);
        Assert.Equal(b, page.Editor!.SnippetId);
    }

    [Fact]
    public async Task EditorDeleteCommand_Cancelled_KeepsSnippet()
    {
        var a = _store.SeedSnippet("a", label: "Alpha").Id;
        var page = new SnippetsPageViewModel(_store, _dialogs);
        await page.RequestSelectAsync(a);
        _dialogs.ConfirmAnswers.Enqueue(false);

        page.Editor!.DeleteCommand.Execute(null);
        await page.PendingDelete;

        Assert.Single(_store.Snippets);
        Assert.Equal(a, page.Editor!.SnippetId);
    }

    [Fact]
    public async Task ReplacedEditor_NoLongerRequestsDelete()
    {
        var a = _store.SeedSnippet("a", label: "Alpha").Id;
        var b = _store.SeedSnippet("b", label: "Bravo").Id;
        var page = new SnippetsPageViewModel(_store, _dialogs);
        await page.RequestSelectAsync(a);
        var old = page.Editor!;
        await page.RequestSelectAsync(b);

        old.DeleteCommand.Execute(null);
        await page.PendingDelete;

        Assert.Empty(_dialogs.Confirms);
    }

    [Fact]
    public async Task Editor_ReceivesClipboardAndTime()
    {
        var clipboard = new FakeClipboardService();
        var a = _store.SeedSnippet("copy me", label: "Alpha").Id;
        var time = new FixedTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 30, 0, TimeSpan.Zero));
        var page = new SnippetsPageViewModel(_store, _dialogs, clipboard, time);
        await page.RequestSelectAsync(a);

        page.Editor!.CopyTextCommand.Execute(null);

        Assert.Equal(["copy me"], clipboard.Texts);
    }

    [Fact]
    public async Task BrowseScope_ChangeKeepsEditorOpen()
    {
        var a = _store.SeedSnippet("a", label: "Alpha", folder: "Work").Id;
        _store.SeedSnippet("b", label: "Bravo");
        var page = new SnippetsPageViewModel(_store, _dialogs);
        await page.RequestSelectAsync(a);

        page.List.SelectedBrowse = page.List.BrowseItems.Single(x => x.Kind == BrowseKind.Unfiled);

        Assert.Equal(a, page.Editor!.SnippetId);
        Assert.Null(page.List.SelectedId);
    }

    [Fact]
    public async Task CanLeave_Dirty_CancelStays_ThenSaveLeaves()
    {
        var (page, a, _) = OpenDirtyOnA();
        _dialogs.ChoiceAnswers.Enqueue(SaveChoice.Cancel);
        _dialogs.ChoiceAnswers.Enqueue(SaveChoice.Save);

        Assert.False(await page.CanLeaveAsync());
        Assert.True(await page.CanLeaveAsync());

        Assert.Equal("alpha edited", _store.Snippets.Single(v => v.Snippet.Id == a).Snippet.Text);
    }

    [Fact]
    public async Task CanLeave_Clean_DoesNotAsk()
    {
        _store.SeedSnippet("alpha", label: "Alpha");
        var page = new SnippetsPageViewModel(_store, _dialogs);

        Assert.True(await page.CanLeaveAsync());
        Assert.Empty(_dialogs.SaveDiscardPrompts);
    }

    [Fact]
    public async Task Delete_Confirmed_RemovesAndSelectsNext()
    {
        var a = _store.SeedSnippet("a", label: "Alpha").Id;
        var b = _store.SeedSnippet("b", label: "Bravo").Id;
        var c = _store.SeedSnippet("c", label: "Charlie").Id;
        var page = new SnippetsPageViewModel(_store, _dialogs);
        Assert.True(await page.RequestSelectAsync(b));
        _dialogs.ConfirmAnswers.Enqueue(true);

        Assert.True(await page.DeleteSelectedAsync());

        Assert.Equal(
            (UiText.DeleteSnippetTitle("Bravo"), UiText.DeleteSnippetBody, UiText.DeleteSnippetConfirm),
            _dialogs.Confirms.Single());
        Assert.DoesNotContain(_store.Snippets, v => v.Snippet.Id == b);
        Assert.Equal(c, page.Editor!.SnippetId);
        Assert.Equal(c, page.List.SelectedId);

        _dialogs.ConfirmAnswers.Enqueue(true);
        Assert.True(await page.DeleteSelectedAsync());
        Assert.Equal(a, page.Editor!.SnippetId);

        _dialogs.ConfirmAnswers.Enqueue(true);
        Assert.True(await page.DeleteSelectedAsync());
        Assert.Null(page.Editor);
        Assert.Null(page.List.SelectedId);
        Assert.False(page.HasEditor);
    }

    [Fact]
    public async Task Delete_Cancelled_NoChange()
    {
        var a = _store.SeedSnippet("a", label: "Alpha").Id;
        var page = new SnippetsPageViewModel(_store, _dialogs);
        await page.RequestSelectAsync(a);
        _dialogs.ConfirmAnswers.Enqueue(false);

        Assert.False(await page.DeleteSelectedAsync());

        Assert.Single(_store.Snippets);
        Assert.Equal(a, page.Editor!.SnippetId);
    }

    [Fact]
    public async Task Delete_StoreException_ShowsErrorAndKeepsSnippet()
    {
        var a = _store.SeedSnippet("a", label: "Alpha").Id;
        var page = new SnippetsPageViewModel(_store, _dialogs);
        await page.RequestSelectAsync(a);
        _dialogs.ConfirmAnswers.Enqueue(true);
        _store.Failure = new StoreException(StoreFailure.DatabaseBusy, "busy");

        Assert.False(await page.DeleteSelectedAsync());

        Assert.Single(_dialogs.Errors);
        Assert.Single(_store.Snippets);
        Assert.Equal(a, page.Editor!.SnippetId);
    }

    [Fact]
    public async Task New_WhileDirty_Guarded()
    {
        var (page, a, _) = OpenDirtyOnA();

        _dialogs.ChoiceAnswers.Enqueue(SaveChoice.Cancel);
        Assert.False(await page.NewAsync());
        Assert.Equal(a, page.Editor!.SnippetId);

        _dialogs.ChoiceAnswers.Enqueue(SaveChoice.Discard);
        Assert.True(await page.NewAsync());
        Assert.True(page.Editor!.IsNew);
        Assert.Null(page.List.SelectedId);
        Assert.Equal(2, _dialogs.SaveDiscardPrompts.Count);
    }

    [Fact]
    public async Task New_Clean_OpensBlankEditorWithoutAsking()
    {
        _store.SeedSnippet("a", label: "Alpha");
        var page = new SnippetsPageViewModel(_store, _dialogs);

        Assert.True(await page.NewAsync());

        Assert.True(page.HasEditor);
        Assert.Empty(_dialogs.SaveDiscardPrompts);
    }

    [Fact]
    public async Task NewSnippet_Saved_IsSelectedInList()
    {
        var page = new SnippetsPageViewModel(_store, _dialogs);
        await page.NewAsync();
        page.Editor!.Text = "Fresh";

        Assert.True(await page.Editor.SaveAsync());

        Assert.Equal(page.Editor.SnippetId, page.List.SelectedId);
        Assert.Equal("Fresh", page.List.SelectedItem?.Label);
    }

    [Fact]
    public async Task SnippetDeletedElsewhere_ClosesEditor()
    {
        var a = _store.SeedSnippet("a", label: "Alpha").Id;
        var page = new SnippetsPageViewModel(_store, _dialogs);
        await page.RequestSelectAsync(a);

        _store.DeleteSnippet(a);

        Assert.Null(page.Editor);
    }

    [Fact]
    public async Task FilteredOutSelection_KeepsEditorAndReselectsWhenVisibleAgain()
    {
        var a = _store.SeedSnippet("a", label: "Alpha").Id;
        _store.SeedSnippet("b", label: "Bravo");
        var page = new SnippetsPageViewModel(_store, _dialogs);
        await page.RequestSelectAsync(a);

        page.List.Query = "bravo";
        Assert.Null(page.List.SelectedId);
        Assert.Equal(a, page.Editor!.SnippetId);

        page.List.Query = "";
        Assert.Equal(a, page.List.SelectedId);
    }

    [Fact]
    public async Task Dispose_UnsubscribesEverything()
    {
        var id = _store.SeedSnippet("a", label: "Alpha").Id;
        var page = new SnippetsPageViewModel(_store, _dialogs);
        await page.RequestSelectAsync(id);
        Assert.Equal(2, _store.ChangedSubscriberCount);

        page.Dispose();

        Assert.Equal(0, _store.ChangedSubscriberCount);
    }
}
