using SmartSnippets.Core.Domain;
using SmartSnippets.Presentation.Tests.Fakes;
using SmartSnippets.Presentation.Text;
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

        Assert.Equal(UiText.RowMeta(UiText.Unfiled, 0), vm.Items[0].Meta);
        Assert.Equal(UiText.RowMeta("Work", 0), vm.Items[1].Meta);
    }

    [Fact]
    public void Meta_Wording_ForNeverOnceAndMany()
    {
        var a = _store.SeedSnippet("x", label: "A", folder: "Support");
        var b = _store.SeedSnippet("x", label: "B");
        var c = _store.SeedSnippet("x", label: "C", folder: "Support");
        _store.RecordUsage(b.Id);
        for (var i = 0; i < 31; i++)
        {
            _store.RecordUsage(c.Id);
        }

        var vm = new SnippetListViewModel(_store);

        Assert.Equal("Support, never used", vm.Items.Single(r => r.Id == a.Id).Meta);
        Assert.Equal("Unfiled, used once", vm.Items.Single(r => r.Id == b.Id).Meta);
        Assert.Equal("Support, used 31 times", vm.Items.Single(r => r.Id == c.Id).Meta);
    }

    [Fact]
    public void Preview_FirstNonBlankLine_CollapsedWhitespace()
    {
        _store.SeedSnippet("\r\n   \r\n  Hello \t  big  world  \r\nsecond line", label: "A");
        var vm = new SnippetListViewModel(_store);

        Assert.Equal("Hello big world", vm.Items[0].Preview);
    }

    [Fact]
    public void Preview_CutAt80WithEllipsis_OnlyWhenLonger()
    {
        _store.SeedSnippet(new string('a', 80), label: "Exact");
        _store.SeedSnippet(new string('b', 81), label: "Long");
        var vm = new SnippetListViewModel(_store);

        Assert.Equal(new string('a', 80), vm.Items.Single(r => r.Label == "Exact").Preview);
        Assert.Equal(new string('b', 80) + "…", vm.Items.Single(r => r.Label == "Long").Preview);
    }

    [Fact]
    public void Browse_ItemsOrderedAllFoldersUnfiled_WithCounts_IgnoringSearch()
    {
        _store.SeedSnippet("1", label: "One", folder: "Zeta");
        _store.SeedSnippet("2", label: "Two", folder: "Alpha");
        _store.SeedSnippet("3", label: "Three", folder: "Alpha");
        _store.SeedSnippet("4", label: "Four");
        var vm = new SnippetListViewModel(_store);

        vm.Query = "zzz-nothing";

        Assert.Equal(
            [
                new BrowseItemViewModel(BrowseKind.All, null, UiText.BrowseAll, 4),
                new BrowseItemViewModel(BrowseKind.Folder, _store.Folders[0].Id, "Alpha", 2),
                new BrowseItemViewModel(BrowseKind.Folder, _store.Folders[1].Id, "Zeta", 1),
                new BrowseItemViewModel(BrowseKind.Unfiled, null, UiText.Unfiled, 1),
            ],
            vm.BrowseItems);
        Assert.Equal(BrowseKind.All, vm.SelectedBrowse!.Kind);
    }

    [Fact]
    public void Browse_EachKindFiltersItems()
    {
        _store.SeedSnippet("1", label: "One", folder: "Work");
        _store.SeedSnippet("2", label: "Two", folder: "Home");
        _store.SeedSnippet("3", label: "Three");
        var vm = new SnippetListViewModel(_store);
        Assert.Equal(3, vm.Items.Count);

        vm.SelectedBrowse = vm.BrowseItems.Single(b => b.Name == "Work");
        Assert.Equal(["One"], vm.Items.Select(r => r.Label));

        vm.SelectedBrowse = vm.BrowseItems.Single(b => b.Kind == BrowseKind.Unfiled);
        Assert.Equal(["Three"], vm.Items.Select(r => r.Label));

        vm.SelectedBrowse = vm.BrowseItems.Single(b => b.Kind == BrowseKind.All);
        Assert.Equal(3, vm.Items.Count);

        vm.SelectedBrowse = null;
        Assert.Equal(BrowseKind.All, vm.SelectedBrowse!.Kind);
        Assert.Equal(3, vm.Items.Count);
    }

    [Fact]
    public void Browse_SearchRunsWithinScope_AndEmptyStateUsesQuery()
    {
        _store.SeedSnippet("refund policy", label: "Refund", folder: "Support");
        _store.SeedSnippet("refund email", label: "Mail", folder: "Sales");
        var vm = new SnippetListViewModel(_store);
        vm.SelectedBrowse = vm.BrowseItems.Single(b => b.Name == "Support");

        vm.Query = "refund";
        Assert.Equal(["Refund"], vm.Items.Select(r => r.Label));

        vm.Query = "email";
        Assert.Empty(vm.Items);
        Assert.Equal(UiText.NoMatches("email"), vm.EmptyState);
    }

    [Fact]
    public void Browse_CountsFollowCreateMoveDelete()
    {
        var folder = _store.SeedFolder("Work");
        var vm = new SnippetListViewModel(_store);
        int Count(BrowseKind kind, long? id = null) => vm.BrowseItems.Single(b => b.Kind == kind && b.FolderId == id).Count;

        var s = _store.SeedSnippet("x", label: "A");
        Assert.Equal(1, Count(BrowseKind.All));
        Assert.Equal(1, Count(BrowseKind.Unfiled));
        Assert.Equal(0, Count(BrowseKind.Folder, folder.Id));

        _store.UpdateSnippet(s.Id, new SnippetDraft("x", "A", true, folder.Id, ""));
        Assert.Equal(0, Count(BrowseKind.Unfiled));
        Assert.Equal(1, Count(BrowseKind.Folder, folder.Id));

        _store.DeleteSnippet(s.Id);
        Assert.Equal(0, Count(BrowseKind.All));
        Assert.Equal(0, Count(BrowseKind.Folder, folder.Id));
    }

    [Fact]
    public void Browse_SelectionRepointedOnRebuild_AndFallsBackToAllWhenFolderDeleted()
    {
        var folder = _store.SeedFolder("Work");
        _store.SeedSnippet("x", label: "A", folder: "Work");
        var vm = new SnippetListViewModel(_store);
        vm.SelectedBrowse = vm.BrowseItems.Single(b => b.FolderId == folder.Id);
        var old = vm.SelectedBrowse;

        _store.SeedSnippet("y", label: "B", folder: "Work");

        Assert.NotSame(old, vm.SelectedBrowse);
        Assert.Equal(2, vm.SelectedBrowse!.Count);
        Assert.Same(vm.SelectedBrowse, vm.BrowseItems.Single(b => b.FolderId == folder.Id));
        Assert.Equal(2, vm.Items.Count);

        _store.DeleteFolder(folder.Id);

        Assert.Equal(BrowseKind.All, vm.SelectedBrowse!.Kind);
        Assert.Equal(2, vm.Items.Count);
    }

    [Fact]
    public void Browse_UnchangedValuesKeepSameInstances()
    {
        var a = _store.SeedSnippet("x", label: "A");
        var vm = new SnippetListViewModel(_store);
        var first = vm.BrowseItems[0];

        _store.RecordUsage(a.Id);

        Assert.Same(first, vm.BrowseItems[0]);
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
        Assert.Equal(UiText.NoSnippets, vm.EmptyState);

        _store.SeedSnippet("hello", label: "Hello");
        Assert.False(vm.IsEmpty);
        Assert.Equal(string.Empty, vm.EmptyState);

        vm.Query = "zzz";
        Assert.True(vm.IsEmpty);
        Assert.Equal(UiText.NoMatches("zzz"), vm.EmptyState);
    }

    [Fact]
    public void FolderRename_UpdatesRowsImmediately()
    {
        var folder = _store.SeedFolder("Clients");
        _store.SeedSnippet("x", label: "A", folder: "Clients");
        var vm = new SnippetListViewModel(_store);
        Assert.Equal(UiText.RowMeta("Clients", 0), vm.Items[0].Meta);

        _store.RenameFolder(folder.Id, "Customers");
        Assert.Equal(UiText.RowMeta("Customers", 0), vm.Items[0].Meta);
        Assert.Equal("Customers", vm.BrowseItems.Single(b => b.Kind == BrowseKind.Folder).Name);

        _store.DeleteFolder(folder.Id);
        Assert.Equal(UiText.RowMeta(UiText.Unfiled, 0), vm.Items[0].Meta);
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
