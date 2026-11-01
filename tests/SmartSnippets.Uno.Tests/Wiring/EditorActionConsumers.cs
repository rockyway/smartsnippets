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
                await vm.SaveAsync(); vm.Text = "changed"; await Execute(vm.DiscardCommand);
                Assert.Equal("original", vm.Text); Assert.False(vm.IsDirty); break;
            case "OnDiscardAccelerator":
                vm.StartNewFolderCommand.Execute(null); OnDiscardAccelerator(new(), new()); Assert.False(vm.IsCreatingFolder); break;
            default: throw new InvalidOperationException("Unmapped editor action");
        }
        Assert.All(f.Host.Store.Snippets, row => Assert.Equal(0, row.Snippet.UsageCount));
        ViewModel = null;
    }
}
