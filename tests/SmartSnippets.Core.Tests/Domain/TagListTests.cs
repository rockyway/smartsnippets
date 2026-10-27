using SmartSnippets.Core.Domain;

namespace SmartSnippets.Core.Tests.Domain;

public sealed class TagListTests
{
    [Fact]
    public void Parse_TrimsDropsEmptyDedupesKeepingFirstSpelling()
    {
        var tags = TagList.Parse("  Work , ,home,WORK, Home ,  ,x");

        Assert.Equal(["Work", "home", "x"], tags);
    }

    [Fact]
    public void FormatThenParse_RoundTrips()
    {
        IReadOnlyList<string> tags = ["Work", "home office", "x"];

        Assert.Equal("Work, home office, x", TagList.Format(tags));
        Assert.Equal(tags, TagList.Parse(TagList.Format(tags)));
    }

    [Fact]
    public void CaseOnlyDuplicates_KeepFirst()
    {
        Assert.Equal(["Alpha"], TagList.Parse("Alpha,alpha,ALPHA"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(",,,")]
    [InlineData(" , ,, ")]
    public void NoRealTags_ReturnsEmpty(string? input)
    {
        Assert.Empty(TagList.Parse(input));
    }

    [Fact]
    public void Storage_RoundTrips()
    {
        IReadOnlyList<string> tags = ["a b", "C"];

        var stored = TagList.ToStorage(tags);

        Assert.Equal("a b,C", stored);
        Assert.Equal(tags, TagList.FromStorage(stored));
    }

    [Fact]
    public void FromStorage_EmptyIsEmptyList()
    {
        Assert.Empty(TagList.FromStorage(""));
        Assert.Empty(TagList.ToStorage([]));
    }
}
