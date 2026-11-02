using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using SmartSnippets.Core.Settings;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Uno.Tests.Composition;
using SmartSnippets.Uno.Tests.Wiring;
using Windows.System;
using static SmartSnippets.Uno.Tests.Wiring.ActionAuditSupport;

namespace SmartSnippets.Uno.Pages;

public sealed partial class FoldersPage
{
    internal async Task Audit(string action, HostFixture f)
    {
        Assert.Same(f.Host.Folders, ViewModel);
        Assert.Equal((Func<Func<Task>, Task>)f.Host.RunOperationAsync, RunOperation);
        ViewModel!.NewFolderName = "created";
        if (action == "ViewModel.CreateCommand" || action == "OnNewFolderKeyDown")
        {
            if (action == "OnNewFolderKeyDown") OnNewFolderKeyDown(this, new() { Key = VirtualKey.Enter });
            else await Execute(ViewModel.CreateCommand);
            Assert.Equal("created", Assert.Single(f.Host.Store.Folders).Name);
            return;
        }
        await ViewModel.CreateAsync();
        var row = Assert.Single(ViewModel.Rows, r => !r.IsBuiltIn);
        var sender = new Button { DataContext = row };
        if (action == "OnRenameClick")
        { OnRenameClick(sender, new()); Assert.True(row.IsRenaming); return; }
        if (action == "OnDeleteClick")
        { OnDeleteClick(sender, new()); Assert.Empty(f.Host.Store.Folders); return; }
        ViewModel.BeginRename(row); row.EditName = "changed";
        switch (action)
        {
            case "OnCommitRenameClick": OnCommitRenameClick(sender, new()); break;
            case "OnCancelRenameClick": OnCancelRenameClick(sender, new()); break;
            case "OnRenameKeyDown": OnRenameKeyDown(sender, new() { Key = VirtualKey.Enter }); break;
            default: throw new InvalidOperationException("Unmapped folder action");
        }
        Assert.False(row.IsRenaming);
        Assert.Equal(action == "OnCancelRenameClick" ? "created" : "changed", Assert.Single(f.Host.Store.Folders).Name);
    }
}

