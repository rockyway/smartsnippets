using SmartSnippets.Core.Query;

namespace SmartSnippets.Presentation.ViewModels;

/// <summary>One row of the snippet list. Value equality lets the list skip a rebuild when nothing visible changed.</summary>
public sealed record SnippetRowViewModel(long Id, string Label, string FolderDisplay, string TagsDisplay)
{
    public const string UnfiledDisplay = "Unfiled";

    public static SnippetRowViewModel From(SnippetView view) => new(
        view.Snippet.Id,
        view.Snippet.Label,
        view.FolderName ?? UnfiledDisplay,
        string.Join(", ", view.Snippet.Tags));
}
