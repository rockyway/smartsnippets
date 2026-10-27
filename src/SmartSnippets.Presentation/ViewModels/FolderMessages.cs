using SmartSnippets.Core.Domain;
using SmartSnippets.Presentation.Text;

namespace SmartSnippets.Presentation.ViewModels;

/// <summary>Inline error text for folder-name failures, shared by the editor's inline folder box and the Folders page.</summary>
internal static class FolderMessages
{
    public static string For(ValidationError error, string attemptedName) => error switch
    {
        ValidationError.FolderNameDuplicate => UiText.FolderNameTaken,
        _ => error.ToMessage(),
    };
}
