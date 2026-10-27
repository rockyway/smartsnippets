using SmartSnippets.Core.Domain;

namespace SmartSnippets.Core.Query;

/// <summary>One row of the store snapshot; <see cref="FolderName"/> is null for Unfiled.</summary>
public sealed record SnippetView(Snippet Snippet, string? FolderName);
