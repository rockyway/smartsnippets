using System.Text.RegularExpressions;
using SmartSnippets.Core.Query;
using SmartSnippets.Presentation.Text;
using SmartSnippets.Presentation.Services;

namespace SmartSnippets.Presentation.ViewModels;

/// <summary>One row of the snippet list. Value equality lets the list skip a rebuild when nothing visible changed.</summary>
public sealed partial record SnippetRowViewModel(long Id, string Label, string Preview, string Meta)
{
    public const int PreviewMaxLength = 80;

    /// <summary>Shared observable source; individual template TextBlocks must bind explicitly.</summary>
    public ITextSizeService? TextSize { get; init; }
    public string FolderName { get; init; } = string.Empty;
    public string TagsDisplay { get; init; } = string.Empty;
    public bool HasFolder => FolderName.Length != 0;
    public string Details => string.Join(" ", new[] { FolderName, TagsDisplay }.Where(s => !string.IsNullOrEmpty(s)));

    // Narrator gets the same label and metadata as the visible row, not snippet text.
    public override string ToString() => string.Join(", ", new[] { Label, FolderName, TagsDisplay }.Where(s => !string.IsNullOrEmpty(s)));

    public static SnippetRowViewModel From(SnippetView view, ITextSizeService? textSize = null) => new(
        view.Snippet.Id,
        view.Snippet.Label,
        PreviewOf(view.Snippet.Text),
        UiText.RowMeta(view.FolderName ?? UiText.Unfiled, view.Snippet.UsageCount))
        {
            TextSize = textSize,
            FolderName = view.FolderName ?? string.Empty,
            TagsDisplay = string.Join(" ", view.Snippet.Tags.Select(tag => "#" + tag)),
        };

    /// <summary>First non-blank line, whitespace runs collapsed to one space, cut to 80 chars plus an ellipsis.</summary>
    public static string PreviewOf(string text)
    {
        foreach (var line in text.Split('\n'))
        {
            var collapsed = Whitespace().Replace(line, " ").Trim();
            if (collapsed.Length == 0)
            {
                continue;
            }

            if (collapsed.Length <= PreviewMaxLength)
            {
                return collapsed;
            }

            var cut = char.IsHighSurrogate(collapsed[PreviewMaxLength - 1]) ? PreviewMaxLength - 1 : PreviewMaxLength;
            return collapsed[..cut] + "…";
        }

        return string.Empty;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
