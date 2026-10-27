using System.Globalization;
using SmartSnippets.Core.Domain;

namespace SmartSnippets.Core.Tests.Domain;

public sealed class LabelSuggesterTests
{
    private static readonly string[] NoTags = [];

    private static int Elements(string s) => new StringInfo(s).LengthInTextElements;

    [Fact]
    public void PrefersFirstTag()
    {
        Assert.Equal("greeting", LabelSuggester.Suggest("Hello there", ["greeting", "other"]));
    }

    [Fact]
    public void FallsBackToFirstNonEmptyLine()
    {
        Assert.Equal("Second", LabelSuggester.Suggest("\n   \n  Second  \nThird", NoTags));
    }

    [Theory]
    [InlineData("\r\n\r\nline\r\nnext", "line")]
    [InlineData("\r\rline\rnext", "line")]
    [InlineData("\n\nline\nnext", "line")]
    [InlineData("\r\n \t \r\n\tline", "line")]
    public void HandlesAllLineEndings(string text, string expected)
    {
        Assert.Equal(expected, LabelSuggester.Suggest(text, NoTags));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\r\n\t\n")]
    public void NothingQualifying_ReturnsEmpty(string text)
    {
        Assert.Equal("", LabelSuggester.Suggest(text, NoTags));
    }

    [Fact]
    public void TruncatesAt60TextElements_DoesNotSplitEmoji()
    {
        var text = new string('a', 59) + "\U0001F468‍\U0001F469‍\U0001F467" + "tail";

        var label = LabelSuggester.Suggest(text, NoTags);

        Assert.Equal(60, Elements(label));
        Assert.EndsWith("\U0001F468‍\U0001F469‍\U0001F467", label);
        Assert.DoesNotContain("tail", label);
    }

    [Fact]
    public void SurrogatePairAtBoundary_NotSplit()
    {
        var text = new string('a', 59) + "\U0001F600\U0001F600";

        var label = LabelSuggester.Suggest(text, NoTags);

        Assert.Equal(new string('a', 59) + "\U0001F600", label);
    }

    [Fact]
    public void CombiningMarkAtBoundary_StaysWithBase()
    {
        var text = new string('a', 59) + "é" + "zzz";

        var label = LabelSuggester.Suggest(text, NoTags);

        Assert.Equal(new string('a', 59) + "é", label);
    }

    [Fact]
    public void ExactlyMaxLength_Unchanged()
    {
        var text = new string('b', LabelSuggester.MaxLength);

        Assert.Equal(text, LabelSuggester.Suggest(text, NoTags));
    }

    [Fact]
    public void LongTag_AlsoTruncated()
    {
        var label = LabelSuggester.Suggest("x", [new string('t', 100)]);

        Assert.Equal(LabelSuggester.MaxLength, label.Length);
    }

    [Fact]
    public void ResolveLabel_AutoWhenNotCustom()
    {
        var draft = new SnippetDraft("Body", "ignored", false, null, "");

        Assert.Equal(("Body", false), LabelSuggester.ResolveLabel(draft, NoTags));
    }

    [Fact]
    public void ResolveLabel_AutoWhenCustomButBlank()
    {
        var draft = new SnippetDraft("Body", "  ", true, null, "");

        Assert.Equal(("tag", false), LabelSuggester.ResolveLabel(draft, ["tag"]));
    }

    [Fact]
    public void ResolveLabel_KeepsCustomVerbatim()
    {
        var draft = new SnippetDraft("Body", " My Label ", true, null, "");

        Assert.Equal((" My Label ", true), LabelSuggester.ResolveLabel(draft, ["tag"]));
    }
}
