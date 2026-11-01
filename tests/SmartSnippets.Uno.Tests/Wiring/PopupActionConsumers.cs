using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using SmartSnippets.Core.Settings;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Presentation.ViewModels;
using SmartSnippets.Uno.Tests.Composition;
using SmartSnippets.Uno.Tests.Wiring;
using Windows.System;
using static SmartSnippets.Uno.Tests.Wiring.ActionAuditSupport;

namespace SmartSnippets.Uno.Popup;

public sealed partial class PopupWindow
{
    internal async Task Audit(string action, HostFixture f, ActionAuditNative native)
    {
        var id = f.Host.Store.CreateSnippet(new("body", "label", true, null, "")).Value!.Id;
        Assert.True(f.Host.ShowCapturedPopup());
        Assert.Same(f.Host.Popup, ViewModel);
        Assert.Equal((Func<Func<Task>, Task>)f.Host.RunOperationAsync, RunOperation);
        Assert.Equal((Func<Action, Task>)f.Host.RunCopyOperationAsync, RunCopy);
        Assert.Equal((Func<Action, Task>)f.Host.RunCopyOperationAsync, EditorControl.RunCopy);
        Task last = Task.CompletedTask;
        var registeredOperation = RunOperation; var registeredCopy = RunCopy;
        RunOperation = op => last = registeredOperation(op);
        RunCopy = op => last = registeredCopy(op);
        Prewarm(); _shows.CompleteWarmup(_shows.WarmupGeneration);
        ShowCaptured(native.Target, () => true);
        var row = ViewModel.Items.Single(r => r.SnippetId == id);
        switch (action)
        {
            case "ViewModel.ToggleLayoutCommand":
                var original = ViewModel.Layout; await Execute(ViewModel.ToggleLayoutCommand);
                Assert.NotEqual(original, ViewModel.Layout); Assert.Equal(ViewModel.Layout, AppSettings.Load(f.Host.SettingsStore).PopupLayout); break;
            case "ViewModel.AddSnippetCommand":
                await Execute(ViewModel.AddSnippetCommand); Assert.True(ViewModel.IsEditorOpen); Assert.True(ViewModel.Editor!.IsNew); break;
            case "ViewModel.OpenMainWindowCommand":
                await Execute(ViewModel.OpenMainWindowCommand); Assert.True(f.Boundary.Main.Visible); Assert.True(f.Boundary.Popups[0].Hides > 0); break;
            case "OnSortLast": case "OnSortUsage": case "OnSortCreated": case "OnSortUpdated":
                ViewModel.SetSort(action == "OnSortLast" ? SortField.Created : SortField.LastUsed);
                if (action == "OnSortLast") OnSortLast(this, new());
                if (action == "OnSortUsage") OnSortUsage(this, new());
                if (action == "OnSortCreated") OnSortCreated(this, new());
                if (action == "OnSortUpdated") OnSortUpdated(this, new());
                Assert.Equal(action switch { "OnSortLast" => SortField.LastUsed, "OnSortUsage" => SortField.UsageCount, "OnSortCreated" => SortField.Created, _ => SortField.Updated }, ViewModel.Sort); break;
            case "OnSortClick":
                ViewModel.SetSort(SortField.Created); OnSortClick(this, new()); Assert.True(SortCreated.IsChecked); Assert.False(SortLast.IsChecked); break;
            case "OnSearchChanged":
                SearchBox.Text = "absent"; OnSearchChanged(this, new()); DispatcherQueue.Tick();
                Assert.Equal("absent", ViewModel.Query); Assert.True(ViewModel.IsEmpty); break;
            case "OnKeyDown":
                FocusManager.Focused = SearchBox; OnKeyDown(this, new() { Key = VirtualKey.Enter }); await last;
                Assert.Equal(1, native.Sends); Assert.Equal(1, Assert.Single(f.Host.Store.Snippets).Snippet.UsageCount); break;
            case "OnItemClick":
                OnItemClick(this, new() { ClickedItem = row }); await last;
                Assert.Equal(1, native.Sends); Assert.Equal(1, Assert.Single(f.Host.Store.Snippets).Snippet.UsageCount); break;
            case "OnContainerContentChanging":
                var container = new ListViewItem { Content = row };
                OnContainerContentChanging(PopupList, new() { Item = row, ItemContainer = container });
                Assert.True(container.IsEnabled); Assert.True(container.IsTabStop);
                container.RaiseRightTap(new()); var menu = Assert.IsType<MenuFlyout>(MenuFlyout.LastShown);
                Assert.Equal(4, menu.Items.Count); Assert.True(menu.Items[0].IsEnabled); Assert.True(menu.Items[1].IsEnabled);
                menu.Items[0].Invoke(); await last; Assert.Equal(1, native.Copies);
                menu.Items[1].Invoke(); await last; Assert.Equal(1, native.Sends);
                Assert.Equal(1, Assert.Single(f.Host.Store.Snippets).Snippet.UsageCount);
                f.Boundary.Capability!.Revoke(); f.Host.RefreshClipboardAvailability();
                Assert.False(menu.Items[0].IsEnabled); Assert.False(menu.Items[1].IsEnabled);
                menu.Items[0].Invoke(); menu.Items[1].Invoke(); await last;
                Assert.Equal(1, native.Copies); Assert.Equal(1, native.Sends);
                menu.Items[2].Invoke(); Assert.True(ViewModel.IsEditorOpen); break;
            case "OnCopyFailedText":
                native.Swap = SwapStatus.Failed; await ViewModel.InsertSelectedAsync();
                Assert.True(ViewModel.CanCopyFailedText);
                OnCopyFailedText(this, new()); await last; Assert.Equal(1, native.Copies);
                native.CopyResult = false; OnCopyFailedText(this, new()); await last;
                Assert.Equal(SmartSnippets.Presentation.Text.UiText.CopyFailed, ViewModel.FooterMessage); Assert.Equal(2, native.Copies); break;
            case "ViewModel.CancelPasteAnywayCommand":
                native.Swap = SwapStatus.NotPreserved; await ViewModel.InsertSelectedAsync();
                Assert.True(ViewModel.IsConfirmingClipboardLoss); await Execute(ViewModel.CancelPasteAnywayCommand);
                Assert.False(ViewModel.HasFooterMessage); Assert.Equal(0, native.Sends); break;
            case "ViewModel.DismissFooterCommand":
                native.Dispatch = DispatchState.Uncertain; await ViewModel.InsertSelectedAsync();
                Assert.True(ViewModel.CanDismissFooter); await Execute(ViewModel.DismissFooterCommand);
                Assert.False(ViewModel.HasFooterMessage); Assert.Equal(0, Assert.Single(f.Host.Store.Snippets).Snippet.UsageCount); break;
            case "OnEditorSave": case "OnEditorCancel": case "OnEditorEscape": case "OnEditorPreviewKeyDown":
                ViewModel.AddSnippetCommand.Execute(null); ViewModel.Editor!.Text = "new body";
                if (action == "OnEditorSave") OnEditorSave(this, new());
                if (action == "OnEditorCancel") OnEditorCancel(this, new());
                if (action == "OnEditorEscape") { f.Boundary.Dialogs.Choices.Enqueue(SaveChoice.Discard); OnEditorEscape(this, EventArgs.Empty); }
                if (action == "OnEditorPreviewKeyDown")
                {
                    InputKeyboardSource.Down.Add(VirtualKey.Control);
                    try { OnEditorPreviewKeyDown(this, new() { Key = VirtualKey.Enter }); }
                    finally { InputKeyboardSource.Down.Clear(); }
                }
                await last; Assert.False(ViewModel.IsEditorOpen);
                Assert.Equal(action is "OnEditorSave" or "OnEditorPreviewKeyDown" ? 2 : 1, f.Host.Store.Snippets.Count); break;
            default: throw new InvalidOperationException("Unmapped popup action");
        }
        Close(); // exercise and release actual handler subscriptions/timers
    }
}
