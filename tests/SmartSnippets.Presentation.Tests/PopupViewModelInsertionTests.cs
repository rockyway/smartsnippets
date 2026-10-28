using SmartSnippets.Core.Settings;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Presentation.Tests.Fakes;
using SmartSnippets.Presentation.Text;
using SmartSnippets.Presentation.ViewModels;

namespace SmartSnippets.Presentation.Tests;

public sealed class PopupViewModelInsertionTests
{
    private readonly FakeSnippetStore _store = new();
    private readonly FakeInsertionService _insertion = new();
    private readonly FakePopupHost _host = new();
    private readonly FakeClipboardService _clipboard = new();
    private readonly FakeElevation _elevation = new();
    private readonly RecordingNotifier _notices = new();
    private int _exits;
    private static readonly InsertionTarget Target = new(11, 22, 33, "Target");
    private PopupViewModel Create() { var vm = new PopupViewModel(_store, new MemorySettingsStore(), new AppSettings(), _insertion, _clipboard, _elevation, _host, new ScriptedDialogService(), _notices, () => _exits++); vm.Open(Target); return vm; }
    private void Outcome(InsertionOutcome outcome, string message = "message") => _insertion.Results.Enqueue(new(outcome, message));

    [Fact]
    public async Task Enter_HidesBeforeInsert_PassesTargetAndId_DefaultOptions()
    {
        var s = _store.SeedSnippet("body"); _insertion.Pending = new();
        _insertion.OnCall = () => Assert.Equal("Hide", _host.Calls[^1]);
        using var vm = Create();
        var task = vm.InsertSelectedAsync(); Assert.Equal("Hide", _host.Calls[0]); var call = Assert.Single(_insertion.Calls);
        Assert.Equal(Target, call.Target); Assert.Equal(s.Id, call.Id); Assert.False(call.Options.AllowClipboardLoss);
        _insertion.Pending.SetResult(new(InsertionOutcome.Inserted, "")); await task;
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DuplicateActivation_WhilePending_DoesNotDispatchOrChangeSelection(bool click)
    {
        _store.SeedSnippet("first"); var other = _store.SeedSnippet("second");
        _insertion.Pending = new(); using var vm = Create();
        var selected = vm.Items[vm.SelectedIndex];
        var first = vm.InsertSelectedAsync();
        Assert.True(vm.IsInserting);
        if (click) await vm.ActivateAsync(vm.Items.Single(i => i.SnippetId == other.Id));
        else await vm.InsertSelectedAsync();
        Assert.Single(_insertion.Calls);
        Assert.Same(selected, vm.Items[vm.SelectedIndex]);
        _insertion.Pending.SetResult(new(InsertionOutcome.Inserted, "")); await first;
    }
    [Fact]
    public async Task Click_EqualsEnter()
    {
        var a = _store.SeedSnippet("a"); var b = _store.SeedSnippet("b"); using var vm = Create();
        await vm.ActivateAsync(vm.Items.Single(i => i.SnippetId == b.Id)); Assert.Equal(b.Id, Assert.Single(_insertion.Calls).Id); Assert.NotEqual(a.Id, b.Id);
    }
    [Fact]
    public async Task Enter_NoSelection_NoCall()
    {
        using var vm = Create(); await vm.InsertSelectedAsync(); Assert.Empty(_insertion.Calls); Assert.Empty(_host.Calls);
    }
    [Fact]
    public async Task Inserted_EmptyMessage_NoNotice()
    {
        _store.SeedSnippet("a"); using var vm = Create(); await vm.InsertSelectedAsync(); Assert.Empty(_notices.Calls); Assert.DoesNotContain("ShowAgain", _host.Calls); Assert.Equal(PopupFooterState.None, vm.FooterState);
    }
    [Theory]
    [InlineData(InsertionOutcome.FocusFailed)]
    [InlineData(InsertionOutcome.TargetElevated)]
    [InlineData(InsertionOutcome.MaybePasted)]
    [InlineData(InsertionOutcome.CopiedForManualPaste)]
    [InlineData(InsertionOutcome.ClipboardNotPreserved)]
    public async Task Recovery_PreservesNonFirstSelectionAndPromptedId(InsertionOutcome outcome)
    {
        _store.SeedSnippet("match alpha", "Alpha");
        _store.SeedSnippet("match beta", "Beta");
        var third = _store.SeedSnippet("match gamma", "Gamma");
        Outcome(outcome); using var vm = Create(); vm.Query = "match"; vm.MoveSelection(1); vm.MoveSelection(1);
        Assert.Equal(third.Id, vm.Items[vm.SelectedIndex].SnippetId);
        await vm.InsertSelectedAsync();
        Assert.Contains("ShowAgain", _host.Calls); Assert.Equal(Target, vm.Target); Assert.Equal("match", vm.Query);
        Assert.Equal(third.Id, vm.Items[vm.SelectedIndex].SnippetId);
        Assert.Equal(third.Id, Assert.Single(_insertion.Calls).Id);
        if (outcome == InsertionOutcome.ClipboardNotPreserved)
        {
            await vm.PasteAnywayCommand.ExecuteAsync(null);
            var retry = _insertion.Calls[1];
            Assert.Equal(third.Id, retry.Id); Assert.Equal(Target, retry.Target); Assert.True(retry.Options.AllowClipboardLoss);
        }
    }

    [Fact]
    public async Task Failure_ShowsAgain_SameTargetQuerySelection_ShowsMessage()
    {
        var s = _store.SeedSnippet("match"); Outcome(InsertionOutcome.TargetClosed, "closed"); using var vm = Create(); vm.Query = "match";
        await vm.InsertSelectedAsync(); Assert.Equal("closed", vm.FooterMessage); Assert.Equal(Target, vm.Target); Assert.Equal("match", vm.Query); Assert.Equal(s.Id, vm.Items[vm.SelectedIndex].SnippetId); Assert.Contains("ShowAgain", _host.Calls);
    }
    [Fact]
    public async Task Prompt_QueryChangeOrMove_CancelsPrompt()
    {
        _store.SeedSnippet("a"); _store.SeedSnippet("b"); Outcome(InsertionOutcome.ClipboardNotPreserved); using var vm = Create(); await vm.InsertSelectedAsync(); vm.MoveSelection(1); Assert.Equal(PopupFooterState.None, vm.FooterState);
        Outcome(InsertionOutcome.ClipboardNotPreserved); await vm.InsertSelectedAsync(); vm.Query = "a"; Assert.Equal(PopupFooterState.None, vm.FooterState);
    }
    [Fact]
    public async Task PasteAnyway_ThenMaybePasted_ShowsCaution()
    {
        _store.SeedSnippet("a"); Outcome(InsertionOutcome.ClipboardNotPreserved); Outcome(InsertionOutcome.MaybePasted, ""); using var vm = Create();
        await vm.InsertSelectedAsync(); await vm.PasteAnywayCommand.ExecuteAsync(null); Assert.Equal(PopupFooterState.MaybePasted, vm.FooterState); Assert.Equal(UiText.MaybePastedFallback, vm.FooterMessage);
    }
    [Fact]
    public async Task CopiedForManualPaste_ShowsAgain_NoCopyText_NextEnterInserts()
    {
        _store.SeedSnippet("a"); Outcome(InsertionOutcome.CopiedForManualPaste, ""); using var vm = Create(); await vm.InsertSelectedAsync();
        Assert.False(vm.CanCopyFailedText); Assert.False(vm.CanRestartAsAdmin); Assert.Equal(UiText.CopiedForManualPasteFallback, vm.FooterMessage); await vm.InsertSelectedAsync(); Assert.Equal(2, _insertion.Calls.Count);
    }
    [Fact]
    public async Task MaybePasted_FirstClick_OnlyDismisses()
    {
        _store.SeedSnippet("a"); Outcome(InsertionOutcome.MaybePasted); using var vm = Create(); await vm.InsertSelectedAsync();
        await vm.ActivateAsync(vm.Items[0]); Assert.Single(_insertion.Calls); Assert.Equal(PopupFooterState.None, vm.FooterState);
        await vm.ActivateAsync(vm.Items[0]); Assert.Equal(2, _insertion.Calls.Count);
    }
    [Fact]
    public async Task MaybePasted_DismissButton_Clears_NextEnterInserts()
    {
        _store.SeedSnippet("a"); Outcome(InsertionOutcome.MaybePasted); using var vm = Create(); await vm.InsertSelectedAsync(); vm.DismissFooterCommand.Execute(null);
        await vm.InsertSelectedAsync(); Assert.Equal(2, _insertion.Calls.Count);
    }
    [Fact]
    public async Task MaybePasted_Esc_Closes()
    {
        _store.SeedSnippet("a"); Outcome(InsertionOutcome.MaybePasted); using var vm = Create(); await vm.InsertSelectedAsync(); var n = _host.Calls.Count(c => c == "Hide"); vm.Close(); Assert.Equal(n + 1, _host.Calls.Count(c => c == "Hide"));
    }
    [Fact]
    public async Task TargetElevated_NoRestart_WhenElevated()
    {
        _store.SeedSnippet("a"); _elevation.IsElevated = true; Outcome(InsertionOutcome.TargetElevated); using var vm = Create(); await vm.InsertSelectedAsync(); Assert.False(vm.CanRestartAsAdmin);
    }
    [Fact]
    public async Task Restart_Started_RequestsExit()
    {
        _store.SeedSnippet("a"); Outcome(InsertionOutcome.TargetElevated); using var vm = Create(); await vm.InsertSelectedAsync(); vm.RestartAsAdminCommand.Execute(null); Assert.Equal(1, _exits);
    }
    [Fact]
    public async Task Busy_NoReopen()
    {
        _store.SeedSnippet("a"); Outcome(InsertionOutcome.Busy); using var vm = Create(); await vm.InsertSelectedAsync(); Assert.DoesNotContain("ShowAgain", _host.Calls);
    }
    [Fact]
    public async Task InsertThrows_TreatedAsFailure_Reopens()
    {
        _store.SeedSnippet("a"); _insertion.ThrowNext = true; using var vm = Create(); await vm.InsertSelectedAsync(); Assert.Equal(PopupFooterState.Error, vm.FooterState); Assert.Equal(UiText.InsertFailedGeneric, vm.FooterMessage); Assert.Contains("ShowAgain", _host.Calls);
    }
    [Fact]
    public async Task PromptCancel_ClearsPrompt_StaysOpen_NoCall()
    {
        _store.SeedSnippet("a"); Outcome(InsertionOutcome.ClipboardNotPreserved); using var vm = Create(); await vm.InsertSelectedAsync();
        var hides = _host.Calls.Count(c => c == "Hide"); vm.CancelPasteAnywayCommand.Execute(null);
        Assert.Equal(PopupFooterState.None, vm.FooterState); Assert.Equal(hides, _host.Calls.Count(c => c == "Hide")); Assert.Single(_insertion.Calls);
        Assert.Equal("Focus:Search", _host.Calls[^1]);
    }
    [Fact]
    public async Task PasteAnyway_ThenInserted_StaysHidden()
    {
        _store.SeedSnippet("a"); Outcome(InsertionOutcome.ClipboardNotPreserved); using var vm = Create(); await vm.InsertSelectedAsync();
        var shows = _host.Calls.Count(c => c == "ShowAgain"); await vm.PasteAnywayCommand.ExecuteAsync(null);
        Assert.Equal(shows, _host.Calls.Count(c => c == "ShowAgain")); Assert.Equal(PopupFooterState.None, vm.FooterState);
    }
    [Fact]
    public async Task ConfirmPrompt_EnterAndClick_DoNotInsert()
    {
        _store.SeedSnippet("a"); Outcome(InsertionOutcome.ClipboardNotPreserved); using var vm = Create(); await vm.InsertSelectedAsync();
        await vm.InsertSelectedAsync(); await vm.ActivateAsync(vm.Items[0]); Assert.Single(_insertion.Calls); Assert.False(vm.CanCopyFailedText);
    }
    [Fact]
    public async Task MaybePasted_QueryOrMove_DismissesBeforeNewSelection()
    {
        _store.SeedSnippet("a"); _store.SeedSnippet("b"); Outcome(InsertionOutcome.MaybePasted); using var vm = Create(); await vm.InsertSelectedAsync();
        vm.MoveSelection(1); Assert.Equal(PopupFooterState.None, vm.FooterState); Outcome(InsertionOutcome.MaybePasted); await vm.InsertSelectedAsync();
        vm.Query = "a"; Assert.Equal(PopupFooterState.None, vm.FooterState);
    }
    [Fact]
    public async Task ClipboardNotPreserved_ShowsAgain_PromptState_FocusFooter_SameSelection()
    {
        var snippet = _store.SeedSnippet("a"); Outcome(InsertionOutcome.ClipboardNotPreserved); using var vm = Create();
        await vm.InsertSelectedAsync(); Assert.Equal(PopupFooterState.ConfirmClipboardLoss, vm.FooterState);
        Assert.Equal(snippet.Id, vm.Items[vm.SelectedIndex].SnippetId); Assert.Contains("ShowAgain", _host.Calls);
        Assert.Equal("Focus:Footer", _host.Calls[^1]); Assert.False(vm.CanCopyFailedText);
    }
    [Fact]
    public async Task PasteAnyway_HidesThenInserts_SameIdAndTarget_AllowClipboardLossTrue()
    {
        var snippet = _store.SeedSnippet("a"); Outcome(InsertionOutcome.ClipboardNotPreserved); using var vm = Create(); await vm.InsertSelectedAsync();
        _insertion.OnCall = () => Assert.Equal("Hide", _host.Calls[^1]);
        await vm.PasteAnywayCommand.ExecuteAsync(null); var call = _insertion.Calls[^1];
        Assert.Equal(snippet.Id, call.Id); Assert.Equal(Target, call.Target); Assert.True(call.Options.AllowClipboardLoss);
    }
    [Fact]
    public async Task MaybePasted_ShowsAgain_CautionState_CopyTextShown_NoRestart()
    {
        _store.SeedSnippet("a"); Outcome(InsertionOutcome.MaybePasted); using var vm = Create(); await vm.InsertSelectedAsync();
        Assert.True(vm.IsFooterCaution); Assert.True(vm.CanCopyFailedText); Assert.False(vm.CanRestartAsAdmin); Assert.Contains("ShowAgain", _host.Calls);
    }
    [Fact]
    public async Task Failure_CopyText_DisappearsIfSnippetDeleted()
    {
        var snippet = _store.SeedSnippet("body"); Outcome(InsertionOutcome.TargetClosed); using var vm = Create(); await vm.InsertSelectedAsync();
        vm.CopyFailedTextCommand.Execute(null); Assert.Equal("body", Assert.Single(_clipboard.Texts));
        _store.DeleteSnippet(snippet.Id); Assert.False(vm.CanCopyFailedText);
    }
    [Fact]
    public async Task ViewModel_NeverRecordsUsage()
    {
        var s = _store.SeedSnippet("a"); using var vm = Create(); await vm.InsertSelectedAsync(); Assert.Equal(0, _store.Snippets.Single(v => v.Snippet.Id == s.Id).Snippet.UsageCount);
    }
}
