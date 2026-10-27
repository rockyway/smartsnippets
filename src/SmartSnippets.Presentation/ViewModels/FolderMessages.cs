using SmartSnippets.Core.Domain;

namespace SmartSnippets.Presentation.ViewModels;

/// <summary>Inline error text for folder-name failures, shared by the editor's flyout and the Folders page.</summary>
internal static class FolderMessages
{
    public static string For(ValidationError error, string attemptedName) => error switch
    {
        ValidationError.FolderNameDuplicate => $"A folder named '{FolderNameRules.Normalize(attemptedName)}' already exists.",
        _ => error.ToMessage(),
    };
}
