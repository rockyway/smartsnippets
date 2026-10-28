using System.Text.Json.Serialization;

namespace SmartSnippets.Core.Porting;

internal sealed record NativeDocument(string Format, int Version, long ExportedAt, IReadOnlyList<NativeFolder> Folders, IReadOnlyList<NativeSnippet> Snippets);
internal sealed record NativeFolder(string Name);
internal sealed record NativeSnippet(string Text, string Label, bool IsLabelCustom, string? Folder, IReadOnlyList<string> Tags,
    int UsageCount, long CreatedAt, long UpdatedAt, long? LastUsedAt);

[JsonSourceGenerationOptions(WriteIndented = false, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(NativeDocument))]
internal sealed partial class NativeJsonContext : JsonSerializerContext;
