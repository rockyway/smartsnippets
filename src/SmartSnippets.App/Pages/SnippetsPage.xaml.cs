using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using SmartSnippets.Presentation.Navigation;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Presentation.ViewModels;
using Windows.System;

namespace SmartSnippets.App.Pages;

/// <summary>Master-detail snippet management: searchable list on the left, <see cref="Controls.SnippetEditor"/> on the right.</summary>
public sealed partial class SnippetsPage : Page, IGuardedPage
{
    private const string WideStateName = "Wide";

    private SnippetsPageViewModel? _viewModel;
    private ITextSizeService? _textSize;
    private bool _isWide;
    private bool _narrowShowsEditor;

    public SnippetsPage()
    {
        InitializeComponent();
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
            }

            _viewModel = value;
            if (_viewModel is not null)
            {
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
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

    // Instance (not static): x:Bind function bindings call these through the page instance.
    private Visibility Show(bool value) => value ? Visibility.Visible : Visibility.Collapsed;

    private Visibility Hide(bool value) => value ? Visibility.Collapsed : Visibility.Visible;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _isWide = LayoutStates.CurrentState?.Name == WideStateName;
        ApplyLayout();
    }

    private void OnLayoutStateChanged(object sender, VisualStateChangedEventArgs e)
    {
        _isWide = e.NewState?.Name == WideStateName;
        ApplyLayout();
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

    /// <summary>Wide: list and editor side by side. Narrow: one pane at a time, with a Back button on the editor.</summary>
    private void ApplyLayout()
    {
        if (_isWide)
        {
            ListColumn.Width = new GridLength(360);
            EditorColumn.Width = new GridLength(1, GridUnitType.Star);
            Grid.SetColumn(EditorPane, 1);
            ListPane.Visibility = Visibility.Visible;
            EditorPane.Visibility = Visibility.Visible;
            BackButton.Visibility = Visibility.Collapsed;
            return;
        }

        ListColumn.Width = new GridLength(1, GridUnitType.Star);
        EditorColumn.Width = new GridLength(0);
        Grid.SetColumn(EditorPane, 0);
        var showEditor = _narrowShowsEditor && _viewModel?.HasEditor == true;
        ListPane.Visibility = showEditor ? Visibility.Collapsed : Visibility.Visible;
        EditorPane.Visibility = showEditor ? Visibility.Visible : Visibility.Collapsed;
        BackButton.Visibility = showEditor ? Visibility.Visible : Visibility.Collapsed;
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

    private void OnListKeyDown(object sender, KeyRoutedEventArgs e)
    {
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
