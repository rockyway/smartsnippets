using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Presentation.Text;

namespace SmartSnippets.App.Services;

/// <summary>ContentDialog host. Dialogs target the main window; it is shown first when hidden (no XamlRoot yet).</summary>
internal sealed class DialogService : IDialogService
{
    private readonly Func<XamlRoot?> _getXamlRoot;
    private readonly Action _showMainWindow;
    private readonly ITextSizeService? _textSize;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public DialogService(Func<XamlRoot?> getXamlRoot, Action showMainWindow, ITextSizeService? textSize = null)
    {
        _textSize = textSize;
        _getXamlRoot = getXamlRoot;
        _showMainWindow = showMainWindow;
    }

    public async Task<bool> ConfirmAsync(string title, string message, string confirmText)
    {
        var result = await ShowAsync(new ContentDialog
        {
            Title = title,
            Content = Body(message),
            PrimaryButtonText = confirmText,
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
        });
        return result == ContentDialogResult.Primary;
    }

    public async Task<SaveChoice> AskSaveDiscardCancelAsync(string title, string message)
    {
        var result = await ShowAsync(new ContentDialog
        {
            Title = title,
            Content = Body(message),
            PrimaryButtonText = UiText.Save,
            SecondaryButtonText = UiText.Discard,
            CloseButtonText = UiText.KeepEditing,
            DefaultButton = ContentDialogButton.Primary,
        });
        return result switch
        {
            ContentDialogResult.Primary => SaveChoice.Save,
            ContentDialogResult.Secondary => SaveChoice.Discard,
            _ => SaveChoice.Cancel,
        };
    }

    public async Task ShowErrorAsync(string title, string message)
    {
        await ShowAsync(new ContentDialog
        {
            Title = title,
            Content = Body(message),
            CloseButtonText = "OK",
            DefaultButton = ContentDialogButton.Close,
        });
    }

    public async Task ShowInfoAsync(string title, string message)
    {
        var body = Body(message);
        body.IsTextSelectionEnabled = true;
        if (_textSize is not null) body.FontSize = _textSize.FontSize;
        await ShowAsync(new ContentDialog
        {
            Title = title,
            Content = body,
            CloseButtonText = "OK",
            DefaultButton = ContentDialogButton.Close,
        });
    }

    private static TextBlock Body(string message) => new() { Text = message, TextWrapping = TextWrapping.Wrap };

    private async Task<ContentDialogResult> ShowAsync(ContentDialog dialog)
    {
        await _gate.WaitAsync();
        try
        {
            var root = _getXamlRoot();
            if (root is null)
            {
                _showMainWindow();
                root = _getXamlRoot();
            }

            if (root is null)
            {
                return ContentDialogResult.None;
            }

            dialog.XamlRoot = root;
            if (root.Content is FrameworkElement content)
            {
                dialog.RequestedTheme = content.RequestedTheme;
            }

            return await dialog.ShowAsync();
        }
        finally
        {
            _gate.Release();
        }
    }
}
