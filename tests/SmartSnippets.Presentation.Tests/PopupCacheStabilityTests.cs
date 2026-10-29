using SmartSnippets.Core.Domain;
using SmartSnippets.Core.Porting;
using SmartSnippets.Core.Settings;
using SmartSnippets.Core.Store;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Presentation.Tests.Fakes;
using SmartSnippets.Presentation.ViewModels;

namespace SmartSnippets.Presentation.Tests;

public sealed class PopupCacheStabilityTests(Xunit.Abstractions.ITestOutputHelper output)
{
    [Fact]
    public void NumericTimingLogsCannotCarryContent()
    {
        Assert.Equal("popup_cache_rebuild_ms=12 rows=50000", PopupLogFormat.CacheRebuild(12, 50000));
        Assert.Equal("popup_show_ms=34", PopupLogFormat.Show(34));
    }

    [Fact]
    public void LargeImportAndUsageBurstRebuildOnceBeforeShow()
    {
        using var vm = Create();
        var candidates = Enumerable.Range(0, 50000).Select(i => new ImportCandidate(i, "body " + i, "Label " + i, null, [], null)).ToList();
        var imported = _store.ImportSnippets(new(candidates, [], DedupeMode.TextOnly));
        _store.RecordUsage(imported.SnippetIds[0]);
        Assert.Single(_work);
        var started = System.Diagnostics.Stopwatch.GetTimestamp();
        vm.OpenPreview();
        var elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        output.WriteLine($"dirty_open_50000_ms={elapsed:F2} rows={vm.CachedRowCount}");
        Assert.Equal(50000, vm.CachedRowCount);
        Assert.Equal(2, vm.CacheRebuildCount);
        Drain();
        Assert.Equal(2, vm.CacheRebuildCount);
    }

    private readonly FakeSnippetStore _store = new();
    private readonly Queue<Action> _work = new();
    private PopupViewModel Create() => new(_store, new MemorySettingsStore(), new(), new FakeInsertionService(),
        new FakeClipboardService(), new FakeElevation(), new FakePopupHost(), new ScriptedDialogService(), new RecordingNotifier(), schedule: action => _work.Enqueue(action));
    private void Drain() { while (_work.TryDequeue(out var action)) action(); }

    [Theory]
    [InlineData(StoreChangeKind.SnippetCreated)]
    [InlineData(StoreChangeKind.SnippetUpdated)]
    [InlineData(StoreChangeKind.SnippetDeleted)]
    [InlineData(StoreChangeKind.FolderCreated)]
    [InlineData(StoreChangeKind.FolderRenamed)]
    [InlineData(StoreChangeKind.FolderDeleted)]
    [InlineData(StoreChangeKind.Imported)]
    [InlineData(StoreChangeKind.UsageRecorded)]
    [InlineData(StoreChangeKind.Reloaded)]
    public void EveryStoreChangeInvalidatesAndOpenIsFreshnessBarrier(StoreChangeKind kind)
    {
        var original = _store.SeedSnippet("PRIVATE_BODY", "Original", "Folder");
        using var vm = Create();
        var before = vm.CacheRebuildCount;
        switch (kind)
        {
            case StoreChangeKind.SnippetCreated: _store.SeedSnippet("new", "New"); break;
            case StoreChangeKind.SnippetUpdated: _store.UpdateSnippet(original.Id, new("PRIVATE_EDIT", "Edited", true, original.FolderId, "tag")); break;
            case StoreChangeKind.SnippetDeleted: _store.DeleteSnippet(original.Id); break;
            case StoreChangeKind.FolderCreated: _store.SeedFolder("Another"); break;
            case StoreChangeKind.FolderRenamed: _store.RenameFolder(original.FolderId!.Value, "Renamed"); break;
            case StoreChangeKind.FolderDeleted: _store.DeleteFolder(original.FolderId!.Value); break;
            case StoreChangeKind.Imported: _store.ImportSnippets(new([new(0, "import body", "Imported", null, [], null)], [], DedupeMode.TextOnly)); break;
            case StoreChangeKind.UsageRecorded: _store.RecordUsage(original.Id); break;
            case StoreChangeKind.Reloaded: _store.Load(); break;
        }
        Assert.Equal(before, vm.CacheRebuildCount);
        Assert.Single(_work);
        vm.OpenPreview(); // Before the scheduled rebuild: must not show stale rows.
        Assert.Equal(before + 1, vm.CacheRebuildCount);
        Assert.Equal(_store.Snippets.Select(s => s.Snippet.Id).Order(), vm.Items.Select(i => i.SnippetId).Order());
        foreach (var row in vm.Items)
        {
            var source = _store.Snippets.Single(s => s.Snippet.Id == row.SnippetId);
            Assert.Equal(source.Snippet.Label, row.Label);
            Assert.Equal(source.FolderName ?? "", row.FolderChip);
            Assert.Empty(row.Preview);
        }
        Drain();
        Assert.Equal(before + 1, vm.CacheRebuildCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EventBurstCoalesces_ReplacesRows_PreservesVisibleQueryAndSelection(bool visible)
    {
        var first = _store.SeedSnippet("one", "Keep one");
        using var vm = Create();
        if (visible) vm.OpenPreview();
        vm.Query = "Keep";
        for (var i = 0; i < 100; i++) _store.SeedSnippet("body " + i, "Keep " + i);
        _store.RecordUsage(first.Id);
        Assert.Single(_work);
        Assert.Equal(1, vm.CacheRebuildCount);
        Drain();
        Assert.Equal(2, vm.CacheRebuildCount);
        Assert.Equal("Keep", vm.Query);
        Assert.Equal(first.Id, vm.Items[vm.SelectedIndex].SnippetId);
        Assert.Equal(101, vm.CachedRowCount);
        _store.DeleteSnippet(first.Id); Drain();
        Assert.Equal(100, vm.CachedRowCount);
        Assert.Equal(100, vm.Items.Count);
        vm.Dispose();
        Assert.Equal(0, vm.CachedRowCount);
        Assert.Equal(0, _store.ChangedSubscriberCount);
    }

    [Fact]
    public void SortLayoutAndFilterChangesFlushPendingData()
    {
        _store.SeedSnippet("first", "First", "Folder");
        using var vm = Create();
        _store.SeedSnippet("second", "Second", "Folder");
        vm.SetSort(SortField.UsageCount);
        Assert.Equal(2, vm.CachedRowCount);
        _store.SeedSnippet("third", "Third", "Folder");
        vm.ToggleLayoutCommand.Execute(null);
        Assert.Equal(3, vm.CachedRowCount);
        Assert.Contains(vm.Items, item => item.Kind == PopupItemKind.Heading);
        _store.SeedSnippet("fourth", "Fourth", "Folder");
        vm.Query = "Fourth";
        Assert.Equal("Fourth", vm.Items.Single(i => i.Kind == PopupItemKind.Snippet).Label);
        var count = vm.CacheRebuildCount;
        Drain();
        Assert.Equal(count, vm.CacheRebuildCount);
    }

    [Fact]
    public void ThrowingSubscriberCannotPreventPopupInvalidation_QueuedWorkIsSafeAfterDispose()
    {
        _store.Changed += (_, _) => throw new InvalidOperationException("PRIVATE_BODY");
        var later = 0;
        using var vm = Create();
        _store.Changed += (_, _) => later++;
        _store.SeedSnippet("PRIVATE_BODY");
        Assert.Equal(1, later);
        Assert.Single(_work);
        vm.Dispose(); Drain();
        Assert.Empty(vm.Items);
        Assert.Equal(1, vm.CacheRebuildCount);
    }
}
