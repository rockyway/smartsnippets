using SmartSnippets.Core.Store;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Presentation.Tests.Fakes;
using SmartSnippets.Presentation.ViewModels;

namespace SmartSnippets.Presentation.Tests;

public sealed class SnippetEditorViewModelTests
{
    private readonly FakeSnippetStore _store = new();
    private readonly ScriptedDialogService _dialogs = new();

    private SnippetEditorViewModel NewEditor() => new(_store, _dialogs, null);

    private SnippetEditorViewModel Edit(long id) => new(_store, _dialogs, id);

    [Fact]
    public void LabelPreview_FollowsFirstTagThenFirstLine()
    {
        var vm = NewEditor();

        vm.Text = "\r\n  Hello world\r\nsecond";
        Assert.Equal("Hello world", vm.Label);
        Assert.Equal("Auto: Hello world", vm.LabelPlaceholder);
        Assert.True(vm.IsLabelAuto);

        vm.TagsInput = " work, mail";
        Assert.Equal("work", vm.Label);
        Assert.Equal("Auto: work", vm.LabelPlaceholder);

        vm.TagsInput = "";
        Assert.Equal("Hello world", vm.Label);
    }

    [Fact]
    public void TypingLabel_SetsCustom_StopsPreview()
    {
        var vm = NewEditor();
        vm.Text = "Hello";

        vm.Label = "Greeting";
        vm.Text = "Something else";

        Assert.True(vm.IsLabelCustom);
        Assert.False(vm.IsLabelAuto);
        Assert.Equal("Greeting", vm.Label);
    }

    [Fact]
    public void ClearingLabel_ReturnsToAuto()
    {
        var vm = NewEditor();
        vm.Text = "Hello";
        vm.Label = "Greeting";

        vm.Label = "";

        Assert.False(vm.IsLabelCustom);
        Assert.Equal("Hello", vm.Label);
        vm.TagsInput = "work";
        Assert.Equal("work", vm.Label);
    }

    [Fact]
    public void TagChips_ShowNormalized()
    {
        var vm = NewEditor();

        vm.TagsInput = " work, Work ,, mail ";

        Assert.Equal(["work", "mail"], vm.TagChips);
    }

    [Fact]
    public void IsDirty_IgnoresTagSpacingOnly()
    {
        var seeded = _store.SeedSnippet("text", tags: "a, b");
        var vm = Edit(seeded.Id);
        Assert.False(vm.IsDirty);

        vm.TagsInput = " a ,b,";
        Assert.False(vm.IsDirty);

        vm.TagsInput = "a, c";
        Assert.True(vm.IsDirty);

        vm.TagsInput = "a, b";
        Assert.False(vm.IsDirty);
    }

    [Fact]
    public void IsDirty_TracksTextLabelAndFolder()
    {
        var folder = _store.SeedFolder("Work");
        var seeded = _store.SeedSnippet("text");
        var vm = Edit(seeded.Id);

        vm.Text = "text ";
        Assert.True(vm.IsDirty);
        vm.Text = "text";
        Assert.False(vm.IsDirty);

        vm.Label = "Custom";
        Assert.True(vm.IsDirty);
        vm.Label = "";
        Assert.False(vm.IsDirty);

        vm.SelectedFolder = vm.FolderChoices.Single(c => c.Id == folder.Id);
        Assert.True(vm.IsDirty);
        vm.SelectedFolder = FolderChoice.Unfiled;
        Assert.False(vm.IsDirty);
    }

    [Fact]
    public async Task Save_WhitespaceText_ShowsTextError_KeepsDirty()
    {
        var vm = NewEditor();
        vm.Text = "   ";

        var saved = await vm.SaveAsync();

        Assert.False(saved);
        Assert.NotEqual(string.Empty, vm.TextError);
        Assert.True(vm.IsDirty);
        Assert.Empty(_store.Snippets);
        Assert.Equal("   ", vm.Text);
        Assert.Null(vm.SavedSnippetId);
    }

