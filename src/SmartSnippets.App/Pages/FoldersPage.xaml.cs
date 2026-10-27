using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using SmartSnippets.Presentation.Navigation;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Presentation.ViewModels;
using Windows.System;

namespace SmartSnippets.App.Pages;

/// <summary>Folder management: create, rename in place, delete. Row actions are Click handlers because DataTemplates cannot x:Bind page members.</summary>
public sealed partial class FoldersPage : Page, IGuardedPage
{
    private FoldersPageViewModel? _viewModel;
    private ITextSizeService? _textSize;

    public FoldersPage()
    {
        InitializeComponent();
    }

    /// <summary>Set by the creator right after construction (no DI container).</summary>
    public FoldersPageViewModel? ViewModel
    {
        get => _viewModel;
        set
        {
            _viewModel = value;
            Bindings.Update();
        }
    }

    /// <summary>Font size source for the folder list.</summary>
    public ITextSizeService? TextSize
    {
        get => _textSize;
        set
        {
            _textSize = value;
            Bindings.Update();
        }
    }

    public Task<bool> CanLeaveAsync() => _viewModel?.CanLeaveAsync() ?? Task.FromResult(true);

    private static FolderRowViewModel? RowOf(object sender) =>
        (sender as FrameworkElement)?.DataContext as FolderRowViewModel;

    private void OnNewFolderKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter && _viewModel is { } viewModel && viewModel.CreateCommand.CanExecute(null))
        {
            e.Handled = true;
            viewModel.CreateCommand.Execute(null);
        }
    }

    private void OnRenameClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel is null || RowOf(sender) is not { } row)
        {
            return;
        }

        _viewModel.BeginRenameCommand.Execute(row);
        FocusRenameBox(row);
    }

    private void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel is not null && RowOf(sender) is { } row)
        {
            _viewModel.DeleteCommand.Execute(row);
        }
    }

    private async void OnCommitRenameClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel is not null && RowOf(sender) is { } row)
        {
            await _viewModel.CommitRenameAsync(row);
        }
    }

    private void OnCancelRenameClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel is not null && RowOf(sender) is { } row)
        {
            _viewModel.CancelRename(row);
        }
    }

    private async void OnRenameKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (_viewModel is null || RowOf(sender) is not { } row)
        {
            return;
        }

        if (e.Key == VirtualKey.Enter)
        {
            e.Handled = true;
            await _viewModel.CommitRenameAsync(row);
        }
        else if (e.Key == VirtualKey.Escape)
        {
            e.Handled = true;
            _viewModel.CancelRename(row);
        }
    }

    /// <summary>Puts the cursor in the row's rename box once the row has switched to rename mode.</summary>
    private void FocusRenameBox(FolderRowViewModel row)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (FolderList.ContainerFromItem(row) is not DependencyObject container)
            {
                return;
            }

            var box = FindRenameBox(container);
            if (box is not null && box.Focus(FocusState.Programmatic))
            {
                box.SelectAll();
            }
        });
    }

    private static TextBox? FindRenameBox(DependencyObject parent)
    {
        var count = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is TextBox { Name: "RenameBox" } box)
            {
                return box;
            }

            if (FindRenameBox(child) is { } found)
            {
                return found;
            }
        }

        return null;
    }
}
