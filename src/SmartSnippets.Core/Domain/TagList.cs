namespace SmartSnippets.Core.Domain;

public static class TagList
{
    private const char Separator = ',';

    public static IReadOnlyList<string> Parse(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return [];
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();
        foreach (var part in input.Split(Separator))
        {
            var tag = part.Trim();
            if (tag.Length > 0 && seen.Add(tag))
            {
                result.Add(tag);
            }
        }

        return result;
    }

    public static string Format(IEnumerable<string> tags) => string.Join(", ", tags);

    /// <summary>Compact form for the database column; tags never contain commas.</summary>
    public static string ToStorage(IEnumerable<string> tags) => string.Join(Separator, tags);

    public static IReadOnlyList<string> FromStorage(string? stored) =>
        string.IsNullOrEmpty(stored) ? [] : stored.Split(Separator);
}
