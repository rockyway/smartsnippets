namespace SmartSnippets.Core.Domain;

public sealed record Folder(long Id, string Name, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
