using SmartSnippets.Core.Query;
using static SmartSnippets.Core.Tests.Query.QueryTestData;

namespace SmartSnippets.Core.Tests.Query;

public sealed class SnippetGroupingTests
{
    [Fact]
    public void FoldersByName_UnfiledLast_EmptyGroupsOmitted_OrderKept()
    {
        SnippetView[] sorted =
        [
            View(1, folder: "work", folderId: 10),
            View(2),
            View(3, folder: "Archive", folderId: 20),
            View(4, folder: "work", folderId: 10),
            View(5),
        ];

        var groups = SnippetGrouping.ByFolder(sorted);

        Assert.Equal(3, groups.Count);
        Assert.Equal(("Archive", 20L), (groups[0].FolderName, groups[0].FolderId));
        Assert.Equal([3L], Ids(groups[0].Items));
        Assert.Equal(("work", 10L), (groups[1].FolderName, groups[1].FolderId));
        Assert.Equal([1L, 4L], Ids(groups[1].Items));
        Assert.Null(groups[2].FolderId);
        Assert.Null(groups[2].FolderName);
        Assert.Equal([2L, 5L], Ids(groups[2].Items));
    }

    [Fact]
    public void OnlyFolderedItems_NoUnfiledGroup()
    {
        var groups = SnippetGrouping.ByFolder([View(1, folder: "A", folderId: 1)]);

        Assert.Single(groups);
    }

    [Fact]
    public void SameNameDifferentCase_TieBrokenByFolderId()
    {
        var groups = SnippetGrouping.ByFolder([
            View(1, folder: "dup", folderId: 7),
            View(2, folder: "DUP", folderId: 3),
        ]);

        Assert.Equal([3L, 7L], groups.Select(g => g.FolderId!.Value));
    }

    [Fact]
    public void Empty_ReturnsNoGroups()
    {
        Assert.Empty(SnippetGrouping.ByFolder([]));
    }
}
