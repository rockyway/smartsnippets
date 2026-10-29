using SmartSnippets.Core.Settings;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Presentation.Tests.Fakes;
using SmartSnippets.Presentation.ViewModels;

namespace SmartSnippets.Presentation.Tests;

public sealed class SharedSortTests
{
    private readonly FakeSnippetStore _store = new();

    [Fact]
    public void ListOrdersByTheSharedSortAndFollowsChanges()
    {
        var a = _store.SeedSnippet("x", label: "Apple");
        var b = _store.SeedSnippet("x", label: "Banana");
        var c = _store.SeedSnippet("x", label: "Cherry");
        _store.RecordUsage(c.Id); _store.RecordUsage(c.Id); _store.RecordUsage(b.Id);
        var settings = new MemorySettingsStore();
        var pref = new SortPreference(settings, SortField.UsageCount);
        using var vm = new SnippetListViewModel(_store, null, pref);
        Assert.Equal([c.Id, b.Id, a.Id], vm.Items.Select(i => i.Id));

        vm.SetSort(SortField.Created);
        Assert.Equal(SortField.Created, vm.Sort);
        Assert.Equal([c.Id, b.Id, a.Id], vm.Items.Select(i => i.Id)); // newest first

        pref.Set(SortField.LastUsed);
        Assert.Equal([b.Id, c.Id, a.Id], vm.Items.Select(i => i.Id)); // b was used last
    }

    [Fact]
    public void ListAndPopupShareOneSortPreference()
    {
        _store.SeedSnippet("x", label: "Apple");
        var settings = new MemorySettingsStore();
        var pref = new SortPreference(settings, SortField.LastUsed);
        using var list = new SnippetListViewModel(_store, null, pref);
        using var popup = new PopupViewModel(_store, settings, new AppSettings(), new FakeInsertionService(),
            new FakeClipboardService(), new FakeElevation(), new FakePopupHost(), new ScriptedDialogService(), new RecordingNotifier(), sortPreference: pref);
        list.SetSort(SortField.Updated);
        Assert.Equal(SortField.Updated, popup.Sort);
        popup.SetSort(SortField.UsageCount);
        Assert.Equal(SortField.UsageCount, list.Sort);
        Assert.Equal("UsageCount", settings.Get(SettingsKeys.PopupSort));
    }
}
