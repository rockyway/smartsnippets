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

    [Fact]
    public void SplitRetainsQuotedEscapesAndEmptyArguments()
    {
        Assert.Equal(new[] { "a", "", "x\"y", @"c:\tmp" },
            LaunchOptions.SplitCommandLine("a \"\" \"x\\\"y\" c:\\tmp"));
    }
}
