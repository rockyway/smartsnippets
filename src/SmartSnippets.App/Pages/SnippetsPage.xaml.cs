using System.ComponentModel;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using SmartSnippets.App.Services;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using SmartSnippets.Core.Settings;
using SmartSnippets.Presentation.Navigation;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Presentation.Text;
using SmartSnippets.Presentation.ViewModels;
using Windows.System;

namespace SmartSnippets.App.Pages;

/// <summary>Master-detail snippet management: Browse pane, searchable list and <see cref="Controls.SnippetEditor"/> side by side.</summary>
public sealed partial class SnippetsPage : Page, IGuardedPage
{
    // Browse 200 + list 320 + an editor that still fits its fields (~360).
    private const double WideMinWidth = 880;

    private SnippetsPageViewModel? _viewModel;
    private ITextSizeService? _textSize;
    private bool _isWide;
    private bool _narrowShowsEditor;

    public SnippetsPage()
    {
        InitializeComponent();
        FlyoutSurface.KeepSolid(SortFlyout);
    }

    /// <summary>Set by the creator right after construction (no DI container).</summary>
    public SnippetsPageViewModel? ViewModel
    {
        get => _viewModel;
        set
        {
            if (_viewModel is not null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
                _viewModel.EditorRevealRequested -= OnEditorRevealRequested;
            }

            _viewModel = value;
            if (_viewModel is not null)
            {
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
                _viewModel.EditorRevealRequested += OnEditorRevealRequested;
            }

            Bindings.Update();
            SyncEditor();
        }
    }

    /// <summary>Font size source for the list and the editor.</summary>
    public ITextSizeService? TextSize
    {
        get => _textSize;
        set
        {
            _textSize = value;
            Bindings.Update();
            EditorControl.TextSize = value;
        }
    }

    public Task<bool> CanLeaveAsync() => _viewModel?.CanLeaveAsync() ?? Task.FromResult(true);

    public string ImportGlyph => UiIcons.Import;
    // Debounced like the hotkey popup: apply the query once typing pauses.
    private Microsoft.UI.Dispatching.DispatcherQueueTimer? _searchTimer;
    private void OnSearchChanged(object sender, TextChangedEventArgs e)
    {
        if (_viewModel is null) return;
        _searchTimer ??= CreateSearchTimer();
        _searchTimer.Stop();
        if (SearchBox.Text != _viewModel.List.Query) _searchTimer.Start();
    }
    private Microsoft.UI.Dispatching.DispatcherQueueTimer CreateSearchTimer()
    {
        var timer = DispatcherQueue.CreateTimer();
        timer.Interval = TimeSpan.FromMilliseconds(120); timer.IsRepeating = false;
        timer.Tick += (_, _) => { timer.Stop(); if (_viewModel is not null) _viewModel.List.Query = SearchBox.Text; };
        return timer;
    }
    private string LastUsedText => UiText.SortName(SortField.LastUsed);
    private string UsageCountText => UiText.SortName(SortField.UsageCount);
    private string CreatedText => UiText.SortName(SortField.Created);
    private string UpdatedText => UiText.SortName(SortField.Updated);
    private void OnSortClick(object sender, RoutedEventArgs e)
    {
        var sort = _viewModel?.List.Sort ?? SortField.LastUsed;
        SortLast.IsChecked = sort == SortField.LastUsed; SortUsage.IsChecked = sort == SortField.UsageCount;
        SortCreated.IsChecked = sort == SortField.Created; SortUpdated.IsChecked = sort == SortField.Updated;
    }
    private void OnSortLast(object sender, RoutedEventArgs e) => _viewModel?.List.SetSort(SortField.LastUsed);
    private void OnSortUsage(object sender, RoutedEventArgs e) => _viewModel?.List.SetSort(SortField.UsageCount);
    private void OnSortCreated(object sender, RoutedEventArgs e) => _viewModel?.List.SetSort(SortField.Created);
    private void OnSortUpdated(object sender, RoutedEventArgs e) => _viewModel?.List.SetSort(SortField.Updated);
    public string ExportGlyph => UiIcons.Export;

    // Instance (not static): x:Bind function bindings call these through the page instance.
    private Visibility Show(bool value) => value ? Visibility.Visible : Visibility.Collapsed;

    private Visibility Hide(bool value) => value ? Visibility.Collapsed : Visibility.Visible;

    private Visibility HasText(string? value) => string.IsNullOrEmpty(value) ? Visibility.Collapsed : Visibility.Visible;

    private void OnBrowseNewFolderClick(object sender, RoutedEventArgs e)
    {
        _viewModel?.List.StartNewFolderCommand.Execute(null);
        // Defer until the row is visible; an invisible TextBox cannot take focus.
        DispatcherQueue.TryEnqueue(() => BrowseNewFolderBox.Focus(FocusState.Programmatic));
    }

    private void OnBrowseNewFolderKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        if (e.Key == VirtualKey.Enter)
        {
            e.Handled = true;
            _viewModel.List.CreateFolderCommand.Execute(null);
        }
        else if (e.Key == VirtualKey.Escape)
        {
            e.Handled = true;
            _viewModel.List.CancelNewFolderCommand.Execute(null);
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _isWide = Root.ActualWidth >= WideMinWidth;
        ApplyLayout();
    }

