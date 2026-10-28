using SmartSnippets.Core.Domain;
using SmartSnippets.Core.Settings;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Presentation.Tests.Fakes;
using SmartSnippets.Presentation.Text;
using SmartSnippets.Presentation.ViewModels;

namespace SmartSnippets.Presentation.Tests;

public sealed class PopupViewModelTests
{
    private readonly FakeSnippetStore _store = new();
    private readonly MemorySettingsStore _settings = new();
    private readonly FakeInsertionService _insertion = new();
    private readonly FakeClipboardService _clipboard = new();
    private readonly FakeElevation _elevation = new();
    private readonly FakePopupHost _host = new();
    private readonly ScriptedDialogService _dialogs = new();
    private readonly RecordingNotifier _notices = new();
    private static readonly InsertionTarget Target = new(1, 2, 3, "Notepad, Untitled");
    private PopupViewModel Create(AppSettings? settings = null) => new(_store, _settings, settings ?? new(), _insertion, _clipboard, _elevation, _host, _dialogs, _notices);

    [Fact]
    public void Search_AndTerms_LayoutsAndStoreChanges()
    {
        var a = _store.SeedSnippet("Refund policy\nsecond", "Refund", "Support", "billing, chat, extra");
        _store.SeedSnippet("other", "Other", "A");
        using var vm = Create();
        vm.Open(Target);
        vm.Query = "REF bill";
        Assert.Equal(a.Id, vm.Items.Single().SnippetId);
        Assert.Equal("Refund policy", vm.Items[0].Preview);
        Assert.Equal("Support", vm.Items[0].FolderChip);
        Assert.Equal("billing", vm.Items[0].Tag1);
        Assert.Equal("chat", vm.Items[0].Tag2);
        vm.ToggleLayoutCommand.Execute(null);
        Assert.Equal("Folder", _settings.Get(SettingsKeys.PopupLayout));
        Assert.Equal("Support (1)", vm.Items[0].HeadingText);
        Assert.Equal(string.Empty, vm.Items[1].FolderChip);
        _store.UpdateSnippet(a.Id, new SnippetDraft("Refund edited", "Refund", true, a.FolderId, "billing"));
        Assert.Equal("Refund edited", vm.Items[1].Preview);
        Assert.Equal(a.Id, vm.Items[vm.SelectedIndex].SnippetId);
        vm.SetSort(SortField.Updated);
        Assert.Equal("Updated", _settings.Get(SettingsKeys.PopupSort));
        Assert.Equal("Sort: updated date", vm.SortTooltip);
        Assert.Equal("1 of 2 snippets", vm.CountText);
    }

    [Fact]
    public void Navigation_SkipsHeadings_AndClamps()
    {
        _store.SeedSnippet("one", "one", "A"); _store.SeedSnippet("two", "two", "B");
        using var vm = Create(new AppSettings { PopupLayout = PopupLayout.Folder });
        vm.Open(Target);
        Assert.Equal(1, vm.SelectedIndex);
        vm.MoveSelection(1); Assert.Equal(3, vm.SelectedIndex);
        vm.MoveSelection(1); Assert.Equal(3, vm.SelectedIndex);
        vm.MoveSelection(-1); Assert.Equal(1, vm.SelectedIndex);
        vm.MoveSelection(-1); Assert.Equal(1, vm.SelectedIndex);
    }

    [Fact]
    public async Task AddAndSave_SelectsNewSnippet_AndKeepsTarget()
    {
        _store.SeedSnippet("old");
        using var vm = Create(); vm.Open(Target); vm.Query = "not found";
        vm.AddSnippetCommand.Execute(null);
        Assert.Equal("Insertion target kept: Notepad, Untitled", vm.EditorTargetLine);
        vm.Editor!.Text = "new text";
        await vm.SaveEditorAsync();
        Assert.False(vm.IsEditorOpen); Assert.Equal(string.Empty, vm.Query);
        Assert.Equal(UiText.SnippetAddedReady, vm.StatusText);
        var id = vm.Items[vm.SelectedIndex].SnippetId;
        await vm.InsertSelectedAsync();
        Assert.Equal((Target, id, default(InsertionOptions)), Assert.Single(_insertion.Calls));
    }

    [Fact]
    public async Task ClipboardLoss_RequiresExplicitConfirmation_AndPendingId()
    {
        var a = _store.SeedSnippet("a", "a"); _store.SeedSnippet("b", "b");
        _insertion.Results.Enqueue(new(InsertionOutcome.ClipboardNotPreserved, "ignored"));
        using var vm = Create(); vm.Open(Target);
        await vm.InsertSelectedAsync();
        Assert.Equal(PopupFooterState.ConfirmClipboardLoss, vm.FooterState);
        Assert.Equal("Focus:Footer", _host.Calls[^1]);
        vm.SelectedIndex = 1;
        await vm.InsertSelectedAsync(); Assert.Single(_insertion.Calls);
        await vm.PasteAnywayCommand.ExecuteAsync(null);
        Assert.Equal(a.Id, _insertion.Calls[1].Id);
        Assert.True(_insertion.Calls[1].Options.AllowClipboardLoss);
    }

    [Fact]
    public async Task MaybePasted_FirstEnterOnlyDismisses()
    {
        _store.SeedSnippet("body");
        _insertion.Results.Enqueue(new(InsertionOutcome.MaybePasted, ""));
        using var vm = Create(); vm.Open(Target);
        await vm.InsertSelectedAsync();
        Assert.Equal(PopupFooterState.MaybePasted, vm.FooterState);
        Assert.True(vm.CanCopyFailedText);
        await vm.InsertSelectedAsync(); Assert.Single(_insertion.Calls);
        await vm.InsertSelectedAsync(); Assert.Equal(2, _insertion.Calls.Count);
    }

    [Fact]
    public async Task SuccessWithNotice_StaysHidden()
    {
        _store.SeedSnippet("body"); _insertion.Results.Enqueue(new(InsertionOutcome.Inserted, "notice"));
        using var vm = Create(); vm.Open(Target); await vm.InsertSelectedAsync();
        Assert.Equal((UiText.InsertNoticeTitle, "notice"), Assert.Single(_notices.Calls));
        Assert.DoesNotContain("ShowAgain", _host.Calls);
    }

    [Fact]
    public async Task EditorDeactivationAndDirtyCancel()
    {
        using var vm = Create(); vm.Open(Target); vm.AddSnippetCommand.Execute(null);
        vm.Editor!.Text = "draft"; vm.OnDeactivated(); Assert.DoesNotContain("Hide", _host.Calls);
        _dialogs.ConfirmAnswers.Enqueue(false); await vm.CancelEditorAsync(true); Assert.True(vm.IsEditorOpen);
        _dialogs.ConfirmAnswers.Enqueue(true); await vm.CancelEditorAsync(true); Assert.False(vm.IsEditorOpen);
    }
}
