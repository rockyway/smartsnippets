using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using SmartSnippets.Uno.Tests.Composition;
using SmartSnippets.Uno.Tests.Wiring;
using Windows.System;
using static SmartSnippets.Uno.Tests.Wiring.ActionAuditSupport;

namespace SmartSnippets.Uno.Controls;

public sealed partial class SnippetEditor
{
    internal async Task Audit(string action, HostFixture f, ActionAuditNative native)
    {
        await f.Host.Snippets.NewAsync();
        Assert.Same(f.Host.Snippets.Editor, ViewModel);
        Assert.Equal((Func<Action, Task>)f.Host.RunCopyOperationAsync, RunCopy);
        var vm = ViewModel!;
        vm.Text = "original";
        switch (action)
        {
            case "OnEditorLoaded":
                OnEditorUnloaded(this, new());
                Assert.Empty(SnippetTextBox.Text);
                OnEditorLoaded(this, new());
                Assert.Equal("original", SnippetTextBox.Text);
                Assert.Equal("original", vm.Text); break;
            case "OnEditorUnloaded":
                OnEditorUnloaded(this, new());
                Assert.Empty(SnippetTextBox.Text);
                Assert.Empty(SnippetPasswordBox.Password);
                vm.Text = "while detached";
                Assert.Empty(SnippetTextBox.Text);
                Assert.Empty(SnippetPasswordBox.Password);
                OnEditorLoaded(this, new());
                Assert.Equal("while detached", SnippetTextBox.Text); break;
            case "OnPasswordInput":
                SnippetTextBox.Focus(FocusState.Keyboard);
                SnippetPasswordBox.Focus(FocusState.Keyboard);
                OnPasswordInput(new CheckBox { IsChecked = true, FocusState = FocusState.Keyboard }, new());
                Assert.True(vm.IsPassword);
                Assert.Empty(SnippetTextBox.Text);
                Assert.Equal(vm.Text, SnippetPasswordBox.Password);
                Assert.True(FocusText()); Assert.Same(SnippetPasswordBox, FocusManager.Focused);
                vm.Text = "first\r\nsecond";
                Assert.Empty(SnippetPasswordBox.Password);
                Assert.Empty(SnippetTextBox.Text);
                Assert.True(FocusText()); Assert.Same(MultilinePasswordMask, FocusManager.Focused);
                OnPasswordInput(new CheckBox { IsChecked = false, FocusState = FocusState.Keyboard }, new());
                Assert.False(vm.IsPassword);
                Assert.Equal("first\r\nsecond", SnippetTextBox.Text);
                Assert.True(FocusText()); Assert.Same(SnippetTextBox, FocusManager.Focused);
                SnippetTextBox.Visibility = Visibility.Collapsed;
                Assert.True(FocusText()); Assert.Same(PasswordCheckBox, FocusManager.Focused);
                SnippetTextBox.Visibility = Visibility.Visible;
                ViewModel = null;
                Assert.Empty(SnippetTextBox.Text); Assert.Empty(SnippetPasswordBox.Password);
                ViewModel = vm; Assert.Equal(vm.Text, SnippetTextBox.Text);
                Assert.Equal("first\r\nsecond", vm.Text);
                vm.IsPassword = true;
                var replacement = new SmartSnippets.Presentation.ViewModels.SnippetEditorViewModel(f.Host.Store, f.Host.Dialogs, null);
                replacement.Text = "replacement";
                ViewModel = replacement;
                Assert.Equal("replacement", SnippetTextBox.Text);
                Assert.Empty(SnippetPasswordBox.Password);
                Assert.Equal("first\r\nsecond", vm.Text);
                Assert.Equal("replacement", replacement.Text);
                replacement.Dispose();
                ViewModel = vm; break;
            case "OnPasswordTextInput":
                vm.IsPassword = true;
                OnPasswordTextInput(new PasswordBox { Password = "changed", FocusState = FocusState.Keyboard }, new());
                Assert.Equal("changed", vm.Text);
                _syncingInput = true;
                OnPasswordTextInput(new PasswordBox { Password = "feedback", FocusState = FocusState.Keyboard }, new());
                _syncingInput = false;
                Assert.Equal("changed", vm.Text);
                OnTextInput(new TextBox { Text = "inactive", FocusState = FocusState.Keyboard }, new());
                Assert.Equal("changed", vm.Text);
                SnippetPasswordBox.Focus(FocusState.Keyboard);
                SnippetPasswordBox.Password = "bad\ninput";
                OnPasswordTextInput(SnippetPasswordBox, new());
                Assert.Equal("changed", SnippetPasswordBox.Password);
                Assert.Equal("changed", vm.Text); Assert.NotEmpty(PasswordInputHint.Text); break;
            case "OnPasswordPaste":
                vm.IsPassword = true;
                try
                {
                    Windows.ApplicationModel.DataTransfer.Clipboard.Text = "multi\nline";
                    var paste = new TextControlPasteEventArgs(); OnPasswordPaste(SnippetPasswordBox, paste);
                    Assert.True(paste.Handled); Assert.Equal("original", vm.Text);
                    Assert.NotEmpty(PasswordInputHint.Text);
                    Windows.ApplicationModel.DataTransfer.Clipboard.Text = "pasted";
                    OnPasswordPaste(SnippetPasswordBox, new());
                    Assert.Equal("pasted", vm.Text);
                    Assert.Equal("pasted", SnippetPasswordBox.Password);
                    Assert.Empty(PasswordInputHint.Text);
                    Assert.Empty(SnippetTextBox.Text);
                    var pending = new TaskCompletionSource<string>();
                    Windows.ApplicationModel.DataTransfer.Clipboard.PendingText = pending.Task;
                    OnPasswordPaste(SnippetPasswordBox, new());
                    ViewModel = null;
                    pending.SetResult("stale");
                    await Task.Yield();
                    Assert.Equal("pasted", vm.Text);
                    Assert.Empty(SnippetPasswordBox.Password);
                    ViewModel = vm;
                    foreach (var change in new[] { "text", "mode", "unload", "discard", "save", "overlap" })
                    {
                        vm.IsPassword = true; vm.Text = "pasted"; vm.Label = "manual";
                        Assert.True(await vm.SaveAsync());
                        PasswordInputHint.Text = "old hint";
                        Windows.ApplicationModel.DataTransfer.Clipboard.Text = "pasted";
                        Windows.ApplicationModel.DataTransfer.Clipboard.PendingText = null;
                        OnPasswordPaste(SnippetPasswordBox, new());
                        Assert.Empty(PasswordInputHint.Text);
                        pending = new TaskCompletionSource<string>();
                        Windows.ApplicationModel.DataTransfer.Clipboard.PendingText = pending.Task;
                        OnPasswordPaste(SnippetPasswordBox, new());
                        switch (change)
                        {
                            case "text": vm.Text = "edited"; break;
                            case "mode": vm.IsPassword = false; break;
                            case "unload": OnEditorUnloaded(this, new()); break;
                            case "discard": vm.ResetToSaved(); Assert.Empty(PasswordInputHint.Text); break;
                            case "save": Assert.True(await vm.SaveAsync()); Assert.Empty(PasswordInputHint.Text); break;
                            case "overlap":
                                Windows.ApplicationModel.DataTransfer.Clipboard.PendingText = null;
                                Windows.ApplicationModel.DataTransfer.Clipboard.Text = "multi\nline";
                                OnPasswordPaste(SnippetPasswordBox, new());
                                Assert.NotEmpty(PasswordInputHint.Text); break;
                        }
                        var expected = vm.Text;
                        pending.SetResult("stale");
                        await Task.Yield();
                        Assert.Equal(expected, vm.Text);
                        OnEditorLoaded(this, new());
                    }
                }
                finally
                {
                    Windows.ApplicationModel.DataTransfer.Clipboard.Text = null;
                    Windows.ApplicationModel.DataTransfer.Clipboard.PendingText = null;
                }
                break;
            case "OnTextInput":
                OnTextInput(new TextBox { Text = "changed", FocusState = FocusState.Keyboard }, new()); Assert.Equal("changed", vm.Text); break;
            case "OnLabelInput":
                vm.IsLabelCustom = true; OnLabelInput(new TextBox { Text = "changed", FocusState = FocusState.Keyboard }, new()); Assert.Equal("changed", vm.Label); break;
            case "OnTagsInput":
                OnTagsInput(new TextBox { Text = "tag", FocusState = FocusState.Keyboard }, new()); Assert.Equal("tag", vm.TagsInput); break;
            case "OnFolderNameInput":
                OnFolderNameInput(new TextBox { Text = "folder", FocusState = FocusState.Keyboard }, new()); Assert.Equal("folder", vm.NewFolderName); break;
            case "OnCustomLabelInput":
                vm.IsLabelCustom = false; OnCustomLabelInput(new ToggleSwitch { IsOn = true, FocusState = FocusState.Keyboard }, new()); Assert.True(vm.IsLabelCustom); break;
            case "OnFolderSelectionChanged":
                f.Host.Store.CreateFolder("folder");
                var folder = vm.FolderChoices.Single(c => c.Id is not null);
                OnFolderSelectionChanged(new ComboBox { SelectedItem = folder, IsDropDownOpen = true }, new());
                Assert.Equal(folder, vm.SelectedFolder); break;
            case "ViewModel.StartNewFolderCommand":
                await Execute(vm.StartNewFolderCommand); Assert.True(vm.IsCreatingFolder); break;
            case "ViewModel.CancelNewFolderCommand":
                vm.StartNewFolderCommand.Execute(null); await Execute(vm.CancelNewFolderCommand); Assert.False(vm.IsCreatingFolder); break;
            case "ViewModel.CreateFolderCommand": case "OnNewFolderKeyDown":
                vm.StartNewFolderCommand.Execute(null); vm.NewFolderName = "folder";
                if (action == "OnNewFolderKeyDown") OnNewFolderKeyDown(this, new() { Key = VirtualKey.Enter }); else await Execute(vm.CreateFolderCommand);
                Assert.Equal("folder", Assert.Single(f.Host.Store.Folders).Name); break;
            case "ViewModel.SaveCommand": case "OnSaveAccelerator":
                if (action == "OnSaveAccelerator") OnSaveAccelerator(new(), new()); else await Execute(vm.SaveCommand);
                Assert.Equal("original", Assert.Single(f.Host.Store.Snippets).Snippet.Text); break;
            case "ViewModel.RetrySaveCommand":
                // A real SQLite trigger fails the save; no fake VM/store is used.
                using (var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={Path.Combine(f.Root, "smartsnippets.db")}"))
                {
                    connection.Open();
                    using var sql = connection.CreateCommand();
                    sql.CommandText = "CREATE TRIGGER audit_save_failure BEFORE INSERT ON snippets BEGIN SELECT RAISE(FAIL, 'synthetic'); END;"; sql.ExecuteNonQuery();
                    Assert.False(await vm.SaveAsync()); Assert.True(vm.HasSaveFailure);
                    sql.CommandText = "DROP TRIGGER audit_save_failure"; sql.ExecuteNonQuery();
                }
                await Execute(vm.RetrySaveCommand); Assert.False(vm.HasSaveFailure); Assert.Single(f.Host.Store.Snippets); break;
            case "CopyAfterAdmissionCommand":
                await Execute(CopyAfterAdmissionCommand); Assert.Equal(1, native.Copies); Assert.True(vm.IsDirty);
                native.CopyResult = false; await Execute(CopyAfterAdmissionCommand);
                Assert.False(string.IsNullOrWhiteSpace(vm.GeneralError)); Assert.Equal(2, native.Copies); break;
            case "ViewModel.DeleteCommand":
                await vm.SaveAsync();
                await f.Host.Snippets.RequestEditAsync(Assert.Single(f.Host.Store.Snippets).Snippet.Id);
                Assert.Same(f.Host.Snippets.Editor, ViewModel);
                await Execute(ViewModel!.DeleteCommand); Assert.Empty(f.Host.Store.Snippets); break;
            case "ViewModel.DiscardCommand":
                await vm.SaveAsync();
                vm.Text = "edited ordinary";
                SnippetTextBox.Writes.Clear();
                Assert.True(await vm.SaveAsync());
                Assert.Empty(SnippetTextBox.Writes);
                vm.Text = "original"; Assert.True(await vm.SaveAsync());
                vm.Text = "changed"; await Execute(vm.DiscardCommand);
                Assert.Equal("original", vm.Text); Assert.False(vm.IsDirty);
                vm.IsPassword = true; vm.Text = "PRIVATE-DRAFT"; vm.Label = "manual";
                SnippetTextBox.Writes.Clear(); SnippetPasswordBox.Writes.Clear();
                vm.ResetToSaved();
                Assert.DoesNotContain("PRIVATE-DRAFT", SnippetTextBox.Writes);
                Assert.DoesNotContain("original", SnippetPasswordBox.Writes);
                Assert.Equal("original", SnippetTextBox.Text);
                vm.IsPassword = true; vm.Text = "PRIVATE-SAVED"; vm.Label = "manual";
                Assert.True(await vm.SaveAsync());
                vm.IsPassword = false; vm.Text = "ordinary draft";
                SnippetTextBox.Writes.Clear(); SnippetPasswordBox.Writes.Clear();
                vm.ResetToSaved();
                Assert.DoesNotContain("PRIVATE-SAVED", SnippetTextBox.Writes);
                Assert.DoesNotContain("ordinary draft", SnippetPasswordBox.Writes);
                Assert.Equal("PRIVATE-SAVED", SnippetPasswordBox.Password);
                break;
            case "OnDiscardAccelerator":
                vm.StartNewFolderCommand.Execute(null); OnDiscardAccelerator(new(), new()); Assert.False(vm.IsCreatingFolder); break;
            default: throw new InvalidOperationException("Unmapped editor action");
        }
        Assert.All(f.Host.Store.Snippets, row => Assert.Equal(0, row.Snippet.UsageCount));
        ViewModel = null;
    }
}
