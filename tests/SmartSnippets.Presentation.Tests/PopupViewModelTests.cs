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
    public void RowActionTextHasRequestedOrder()
    {
        Assert.Equal(["Copy", "Insert", "Edit", "Delete"],
            new[] { UiText.RowActionCopy, UiText.RowActionInsert, UiText.RowActionEdit, UiText.RowActionDelete });
    }

    [Fact]
    public async Task RowActions_CopyEditAndCancelDeleteDoNotRecordUsage()
    {
        var snippet = _store.SeedSnippet("sensitive synthetic text", "Label");
        _dialogs.ConfirmAnswers.Enqueue(false);
        var usage = 0; _store.OnRecordUsage = _ => usage++;
        using var vm = Create(); vm.OpenPreview();
        var row = Assert.Single(vm.Items);
        Assert.True(vm.CanUseRowActions(row));
        Assert.False(vm.CanCopyRow(row));
        vm.CopyRow(row);
        Assert.Empty(_clipboard.Texts);
        vm.EditRow(row);
        Assert.True(vm.IsEditorOpen);
        Assert.Equal(snippet.Id, vm.Editor!.SnippetId);
        Assert.DoesNotContain(_host.Calls, call => call.StartsWith("Edit:", StringComparison.Ordinal));
        await vm.CancelEditorAsync(false);
        await vm.DeleteRowAsync(row);
        Assert.Single(_dialogs.Confirms);
        Assert.Single(_store.Snippets);
        Assert.Equal(0, usage);
        Assert.Empty(_insertion.Calls);
    }

    [Fact]
    public void NormalPopupRowCopyWritesClipboard()
    {
        _store.SeedSnippet("synthetic body");
        using var vm = Create(); vm.Open(Target);
        var row = Assert.Single(vm.Items);
        Assert.True(vm.CanCopyRow(row));
        vm.CopyRow(row);
        Assert.Equal("synthetic body", Assert.Single(_clipboard.Texts));
    }

    [Fact]
    public async Task PreviewRowInsertClosesWithoutInsertion()
    {
        _store.SeedSnippet("body");
        using var vm = Create(); vm.OpenPreview();
        await vm.InsertRowAsync(Assert.Single(vm.Items));
        Assert.Empty(_insertion.Calls);
        Assert.Empty(_clipboard.Texts);
        Assert.Contains("Hide", _host.Calls);
    }

    [Fact]
    public void PopupRowsAndHeadingsShareObservableTextSize()
    {
        var size = new TextSizeService();
        _store.SeedSnippet("one", folder: "Work");
        using var vm = new PopupViewModel(_store, _settings, new AppSettings { PopupLayout = PopupLayout.Folder },
            _insertion, _clipboard, _elevation, _host, _dialogs, _notices, textSize: size);
        Assert.Equal(2, vm.Items.Count);
        Assert.False(vm.CanUseRowActions(vm.Items.Single(item => item.Kind == PopupItemKind.Heading)));
        Assert.All(vm.Items, item => Assert.Same(size, item.TextSize));
        size.Apply(TextSizePreset.Custom, 32);
        Assert.All(vm.Items, item => Assert.Equal(32, item.TextSize!.FontSize));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PreviewSelection_ClosesWithoutInsertionClipboardOrUsage(bool click)
    {
        var snippet = _store.SeedSnippet("preview");
        var usageCalls = 0;
        _store.OnRecordUsage = _ => usageCalls++;
        using var vm = Create();
        vm.Open(Target);
        Assert.Equal(PopupOpenMode.Fresh, vm.OpenPreview());
        Assert.Null(vm.Target);
        Assert.True(vm.IsPreview);
        if (click) await vm.ActivateAsync(vm.Items.Single());
        else await vm.InsertSelectedAsync();
        Assert.Contains("Hide", _host.Calls);
        Assert.Empty(_insertion.Calls);
        Assert.Empty(_clipboard.Texts);
        Assert.Equal(0, usageCalls);
        Assert.Equal(0, _store.Snippets.Single(s => s.Snippet.Id == snippet.Id).Snippet.UsageCount);
        vm.Open(Target);
        Assert.False(vm.IsPreview);
        await vm.InsertSelectedAsync();
        Assert.Single(_insertion.Calls);
    }

    [Fact]
    public void PreviewAddEditor_HasNoClipboardCopyButNormalAddDoes()
    {
        using (var preview = Create())
        {
            preview.OpenPreview();
            preview.AddSnippetCommand.Execute(null);
            Assert.False(preview.Editor!.CopyTextCommand.CanExecute(null));
        }
        using var normal = Create();
        normal.Open(Target);
        normal.AddSnippetCommand.Execute(null);
        Assert.True(normal.Editor!.CopyTextCommand.CanExecute(null));
    }

    [Fact]
    public void PreviewDoesNotReplaceEditorTarget()
    {
        using var vm = Create();
        vm.Open(Target);
        vm.AddSnippetCommand.Execute(null);
        Assert.Equal(PopupOpenMode.Ignored, vm.OpenPreview());
        Assert.Equal(Target, vm.Target);
    }

    [Fact]
    public void Search_AndTerms_LayoutsAndStoreChanges()
    {
        var a = _store.SeedSnippet("Refund policy\nsecond", "Refund", "Support", "billing, chat, extra");
        _store.SeedSnippet("other", "Other", "A");
        using var vm = Create();
        vm.Open(Target);
        vm.Query = "REF bill";
        Assert.Equal(a.Id, vm.Items.Single().SnippetId);
        Assert.Empty(vm.Items[0].Preview);
        Assert.Equal("Support", vm.Items[0].FolderChip);
        Assert.Equal("billing", vm.Items[0].Tag1);
        Assert.Equal("chat", vm.Items[0].Tag2);
        Assert.Equal("#billing #chat #extra", vm.Items[0].TagsDisplay);
        Assert.Equal("Refund, Support, #billing #chat #extra", vm.Items[0].ToString());
        Assert.DoesNotContain("Refund policy", vm.Items[0].ToString());
        vm.ToggleLayoutCommand.Execute(null);
        Assert.Equal("Folder", _settings.Get(SettingsKeys.PopupLayout));
        Assert.Equal("Support (1)", vm.Items[0].HeadingText);
        Assert.Equal(string.Empty, vm.Items[1].FolderChip);
        _store.UpdateSnippet(a.Id, new SnippetDraft("Refund edited", "Refund", true, a.FolderId, "billing"));
        Assert.Empty(vm.Items[1].Preview);
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
    public async Task MainInsertAfterMaybePasted_DismissesBeforeChangingQueryOrInserting()
    {
        var first = _store.SeedSnippet("first", "First");
        var second = _store.SeedSnippet("second", "Second");
        _insertion.Results.Enqueue(new(InsertionOutcome.MaybePasted, ""));
        using var vm = Create(); vm.Open(Target); vm.Query = "First";
        await vm.InsertSelectedAsync();
        await vm.InsertFromMainAsync(second.Id);
        Assert.Equal(PopupFooterState.None, vm.FooterState);
        Assert.Equal("First", vm.Query);
        Assert.Equal((Target, first.Id, default(InsertionOptions)), Assert.Single(_insertion.Calls));
        await vm.InsertFromMainAsync(second.Id);
        Assert.Equal((Target, second.Id, default(InsertionOptions)), _insertion.Calls[1]);
    }

    [Fact]
    public async Task SuccessWithNotice_StaysHidden()
    {
        _store.SeedSnippet("body"); _insertion.Results.Enqueue(new(InsertionOutcome.Inserted, "notice"));
        using var vm = Create(); vm.Open(Target); await vm.InsertSelectedAsync();
        Assert.Equal((UiText.InsertNoticeTitle, "notice"), Assert.Single(_notices.Calls));
        Assert.DoesNotContain("ShowAgain", _host.Calls);
    }

    [Theory]
    [InlineData(InsertionOutcome.TargetClosed, PopupFooterState.Error, true)]
    [InlineData(InsertionOutcome.TargetElevated, PopupFooterState.Error, true)]
    [InlineData(InsertionOutcome.FocusFailed, PopupFooterState.Error, true)]
    [InlineData(InsertionOutcome.ClipboardBusy, PopupFooterState.Error, true)]
    [InlineData(InsertionOutcome.PasteFailed, PopupFooterState.Error, true)]
    [InlineData(InsertionOutcome.SnippetMissing, PopupFooterState.Error, false)]
    [InlineData(InsertionOutcome.MaybePasted, PopupFooterState.MaybePasted, true)]
    [InlineData(InsertionOutcome.CopiedForManualPaste, PopupFooterState.ManualPaste, false)]
    [InlineData(InsertionOutcome.ClipboardNotPreserved, PopupFooterState.ConfirmClipboardLoss, false)]
    public async Task Outcomes_ReopenWithAppropriateState(InsertionOutcome outcome, PopupFooterState state, bool canCopy)
    {
        _store.SeedSnippet("body"); _insertion.Results.Enqueue(new(outcome, "message"));
        using var vm = Create(); vm.Open(Target); await vm.InsertSelectedAsync();
        Assert.Equal(state, vm.FooterState); Assert.Equal(canCopy, vm.CanCopyFailedText);
        Assert.Contains("ShowAgain", _host.Calls);
        vm.Open(Target); Assert.Equal(PopupFooterState.None, vm.FooterState);
    }

    [Fact]
    public async Task InFlight_InsertionBlocksOpenAndDeactivation()
    {
        _store.SeedSnippet("body"); _insertion.Pending = new();
        using var vm = Create(); vm.Open(Target);
        var task = vm.InsertSelectedAsync();
        Assert.True(vm.IsInserting); Assert.Equal(PopupOpenMode.Ignored, vm.Open(new(4, 5, 6, "other")));
        vm.OnDeactivated(); await vm.InsertSelectedAsync(); Assert.Single(_insertion.Calls);
        _insertion.Pending.SetResult(new(InsertionOutcome.Inserted, "")); await task;
        Assert.False(vm.IsInserting); Assert.Equal(Target, vm.Target);
    }

    [Fact]
    public async Task PromptEsc_CancelsWithoutHiding()
    {
        _store.SeedSnippet("body"); _insertion.Results.Enqueue(new(InsertionOutcome.ClipboardNotPreserved, ""));
        using var vm = Create(); vm.Open(Target); await vm.InsertSelectedAsync();
        var hides = _host.Calls.Count(c => c == "Hide"); vm.Close();
        Assert.Equal(PopupFooterState.None, vm.FooterState); Assert.Equal(hides, _host.Calls.Count(c => c == "Hide"));
        vm.Close(); Assert.Equal(hides + 1, _host.Calls.Count(c => c == "Hide"));
    }

    [Fact]
    public async Task Elevated_CopyAndRestart()
    {
        _store.SeedSnippet("body"); _insertion.Results.Enqueue(new(InsertionOutcome.TargetElevated, "elevated"));
        using var vm = Create(); vm.Open(Target); await vm.InsertSelectedAsync();
        Assert.True(vm.CanRestartAsAdmin); vm.CopyFailedTextCommand.Execute(null);
        Assert.Equal("body", Assert.Single(_clipboard.Texts));
        _elevation.NextResult = new(ElevationOutcome.Cancelled, "cancelled");
        vm.RestartAsAdminCommand.Execute(null); Assert.Equal("cancelled", vm.FooterMessage);
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
