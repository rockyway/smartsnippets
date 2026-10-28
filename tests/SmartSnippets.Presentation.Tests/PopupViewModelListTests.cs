using SmartSnippets.Core.Domain;
using SmartSnippets.Core.Settings;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Presentation.Tests.Fakes;
using SmartSnippets.Presentation.Text;
using SmartSnippets.Presentation.ViewModels;

namespace SmartSnippets.Presentation.Tests;

public sealed class PopupViewModelListTests
{
    private readonly FakeSnippetStore _store = new();
    private readonly MemorySettingsStore _settings = new();
    private readonly FakePopupHost _host = new();
    private static readonly InsertionTarget Target = new(1, 2, 3, "target");
    private PopupViewModel Create(AppSettings? settings = null) => new(_store, _settings, settings ?? new(), new FakeInsertionService(), new FakeClipboardService(), new FakeElevation(), _host, new ScriptedDialogService(), new RecordingNotifier());
    private static long Selected(PopupViewModel vm) => vm.Items[vm.SelectedIndex].SnippetId;

    [Fact]
    public void Open_DefaultSort_LastUsed_NeverUsedLast()
    {
        var a = _store.SeedSnippet("a"); var b = _store.SeedSnippet("b"); _store.RecordUsage(b.Id);
        using var vm = Create(); vm.Open(Target); Assert.Equal(new[] { b.Id, a.Id }, vm.Items.Select(i => i.SnippetId));
    }
    [Fact]
    public void Sort_TiesByLabelThenId()
    {
        var b = _store.SeedSnippet("b", "Beta"); var a = _store.SeedSnippet("a", "Alpha"); var a2 = _store.SeedSnippet("c", "Alpha");
        using var vm = Create(); Assert.Equal(new[] { a.Id, a2.Id, b.Id }, vm.Items.Select(i => i.SnippetId));
    }
    [Fact]
    public void Search_AndTerms_SubstringCaseInsensitive_AcrossLabelTextFolderTags()
    {
        var a = _store.SeedSnippet("ref body", "Policy", "Support", "billing, chat"); _store.SeedSnippet("ref", "Policy", "Other");
        using var vm = Create(); vm.Query = "POL ref PORT BILL"; Assert.Equal(a.Id, Assert.Single(vm.Items).SnippetId);
    }
    [Fact]
    public void Search_KeepsLayoutAndSort()
    {
        _store.SeedSnippet("alpha"); using var vm = Create(new AppSettings { PopupLayout = PopupLayout.Folder, PopupSort = SortField.Updated });
        vm.Query = "alpha"; Assert.Equal(PopupLayout.Folder, vm.Layout); Assert.Equal(SortField.Updated, vm.Sort);
    }
    [Fact]
    public void Flat_RowChips_FolderOrUnfiled_PlusAtMostTwoTags()
    {
        _store.SeedSnippet("a", "A", "Folder", "one,two,three"); _store.SeedSnippet("b", "B");
        using var vm = Create(); Assert.Equal("Folder", vm.Items[0].FolderChip); Assert.Equal("one", vm.Items[0].Tag1); Assert.Equal("two", vm.Items[0].Tag2); Assert.Equal("Unfiled", vm.Items[1].FolderChip);
    }
    [Fact]
    public void Preview_FirstNonBlankLine()
    {
        _store.SeedSnippet("\n  first   line \nsecond"); using var vm = Create(); Assert.Equal("first line", vm.Items[0].Preview);
    }
    [Fact]
    public void FolderLayout_HeadingsWithCounts_FoldersByName_UnfiledLast()
    {
        _store.SeedSnippet("z", "z", "Z"); _store.SeedSnippet("a", "a", "A"); _store.SeedSnippet("unfiled", "u");
        using var vm = Create(new AppSettings { PopupLayout = PopupLayout.Folder });
        Assert.Equal(new[] { "A (1)", "Z (1)", "Unfiled (1)" }, vm.Items.Where(i => i.Kind == PopupItemKind.Heading).Select(i => i.HeadingText));
        Assert.All(vm.Items.Where(i => i.Kind == PopupItemKind.Snippet), i => Assert.Equal("", i.FolderChip));
    }
    [Fact]
    public void ToggleLayout_PersistsKey_AndTooltip()
    {
        using var vm = Create(); vm.ToggleLayoutCommand.Execute(null); Assert.Equal("Folder", _settings.Get(SettingsKeys.PopupLayout)); Assert.Equal("Layout: folder", vm.LayoutTooltip);
    }
    [Fact]
    public void SetSort_PersistsKey_AndTooltip()
    {
        using var vm = Create(); vm.SetSort(SortField.UsageCount); Assert.Equal("UsageCount", _settings.Get(SettingsKeys.PopupSort)); Assert.Equal("Sort: usage count", vm.SortTooltip);
    }
    [Fact]
    public void Ctor_ReadsLayoutAndSortFromSettings()
    {
        using var vm = Create(new AppSettings { PopupLayout = PopupLayout.Folder, PopupSort = SortField.Created }); Assert.True(vm.IsFolderLayout); Assert.Equal(SortField.Created, vm.Sort);
    }
    [Fact]
    public void CountText_NOfM_AndEmpty()
    {
        _store.SeedSnippet("a"); using var vm = Create(); vm.Query = "none";
        Assert.Equal("0 of 1 snippet", vm.CountText); Assert.True(vm.IsEmpty); Assert.Equal(UiText.NoSnippetsMatch, vm.EmptyTitle); Assert.Equal(UiText.NoSnippetsMatchBody, vm.EmptyBody);
    }
    [Fact]
    public void WhileShown_FolderRenamed_HeadingAndChipsUpdate_SelectionKept()
    {
        var a = _store.SeedSnippet("a", "A", "Old"); using var vm = Create(); vm.Open(Target);
        _store.RenameFolder(a.FolderId!.Value, "New"); Assert.Equal(a.Id, Selected(vm)); Assert.Equal("New", vm.Items[0].FolderChip);
        vm.ToggleLayoutCommand.Execute(null); Assert.Equal("New (1)", vm.Items[0].HeadingText);
    }
    [Fact]
    public void WhileShown_SelectedSnippetEdited_RowUpdates_SelectionKept()
    {
        var a = _store.SeedSnippet("a", "A"); using var vm = Create(); vm.Open(Target);
        _store.UpdateSnippet(a.Id, new SnippetDraft("changed", "Changed", true, null, "")); Assert.Equal(a.Id, Selected(vm)); Assert.Equal("Changed", vm.Items[0].Label);
    }
    [Fact]
    public void WhileShown_SelectedSnippetDeleted_SelectsFirstSnippet()
    {
        var a = _store.SeedSnippet("a", "A"); var b = _store.SeedSnippet("b", "B"); using var vm = Create(); vm.Open(Target);
        _store.DeleteSnippet(a.Id); Assert.Equal(b.Id, Selected(vm));
    }
    [Fact]
    public void WhileShown_SnippetAdded_AppearsInSortOrder_SelectionKept()
    {
        var b = _store.SeedSnippet("b", "B"); using var vm = Create(); vm.Open(Target);
        var a = _store.SeedSnippet("a", "A"); Assert.Equal(new[] { a.Id, b.Id }, vm.Items.Select(i => i.SnippetId)); Assert.Equal(b.Id, Selected(vm));
    }
    [Fact]
    public void WhileShown_FolderDeleted_RowsShowUnfiled()
    {
        var a = _store.SeedSnippet("a", "A", "Folder"); using var vm = Create(); vm.Open(Target); _store.DeleteFolder(a.FolderId!.Value);
        Assert.Equal("Unfiled", vm.Items[0].FolderChip);
    }
    [Fact]
    public void WhileShown_QueryKept_ResultRefiltered()
    {
        var a = _store.SeedSnippet("match", "A"); var b = _store.SeedSnippet("match", "B"); using var vm = Create(); vm.Open(Target); vm.Query = "match";
        _store.UpdateSnippet(a.Id, new SnippetDraft("other", "A", true, null, "")); Assert.Equal("match", vm.Query); Assert.Equal(b.Id, Selected(vm));
    }
    [Fact]
    public void WhileHidden_StoreChanged_NextOpenShowsCurrentData()
    {
        var a = _store.SeedSnippet("a", "A", "Old"); using var vm = Create(); vm.Open(Target); vm.Close();
        _store.RenameFolder(a.FolderId!.Value, "New"); _store.UpdateSnippet(a.Id, new SnippetDraft("b", "B", true, a.FolderId, "")); vm.Open(Target);
        Assert.Equal("B", vm.Items[0].Label); Assert.Equal("New", vm.Items[0].FolderChip);
    }
    [Fact]
    public void SameResult_DoesNotReplaceItems()
    {
        _store.SeedSnippet("alpha"); using var vm = Create(); var before = vm.Items; vm.Query = "a"; Assert.Same(before, vm.Items);
    }
    [Fact]
    public void Items_IsConcreteList() { using var vm = Create(); Assert.IsType<List<PopupItemViewModel>>(vm.Items); }
    [Fact]
    public void Dispose_Unsubscribes()
    {
        var count = _store.ChangedSubscriberCount; var vm = Create(); Assert.Equal(count + 1, _store.ChangedSubscriberCount); vm.Dispose(); Assert.Equal(count, _store.ChangedSubscriberCount);
    }
}