    [Fact]
    public async Task Save_StoreException_KeepsContent_ShowsError()
    {
        var vm = NewEditor();
        vm.Text = "keep me";
        vm.TagsInput = "a, b";
        _store.Failure = new StoreException(StoreFailure.DatabaseBusy, "The snippet database is busy. Please try again.");

        var saved = await vm.SaveAsync();

        Assert.False(saved);
        var error = Assert.Single(_dialogs.Errors);
        Assert.Contains("busy", error.Message);
        Assert.Equal("keep me", vm.Text);
        Assert.Equal("a, b", vm.TagsInput);
        Assert.True(vm.IsDirty);
        Assert.False(vm.IsSaving);

        _store.Failure = null;
        Assert.True(await vm.SaveAsync());
    }

    [Fact]
    public async Task Save_Ok_ClearsDirty_SetsSavedId()
    {
        var vm = NewEditor();
        var raised = new List<long>();
        vm.Saved += (_, id) => raised.Add(id);
        vm.Text = "Hello";
        vm.TagsInput = " work, Work ,, mail ";

        var saved = await vm.SaveAsync();

        Assert.True(saved);
        Assert.False(vm.IsDirty);
        var snippet = Assert.Single(_store.Snippets).Snippet;
        Assert.Equal(snippet.Id, vm.SavedSnippetId);
        Assert.Equal(snippet.Id, vm.SnippetId);
        Assert.Equal([snippet.Id], raised);
        Assert.Equal("work, mail", vm.TagsInput);
        Assert.Equal("work", vm.Label);
        Assert.True(vm.HasMetadata);

        vm.Text = "Hello again";
        Assert.True(await vm.SaveAsync());
        Assert.Single(_store.Snippets);
        Assert.Single(_store.UpdatedDrafts);
    }

    [Fact]
    public async Task InlineFolder_CreatesAndSelects()
    {
        var vm = NewEditor();
        vm.Text = "Hello";
        vm.NewFolderName = "  Clients ";

        var created = await vm.CreateFolderAsync();

        Assert.True(created);
        var folder = Assert.Single(_store.Folders);
        Assert.Equal("Clients", folder.Name);
        Assert.Equal(folder.Id, vm.SelectedFolder.Id);
        Assert.Equal(["Unfiled", "Clients"], vm.FolderChoices.Select(c => c.Name));
        Assert.Equal(string.Empty, vm.NewFolderName);
        Assert.Equal(string.Empty, vm.NewFolderError);
        Assert.True(vm.IsDirty);

        await vm.SaveAsync();
        Assert.Equal(folder.Id, _store.Snippets[0].Snippet.FolderId);
    }

    [Fact]
    public async Task InlineFolder_Duplicate_ShowsFlyoutError()
    {
        _store.SeedFolder("Clients");
        var vm = NewEditor();
        vm.NewFolderName = "clients";

        var created = await vm.CreateFolderAsync();

        Assert.False(created);
        Assert.Contains("already exists", vm.NewFolderError);
        Assert.Single(_store.Folders);
        Assert.Null(vm.SelectedFolder.Id);

        vm.NewFolderName = "";
        Assert.False(await vm.CreateFolderAsync());
        Assert.NotEqual(string.Empty, vm.NewFolderError);
    }

    [Fact]
    public void StoreFolderDeleted_ClearsFolderChoice()
    {
        var folder = _store.SeedFolder("Work");
        var seeded = _store.SeedSnippet("text", folder: "Work");
        var vm = Edit(seeded.Id);
        Assert.Equal(folder.Id, vm.SelectedFolder.Id);

        _store.DeleteFolder(folder.Id);

        Assert.Null(vm.SelectedFolder.Id);
        Assert.Equal(["Unfiled"], vm.FolderChoices.Select(c => c.Name));
        Assert.False(vm.IsDirty);
    }

    [Fact]
    public void StoreFolderDeleted_WhileDirty_StaysDirtyAndKeepsEdits()
    {
        var folder = _store.SeedFolder("Work");
        var seeded = _store.SeedSnippet("text", folder: "Work");
        var vm = Edit(seeded.Id);
        vm.Text = "edited";

        _store.DeleteFolder(folder.Id);

        Assert.Equal("edited", vm.Text);
        Assert.Null(vm.SelectedFolder.Id);
        Assert.True(vm.IsDirty);
    }

