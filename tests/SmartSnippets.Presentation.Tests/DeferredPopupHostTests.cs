using SmartSnippets.Core.Settings;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Presentation.Tests.Fakes;
using SmartSnippets.Presentation.ViewModels;

namespace SmartSnippets.Presentation.Tests;

public sealed class DeferredPopupHostTests
{
    private static readonly InsertionTarget Target = new(1, 42, 3, "Notepad");

    [Theory]
    [InlineData(InsertionOutcome.MaybePasted, PopupFooterState.MaybePasted)]
    [InlineData(InsertionOutcome.ClipboardNotPreserved, PopupFooterState.ConfirmClipboardLoss)]
    [InlineData(InsertionOutcome.CopiedForManualPaste, PopupFooterState.ManualPaste)]
    [InlineData(InsertionOutcome.FocusFailed, PopupFooterState.Error)]
    public async Task ExitGuardSuspended_InsertionCompletes_CancelPresentsRecoveryWithoutFreshOpen(InsertionOutcome outcome, PopupFooterState state)
    {
        var store = new FakeSnippetStore();
        store.SeedSnippet("match first", "A");
        var selected = store.SeedSnippet("match second", "B");
        var inner = new FakeInsertionService { Pending = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        var insertion = new InsertionShutdownCoordinator(inner);
        var window = new FakePopupHost();
        var host = new DeferredPopupHost(window);
        using var vm = new PopupViewModel(store, new MemorySettingsStore(), new AppSettings(), insertion,
            new FakeClipboardService(), new FakeElevation(), host, new ScriptedDialogService(), new RecordingNotifier());
        vm.Open(Target); vm.Query = "match"; vm.MoveSelection(1);
        var insert = vm.InsertSelectedAsync();
        Assert.True(insertion.TryBeginShutdown());
        host.Suspend();
        var exitGuard = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task ConsiderExitAsync()
        {
            if (!await exitGuard.Task)
            {
                insertion.ResumeAdmission();
                host.Resume();
            }
        }
        var exit = ConsiderExitAsync();
        inner.Pending.SetResult(new InsertionResult(outcome, "recovery message"));
        await insert;
        Assert.DoesNotContain("ShowAgain", window.Calls);
        Assert.Equal(state, vm.FooterState);

        exitGuard.SetResult(false); await exit;
        Assert.Contains("ShowAgain", window.Calls);
        Assert.Equal(Target, vm.Target); Assert.Equal("match", vm.Query);
        Assert.Equal(selected.Id, vm.Items[vm.SelectedIndex].SnippetId);
        Assert.Equal(outcome == InsertionOutcome.ClipboardNotPreserved ? SmartSnippets.Presentation.Text.UiText.ClipboardLossPrompt : "recovery message", vm.FooterMessage);
        if (outcome == InsertionOutcome.ClipboardNotPreserved) Assert.Equal("Focus:Footer", window.Calls[^1]);
        if (outcome == InsertionOutcome.MaybePasted)
        {
            await vm.InsertSelectedAsync();
            Assert.Single(inner.Calls);
            Assert.Equal(PopupFooterState.None, vm.FooterState);
        }
    }

    [Fact]
    public void EditRespectsShutdownSuspension()
    {
        var window = new FakePopupHost(); var host = new DeferredPopupHost(window);
        host.EditSnippet(1);
        host.Suspend(); host.EditSnippet(2);
        host.Commit(); host.EditSnippet(3);
        Assert.Equal(["Edit:1"], window.Calls);
    }

    [Fact]
    public void CommittedShutdown_DiscardsPendingAndFutureRecovery()
    {
        var window = new FakePopupHost(); var host = new DeferredPopupHost(window);
        host.Suspend(); host.ShowAgain(); host.Focus(PopupFocusTarget.Footer); host.Commit(); host.Resume(); host.ShowAgain();
        Assert.Empty(window.Calls);
    }

    [Fact]
    public void DismissalFocus_DiscardsPendingRecovery()
    {
        var window = new FakePopupHost(); var host = new DeferredPopupHost(window);
        host.Suspend(); host.ShowAgain(); host.Focus(PopupFocusTarget.Search); host.Resume();
        Assert.Empty(window.Calls);
    }

    [Fact]
    public void DeliberateHide_DiscardsPendingRecovery()
    {
        var window = new FakePopupHost(); var host = new DeferredPopupHost(window);
        host.Suspend(); host.ShowAgain(); host.Hide(); host.Resume();
        Assert.Equal(["Hide"], window.Calls);
    }
}
