using SmartSnippets.Core.Query;
using SmartSnippets.Core.Settings;
using static SmartSnippets.Core.Tests.Query.QueryTestData;

namespace SmartSnippets.Core.Tests.Query;

public sealed class SnippetSortTests
{
    [Fact]
    public void LastUsed_NeverUsedLast()
    {
        var sorted = SnippetSort.Apply(
            [View(1, label: "a"), View(2, label: "b", lastUsedDay: 3), View(3, label: "c", lastUsedDay: 7), View(4, label: "d")],
            SortField.LastUsed);

        Assert.Equal([3L, 2L, 1L, 4L], Ids(sorted));
    }

    [Fact]
    public void Ties_LabelThenId()
    {
        var sorted = SnippetSort.Apply(
            [View(3, label: "b", usage: 5), View(2, label: "B", usage: 5), View(1, label: "c", usage: 5), View(4, label: "a", usage: 5)],
            SortField.UsageCount);

        // Label OrdinalIgnoreCase ascending (a, b/B, c); b vs B tie broken by Id ascending.
        Assert.Equal([4L, 2L, 3L, 1L], Ids(sorted));
    }

    [Fact]
    public void EachField_Descending()
    {
        SnippetView[] items =
        [
            View(1, label: "a", usage: 1, createdDay: 5, updatedDay: 1, lastUsedDay: 1),
            View(2, label: "b", usage: 3, createdDay: 1, updatedDay: 9, lastUsedDay: 4),
            View(3, label: "c", usage: 2, createdDay: 9, updatedDay: 4, lastUsedDay: 2),
        ];

        Assert.Equal([2L, 3L, 1L], Ids(SnippetSort.Apply(items, SortField.UsageCount)));
        Assert.Equal([3L, 1L, 2L], Ids(SnippetSort.Apply(items, SortField.Created)));
        Assert.Equal([2L, 3L, 1L], Ids(SnippetSort.Apply(items, SortField.Updated)));
        Assert.Equal([2L, 3L, 1L], Ids(SnippetSort.Apply(items, SortField.LastUsed)));
    }

    [Fact]
    public void Deterministic_RegardlessOfInputOrder()
    {
        SnippetView[] items = [View(1, label: "x"), View(2, label: "x"), View(3, label: "x"), View(4, label: "y")];

        var forward = Ids(SnippetSort.Apply(items, SortField.Created));
        var reversed = Ids(SnippetSort.Apply(items.Reverse(), SortField.Created));

        Assert.Equal(forward, reversed);
        Assert.Equal([1L, 2L, 3L, 4L], forward);
    }

    [Fact]
    public void EmptyInput_ReturnsEmpty()
    {
        Assert.Empty(SnippetSort.Apply([], SortField.LastUsed));
    }
}
