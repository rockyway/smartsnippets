using Microsoft.UI.Xaml.Controls;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Presentation.ViewModels;
using SmartSnippets.UnoProbe;
using static SmartSnippets.UnoFlow.Tests.ProbePopupDriverTests;

namespace SmartSnippets.UnoFlow.Tests;

/// <summary>Executes the actual host code-behind, not a copied handler graph.
/// UI primitives are test-only stubs; XAML routing is separately STATIC-audited.</summary>
public sealed class FlowPageHandlerTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PreviewRealPageEnterOrRowHidesWithoutPopupCopy(bool enter)
    {
        using var h = new Harness(); var page = new FlowProbePage(h.Flow, isPopup: true);
        page.TestContainers(); page.TestPreview(); h.Surfaces.Single().Ready();
        page.TestPopupCopy(); // Calls actual Popup.CopyRow via shared composition.
        if (enter) page.TestEnter(); else page.TestRow();
        Assert.Equal("hide", h.Surfaces.Single().Events.Last());
        Assert.Equal(0, h.Flow.Store.Snippets.Single().Snippet.UsageCount);
        var main = new FlowProbePage(h.Flow);
        main.TestCopy(); // Main Copy after preview must refuse, never silently suppress.
        Assert.Contains("Clipboard adapter unavailable", main.TestStatus);
    }
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RealPageShortcutMenuOpenMainAndTypedRowInsert(bool shortcut)
    {
        var native = new Native(); using var h = new Harness(native: native); var page = new FlowProbePage(h.Flow);
        if (shortcut) page.TestShortcut(); else page.TestMenu();
        Assert.Single(native.Calls, c => c == "capture"); h.Surfaces.Single().Ready();
        page.TestOpenMain(); Assert.Equal(1, h.MainShows);
        page.TestMainInsert(h.Flow.Main.List.Items.Single().Id);
        await h.Flow.ShutdownAsync(); // Drains the actual async-void handler's tracked VM operation.
        Assert.Equal(1, native.Dispatches); Assert.Equal(1, native.Restores);
    }
    [Fact]
    public async Task FooterFocusTargetsFooterAndActualCancelHandlerReturnsSearchFocus()
    {
        var native = new Native { Swap = SwapStatus.NotPreserved }; using var h = new Harness(native: native);
        var page = new FlowProbePage(h.Flow); page.TestShortcut(); h.Surfaces.Single().Ready();
        await h.Flow.OnEnterAsync();
        Assert.Equal(PopupFooterState.ConfirmClipboardLoss, h.Flow.Popup.FooterState);
        Assert.Equal(2, h.Surfaces.Single().Events.Count(e => e == "show"));
        Assert.Equal(PopupFocusTarget.Footer, h.Surfaces.Single().Focuses.Last());
        page.ApplyFocus(PopupFocusTarget.Footer); Assert.Equal(1, page.FooterFocuses); Assert.Equal(0, page.SearchFocuses);
        page.TestFooterDismiss(); Assert.Equal(PopupFooterState.None, h.Flow.Popup.FooterState);
        Assert.Equal(PopupFocusTarget.Search, h.Surfaces.Single().Focuses.Last());
        page.ApplyFocus(PopupFocusTarget.Editor); Assert.Contains("NOT IMPLEMENTED", page.TestStatus);
        Assert.Equal(0, page.SearchFocuses); // Never pretend editor focus is Search.
    }
    [Fact]
    public async Task CautionReShowsAndRealFooterDismissNeverRetriesDispatch()
    {
        var native = new Native { Dispatch = DispatchState.Uncertain }; using var h = new Harness(native: native);
        var page = new FlowProbePage(h.Flow); page.TestShortcut(); h.Surfaces.Single().Ready();
        await h.Flow.OnEnterAsync(); Assert.Equal(2, h.Surfaces.Single().Events.Count(e => e == "show"));
        Assert.Equal(PopupFooterState.MaybePasted, h.Flow.Popup.FooterState);
        page.TestFooterDismiss(); Assert.Equal(PopupFooterState.None, h.Flow.Popup.FooterState);
        Assert.Equal(1, native.Dispatches); Assert.Equal(0, h.Flow.Store.Snippets.Single().Snippet.UsageCount);
    }
    [Fact]
    public void PopupPreviewCannotCopyButMainCopyAfterPreviewStillReachesBoundary()
    {
        var native = new Native(); using var h = new Harness(native: native);
        var popup = new FlowProbePage(h.Flow, isPopup: true); var main = new FlowProbePage(h.Flow);
        popup.TestPreview(); h.Surfaces.Single().Ready();
        popup.TestCopy(); popup.TestPopupCopy(); h.Flow.Popup.CopyRow(h.Flow.Popup.Items[h.Flow.Popup.SelectedIndex]);
        Assert.DoesNotContain("copy", native.Calls);
        popup.TestHide(); main.TestCopy();
        Assert.Single(native.Calls, c => c == "copy"); Assert.Equal(0, native.Dispatches);
        Assert.Equal(0, h.Flow.Store.Snippets.Single().Snippet.UsageCount);
    }
    [Fact]
    public async Task InjectedNativeForwardingIsExecutedInSharedDriver()
    {
        var native = new Native(); using var h = new Harness(native: native); var driver = h.Driver;
        Assert.Equal(native.Target, driver.CaptureTarget());
        Assert.Equal(TargetCheck.Ok, driver.Check(native.Target)); Assert.True(driver.IsTargetForeground(native.Target));
        Assert.Equal(FocusPath.Direct, await driver.RestoreFocusAsync(native.Target, default));
        Assert.True(driver.ReleaseHeldModifiers()); Assert.True(driver.ReleasePasteKeys());
        Assert.Equal(SwapStatus.Swapped, driver.TrySwapClipboard("synthetic", false).Status);
        Assert.Equal(SwapStatus.Swapped, driver.TryPlaceText("synthetic"));
        Assert.Equal(DispatchState.Dispatched, driver.SendPaste());
        Assert.Equal(RestoreStatus.Restored, driver.TryRestoreClipboard(new Snapshot()));
        driver.DiscardClipboardSnapshot(new Snapshot()); driver.SetText("synthetic");
        Assert.Equal(new[] { "capture", "check", "foreground", "restore-focus", "modifiers", "keys", "swap", "place", "paste", "restore", "discard", "copy" }, native.Calls);
    }
}
