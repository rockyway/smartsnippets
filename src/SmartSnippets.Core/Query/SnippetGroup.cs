namespace SmartSnippets.Core.Query;

/// <summary>A folder heading with its items; <see cref="FolderId"/> is null for Unfiled.</summary>
public sealed record SnippetGroup(long? FolderId, string? FolderName, IReadOnlyList<SnippetView> Items);
