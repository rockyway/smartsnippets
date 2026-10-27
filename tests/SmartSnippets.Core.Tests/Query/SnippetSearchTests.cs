using SmartSnippets.Core.Query;
using static SmartSnippets.Core.Tests.Query.QueryTestData;

namespace SmartSnippets.Core.Tests.Query;

public sealed class SnippetSearchTests
{
    [Fact]
    public void AndAcrossTerms_EachTermAnyField()
    {
        var search = new SnippetSearch([
            View(1, label: "Greeting", text: "hello world"),
            View(2, label: "Hello", text: "bye"),
            View(3, label: "Other", text: "greeting only"),
        ]);

        Assert.Equal([1L], Ids(search.Filter("greeting WORLD")));
        Assert.Equal([1L, 2L], Ids(search.Filter("hello")));
        Assert.Empty(search.Filter("hello nomatch"));
    }

    [Fact]
    public void MatchesFolderNameAndTags()
    {
        var search = new SnippetSearch([
            View(1, folder: "Invoices", tags: ["urgent", "Q4"]),
            View(2, tags: ["misc"]),
        ]);

        Assert.Equal([1L], Ids(search.Filter("invoice")));
        Assert.Equal([1L], Ids(search.Filter("q4")));
        Assert.Equal([1L], Ids(search.Filter("invoices urgent")));
        Assert.Empty(search.Filter("urgentmisc"));
    }

    [Fact]
    public void UnfiledIsNotSearchable()
    {
        var search = new SnippetSearch([View(1, label: "a", text: "b")]);

        Assert.Empty(search.Filter("unfiled"));
    }

    [Fact]
    public void CaseInsensitive_Unicode()
    {
        var search = new SnippetSearch([View(1, label: "Ärger", text: "ÜBER"), View(2, label: "x", text: "y")]);

        Assert.Equal([1L], Ids(search.Filter("ärGER")));
        Assert.Equal([1L], Ids(search.Filter("über")));
    }

    [Fact]
    public void AccentsAndSpecialFolding_AreNotFolded()
    {
        // Accepted per plan 003 R1: OrdinalIgnoreCase does not fold accents or ß/SS.
        var search = new SnippetSearch([View(1, label: "naïve"), View(2, label: "STRASSE"), View(3, label: "straße")]);

        Assert.Empty(search.Filter("naive"));
        Assert.Equal([2L], Ids(search.Filter("strasse")));
        Assert.Equal([3L], Ids(search.Filter("STRAßE")));
    }

    [Fact]
    public void TurkishDottedI_ActualBehaviour()
    {
        // Accepted per plan 003 R1: OrdinalIgnoreCase maps i<->I only; U+0130 (İ) is a different letter.
        var search = new SnippetSearch([View(1, label: "İstanbul"), View(2, label: "istanbul"), View(3, label: "ISTANBUL")]);

        Assert.Equal([2L, 3L], Ids(search.Filter("istanbul")));
        Assert.Equal([1L], Ids(search.Filter("İstanbul")));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(" \t\r\n ")]
    public void EmptyOrWhitespaceQuery_ReturnsAllInOrder(string? query)
    {
        var search = new SnippetSearch([View(3), View(1), View(2)]);

        Assert.Equal([3L, 1L, 2L], Ids(search.Filter(query)));
    }

    [Fact]
    public void PreservesInputOrder()
    {
        var search = new SnippetSearch([
            View(5, label: "zeta item"),
            View(2, label: "alpha item"),
            View(9, label: "mid item"),
            View(1, label: "nope"),
        ]);

        Assert.Equal([5L, 2L, 9L], Ids(search.Filter("item")));
    }

    [Fact]
    public void MultipleWhitespaceBetweenTerms_AndEdges()
    {
        var search = new SnippetSearch([View(1, label: "foo bar")]);

        Assert.Equal([1L], Ids(search.Filter("  foo \t  bar\n")));
    }

    [Fact]
    public void EmptySnapshot_ReturnsEmpty()
    {
        var search = new SnippetSearch([]);

        Assert.Empty(search.Filter("x"));
        Assert.Empty(search.Filter(null));
    }

    [Fact]
    public void TermDoesNotSpanFields()
    {
        var search = new SnippetSearch([View(1, label: "abc", text: "def")]);

        Assert.Empty(search.Filter("cd"));
    }
}
