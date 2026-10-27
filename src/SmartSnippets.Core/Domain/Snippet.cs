namespace SmartSnippets.Core.Domain;

public sealed record Snippet(
    long Id,
    string Text,
    string Label,
    bool IsLabelCustom,
    long? FolderId,
    IReadOnlyList<string> Tags,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? LastUsedAt,
    int UsageCount);
