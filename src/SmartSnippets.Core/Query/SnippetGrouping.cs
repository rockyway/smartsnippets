namespace SmartSnippets.Core.Query;

public static class SnippetGrouping
{
    /// <summary>Groups already-sorted items by folder; folders by name, Unfiled last, empty groups omitted.</summary>
    public static IReadOnlyList<SnippetGroup> ByFolder(IReadOnlyList<SnippetView> sorted)
    {
        var folders = new Dictionary<long, (string Name, List<SnippetView> Items)>();
        var unfiled = new List<SnippetView>();
        foreach (var view in sorted)
        {
            if (view.Snippet.FolderId is not { } id)
            {
                unfiled.Add(view);
            }
            else if (folders.TryGetValue(id, out var entry))
            {
                entry.Items.Add(view);
            }
            else
            {
                folders[id] = (view.FolderName ?? "", [view]);
            }
        }

        var groups = folders
            .Select(f => new SnippetGroup(f.Key, f.Value.Name, f.Value.Items))
            .OrderBy(g => g.FolderName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(g => g.FolderId)
            .ToList();
        if (unfiled.Count > 0)
        {
            groups.Add(new SnippetGroup(null, null, unfiled));
        }

        return groups;
    }
}
