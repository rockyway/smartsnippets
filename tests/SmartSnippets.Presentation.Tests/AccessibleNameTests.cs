using SmartSnippets.Presentation.ViewModels;

namespace SmartSnippets.Presentation.Tests;

/// <summary>
/// WinUI list items take their UI Automation name (what Narrator reads) from ToString() of the bound item.
/// Without these overrides it reads the record/type name ("BrowseItemViewModel { Kind = All, ... }").
/// </summary>
public sealed class AccessibleNameTests
{
    [Fact]
    public void BrowseItem_ReadsAsNameAndCount()
    {
        Assert.Equal("Clients, 2 snippets", new BrowseItemViewModel(BrowseKind.Folder, 1, "Clients", 2).ToString());
        Assert.Equal("All, 1 snippet", new BrowseItemViewModel(BrowseKind.All, null, "All", 1).ToString());
    }

    [Fact]
    public void SnippetRow_ReadsAsLabelAndMeta()
    {
        Assert.Equal("Greeting, Clients, used once", new SnippetRowViewModel(1, "Greeting", "Hello", "Clients, used once").ToString());
    }

    [Fact]
    public void PopupItem_ToString_SnippetAndHeading()
    {
        var row = new PopupItemViewModel(PopupItemKind.Snippet, 1, "Refund policy", "", "Support", "support", "billing", "");
        var heading = new PopupItemViewModel(PopupItemKind.Heading, 0, "", "", "", "", "", "Support (2)");
        Assert.Equal("Refund policy, Support, support, billing", row.ToString());
        Assert.Equal("Support, 2 snippets", heading.ToString());
    }

    [Fact]
    public void FolderRow_ReadsAsNameAndCount()
    {
        Assert.Equal("Clients, 3 snippets", new FolderRowViewModel(1, "Clients", 3).ToString());
        Assert.Equal("Unfiled, 0 snippets, built in", new FolderRowViewModel(0, "Unfiled", 0, isBuiltIn: true).ToString());
    }
}
