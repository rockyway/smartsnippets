using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Presentation.ViewModels;
using Windows.System;

namespace SmartSnippets.App.Controls;

/// <summary>
/// Snippet editor bound to one <see cref="SnippetEditorViewModel"/>. Host-agnostic: the Snippets page hosts it
/// today and the popup's "Add snippet" (plan 006) hosts it unchanged. Hosts set the properties in code.
/// </summary>
public sealed partial class SnippetEditor : UserControl
{
    private SnippetEditorViewModel? _viewModel;
    private ITextSizeService? _textSize;

    public SnippetEditor()
    {
        InitializeComponent();
    }

    /// <summary>The edit session to show; null clears the editor. Not a dependency property, so hosts set it in code.</summary>
    public SnippetEditorViewModel? ViewModel
    {
        get => _viewModel;
        set
        {
            _viewModel = value;
            Bindings.Update();
        }
    }

    /// <summary>Font size source for the text, label and tags boxes.</summary>
    public ITextSizeService? TextSize
    {
        get => _textSize;
        set
        {
            _textSize = value;
            Bindings.Update();
        }
    }

    /// <summary>Moves focus to the text box (hosts call this when a new editor opens).</summary>
    public bool FocusText() => SnippetTextBox.Focus(FocusState.Programmatic);

    // Instance (not static): x:Bind function bindings call these through the control instance.
    private Visibility Show(bool value) => value ? Visibility.Visible : Visibility.Collapsed;

    private Visibility HasText(string? value) => string.IsNullOrEmpty(value) ? Visibility.Collapsed : Visibility.Visible;

    private void OnSaveAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        if (_viewModel?.SaveCommand.CanExecute(null) == true)
        {
            _viewModel.SaveCommand.Execute(null);
        }
    }

    private void OnRevertAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (_viewModel is null)
        {
            return;
        }

        args.Handled = true;
        _viewModel.RevertCommand.Execute(null);
    }

    private void OnFolderFlyoutOpened(object? sender, object e)
    {
        if (sender is Flyout { Content: Panel { Children.Count: > 0 } panel } && panel.Children[0] is Control first)
        {
            first.Focus(FocusState.Programmatic);
        }
    }

    private async void OnCreateFolderClick(object sender, RoutedEventArgs e) => await CreateFolderAsync();

    private async void OnNewFolderKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            e.Handled = true;
            await CreateFolderAsync();
        }
    }

    private async Task CreateFolderAsync()
    {
        if (_viewModel is null)
        {
            return;
        }

        if (await _viewModel.CreateFolderAsync())
        {
            FolderButton.Flyout?.Hide();
        }
    }
}
