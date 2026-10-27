using System.Globalization;

namespace SmartSnippets.Core.Domain;

public static class LabelSuggester
{
    public const int MaxLength = 60;

    public static string Suggest(string text, IReadOnlyList<string> tags)
    {
        var source = tags.Count > 0 ? tags[0] : FirstNonBlankLine(text);
        return Truncate(source.Trim(), MaxLength);
    }

    /// <summary>
    /// Final label for a draft: automatic unless the user set a non-blank custom label.
    /// </summary>
    public static (string Label, bool IsCustom) ResolveLabel(SnippetDraft draft, IReadOnlyList<string> tags) =>
        !draft.IsLabelCustom || string.IsNullOrWhiteSpace(draft.Label)
            ? (Suggest(draft.Text, tags), false)
            : (draft.Label, true);

    private static string FirstNonBlankLine(string text)
    {
        var start = 0;
        while (start <= text.Length)
        {
            var end = text.AsSpan(start).IndexOfAny('\r', '\n');
            var line = end < 0 ? text[start..] : text.Substring(start, end);
            if (!string.IsNullOrWhiteSpace(line))
            {
                return line;
            }

            if (end < 0)
            {
                break;
            }

            start += end + 1;
        }

        return "";
    }

    private static string Truncate(string value, int maxElements)
    {
        var enumerator = StringInfo.GetTextElementEnumerator(value);
        var count = 0;
        while (enumerator.MoveNext())
        {
            if (++count > maxElements)
            {
                return value[..enumerator.ElementIndex];
            }
        }

        return value;
    }
}
