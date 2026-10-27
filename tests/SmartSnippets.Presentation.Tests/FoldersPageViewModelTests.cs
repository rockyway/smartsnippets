using SmartSnippets.Core.Store;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Presentation.Tests.Fakes;
using SmartSnippets.Presentation.ViewModels;

namespace SmartSnippets.Presentation.Tests;

public sealed class FoldersPageViewModelTests
{
    private readonly FakeSnippetStore _store = new();
    private readonly ScriptedDialogService _dialogs = new();

    private FoldersPageViewModel Create() => new(_store, _dialogs);

    [Fact]
    public async Task Create_Duplicate_CaseInsensitive_ShowsError()
    {
        _store.SeedFolder("Clients");
        var vm = Create();
        vm.NewFolderName = "clients";

        await vm.CreateAsync();

        Assert.Equal("A folder named 'clients' already exists.", vm.NewFolderError);
        Assert.Single(vm.Rows);
        Assert.Equal("clients", vm.NewFolderName);
    }

    [Fact]
    public async Task Create_Empty_ShowsError()
    {
        var vm = Create();
        vm.NewFolderName = "   ";

        await vm.CreateAsync();

        Assert.NotEqual(string.Empty, vm.NewFolderError);
        Assert.Empty(vm.Rows);
        Assert.True(vm.IsEmpty);
    }

    [Fact]
    public async Task Create_Ok_AddsSortedRowAndClearsBox()
    {
        _store.SeedFolder("Work");
        var vm = Create();
        vm.NewFolderName = " alpha ";

        await vm.CreateAsync();

        Assert.Equal(["alpha", "Work"], vm.Rows.Select(r => r.Name));
        Assert.Equal(string.Empty, vm.NewFolderName);
        Assert.Equal(string.Empty, vm.NewFolderError);
    }

    [Fact]
    public async Task Create_StoreException_ShowsDialog()
    {
        var vm = Create();
        vm.NewFolderName = "x";
        _store.Failure = new StoreException(StoreFailure.DatabaseBusy, "busy");

        await vm.CreateAsync();

        Assert.Single(_dialogs.Errors);
        Assert.Equal("x", vm.NewFolderName);
    }

    [Fact]
    public async Task Rename_ToSameNameDifferentCase_Allowed()
    {
        _store.SeedFolder("Clients");
        var vm = Create();
        var row = vm.Rows[0];
        vm.BeginRename(row);
        row.EditName = "CLIENTS";

        Assert.True(await vm.CommitRenameAsync(row));

        Assert.False(row.IsRenaming);
        Assert.Equal("CLIENTS", row.Name);
        Assert.Equal("CLIENTS", _store.Folders[0].Name);
    }

    [Fact]
    public async Task Rename_ToOtherExisting_ShowsError()
    {
        _store.SeedFolder("Clients");
        _store.SeedFolder("Work");
        var vm = Create();
        var row = vm.Rows.Single(r => r.Name == "Work");
        vm.BeginRename(row);
        row.EditName = "clients";

        Assert.False(await vm.CommitRenameAsync(row));

        Assert.True(row.IsRenaming);
        Assert.Contains("already exists", row.RenameError);
        Assert.Equal("Work", row.Name);
    }

    [Fact]
    public async Task Rename_Empty_ShowsError_AndCancelRestores()
    {
        _store.SeedFolder("Work");
        var vm = Create();
        var row = vm.Rows[0];
        vm.BeginRename(row);
        row.EditName = " ";

        Assert.False(await vm.CommitRenameAsync(row));
        Assert.NotEqual(string.Empty, row.RenameError);

        vm.CancelRename(row);
        Assert.False(row.IsRenaming);
        Assert.Equal("Work", row.EditName);
        Assert.Equal(string.Empty, row.RenameError);
    }

    [Fact]
    public async Task Rename_Unchanged_ClosesWithoutStoreCall()
    {
        _store.SeedFolder("Work");
        var before = _store.Raised.Count;
        var vm = Create();
        var row = vm.Rows[0];
        vm.BeginRename(row);

        Assert.True(await vm.CommitRenameAsync(row));

        Assert.False(row.IsRenaming);
        Assert.Equal(before, _store.Raised.Count);
    }

    [Fact]
    public async Task Delete_ConfirmTextStatesCount()
    {
        _store.SeedSnippet("a", folder: "Work");
        _store.SeedSnippet("b", folder: "Work");
        _store.SeedFolder("Solo");
        _store.SeedSnippet("c", folder: "Solo");
        var vm = Create();
        _dialogs.ConfirmAnswers.Enqueue(false);
        _dialogs.ConfirmAnswers.Enqueue(false);

        await vm.DeleteAsync(vm.Rows.Single(r => r.Name == "Work"));
        await vm.DeleteAsync(vm.Rows.Single(r => r.Name == "Solo"));

        Assert.Equal("Delete 'Work'? 2 snippets will move to Unfiled.", _dialogs.Confirms[0].Message);
        Assert.Equal("Delete 'Solo'? 1 snippet will move to Unfiled.", _dialogs.Confirms[1].Message);
        Assert.Equal(2, vm.Rows.Count);
    }

