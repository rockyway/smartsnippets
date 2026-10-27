namespace SmartSnippets.Core.Query;

/// <summary>In-memory AND-of-terms substring search over a snapshot (OrdinalIgnoreCase).</summary>
public sealed class SnippetSearch
{
    private readonly SnippetView[] _items;
    private readonly string[][] _fields;

    public SnippetSearch(IReadOnlyList<SnippetView> items)
    {
        _items = [.. items];
        _fields = new string[_items.Length][];
        for (var i = 0; i < _items.Length; i++)
        {
            var s = _items[i].Snippet;
            var fields = new List<string>(3 + s.Tags.Count) { s.Label, s.Text };
            if (_items[i].FolderName is { } folder)
            {
                fields.Add(folder);
            }

            fields.AddRange(s.Tags);
            _fields[i] = [.. fields];
        }
    }

    public IReadOnlyList<SnippetView> Filter(string? query)
    {
        var terms = Split(query);
        if (terms.Count == 0)
        {
            return _items;
        }

        var result = new List<SnippetView>();
        for (var i = 0; i < _items.Length; i++)
        {
            if (MatchesAll(_fields[i], terms))
            {
                result.Add(_items[i]);
            }
        }

        return result;
    }

    private static bool MatchesAll(string[] fields, List<string> terms)
    {
        foreach (var term in terms)
        {
            var found = false;
            foreach (var field in fields)
            {
                if (field.Contains(term, StringComparison.OrdinalIgnoreCase))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                return false;
            }
        }

        return true;
    }

    private static List<string> Split(string? query)
    {
        var terms = new List<string>();
        if (string.IsNullOrEmpty(query))
        {
            return terms;
        }

        var start = -1;
        for (var i = 0; i <= query.Length; i++)
        {
            var isSpace = i == query.Length || char.IsWhiteSpace(query[i]);
            if (!isSpace && start < 0)
            {
                start = i;
            }
            else if (isSpace && start >= 0)
            {
                terms.Add(query[start..i]);
                start = -1;
            }
        }

        return terms;
    }
}