    private void OnRootSizeChanged(object sender, SizeChangedEventArgs e)
    {
        var wide = e.NewSize.Width >= WideMinWidth;
        if (wide != _isWide)
        {
            _isWide = wide;
            ApplyLayout();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SnippetsPageViewModel.Editor))
        {
            SyncEditor();
        }
    }

    /// <summary>The editor control's ViewModel is a plain property, so the page pushes the current editor into it.</summary>
    private void SyncEditor()
    {
        var editor = _viewModel?.Editor;
        EditorControl.ViewModel = editor;
        EditorControl.TextSize = _textSize;
        _narrowShowsEditor = editor is not null;
        ApplyLayout();
    }

    /// <summary>
    /// Wide: browse, list and editor side by side. Narrow: the browse pane becomes a ComboBox above the list and
    /// one pane (list or editor) shows at a time, with a Back button on the editor.
    /// </summary>
    private void ApplyLayout()
    {
        if (_isWide)
        {
            BrowseColumn.Width = new GridLength(200);
            ListColumn.Width = new GridLength(320);
            EditorColumn.Width = new GridLength(1, GridUnitType.Star);
            Grid.SetColumn(EditorPane, 2);
            BrowsePane.Visibility = Visibility.Visible;
            BrowseCombo.Visibility = Visibility.Collapsed;
            ListPane.Visibility = Visibility.Visible;
            EditorPane.Visibility = Visibility.Visible;
            BackButton.Visibility = Visibility.Collapsed;
            return;
        }

        BrowseColumn.Width = new GridLength(0);
        ListColumn.Width = new GridLength(1, GridUnitType.Star);
        EditorColumn.Width = new GridLength(0);
        Grid.SetColumn(EditorPane, 1);
        BrowsePane.Visibility = Visibility.Collapsed;
        BrowseCombo.Visibility = Visibility.Visible;
        var showEditor = _narrowShowsEditor && _viewModel?.HasEditor == true;
        ListPane.Visibility = showEditor ? Visibility.Collapsed : Visibility.Visible;
        EditorPane.Visibility = showEditor ? Visibility.Visible : Visibility.Collapsed;
        BackButton.Visibility = showEditor ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnEditorRevealRequested(object? sender, EventArgs e)
    {
        _narrowShowsEditor = true;
        ApplyLayout();
    }

    private void OnBackClick(object sender, RoutedEventArgs e)
    {
        _narrowShowsEditor = false;
        ApplyLayout();
    }

    private void OnItemClick(object sender, ItemClickEventArgs e)
    {
        // Clicking the already selected row raises no selection change, so narrow mode switches panes here.
        _narrowShowsEditor = true;
        ApplyLayout();
    }

    private void OnSnippetContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs e)
    {
        e.ItemContainer.RightTapped -= OnSnippetRightTapped;
        e.ItemContainer.RightTapped += OnSnippetRightTapped;
    }

    private void OnSnippetRightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (sender is not ListViewItem container || container.Content is not SnippetRowViewModel row) return;
        e.Handled = true;
        ShowSnippetMenu(container, row, e.GetPosition(container));
    }

    private void ShowSnippetMenu(ListViewItem container, SnippetRowViewModel row, Windows.Foundation.Point? position = null)
    {
        if (_viewModel is not { } vm) return;
        var menu = new MenuFlyout { MenuFlyoutPresenterStyle = (Style)Application.Current.Resources["SolidMenuFlyoutPresenterStyle"] };
        FlyoutSurface.KeepSolid(menu);
        var copy = new MenuFlyoutItem { Text = UiText.RowActionCopy };
        var insert = new MenuFlyoutItem { Text = UiText.RowActionInsert, IsEnabled = vm.CanInsertRow };
        var edit = new MenuFlyoutItem { Text = UiText.RowActionEdit };
        var delete = new MenuFlyoutItem { Text = UiText.RowActionDelete };
        copy.Click += (_, _) => vm.CopyRow(row.Id);
        insert.Click += async (_, _) => await vm.InsertRowAsync(row.Id);
        edit.Click += async (_, _) => await vm.RequestEditAsync(row.Id);
        delete.Click += async (_, _) => await vm.DeleteRowAsync(row.Id);
        menu.Items.Add(copy); menu.Items.Add(insert); menu.Items.Add(edit); menu.Items.Add(delete);
        if (position is { } point) menu.ShowAt(container, new Microsoft.UI.Xaml.Controls.Primitives.FlyoutShowOptions { Position = point });
        else menu.ShowAt(container); // Keyboard invocation remains anchored to the selected row.
    }

    private void OnListKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if ((e.Key == VirtualKey.Application || (e.Key == VirtualKey.F10 &&
            (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift) & Windows.UI.Core.CoreVirtualKeyStates.Down) != 0)) &&
            SnippetList.SelectedItem is SnippetRowViewModel row && SnippetList.ContainerFromItem(row) is ListViewItem container)
        {
            e.Handled = true;
            ShowSnippetMenu(container, row);
            return;
        }
        if (e.Key == VirtualKey.Delete && _viewModel is { } viewModel && viewModel.DeleteSelectedCommand.CanExecute(null))
        {
            e.Handled = true;
            viewModel.DeleteSelectedCommand.Execute(null);
        }
    }

    private void OnNewAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        _viewModel?.NewCommand.Execute(null);
    }

    private void OnFindAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        if (!_isWide)
        {
            _narrowShowsEditor = false;
            ApplyLayout();
        }

        SearchBox.Focus(FocusState.Programmatic);
        SearchBox.SelectAll();
    }
}