    [Fact]
    public async Task Delete_Confirmed_SnippetsUnfiled()
    {
        _store.SeedSnippet("a", folder: "Work");
        _store.SeedSnippet("b", folder: "Work");
        var vm = Create();
        _dialogs.ConfirmAnswers.Enqueue(true);

        await vm.DeleteAsync(vm.Rows[0]);

        Assert.Empty(vm.Rows);
        Assert.Empty(_store.Folders);
        Assert.All(_store.Snippets, v =>
        {
            Assert.Null(v.Snippet.FolderId);
            Assert.Null(v.FolderName);
        });
    }

    [Fact]
    public async Task Delete_StoreException_ShowsDialogAndKeepsRow()
    {
        _store.SeedFolder("Work");
        var vm = Create();
        _dialogs.ConfirmAnswers.Enqueue(true);
        _store.Failure = new StoreException(StoreFailure.DatabaseBusy, "busy");

        await vm.DeleteAsync(vm.Rows[0]);

        Assert.Single(_dialogs.Errors);
        Assert.Single(vm.Rows);
    }

    [Fact]
    public void Counts_UpdateOnSnippetChanges()
    {
        var folder = _store.SeedFolder("Work");
        var vm = Create();
        Assert.Equal(0, vm.Rows[0].SnippetCount);

        var s = _store.SeedSnippet("a", folder: "Work");
        _store.SeedSnippet("b", folder: "Work");
        Assert.Equal(2, vm.Rows[0].SnippetCount);
        Assert.Equal("2 snippets", vm.Rows[0].SnippetCountDisplay);

        _store.DeleteSnippet(s.Id);
        Assert.Equal(1, vm.Rows[0].SnippetCount);
        Assert.Equal("1 snippet", vm.Rows[0].SnippetCountDisplay);

        _store.UpdateSnippet(_store.Snippets[0].Snippet.Id, new(_store.Snippets[0].Snippet.Text, "", false, null, ""));
        Assert.Equal(0, vm.Rows[0].SnippetCount);
        Assert.Equal(folder.Id, vm.Rows[0].Id);
    }

    [Fact]
    public void ExternalChanges_RefreshRowsAndKeepRowInstances()
    {
        var work = _store.SeedFolder("Work");
        var vm = Create();
        var row = vm.Rows[0];

        _store.RenameFolder(work.Id, "Office");
        _store.SeedFolder("Another");

        Assert.Equal(["Another", "Office"], vm.Rows.Select(r => r.Name));
        Assert.Same(row, vm.Rows.Single(r => r.Id == work.Id));
    }

    [Fact]
    public async Task CanLeave_RenameDirty_AsksSaveDiscardCancel()
    {
        _store.SeedFolder("Work");
        var vm = Create();
        var row = vm.Rows[0];
        vm.BeginRename(row);
        row.EditName = "Office";

        _dialogs.ChoiceAnswers.Enqueue(SaveChoice.Cancel);
        Assert.False(await vm.CanLeaveAsync());
        Assert.True(row.IsRenaming);

        _dialogs.ChoiceAnswers.Enqueue(SaveChoice.Save);
        Assert.True(await vm.CanLeaveAsync());
        Assert.Equal("Office", _store.Folders[0].Name);
        Assert.False(vm.IsRenameInProgress);
    }

    [Fact]
    public async Task CanLeave_RenameDirty_DiscardAndUnchanged()
    {
        _store.SeedFolder("Work");
        var vm = Create();
        var row = vm.Rows[0];
        vm.BeginRename(row);

        Assert.True(await vm.CanLeaveAsync());
        Assert.False(row.IsRenaming);
        Assert.Empty(_dialogs.SaveDiscardPrompts);

        vm.BeginRename(row);
        row.EditName = "Office";
        _dialogs.ChoiceAnswers.Enqueue(SaveChoice.Discard);
        Assert.True(await vm.CanLeaveAsync());
        Assert.Equal("Work", _store.Folders[0].Name);
        Assert.False(row.IsRenaming);
    }

    [Fact]
    public async Task CanLeave_SaveFails_Stays()
    {
        _store.SeedFolder("Work");
        _store.SeedFolder("Home");
        var vm = Create();
        var row = vm.Rows.Single(r => r.Name == "Work");
        vm.BeginRename(row);
        row.EditName = "home";
        _dialogs.ChoiceAnswers.Enqueue(SaveChoice.Save);

        Assert.False(await vm.CanLeaveAsync());
        Assert.True(row.IsRenaming);
    }

    [Fact]
    public void BeginRename_CancelsOtherRename()
    {
        _store.SeedFolder("A");
        _store.SeedFolder("B");
        var vm = Create();

        vm.BeginRename(vm.Rows[0]);
        vm.BeginRename(vm.Rows[1]);

        Assert.False(vm.Rows[0].IsRenaming);
        Assert.True(vm.Rows[1].IsRenaming);
    }

    [Fact]
    public void Dispose_Unsubscribes()
    {
        var vm = Create();
        Assert.Equal(1, _store.ChangedSubscriberCount);

        vm.Dispose();

        Assert.Equal(0, _store.ChangedSubscriberCount);
    }
}
