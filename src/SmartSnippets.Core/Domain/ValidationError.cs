namespace SmartSnippets.Core.Domain;

public enum ValidationError
{
    TextRequired,
    LabelRequired,
    FolderNameRequired,
    FolderNameDuplicate,
    FolderNotFound,
    SnippetNotFound,
}

public static class ValidationErrorMessages
{
    public static string ToMessage(this ValidationError error) => error switch
    {
        ValidationError.TextRequired => "Snippet text cannot be empty.",
        ValidationError.LabelRequired => "Label cannot be empty.",
        ValidationError.FolderNameRequired => "Folder name cannot be empty.",
        ValidationError.FolderNameDuplicate => "A folder with this name already exists.",
        ValidationError.FolderNotFound => "The selected folder no longer exists.",
        ValidationError.SnippetNotFound => "The snippet no longer exists.",
        _ => "Invalid input.",
    };
}
