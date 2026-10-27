using SmartSnippets.Core.Settings;

namespace SmartSnippets.Core.Query;

public static class SnippetSort
{
    /// <summary>Descending by field; never-used last for LastUsed; ties by label (ascending) then id. Total order.</summary>
    public static IReadOnlyList<SnippetView> Apply(IEnumerable<SnippetView> items, SortField field)
    {
        var list = new List<SnippetView>(items);
        list.Sort((a, b) => Compare(a, b, field));
        return list;
    }

    private static int Compare(SnippetView a, SnippetView b, SortField field)
    {
        var x = a.Snippet;
        var y = b.Snippet;
        var c = field switch
        {
            SortField.LastUsed => CompareNullableDesc(x.LastUsedAt, y.LastUsedAt),
            SortField.UsageCount => y.UsageCount.CompareTo(x.UsageCount),
            SortField.Created => y.CreatedAt.CompareTo(x.CreatedAt),
            SortField.Updated => y.UpdatedAt.CompareTo(x.UpdatedAt),
            _ => 0,
        };
        if (c != 0)
        {
            return c;
        }

        c = string.Compare(x.Label, y.Label, StringComparison.OrdinalIgnoreCase);
        return c != 0 ? c : x.Id.CompareTo(y.Id);
    }

    private static int CompareNullableDesc(DateTimeOffset? x, DateTimeOffset? y) => (x, y) switch
    {
        (null, null) => 0,
        (null, _) => 1,
        (_, null) => -1,
        _ => y.Value.CompareTo(x.Value),
    };
}
