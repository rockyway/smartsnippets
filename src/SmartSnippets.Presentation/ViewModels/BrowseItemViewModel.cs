namespace SmartSnippets.Presentation.ViewModels;

public enum BrowseKind
{
    All,
    Folder,
    Unfiled,
}

/// <summary>One entry of the main window's Browse pane. <see cref="FolderId"/> is set only for <see cref="BrowseKind.Folder"/>.</summary>
public sealed record BrowseItemViewModel(BrowseKind Kind, long? FolderId, string Name, int Count)
{
    // The list item's UI Automation name (what Narrator reads) comes from ToString().
    public override string ToString() => $"{Name}, {Text.UiText.SnippetCount(Count)}";
}
