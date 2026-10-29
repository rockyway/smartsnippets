using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Presentation.Text;
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
    private bool _isCompact;
    private bool _rebinding;

    public event EventHandler? EscapePressed;

    public bool IsCompact
    {
        get => _isCompact;
        set { _isCompact = value; Bindings.Update(); }
    }

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
            _rebinding = true;
            try
            {
                if (_viewModel is not null)
                    _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

                _viewModel = value;
                if (_viewModel is not null)
                    _viewModel.PropertyChanged += OnViewModelPropertyChanged;

                Bindings.Update();
            }
            finally { _rebinding = false; }
        }
    }

    private bool IsUserInput(Control control) => !_rebinding && _viewModel is not null && control.FocusState != FocusState.Unfocused;
    private void OnTextInput(object sender, TextChangedEventArgs e) { if (sender is TextBox box && IsUserInput(box)) _viewModel!.Text = box.Text; }
    private void OnLabelInput(object sender, TextChangedEventArgs e) { if (sender is TextBox box && IsUserInput(box)) _viewModel!.Label = box.Text; }
    private void OnTagsInput(object sender, TextChangedEventArgs e) { if (sender is TextBox box && IsUserInput(box)) _viewModel!.TagsInput = box.Text; }
    private void OnFolderNameInput(object sender, TextChangedEventArgs e) { if (sender is TextBox box && IsUserInput(box)) _viewModel!.NewFolderName = box.Text; }
    private void OnCustomLabelInput(object sender, RoutedEventArgs e) { if (sender is ToggleSwitch toggle && IsUserInput(toggle)) _viewModel!.IsLabelCustom = toggle.IsOn; }
    private void OnFolderSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox box && !_rebinding && (box.IsDropDownOpen || box.FocusState == FocusState.Keyboard)
            && box.SelectedItem is FolderChoice choice)
            _viewModel?.SelectFolder(choice);
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

    private Visibility Hide(bool value) => value ? Visibility.Collapsed : Visibility.Visible;

    // Wording comes from UiText (design reference); instance properties so x:Bind can reach them.
    private string CustomLabelText => UiText.CustomLabel;

    private string UnsavedChangesText => UiText.UnsavedChanges;

    private string DiscardText => UiText.Discard;

    private string SaveText => UiText.Save;

    private string TryAgainText => UiText.TryAgain;

    private string CopyTextLabel => UiText.CopyText;

    private Visibility HasText(string? value) => string.IsNullOrEmpty(value) ? Visibility.Collapsed : Visibility.Visible;

    private void OnSaveAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        if (_viewModel?.SaveCommand.CanExecute(null) == true)
        {
            _viewModel.SaveCommand.Execute(null);
        }
    }

    private void OnDiscardAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (_viewModel is null)
        {
            return;
        }

        args.Handled = true;

        // Esc while the inline new-folder row is open cancels that row, not the whole edit.
        if (_viewModel.IsCreatingFolder)
        {
            CancelNewFolder();
            return;
        }

        if (IsCompact)
        {
            EscapePressed?.Invoke(this, EventArgs.Empty);
            return;
        }

        // Esc is easy to hit by accident, so unlike the Discard button it asks first.
        _ = _viewModel.ConfirmDiscardAsync();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SnippetEditorViewModel.IsCreatingFolder) && _viewModel?.IsCreatingFolder == true)
        {
            // Defer until the row is visible; an invisible TextBox cannot take focus.
            DispatcherQueue.TryEnqueue(() => NewFolderBox.Focus(FocusState.Programmatic));
        }
    }

    private void OnNewFolderKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        if (e.Key == VirtualKey.Enter)
        {
            e.Handled = true;
            if (_viewModel.CreateFolderCommand.CanExecute(null))
            {
                _viewModel.CreateFolderCommand.Execute(null);
            }
        }
        else if (e.Key == VirtualKey.Escape)
        {
            e.Handled = true;
            CancelNewFolder();
        }
    }

    private void CancelNewFolder()
    {
        if (_viewModel?.CancelNewFolderCommand.CanExecute(null) == true)
        {
            _viewModel.CancelNewFolderCommand.Execute(null);
        }
    }
}