public sealed partial class SettingsPage
{
    internal async Task Audit(string action, HostFixture f)
    {
        Assert.Same(f.Host.SettingsPage, ViewModel);
        switch (action)
        {
            case "OnThemeSelectionChanged":
                ThemeRadios.SelectedIndex = 1; OnThemeSelectionChanged(this, new());
                Assert.Equal(ThemeMode.Dark, AppSettings.Load(f.Host.SettingsStore).Theme); break;
            case "OnPaletteSelectionChanged":
                ColorPaletteRadios.SelectedIndex = 2; OnPaletteSelectionChanged(this, new());
                Assert.Equal(ColorPalette.Forest, AppSettings.Load(f.Host.SettingsStore).ColorPalette); break;
            case "OnTextSizeSelectionChanged":
                TextSizeRadios.SelectedIndex = 3; OnTextSizeSelectionChanged(this, new());
                Assert.Equal(TextSizePreset.Custom, AppSettings.Load(f.Host.SettingsStore).TextSizePreset); break;
            case "OnCustomSizeGotFocus":
                OnCustomSizeGotFocus(CustomSizeBox, new());
                OnCustomSizeChanged(CustomSizeBox, new() { NewValue = 25 });
                Assert.Equal(25, ViewModel.CustomTextSize); break;
            case "OnCustomSizeChanged":
                OnCustomSizeGotFocus(CustomSizeBox, new());
                OnCustomSizeChanged(CustomSizeBox, new() { NewValue = 26 });
                Assert.Equal(26, AppSettings.Load(f.Host.SettingsStore).CustomTextSize); break;
            case "OnCustomSizeLostFocus":
                OnCustomSizeGotFocus(CustomSizeBox, new()); CustomSizeBox.Value = 27;
                OnCustomSizeLostFocus(CustomSizeBox, new());
                Assert.Equal(27, AppSettings.Load(f.Host.SettingsStore).CustomTextSize); break;
            case "OnStartupToggled":
                StartupToggle.IsOn = true; OnStartupToggled(this, new());
                Assert.False(ViewModel.StartAtLogin); Assert.True(ViewModel.IsStatusVisible);
                Assert.False(string.IsNullOrWhiteSpace(ViewModel.StatusMessage)); break;
            case "OnChangeClick":
                OnChangeClick(this, new()); Assert.Equal(Visibility.Visible, Recorder.Visibility);
                Assert.Equal(Visibility.Collapsed, KeycapsPanel.Visibility); break;
            case "OnGestureCaptured":
                var gesture = new HotkeyGesture(HotkeyGesture.ModControl, 65);
                OnGestureCaptured(this, gesture); Assert.Null(ViewModel.CandidateGesture);
                Assert.Equal(SmartSnippets.Presentation.ViewModels.StatusSeverity.Error, ViewModel.HotkeyFeedbackSeverity);
                Assert.False(string.IsNullOrWhiteSpace(ViewModel.HotkeyFeedback));
                Assert.Equal(Visibility.Collapsed, Recorder.Visibility); break;
            case "OnRecorderCleared": case "OnRecorderCanceled": case "OnRecorderLostFocus":
                Recorder.Visibility = Visibility.Visible; KeycapsPanel.Visibility = Visibility.Collapsed;
                if (action == "OnRecorderCleared") OnRecorderCleared(this, EventArgs.Empty);
                if (action == "OnRecorderCanceled") OnRecorderCanceled(this, EventArgs.Empty);
                if (action == "OnRecorderLostFocus") OnRecorderLostFocus(this, new());
                Assert.Equal(Visibility.Collapsed, Recorder.Visibility); Assert.Equal(Visibility.Visible, KeycapsPanel.Visibility); break;
            case "ViewModel.RestoreDefaultHotkeyCommand":
                await Execute(ViewModel.RestoreDefaultHotkeyCommand);
                Assert.Equal(SmartSnippets.Presentation.ViewModels.StatusSeverity.Error, ViewModel.HotkeyFeedbackSeverity);
                Assert.False(string.IsNullOrWhiteSpace(ViewModel.HotkeyFeedback)); break;
            case "ViewModel.PreviewGlobalDialogCommand":
                await Execute(ViewModel.PreviewGlobalDialogCommand);
                Assert.Equal(1, Assert.Single(f.Boundary.Popups).Shows); Assert.True(f.Host.Popup.IsPreview); break;
            default: throw new InvalidOperationException("Unmapped settings action");
        }
    }
}

