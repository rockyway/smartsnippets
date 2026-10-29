using SmartSnippets.Presentation.Navigation;

namespace SmartSnippets.Presentation.Tests;

public sealed class LaunchOptionsTests
{
    [Fact]
    public void UnknownArgumentsDoNotRequestNavigation()
    {
        var options = LaunchOptions.Parse(["--background", "--unknown", "foo"]);
        Assert.False(options.HasRequest);
        Assert.Null(options.PreviewQuery);
    }

    [Theory]
    [InlineData("snippets", PageKind.Snippets)]
    [InlineData("folders", PageKind.Folders)]
    [InlineData("SETTINGS", PageKind.Settings)]
    public void ParsesPage(string page, PageKind expected) =>
        Assert.Equal(expected, LaunchOptions.Parse(["--page", page]).Page);

    [Fact]
    public void ParsesPreviewAndQuotedRedirectedQuery()
    {
        var args = LaunchOptions.SplitCommandLine("--page settings --preview-popup --preview-popup-query \"two words\"");
        var options = LaunchOptions.Parse(args);
        Assert.Equal(PageKind.Settings, options.Page);
        Assert.True(options.PreviewPopup);
        Assert.Equal("two words", options.PreviewQuery);
    }

    [Fact]
    public void QueryAloneDoesNotOpenPopupAndInvalidPageIsIgnored()
    {
        var options = LaunchOptions.Parse(["--page", "bad", "--preview-popup-query", "secret"]);
        Assert.False(options.HasRequest);
        Assert.Null(options.PreviewQuery);
    }

    [Fact]
    public void MissingPageValueDoesNotSwallowPreviewFlag()
    {
        var options = LaunchOptions.Parse(["--page", "--preview-popup"]);
        Assert.Null(options.Page);
        Assert.True(options.PreviewPopup);
    }

    [Fact]
    public void DoubledQuoteInQuotedArgumentMatchesInitialLaunch()
    {
        var initial = new[] { "--preview-popup", "--preview-popup-query", "a\"b" };
        var redirected = LaunchOptions.SplitCommandLine("--preview-popup --preview-popup-query \"a\"\"b\"");
        Assert.Equal(initial, redirected);
        Assert.Equal(LaunchOptions.Parse(initial), LaunchOptions.Parse(redirected));
    }

    [Theory]
    [InlineData("--preview-popup --preview-popup-sort-menu")]
    [InlineData("--PREVIEW-POPUP-SORT-MENU --PREVIEW-POPUP")]
    public void SortMenuPreviewMatchesInitialAndRedirectedLaunch(string commandLine)
    {
        var initial = LaunchOptions.Parse(commandLine.Split(' '));
        var redirected = LaunchOptions.Parse(LaunchOptions.SplitCommandLine(commandLine));
        Assert.True(initial.PreviewPopup);
        Assert.True(initial.PreviewPopupSortMenu);
        Assert.True(initial.HasRequest);
        Assert.Equal(initial, redirected);
    }

    [Fact]
    public void SortMenuFlagAloneDoesNotRequestPopup()
    {
        var options = LaunchOptions.Parse(["--preview-popup-sort-menu"]);
        Assert.False(options.HasRequest);
        Assert.False(options.PreviewPopupSortMenu);
    }

    [Fact]
    public void SortMenuFlagDoesNotGetConsumedAsMissingQueryValue()
    {
        var options = LaunchOptions.Parse(["--preview-popup", "--preview-popup-query", "--preview-popup-sort-menu"]);
        Assert.Null(options.PreviewQuery);
        Assert.True(options.PreviewPopupSortMenu);
    }

    [Fact]
    public void SortMenuPreviewRetainsQueryAndPage()
    {
        var initial = LaunchOptions.Parse(["--page", "settings", "--preview-popup", "--preview-popup-sort-menu", "--preview-popup-query", "two words"]);
        var redirected = LaunchOptions.Parse(LaunchOptions.SplitCommandLine("--page settings --preview-popup --preview-popup-sort-menu --preview-popup-query \"two words\""));
        Assert.Equal(PageKind.Settings, initial.Page);
        Assert.Equal("two words", initial.PreviewQuery);
        Assert.True(initial.PreviewPopupSortMenu);
        Assert.Equal(initial, redirected);
    }

    [Fact]
    public void SplitRetainsQuotedEscapesAndEmptyArguments()
    {
        Assert.Equal(new[] { "a", "", "x\"y", @"c:\tmp" },
            LaunchOptions.SplitCommandLine("a \"\" \"x\\\"y\" c:\\tmp"));
    }

    [Fact]
    public void SplitUsesOnlySpaceAndTabAsDelimiters()
    {
        Assert.Equal(new[] { "a b", "c" }, LaunchOptions.SplitCommandLine("a b	c"));
    }
}
