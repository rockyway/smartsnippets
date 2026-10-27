using SmartSnippets.Core.Store;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Presentation.Tests.Fakes;
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

        Assert.Single(_dialogs.SaveDiscardPrompts);
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

        Assert.Equal("Delete 'Bravo'? This cannot be undone.", _dialogs.Confirms.Single().Message);
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
