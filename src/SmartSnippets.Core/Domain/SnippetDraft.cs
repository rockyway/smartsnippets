namespace SmartSnippets.Core.Domain;

/// <summary>What an editor submits; <see cref="TagsInput"/> is the raw comma-separated text.</summary>
public sealed record SnippetDraft(string Text, string Label, bool IsLabelCustom, long? FolderId, string TagsInput);
