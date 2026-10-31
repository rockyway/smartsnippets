using SmartSnippets.Core.Domain;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Uno.Tests.Composition;

namespace SmartSnippets.Uno.Tests.Flows;

public sealed class CandidateNativeGateFlowTests
{
    [Fact]
    public async Task DeniedGate_RejectsInsertAndClipboardLossBeforeAnyUsage()
    {
        using var f = new HostFixture(); await f.Start();
        var snippet = f.Host.Store.CreateSnippet(new SnippetDraft("synthetic", "Label", true, null, "")).Value!;
        var target = new InsertionTarget(123, 456, 789, "sensitive synthetic title");
        foreach (var loss in new[] { false, true })
        {
            var result = await f.Host.Gate.InsertAsync(target, snippet.Id, new(loss));
            Assert.Equal(InsertionOutcome.PasteFailed, result.Outcome);
        }
        f.Host.Popup.Open(target);
        await f.Host.Popup.InsertSelectedAsync();
        Assert.False(f.Host.Popup.IsConfirmingClipboardLoss);
        Assert.False(f.Host.Popup.CanCopyFailedText);
        Assert.False(f.Host.Popup.CopyFailedTextCommand.CanExecute(null));
        Assert.Contains("unavailable", f.Host.Popup.FooterMessage);
        await f.Host.Snippets.InsertRowAsync(snippet.Id);
        Assert.Equal(0, Assert.Single(f.Host.Store.Snippets).Snippet.UsageCount);
    }
    [Fact]
    public async Task Preview_SelectionRowEditorAndFooterNeverInsertOrCreditUsage()
    {
        using var f = new HostFixture(); await f.Start();
        var snippet = f.Host.Store.CreateSnippet(new("synthetic", "Label", true, null, "")).Value!;
        f.Host.ShowPreview();
        var row = f.Host.Popup.Items.First(i => i.SnippetId == snippet.Id);
        Assert.False(f.Host.Popup.CanCopyRow(row)); f.Host.Popup.CopyRow(row);
        await f.Host.Popup.ActivateAsync(row);
        await f.Host.Popup.InsertFromMainAsync(snippet.Id);
        f.Host.Popup.CopyFailedTextCommand.Execute(null);
        f.Host.Popup.EditRow(row);
        Assert.NotNull(f.Host.Popup.Editor);
        Assert.False(f.Host.Popup.Editor.CopyTextCommand.CanExecute(null));
        await f.Host.Popup.CancelEditorAsync(false);
        await f.Host.Popup.InsertSelectedAsync();
        Assert.Null(f.Host.Popup.Target); Assert.True(f.Host.Popup.IsPreview);
        Assert.Equal(0, Assert.Single(f.Host.Store.Snippets).Snippet.UsageCount);
    }
    [Fact]
    public async Task AllCopyConsumers_UnavailableRetainDraftAndEmitNoSuccess()
    {
        using var f = new HostFixture(); await f.Start();
        var snippet = f.Host.Store.CreateSnippet(new("synthetic secret body", "Synthetic", true, null, "")).Value!;
        Assert.False(f.Host.Snippets.CanCopyRow);
        f.Host.Snippets.CopyRow(snippet.Id);
        Assert.True(await f.Host.Snippets.RequestEditAsync(snippet.Id));
        var editor = f.Host.Snippets.Editor!; editor.Text = "unsaved synthetic body";
        f.Host.RefreshClipboardAvailability();
        Assert.False(editor.CopyTextCommand.CanExecute(null));
        editor.CopyTextCommand.Execute(null);
        Assert.True(editor.IsDirty); Assert.Equal("unsaved synthetic body", editor.Text);
        f.Host.Popup.Open(new(123, 456, 789, "synthetic sensitive title"));
        var row = f.Host.Popup.Items.First(i => i.SnippetId == snippet.Id);
        Assert.False(f.Host.Popup.CanCopyRow(row)); f.Host.Popup.CopyRow(row);
        await f.Host.Popup.InsertSelectedAsync();
        f.Host.RefreshClipboardAvailability(); f.Host.Popup.CopyFailedTextCommand.Execute(null);
        Assert.False(f.Host.Popup.IsConfirmingClipboardLoss);
        Assert.Equal(0, Assert.Single(f.Host.Store.Snippets).Snippet.UsageCount);
        SmartSnippets.Core.Logging.AppLog.Shutdown();
        var logs = string.Join(Environment.NewLine, Directory.GetFiles(Path.Combine(f.Root, "logs")).Select(File.ReadAllText));
        Assert.DoesNotContain("_copied", logs); Assert.DoesNotContain("popup_row_copied", logs);
        Assert.DoesNotContain("popup_copy_text", logs); Assert.DoesNotContain("insert_start", logs);
        Assert.DoesNotContain("synthetic secret body", logs); Assert.DoesNotContain("synthetic sensitive title", logs);
    }
    [Fact]
    public async Task Shutdown_RejectsLateInsertionAndCopy()
    {
        using var f = new HostFixture(); await f.Start(); await f.Host.ExitAsync();
        Assert.False(f.Host.Gate.TrySetText("late"));
        Assert.Equal(InsertionOutcome.PasteFailed, (await f.Host.Gate.InsertAsync(new(0, 0, 0, ""), 1)).Outcome);
        f.Host.ShowPreview(); Assert.Single(f.Boundary.Popups);
    }
}
