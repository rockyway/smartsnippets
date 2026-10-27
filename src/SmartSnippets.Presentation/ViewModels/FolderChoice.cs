namespace SmartSnippets.Presentation.ViewModels;

/// <summary>One entry of the editor's folder drop-down; <see cref="Id"/> is null for Unfiled.</summary>
public sealed record FolderChoice(long? Id, string Name)
{
    public const string UnfiledName = "Unfiled";

    public static FolderChoice Unfiled { get; } = new(null, UnfiledName);

    public bool IsUnfiled => Id is null;

    public override string ToString() => Name;
}
