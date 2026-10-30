using SmartSnippets.Core.Domain;
using SmartSnippets.Core.Store;
using SmartSnippets.Presentation.Tests.Fakes;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Core.Settings;
using SmartSnippets.Presentation.Text;
using SmartSnippets.Presentation.ViewModels;

namespace SmartSnippets.Presentation.Tests;

public sealed class SnippetListViewModelTests
{
    private readonly FakeSnippetStore _store = new();

    [Fact]
    public void RowProjectsLabelFolderAndAllTagsWithoutTextInAccessibleName()
    {
        _store.SeedSnippet("private body", label: "Display label", folder: "Clients", tags: "mail,work,third");
        using var vm = new SnippetListViewModel(_store);
        var row = Assert.Single(vm.Items);
        Assert.Equal("Display label", row.Label);
        Assert.Equal("Clients", row.FolderName);
        Assert.Equal("#mail #work #third", row.TagsDisplay);
        Assert.DoesNotContain("private body", row.ToString());
        Assert.Equal("Display label, Clients, #mail #work #third", row.ToString());
    }

    [Fact]
    public void BodyOnlyEdit_ReplacesHoverContentBeyondPreviewBoundary()
    {
        var body = new string('a', 100);
        var snippet = _store.SeedSnippet(body + " old", label: "Label");
        using var vm = new SnippetListViewModel(_store);
        var before = Assert.Single(vm.Items);
        _store.UpdateSnippet(snippet.Id, new(body + " new", "Label", true, null, ""));
        var after = Assert.Single(vm.Items);
        Assert.Equal(before.Preview, after.Preview);
        Assert.NotEqual(before, after);
        Assert.Equal(body + " new", after.ContentText);
        Assert.DoesNotContain(after.ContentText, after.ToString());
        var equal = after with { ContentText = new string(after.ContentText.ToCharArray()) };
        Assert.Equal(after, equal); Assert.Equal(after.GetHashCode(), equal.GetHashCode());
    }

    [Fact]
    public void RowsShareObservableTextSizeAcrossRebuilds()
    {
        var size = new TextSizeService();
        _store.SeedSnippet("first");
        using var vm = new SnippetListViewModel(_store, size);
        var row = Assert.Single(vm.Items);
        Assert.Same(size, row.TextSize);
        size.Apply(TextSizePreset.Custom, 32);
        Assert.Equal(32, row.TextSize!.FontSize);
        _store.SeedSnippet("second");
        Assert.All(vm.Items, item => Assert.Same(size, item.TextSize));
    }

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
        // Empty text names the scope so a search miss in one folder is not read as a miss everywhere.
        Assert.Equal(UiText.NoMatchesIn("email", "Support"), vm.EmptyState);
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
    public void RecordingUsageOfAVisibleRow_DoesNotRebuildTheList()
    {
        var a = _store.SeedSnippet("x", label: "Alpha");
        _store.SeedSnippet("x", label: "Beta");
        var vm = new SnippetListViewModel(_store);
        vm.SelectedId = a.Id;
        var before = vm.Items.ToList();
        var collectionChanges = 0;
        vm.Items.CollectionChanged += (_, _) => collectionChanges++;

        _store.RecordUsage(a.Id);

        Assert.Equal(0, collectionChanges);
        Assert.Equal(before.Count, vm.Items.Count);
        for (var i = 0; i < before.Count; i++)
        {
            Assert.Same(before[i], vm.Items[i]);
        }

        Assert.Equal(a.Id, vm.SelectedId);
    }

    [Fact]
    public void Dispose_Unsubscribes()
    {
        var vm = new SnippetListViewModel(_store);
        Assert.Equal(1, _store.ChangedSubscriberCount);

        vm.Dispose();

        Assert.Equal(0, _store.ChangedSubscriberCount);
    }

    [Fact]
    public void EmptyStates_NameTheBrowseScope()
    {
        _store.SeedSnippet("hello", label: "Hello");
        var email = _store.SeedFolder("Email");
        var vm = new SnippetListViewModel(_store);

        vm.SelectedBrowse = vm.BrowseItems.Single(b => b.FolderId == email.Id);
        Assert.Equal(UiText.NoSnippetsIn("Email"), vm.EmptyState);

        vm.Query = "hello";
        Assert.Equal(UiText.NoMatchesIn("hello", "Email"), vm.EmptyState);
    }

    [Fact]
    public void BrowseCounts_UnchangedBySearch_UpdatedByStore()
    {
        _store.SeedSnippet("a", label: "A", folder: "Work");
        var vm = new SnippetListViewModel(_store);
        var before = vm.BrowseItems.ToList();

        vm.Query = "zzz";
        Assert.Equal(before, vm.BrowseItems);

        _store.SeedSnippet("b", label: "B", folder: "Work");
        Assert.Equal(2, vm.BrowseItems.Single(b => b.Kind == BrowseKind.Folder).Count);
        Assert.Equal(2, vm.BrowseItems.Single(b => b.Kind == BrowseKind.All).Count);
    }

    [Fact]
    public void QuickAddFolder_CreatesAndSelectsIt()
    {
        var vm = new SnippetListViewModel(_store);

        vm.StartNewFolderCommand.Execute(null);
        Assert.True(vm.IsCreatingFolder);
        vm.NewFolderName = "Support";
        vm.CreateFolderCommand.Execute(null);

        Assert.False(vm.IsCreatingFolder);
        Assert.Equal(string.Empty, vm.NewFolderName);
        var folder = Assert.Single(_store.Folders);
        Assert.Equal(BrowseKind.Folder, vm.SelectedBrowse!.Kind);
        Assert.Equal(folder.Id, vm.SelectedBrowse.FolderId);
        Assert.Contains(vm.BrowseItems, b => b.FolderId == folder.Id && b.Name == "Support");
    }

    [Fact]
    public void QuickAddFolder_Duplicate_ShowsInlineError_KeepsRowOpen()
    {
        _store.SeedFolder("Support");
        var vm = new SnippetListViewModel(_store);

        vm.StartNewFolderCommand.Execute(null);
        vm.NewFolderName = "support";
        vm.CreateFolderCommand.Execute(null);

        Assert.True(vm.IsCreatingFolder);
        Assert.Equal(UiText.FolderNameTaken, vm.NewFolderError);
        Assert.Single(_store.Folders);

        vm.NewFolderName = "Support 2";
        Assert.Equal(string.Empty, vm.NewFolderError);
    }

    [Fact]
    public void QuickAddFolder_StoreFailure_ShowsInlineError()
    {
        var vm = new SnippetListViewModel(_store);
        _store.Failure = new StoreException(StoreFailure.DatabaseBusy, "busy");

        vm.StartNewFolderCommand.Execute(null);
        vm.NewFolderName = "Support";
        vm.CreateFolderCommand.Execute(null);

        Assert.True(vm.IsCreatingFolder);
        Assert.Equal(UiText.FolderCreateFailed, vm.NewFolderError);
    }

    [Fact]
    public void QuickAddFolder_Cancel_ClosesAndClears()
    {
        var vm = new SnippetListViewModel(_store);
        vm.StartNewFolderCommand.Execute(null);
        vm.NewFolderName = "Draft";

        vm.CancelNewFolderCommand.Execute(null);

        Assert.False(vm.IsCreatingFolder);
        Assert.Equal(string.Empty, vm.NewFolderName);
        Assert.Empty(_store.Folders);
    }
}