    [Fact]
    public void StoreFolderRenamed_UpdatesChoicesAndKeepsSelection()
    {
        var folder = _store.SeedFolder("Work");
        var seeded = _store.SeedSnippet("text", folder: "Work");
        var vm = Edit(seeded.Id);

        _store.RenameFolder(folder.Id, "Office");

        Assert.Equal("Office", vm.SelectedFolder.Name);
        Assert.Equal(folder.Id, vm.SelectedFolder.Id);
        Assert.False(vm.IsDirty);
    }

    [Fact]
    public void SnippetDeletedElsewhere_MarksDeletedAndRaisesRemoved()
    {
        var seeded = _store.SeedSnippet("text");
        var vm = Edit(seeded.Id);
        var removed = 0;
        vm.Removed += (_, _) => removed++;

        _store.DeleteSnippet(seeded.Id);

        Assert.True(vm.IsDeleted);
        Assert.Equal(1, removed);
        Assert.False(vm.SaveCommand.CanExecute(null));
    }

    [Fact]
    public async Task DoubleSave_Blocked()
    {
        var vm = NewEditor();
        vm.Text = "Hello";
        var gate = new TaskCompletionSource();
        _dialogs.ErrorGate = gate;
        _store.Failure = new StoreException(StoreFailure.DatabaseBusy, "busy");

        var first = vm.SaveAsync();
        Assert.True(vm.IsSaving);
        var second = await vm.SaveAsync();

        Assert.False(second);
        Assert.Single(_store.CreatedDrafts);
        gate.SetResult();
        Assert.False(await first);
        Assert.False(vm.IsSaving);
    }

    [Fact]
    public async Task DoubleSave_Reentrant_CreatesOnce()
    {
        var vm = NewEditor();
        vm.Text = "Hello";
        Task<bool>? inner = null;
        _store.OnSaveCalled = () => inner ??= vm.SaveAsync();

        Assert.True(await vm.SaveAsync());

        Assert.False(await inner!);
        Assert.Single(_store.Snippets);
    }

    [Fact]
    public async Task TextNeverTrimmedOnSave()
    {
        var vm = NewEditor();
        const string text = "  Hello\r\n  wörld \U0001F600 \r\n\n  ";
        vm.Text = text;

        await vm.SaveAsync();

        Assert.Equal(text, Assert.Single(_store.CreatedDrafts).Text);
        Assert.Equal(text, _store.Snippets[0].Snippet.Text);
        Assert.Equal(text, vm.Text);
    }

    [Fact]
    public async Task Revert_Dirty_ConfirmsThenRestores()
    {
        var seeded = _store.SeedSnippet("original", label: "Mine", tags: "a");
        var vm = Edit(seeded.Id);
        vm.Text = "changed";
        vm.TagsInput = "z";

        _dialogs.ConfirmAnswers.Enqueue(false);
        Assert.False(await vm.RevertAsync());
        Assert.Equal("changed", vm.Text);

        _dialogs.ConfirmAnswers.Enqueue(true);
        Assert.True(await vm.RevertAsync());
        Assert.Equal("original", vm.Text);
        Assert.Equal("a", vm.TagsInput);
        Assert.Equal("Mine", vm.Label);
        Assert.True(vm.IsLabelCustom);
        Assert.False(vm.IsDirty);
    }

    [Fact]
    public async Task Revert_Clean_DoesNotAsk()
    {
        var vm = Edit(_store.SeedSnippet("original").Id);

        Assert.True(await vm.RevertAsync());
        Assert.Empty(_dialogs.Confirms);
    }

    [Fact]
    public void Metadata_ShowsDatesAndUsage()
    {
        var seeded = _store.SeedSnippet("text");
        var vm = Edit(seeded.Id);
        Assert.NotEqual(string.Empty, vm.CreatedDisplay);
        Assert.Equal(SnippetEditorViewModel.NeverDisplay, vm.LastUsedDisplay);
        Assert.Equal("0", vm.UsageCountDisplay);

        _store.RecordUsage(seeded.Id);

        Assert.Equal("1", vm.UsageCountDisplay);
        Assert.NotEqual(SnippetEditorViewModel.NeverDisplay, vm.LastUsedDisplay);
    }

    [Fact]
    public void Dispose_Unsubscribes()
    {
        var vm = NewEditor();
        Assert.Equal(1, _store.ChangedSubscriberCount);

        vm.Dispose();

        Assert.Equal(0, _store.ChangedSubscriberCount);
    }
}
