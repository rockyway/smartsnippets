using SmartSnippets.Core.Store;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Presentation.Tests.Fakes;
using SmartSnippets.Presentation.Text;
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
    public async Task FolderSelection_NullFeedbackDuringSwapAndRefresh_DoesNotMoveSnippet()
    {
        var a = _store.SeedSnippet("first", folder: "First");
        var b = _store.SeedSnippet("second", folder: "Second", tags: "work");
        using var page = new SnippetsPageViewModel(_store, _dialogs);
        Assert.True(await page.RequestSelectAsync(a.Id));
        // Back in the narrow view only hides the current editor; it does not clear the VM.
        Assert.True(await page.RequestSelectAsync(b.Id));
        var editor = page.Editor!;
        editor.SelectedFolder = null!; // transient ComboBox SelectedItem feedback
        Assert.Equal(b.FolderId, editor.SelectedFolder.Id);
        Assert.False(editor.IsDirty);
        editor.FolderChoices.CollectionChanged += (_, _) => editor.SelectedFolder = null!;
        _store.SeedFolder("Another");
        Assert.Equal(b.FolderId, editor.SelectedFolder.Id);
        Assert.Contains(editor.FolderChoices, choice => ReferenceEquals(choice, editor.SelectedFolder));
        Assert.False(editor.IsDirty);
        Assert.True(await editor.SaveAsync());
        Assert.Equal(b.FolderId, _store.Snippets.Single(s => s.Snippet.Id == b.Id).Snippet.FolderId);
        Assert.Equal(b.FolderId, _store.UpdatedDrafts.Last().Draft.FolderId);
    }

    [Fact]
    public void FolderSelection_ExplicitUnfiled_IsAnEdit()
    {
        var b = _store.SeedSnippet("second", folder: "Second");
        using var editor = Edit(b.Id);
        editor.SelectedFolder = editor.FolderChoices.Single(c => c.Id is null);
        Assert.Null(editor.SelectedFolder.Id);
        Assert.True(editor.IsDirty);
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
        Assert.Empty(_dialogs.Errors);
        Assert.True(vm.HasSaveFailure);
        Assert.Equal(UiText.SaveFailed, vm.SaveFailureMessage);
        Assert.Equal("keep me", vm.Text);
        Assert.Equal("a, b", vm.TagsInput);
        Assert.True(vm.IsDirty);
        Assert.False(vm.IsSaving);

        _store.Failure = null;
        Assert.True(vm.RetrySaveCommand.CanExecute(null));
        await vm.RetrySaveCommand.ExecuteAsync(null);
        Assert.False(vm.HasSaveFailure);
        Assert.Equal(string.Empty, vm.SaveFailureMessage);
        Assert.False(vm.IsDirty);
        Assert.Single(_store.Snippets);
    }

    [Fact]
    public async Task SaveFailure_StaysUntilSuccess_AndDiscardClearsIt()
    {
        var vm = NewEditor();
        vm.Text = "keep me";
        _store.Failure = new StoreException(StoreFailure.DatabaseBusy, "busy");
        await vm.SaveAsync();
        await vm.SaveAsync();
        Assert.True(vm.HasSaveFailure);

        vm.DiscardCommand.Execute(null);

        Assert.False(vm.HasSaveFailure);
        Assert.Equal(string.Empty, vm.Text);
    }

    [Fact]
    public void CopyText_UsesClipboard_AndIsDisabledWithoutOne()
    {
        var clipboard = new FakeClipboardService();
        var vm = new SnippetEditorViewModel(_store, _dialogs, null, clipboard);
        vm.Text = "  copy me\r\n";
        Assert.True(vm.CopyTextCommand.CanExecute(null));

        vm.CopyTextCommand.Execute(null);

        Assert.Equal(["  copy me\r\n"], clipboard.Texts);
        Assert.False(NewEditor().CopyTextCommand.CanExecute(null));
    }

    [Fact]
    public void AutoLabelHint_OnlyWhileAutomatic()
    {
        var vm = NewEditor();
        var changed = new List<string?>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
        Assert.Equal(UiText.AutoLabelHint, vm.AutoLabelHint);

        vm.Label = "Mine";

        Assert.Equal(string.Empty, vm.AutoLabelHint);
        Assert.Contains(nameof(SnippetEditorViewModel.AutoLabelHint), changed);

        vm.IsLabelCustom = false;
        Assert.Equal(UiText.AutoLabelHint, vm.AutoLabelHint);
    }

    [Fact]
    public void TurningCustomLabelOff_LabelFollowsSuggestionAgain()
    {
        var vm = NewEditor();
        vm.Text = "Hello";
        vm.Label = "Mine";
        Assert.True(vm.IsLabelCustom);

        vm.IsLabelCustom = false;

        Assert.Equal("Hello", vm.Label);
        vm.TagsInput = "work";
        Assert.Equal("work", vm.Label);
    }

    [Fact]
    public async Task InlineFolder_StartCancelCreate()
    {
        var vm = NewEditor();
        Assert.False(vm.IsCreatingFolder);

        vm.StartNewFolderCommand.Execute(null);
        Assert.True(vm.IsCreatingFolder);
        vm.NewFolderName = "Half";
        vm.CancelNewFolderCommand.Execute(null);
        Assert.False(vm.IsCreatingFolder);
        Assert.Equal(string.Empty, vm.NewFolderName);
        Assert.Empty(_store.Folders);

        vm.StartNewFolderCommand.Execute(null);
        vm.NewFolderName = "Clients";
        await vm.CreateFolderCommand.ExecuteAsync(null);
        Assert.False(vm.IsCreatingFolder);
        Assert.Equal("Clients", vm.SelectedFolder.Name);
    }

    [Fact]
    public async Task InlineFolder_Duplicate_KeepsBoxOpenWithDesignText_StartClearsError()
    {
        _store.SeedFolder("Clients");
        var vm = NewEditor();
        vm.StartNewFolderCommand.Execute(null);
        vm.NewFolderName = "CLIENTS";

        Assert.False(await vm.CreateFolderAsync());

        Assert.True(vm.IsCreatingFolder);
        Assert.Equal(UiText.FolderNameTaken, vm.NewFolderError);

        vm.CancelNewFolderCommand.Execute(null);
        vm.StartNewFolderCommand.Execute(null);
        Assert.Equal(string.Empty, vm.NewFolderError);
    }

    [Fact]
    public void Delete_OnlyForSavedSnippet_RaisesRequest()
    {
        Assert.False(NewEditor().DeleteCommand.CanExecute(null));
        var seeded = _store.SeedSnippet("text");
        var vm = Edit(seeded.Id);
        var requests = 0;
        vm.DeleteRequested += (_, _) => requests++;
        Assert.True(vm.DeleteCommand.CanExecute(null));

        vm.DeleteCommand.Execute(null);
        Assert.Equal(1, requests);

        _store.DeleteSnippet(seeded.Id);
        Assert.False(vm.DeleteCommand.CanExecute(null));
    }

    [Fact]
    public async Task Delete_BecomesAvailableAfterFirstSave()
    {
        var vm = NewEditor();
        vm.Text = "x";
        await vm.SaveAsync();

        Assert.True(vm.DeleteCommand.CanExecute(null));
    }

    [Fact]
    public void Stats_FormatDatesAndUsage()
    {
        var seeded = _store.SeedSnippet("text");
        var now = new DateTimeOffset(2026, 3, 3, 12, 0, 0, TimeSpan.Zero);
        var vm = new SnippetEditorViewModel(_store, _dialogs, seeded.Id, null, new FixedTimeProvider(now));

        Assert.Equal("1 Jan 2026", vm.CreatedDisplay);
        Assert.Equal("1 Jan 2026", vm.UpdatedDisplay);
        Assert.Equal("Never", vm.LastUsedDisplay);
        Assert.Equal(UiText.UsedTimes(0), vm.UsageCountDisplay);
    }

    [Fact]
    public void Stats_LastUsed_TodayShowsTime_OtherDayShowsDate()
    {
        var seeded = _store.SeedSnippet("text");
        _store.RecordUsage(seeded.Id);
        var usedAt = _store.Snippets[0].Snippet.LastUsedAt!.Value;
        var time = new FixedTimeProvider(usedAt.AddMinutes(5));
        var vm = new SnippetEditorViewModel(_store, _dialogs, seeded.Id, null, time);

        Assert.Equal($"Today, {usedAt:HH:mm}", vm.LastUsedDisplay);
        Assert.Equal("Once", vm.UsageCountDisplay);

        _store.RecordUsage(seeded.Id);
        var later = new SnippetEditorViewModel(_store, _dialogs, seeded.Id, null, new FixedTimeProvider(usedAt.AddDays(40)));
        Assert.Equal(usedAt.AddMinutes(1).ToString("d MMM yyyy", System.Globalization.CultureInfo.InvariantCulture), later.LastUsedDisplay);
        Assert.Equal("2 times", later.UsageCountDisplay);
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
        Assert.Equal(UiText.FolderNameTaken, vm.NewFolderError);
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
        _store.Failure = new StoreException(StoreFailure.DatabaseBusy, "busy");
        Task<bool>? inner = null;
        var savingDuringFirst = false;
        _store.OnSaveCalled = () =>
        {
            savingDuringFirst = vm.IsSaving;
            inner ??= vm.SaveAsync();
        };

        var first = await vm.SaveAsync();

        Assert.True(savingDuringFirst);
        Assert.False(first);
        Assert.False(await inner!);
        Assert.Single(_store.CreatedDrafts);
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
    public void Discard_Dirty_RestoresWithoutDialog()
    {
        var seeded = _store.SeedSnippet("original", label: "Mine", tags: "a");
        var vm = Edit(seeded.Id);
        vm.Text = "changed";
        vm.TagsInput = "z";

        vm.DiscardCommand.Execute(null);

        Assert.Equal("original", vm.Text);
        Assert.Equal("a", vm.TagsInput);
        Assert.Equal("Mine", vm.Label);
        Assert.True(vm.IsLabelCustom);
        Assert.False(vm.IsDirty);
        Assert.Empty(_dialogs.Confirms);
        Assert.Empty(_dialogs.SaveDiscardPrompts);
    }

    [Fact]
    public void Discard_NewSnippet_Blanks()
    {
        var vm = NewEditor();
        vm.Text = "typed";

        vm.DiscardCommand.Execute(null);

        Assert.Equal(string.Empty, vm.Text);
        Assert.False(vm.IsDirty);
        Assert.Empty(_dialogs.Confirms);
    }

    [Fact]
    public void Metadata_ShowsDatesAndUsage()
    {
        var seeded = _store.SeedSnippet("text");
        var vm = Edit(seeded.Id);
        Assert.NotEqual(string.Empty, vm.CreatedDisplay);
        Assert.Equal(SnippetEditorViewModel.NeverDisplay, vm.LastUsedDisplay);
        Assert.Equal(UiText.UsedTimes(0), vm.UsageCountDisplay);

        _store.RecordUsage(seeded.Id);

        Assert.Equal(UiText.UsedTimes(1), vm.UsageCountDisplay);
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

    [Fact]
    public void CustomLabel_ToggleOffThenOn_RestoresCustomLabel()
    {
        var snippet = _store.SeedSnippet("Hello", label: "Refund reply", tags: "support");
        var vm = Edit(snippet.Id);
        Assert.True(vm.IsLabelCustom);

        vm.IsLabelCustom = false;
        Assert.Equal("support", vm.Label);

        vm.IsLabelCustom = true;
        Assert.Equal("Refund reply", vm.Label);
        Assert.False(vm.IsDirty);
    }

    [Fact]
    public void CustomLabel_TypingAfterAuto_KeepsTypedLabel()
    {
        var snippet = _store.SeedSnippet("Hello", label: "Refund reply", tags: "support");
        var vm = Edit(snippet.Id);

        vm.IsLabelCustom = false;
        vm.Label = "Typed";

        Assert.True(vm.IsLabelCustom);
        Assert.Equal("Typed", vm.Label);
    }

    [Fact]
    public async Task ConfirmDiscard_Declined_KeepsEdits()
    {
        var snippet = _store.SeedSnippet("Hello", label: "Greeting");
        var vm = Edit(snippet.Id);
        vm.Text = "Edited";
        _dialogs.ConfirmAnswers.Enqueue(false);

        Assert.False(await vm.ConfirmDiscardAsync());

        Assert.Equal("Edited", vm.Text);
        Assert.True(vm.IsDirty);
        var prompt = Assert.Single(_dialogs.Confirms);
        Assert.Equal((UiText.DiscardTitle, UiText.DiscardBody, UiText.Discard), prompt);
    }

    [Fact]
    public async Task ConfirmDiscard_Accepted_Resets()
    {
        var snippet = _store.SeedSnippet("Hello", label: "Greeting");
        var vm = Edit(snippet.Id);
        vm.Text = "Edited";
        _dialogs.ConfirmAnswers.Enqueue(true);

        Assert.True(await vm.ConfirmDiscardAsync());

        Assert.Equal("Hello", vm.Text);
        Assert.False(vm.IsDirty);
    }

    [Fact]
    public async Task ConfirmDiscard_NotDirty_NoDialog()
    {
        var snippet = _store.SeedSnippet("Hello", label: "Greeting");
        var vm = Edit(snippet.Id);

        Assert.False(await vm.ConfirmDiscardAsync());
        Assert.Empty(_dialogs.Confirms);
    }
}