public sealed partial class SnippetsPage
{
    internal async Task Audit(string action, HostFixture f)
    {
        Assert.Same(f.Host.Snippets, ViewModel); Assert.Same(f.Host.TextSize, TextSize);
        Assert.Equal((Func<Func<Task>, Task>)f.Host.RunOperationAsync, RunOperation);
        Assert.Equal((Func<Action, Task>)f.Host.RunCopyOperationAsync, RunCopy);
        var vm = ViewModel!;
        switch (action)
        {
            case "ViewModel.NewCommand": case "OnNewAccelerator":
                if (action == "OnNewAccelerator") OnNewAccelerator(new(), new()); else await Execute(vm.NewCommand);
                Assert.NotNull(vm.Editor); Assert.True(vm.Editor.IsNew); break;
            case "OnBrowseNewFolderClick":
                OnBrowseNewFolderClick(this, new()); Assert.True(vm.List.IsCreatingFolder); break;
            case "ViewModel.List.CancelNewFolderCommand":
                vm.List.StartNewFolderCommand.Execute(null); await Execute(vm.List.CancelNewFolderCommand);
                Assert.False(vm.List.IsCreatingFolder); break;
            case "ViewModel.List.CreateFolderCommand": case "OnBrowseNewFolderKeyDown":
                vm.List.StartNewFolderCommand.Execute(null); vm.List.NewFolderName = "created";
                if (action == "OnBrowseNewFolderKeyDown") OnBrowseNewFolderKeyDown(this, new() { Key = VirtualKey.Enter });
                else await Execute(vm.List.CreateFolderCommand);
                Assert.Equal("created", Assert.Single(f.Host.Store.Folders).Name); break;
            case "OnSearchChanged":
                SearchBox.Text = "absent"; OnSearchChanged(this, new()); DispatcherQueue.Tick();
                Assert.Equal("absent", vm.List.Query); break;
            case "OnSearchKeyDown":
                SearchBox.Text = "absent"; vm.List.Query = "absent"; OnSearchKeyDown(this, new() { Key = VirtualKey.Escape });
                Assert.Equal("", vm.List.Query); Assert.Equal("", SearchBox.Text); break;
            case "OnSortLast": case "OnSortUsage": case "OnSortCreated": case "OnSortUpdated":
                vm.List.SetSort(action == "OnSortLast" ? SortField.Created : SortField.LastUsed);
                if (action == "OnSortLast") OnSortLast(this, new());
                if (action == "OnSortUsage") OnSortUsage(this, new());
                if (action == "OnSortCreated") OnSortCreated(this, new());
                if (action == "OnSortUpdated") OnSortUpdated(this, new());
                Assert.Equal(action switch { "OnSortLast" => SortField.LastUsed, "OnSortUsage" => SortField.UsageCount, "OnSortCreated" => SortField.Created, _ => SortField.Updated }, vm.List.Sort); break;
            case "OnSortClick":
                vm.List.SetSort(SortField.UsageCount); OnSortClick(this, new()); Assert.True(SortUsage.IsChecked); Assert.False(SortLast.IsChecked); break;
            case "OnLoaded":
                Root.ActualWidth = 1000; OnLoaded(this, new()); Assert.Equal(200, BrowseColumn.Width.Value); break;
            case "OnRootSizeChanged":
                OnRootSizeChanged(this, new() { NewSize = new(1000,700) }); Assert.Equal(200, BrowseColumn.Width.Value); break;
            case "OnBackClick": case "OnItemClick":
                await vm.NewAsync();
                if (action == "OnBackClick") { OnBackClick(this, new()); Assert.Equal(Visibility.Visible, ListPane.Visibility); }
                else { OnBackClick(this, new()); OnItemClick(this, new()); Assert.Equal(Visibility.Visible, EditorPane.Visibility); }
                break;
            case "OnFindAccelerator":
                OnFindAccelerator(new(), new()); Assert.Same(SearchBox, FocusManager.Focused); break;
            case "ViewModel.Porting.ImportCommand":
                f.Boundary.Files.ImportPath = Path.Combine(f.Root, "invalid.json"); File.WriteAllText(f.Boundary.Files.ImportPath, "{broken");
                await Execute(vm.Porting.ImportCommand); Assert.Equal(1, f.Boundary.Dialogs.Errors); break;
            case "ViewModel.Porting.ExportCommand":
                f.Host.Store.CreateSnippet(new("body", "label", true, null, ""));
                f.Boundary.Files.ExportPath = Path.Combine(f.Root, "export.json");
                await Execute(vm.Porting.ExportCommand); Assert.True(File.Exists(f.Boundary.Files.ExportPath)); break;
            case "OnListKeyDown":
                var id = f.Host.Store.CreateSnippet(new("body", "label", true, null, "")).Value!.Id;
                vm.List.SelectedItem = vm.List.Items.Single(r => r.Id == id);
                OnListKeyDown(this, new() { Key = VirtualKey.Delete }); Assert.Empty(f.Host.Store.Snippets); break;
            case "OnSnippetContainerContentChanging":
                f.Host.Store.CreateSnippet(new("body", "label", true, null, ""));
                var row = Assert.Single(vm.List.Items); var container = new ListViewItem { Content = row };
                OnSnippetContainerContentChanging(SnippetList, new() { ItemContainer = container });
                container.RaiseRightTap(new());
                var menu = Assert.IsType<MenuFlyout>(MenuFlyout.LastShown); Assert.Equal(4, menu.Items.Count);
                Assert.False(menu.Items[0].IsEnabled); Assert.False(menu.Items[1].IsEnabled);
                menu.Items[2].Invoke(); Assert.Equal(row.Id, vm.Editor!.SnippetId); break;
            default: throw new InvalidOperationException("Unmapped snippets action");
        }
        ViewModel = null; // detach the actual page's VM callbacks before disposing the composition
    }
}
