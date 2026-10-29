using SmartSnippets.Core.Settings;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Presentation.Tests.Fakes;
using SmartSnippets.Presentation.Text;
using SmartSnippets.Presentation.ViewModels;

namespace SmartSnippets.Presentation.Tests;

public sealed class PopupViewModelEditTests
{
    private readonly FakeSnippetStore _store = new();
    private readonly FakePopupHost _host = new();
    private readonly FakeInsertionService _insertion = new();
    private readonly FakeClipboardService _clipboard = new();
    private readonly ScriptedDialogService _dialogs = new();
    private static readonly InsertionTarget Target = new(123, 456, 789, "synthetic target");

    private PopupViewModel Create(Action<Action>? schedule = null)
    {
        var vm = new PopupViewModel(_store, new MemorySettingsStore(), new AppSettings(),
            _insertion, _clipboard, new FakeElevation(), _host, _dialogs, new RecordingNotifier(), schedule: schedule);
        vm.Open(Target);
        return vm;
    }

    [Fact]
    public async Task Edit_KeepsSurfaceAndOriginalTarget_InsertionBlockedUntilSave()
    {
        var a = _store.SeedSnippet("A original text", "A label");
        var snippet = _store.SeedSnippet("B original text", "B label");
        var c = _store.SeedSnippet("C original text", "C label");
        using var vm = Create();
        Assert.Equal(3, vm.Items.Count);
        Assert.NotEqual(snippet.Id, vm.Items[0].SnippetId);
        var row = Assert.Single(vm.Items, item => item.SnippetId == snippet.Id);
        vm.EditRow(row);
        Assert.True(vm.IsEditorOpen);
        Assert.Equal(snippet.Id, vm.Editor!.SnippetId);
        Assert.Equal(UiText.EditSnippet, vm.EditorTitle);
        Assert.Equal("B original text", vm.Editor.Text);
        Assert.Equal("Focus:Editor", _host.Calls[^1]);
        vm.OnDeactivated();
        Assert.Equal(PopupOpenMode.Resumed, vm.Open(new(9, 8, 7, "other")));
        Assert.Equal(Target, vm.Target);
        await vm.InsertSelectedAsync();
        await vm.ActivateAsync(row);
        Assert.Empty(_insertion.Calls);
        Assert.DoesNotContain("Hide", _host.Calls);
        Assert.DoesNotContain(_host.Calls, call => call.StartsWith("Edit:", StringComparison.Ordinal));
        vm.Editor.Text = "B changed text";
        vm.Editor.Label = "B changed label";
        await vm.SaveEditorAsync();
        Assert.False(vm.IsEditorOpen);
        Assert.Equal(3, _store.Snippets.Count);
        Assert.Equal(a, _store.Snippets.Single(s => s.Snippet.Id == a.Id).Snippet);
        Assert.Equal(c, _store.Snippets.Single(s => s.Snippet.Id == c.Id).Snippet);
        var saved = _store.Snippets.Single(s => s.Snippet.Id == snippet.Id).Snippet;
        Assert.Equal("B changed text", saved.Text);
        Assert.Equal("B changed label", saved.Label);
        var selected = vm.Items[vm.SelectedIndex];
        Assert.Equal(PopupItemKind.Snippet, selected.Kind);
        Assert.Equal(snippet.Id, selected.SnippetId);
        Assert.Equal("B changed label", selected.Label);
        Assert.Equal("Focus:Search", _host.Calls[^1]);
        Assert.DoesNotContain("Hide", _host.Calls);
        await vm.InsertSelectedAsync();
        var insertion = Assert.Single(_insertion.Calls);
        Assert.Equal(Target, insertion.Target);
        Assert.Equal(snippet.Id, insertion.Id);
        Assert.Equal(default, insertion.Options);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Edit_SaveFlushesQueuedCache_RepairsOnlyHiddenQuery(bool hidesResult)
    {
        var a = _store.SeedSnippet("A match original", "A match");
        var b = _store.SeedSnippet("B match original", "B match");
        var c = _store.SeedSnippet("C match original", "C match");
        var queued = new Queue<Action>();
        using var vm = Create(queued.Enqueue);
        vm.Query = "match";
        Assert.Equal(3, vm.Items.Count);
        Assert.NotEqual(b.Id, vm.Items[0].SnippetId);
        var row = Assert.Single(vm.Items, item => item.SnippetId == b.Id);
        vm.EditRow(row);
        Assert.Equal(b.Id, vm.Editor!.SnippetId);
        Assert.Equal("B match original", vm.Editor.Text);
        var changedLabel = hidesResult ? "B other" : "B match updated";
        vm.Editor.Label = changedLabel;
        vm.Editor.Text = "B changed text";
        await vm.SaveEditorAsync();
        Assert.Equal(hidesResult ? "" : "match", vm.Query);
        Assert.Equal(3, _store.Snippets.Count);
        Assert.Equal(a, _store.Snippets.Single(s => s.Snippet.Id == a.Id).Snippet);
        Assert.Equal(c, _store.Snippets.Single(s => s.Snippet.Id == c.Id).Snippet);
        var saved = _store.Snippets.Single(s => s.Snippet.Id == b.Id).Snippet;
        Assert.Equal("B changed text", saved.Text);
        Assert.Equal(changedLabel, saved.Label);
        var selected = vm.Items[vm.SelectedIndex];
        Assert.Equal(PopupItemKind.Snippet, selected.Kind);
        Assert.Equal(b.Id, selected.SnippetId);
        Assert.Equal(changedLabel, selected.Label);
        while (queued.TryDequeue(out var action)) action();
        Assert.Equal(selected, vm.Items[vm.SelectedIndex]);
        await vm.InsertSelectedAsync();
        var insertion = Assert.Single(_insertion.Calls);
        Assert.Equal(Target, insertion.Target);
        Assert.Equal(b.Id, insertion.Id);
        Assert.Equal(default, insertion.Options);
    }

    [Fact]
    public async Task Edit_CancelRestoresQuerySelectionAndFocus_WithoutSaving()
    {
        var a = _store.SeedSnippet("match one", "A");
        var b = _store.SeedSnippet("match two", "B");
        var c = _store.SeedSnippet("match three", "C");
        using var vm = Create();
        vm.Query = "match";
        Assert.Equal(3, vm.Items.Count);
        Assert.NotEqual(b.Id, vm.Items[0].SnippetId);
        vm.SelectedIndex = vm.Items.FindIndex(row => row.SnippetId == b.Id);
        vm.EditRow(vm.Items[vm.SelectedIndex]);
        Assert.Equal(b.Id, vm.Editor!.SnippetId);
        Assert.Equal("match two", vm.Editor.Text);
        vm.Editor.Text = "discard me";
        _dialogs.ConfirmAnswers.Enqueue(false);
        await vm.CancelEditorAsync(true);
        Assert.True(vm.IsEditorOpen);
        _dialogs.ConfirmAnswers.Enqueue(true);
        await vm.CancelEditorAsync(true);
        Assert.False(vm.IsEditorOpen);
        Assert.Equal("match", vm.Query);
        var selected = vm.Items[vm.SelectedIndex];
        Assert.Equal(PopupItemKind.Snippet, selected.Kind);
        Assert.Equal(b.Id, selected.SnippetId);
        Assert.Equal("B", selected.Label);
        Assert.Equal(3, _store.Snippets.Count);
        Assert.Equal(a, _store.Snippets.Single(s => s.Snippet.Id == a.Id).Snippet);
        Assert.Equal(b, _store.Snippets.Single(s => s.Snippet.Id == b.Id).Snippet);
        Assert.Equal(c, _store.Snippets.Single(s => s.Snippet.Id == c.Id).Snippet);
        Assert.Equal("Focus:Search", _host.Calls[^1]);
        Assert.DoesNotContain("Hide", _host.Calls);
        Assert.Empty(_insertion.Calls);
    }

    [Fact]
    public async Task Preview_EditAllowedLikeAdd_ButNoClipboardInsertionOrUsage()
    {
        _store.SeedSnippet("before");
        var usage = 0;
        _store.OnRecordUsage = _ => usage++;
        using var vm = Create();
        vm.OpenPreview();
        vm.EditRow(Assert.Single(vm.Items));
        Assert.True(vm.IsEditorOpen);
        Assert.False(vm.Editor!.CopyTextCommand.CanExecute(null));
        vm.Editor.Text = "preview edited";
        vm.Editor.CopyTextCommand.Execute(null);
        vm.OnDeactivated();
        Assert.DoesNotContain("Hide", _host.Calls);
        await vm.SaveEditorAsync();
        Assert.True(vm.IsPreview);
        Assert.Null(vm.Target);
        Assert.Equal("preview edited", _store.Snippets.Single().Snippet.Text);
        await vm.InsertSelectedAsync();
        Assert.Empty(_clipboard.Texts);
        Assert.Empty(_insertion.Calls);
        Assert.Equal(0, usage);
        Assert.Contains("Hide", _host.Calls);
    }

    [Fact]
    public async Task Edit_SaveWithQueryRepair_DoesNotArmMaybePastedRetry()
    {
        _store.SeedSnippet("match", "match");
        using var vm = Create();
        vm.Query = "match";
        _insertion.Results.Enqueue(new(InsertionOutcome.MaybePasted, "caution"));
        await vm.InsertSelectedAsync();
        vm.EditRow(Assert.Single(vm.Items));
        await vm.InsertSelectedAsync();
        Assert.Equal(PopupFooterState.MaybePasted, vm.FooterState);
        vm.Editor!.Label = "other";
        vm.Editor.Text = "other";
        await vm.SaveEditorAsync();
        Assert.Empty(vm.Query);
        Assert.Equal(PopupFooterState.MaybePasted, vm.FooterState);
        await vm.InsertSelectedAsync();
        Assert.Single(_insertion.Calls); // First Enter only dismisses caution.
        await vm.InsertSelectedAsync();
        Assert.Equal(2, _insertion.Calls.Count);
    }

    [Fact]
    public async Task ClipboardLossConfirmation_BlocksBothEditorRoutes()
    {
        _store.SeedSnippet("body");
        using var vm = Create();
        _insertion.Results.Enqueue(new(InsertionOutcome.ClipboardNotPreserved, ""));
        await vm.InsertSelectedAsync();
        vm.EditRow(Assert.Single(vm.Items));
        vm.AddSnippetCommand.Execute(null);
        Assert.False(vm.IsEditorOpen);
        Assert.Equal(PopupFooterState.ConfirmClipboardLoss, vm.FooterState);
        Assert.Single(_insertion.Calls);
    }
}
