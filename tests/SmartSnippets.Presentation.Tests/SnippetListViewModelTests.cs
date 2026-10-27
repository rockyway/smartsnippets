using SmartSnippets.Presentation.Tests.Fakes;
using SmartSnippets.Presentation.ViewModels;

namespace SmartSnippets.Presentation.Tests;

public sealed class SnippetListViewModelTests
{
    private readonly FakeSnippetStore _store = new();

    [Fact]
    public void Search_FiltersAcrossFields()
    {
        _store.SeedSnippet("alpha body", label: "One", folder: "Clients", tags: "mail");
        _store.SeedSnippet("beta body", label: "Two", tags: "work");
        _store.SeedSnippet("gamma", label: "Three", folder: "Home");
        var vm = new SnippetListViewModel(_store);

        vm.Query = "clie mail";
        Assert.Equal(["One"], vm.Items.Select(r => r.Label));

        vm.Query = "BODY";
        Assert.Equal(["One", "Two"], vm.Items.Select(r => r.Label));

        vm.Query = "";
        Assert.Equal(3, vm.Items.Count);
    }

    [Fact]
    public void OrderedByLabelThenId()
    {
        var b = _store.SeedSnippet("x", label: "banana");
        var a = _store.SeedSnippet("x", label: "Apple");
        var b2 = _store.SeedSnippet("x", label: "Banana");
        var vm = new SnippetListViewModel(_store);

        Assert.Equal([a.Id, b.Id, b2.Id], vm.Items.Select(r => r.Id));
    }

    [Fact]
    public void Unfiled_DisplayedForNullFolder()
    {
        _store.SeedSnippet("x", label: "A", tags: "one, two");
        _store.SeedSnippet("y", label: "B", folder: "Work");
        var vm = new SnippetListViewModel(_store);

        Assert.Equal("Unfiled", vm.Items[0].FolderDisplay);
        Assert.Equal("one, two", vm.Items[0].TagsDisplay);
        Assert.Equal("Work", vm.Items[1].FolderDisplay);
        Assert.Equal(string.Empty, vm.Items[1].TagsDisplay);
    }

    [Fact]
    public void Changed_RebuildsAndKeepsSelection()
    {
        var a = _store.SeedSnippet("x", label: "Alpha");
        var c = _store.SeedSnippet("x", label: "Charlie");
        var vm = new SnippetListViewModel(_store);
        vm.SelectedId = c.Id;

        var b = _store.SeedSnippet("x", label: "Bravo");

        Assert.Equal([a.Id, b.Id, c.Id], vm.Items.Select(r => r.Id));
        Assert.Equal(c.Id, vm.SelectedId);
        Assert.Equal(c.Id, vm.SelectedItem?.Id);

        _store.DeleteSnippet(c.Id);
        Assert.Null(vm.SelectedId);
    }

    [Fact]
    public void EmptyStates_Distinguished()
    {
        var vm = new SnippetListViewModel(_store);
        Assert.True(vm.IsEmpty);
        Assert.Equal("No snippets yet", vm.EmptyState);

        _store.SeedSnippet("hello", label: "Hello");
        Assert.False(vm.IsEmpty);
        Assert.Equal(string.Empty, vm.EmptyState);

        vm.Query = "zzz";
        Assert.True(vm.IsEmpty);
        Assert.Equal("No matches for 'zzz'", vm.EmptyState);
    }

    [Fact]
    public void FolderRename_UpdatesRowsImmediately()
    {
        var folder = _store.SeedFolder("Clients");
        _store.SeedSnippet("x", label: "A", folder: "Clients");
        var vm = new SnippetListViewModel(_store);
        Assert.Equal("Clients", vm.Items[0].FolderDisplay);

        _store.RenameFolder(folder.Id, "Customers");
        Assert.Equal("Customers", vm.Items[0].FolderDisplay);

        _store.DeleteFolder(folder.Id);
        Assert.Equal("Unfiled", vm.Items[0].FolderDisplay);
    }

    [Fact]
    public void FilteredOutSelection_IsClearedAndRebuildSkippedWhenUnchanged()
    {
        var a = _store.SeedSnippet("x", label: "Alpha");
        _store.SeedSnippet("x", label: "Beta");
        var vm = new SnippetListViewModel(_store);
        vm.SelectedId = a.Id;

        vm.Query = "beta";
        Assert.Null(vm.SelectedId);

        var first = vm.Items[0];
        _store.RecordUsage(a.Id);
        Assert.Same(first, vm.Items[0]);
    }

    [Fact]
    public void Dispose_Unsubscribes()
    {
        var vm = new SnippetListViewModel(_store);
        Assert.Equal(1, _store.ChangedSubscriberCount);

        vm.Dispose();

        Assert.Equal(0, _store.ChangedSubscriberCount);
    }
}
