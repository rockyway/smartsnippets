using SmartSnippets.Core.Domain;

namespace SmartSnippets.Core.Porting;

public sealed record PlannedSnippet(string Text, string Label, bool IsLabelCustom, string? FolderKey, IReadOnlyList<string> Tags, ImportStats? Stats);
public sealed record ImportPlan(IReadOnlyList<PlannedSnippet> Snippets, IReadOnlyList<string> NewFolderNames, IReadOnlyDictionary<string, long> ExistingFolderIds, int SkippedDuplicates, int Rejected);

internal readonly record struct FullRowKey(string Text, string Label, string FolderKey, string[] Tags);

internal sealed class FullRowKeyComparer : IEqualityComparer<FullRowKey>
{
    internal static FullRowKeyComparer Instance { get; } = new();
    public bool Equals(FullRowKey a, FullRowKey b) =>
        string.Equals(a.Text, b.Text, StringComparison.Ordinal) &&
        string.Equals(a.Label, b.Label, StringComparison.Ordinal) &&
        string.Equals(a.FolderKey, b.FolderKey, StringComparison.Ordinal) &&
        a.Tags.AsSpan().SequenceEqual(b.Tags);
    public int GetHashCode(FullRowKey key)
    {
        var hash = new HashCode();
        hash.Add(StringComparer.Ordinal.GetHashCode(key.Text));
        hash.Add(StringComparer.Ordinal.GetHashCode(key.Label));
        hash.Add(StringComparer.Ordinal.GetHashCode(key.FolderKey));
        foreach (var tag in key.Tags) hash.Add(StringComparer.Ordinal.GetHashCode(tag));
        return hash.ToHashCode();
    }
}

public static class ImportPlanner
{
    public static ImportPlan Plan(ImportBatch batch, IReadOnlyList<Snippet> existing, IReadOnlyList<Folder> folders)
    {
        var byId = folders.ToDictionary(f => f.Id);
        var byKey = folders.ToDictionary(f => FolderNameRules.Key(f.Name), f => f.Id, StringComparer.Ordinal);
        var usedExisting = new Dictionary<string, long>(StringComparer.Ordinal);
        var newNames = new Dictionary<string, string>(StringComparer.Ordinal);
        var textKeys = new HashSet<string>(StringComparer.Ordinal);
        var rowKeys = new HashSet<FullRowKey>(FullRowKeyComparer.Instance);
        foreach (var snippet in existing)
        {
            var text = LineEndings.ToCrLf(snippet.Text);
            if (batch.Dedupe == DedupeMode.TextOnly) textKeys.Add(text);
            else
            {
                string folderKey = snippet.FolderId is { } id && byId.TryGetValue(id, out var folder) ? FolderNameRules.Key(folder.Name) : "";
                rowKeys.Add(new(text, snippet.Label, folderKey, TagList.Parse(string.Join(',', snippet.Tags)).ToArray()));
            }
        }
        void EnsureFolder(string name, string key)
        {
            if (byKey.TryGetValue(key, out var id)) usedExisting.TryAdd(key, id);
            else newNames.TryAdd(key, FolderNameRules.Normalize(name));
        }
        foreach (var name in batch.FolderNames)
            if (FolderNameRules.Validate(name) is null) EnsureFolder(name, FolderNameRules.Key(name));

        var snippets = new List<PlannedSnippet>();
        int duplicates = 0, rejected = 0;
        foreach (var candidate in batch.Candidates)
        {
            var tags = TagList.Parse(string.Join(',', candidate.Tags));
            bool custom = !string.IsNullOrWhiteSpace(candidate.Label);
            var label = custom ? candidate.Label! : LabelSuggester.Suggest(candidate.Text, tags);
            string? folderKey = candidate.FolderName is { } name && FolderNameRules.Validate(name) is null ? FolderNameRules.Key(name) : null;
            if (SnippetValidator.Validate(candidate.Text, label).Count != 0) { rejected++; continue; }
            var textKey = LineEndings.ToCrLf(candidate.Text);
            bool added = batch.Dedupe == DedupeMode.TextOnly ? textKeys.Add(textKey) : rowKeys.Add(new(textKey, label, folderKey ?? "", tags.ToArray()));
            if (!added) { duplicates++; continue; }
            if (folderKey is not null) EnsureFolder(candidate.FolderName!, folderKey);
            snippets.Add(new(candidate.Text, label, custom, folderKey, tags, candidate.Stats));
        }
        return new(snippets, newNames.Values.ToArray(), usedExisting, duplicates, rejected);
    }
}
