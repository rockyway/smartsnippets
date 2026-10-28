namespace SmartSnippets.Presentation.Services;

public enum SaveChoice
{
    Save,
    Discard,
    Cancel,
}

/// <summary>Modal dialogs; implementations serialize calls so only one dialog is open at a time.</summary>
public interface IDialogService
{
    Task<bool> ConfirmAsync(string title, string message, string confirmText);

    Task<SaveChoice> AskSaveDiscardCancelAsync(string title, string message);

    Task ShowErrorAsync(string title, string message);

    /// <summary>Informational dialog with one OK button; body uses snippet text size.</summary>
    Task ShowInfoAsync(string title, string message);
}
