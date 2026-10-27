using SmartSnippets.Core.Domain;

namespace SmartSnippets.Core.Tests.Domain;

public sealed class SnippetValidatorTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\r\n\t \n")]
    public void WhitespaceOnlyText_Rejected(string text)
    {
        var errors = SnippetValidator.Validate(text, "label");

        Assert.Equal([ValidationError.TextRequired], errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  \t")]
    public void WhitespaceOnlyLabel_Rejected(string label)
    {
        var errors = SnippetValidator.Validate("text", label);

        Assert.Equal([ValidationError.LabelRequired], errors);
    }

    [Fact]
    public void BothBlank_ReportsBoth()
    {
        Assert.Equal(
            [ValidationError.TextRequired, ValidationError.LabelRequired],
            SnippetValidator.Validate(" ", " "));
    }

    [Fact]
    public void Valid_NoErrors_AndLeadingTrailingWhitespaceAllowed()
    {
        Assert.Empty(SnippetValidator.Validate("  keep me \r\n", "label"));
    }

    [Fact]
    public void TextIsNeverModified()
    {
        const string expected = "  line1\r\nline2\rline3\n\t trailing  ";
        var text = new string(expected.AsSpan());

        SnippetValidator.Validate(text, "label");

        Assert.Equal(expected, text);
    }

    [Fact]
    public void EveryErrorHasMessage()
    {
        foreach (var error in Enum.GetValues<ValidationError>())
        {
            Assert.False(string.IsNullOrWhiteSpace(error.ToMessage()));
        }
    }
}
