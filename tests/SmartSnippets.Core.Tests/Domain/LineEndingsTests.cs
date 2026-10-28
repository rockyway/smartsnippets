using SmartSnippets.Core.Domain;

namespace SmartSnippets.Core.Tests.Domain;

public sealed class LineEndingsTests
{
    [Theory]
    [InlineData("", "")]
    [InlineData("a\rb", "a\r\nb")]
    [InlineData("a\nb", "a\r\nb")]
    [InlineData("a\r\nb\rc\nd", "a\r\nb\r\nc\r\nd")]
    [InlineData("\r\r", "\r\n\r\n")]
    [InlineData("\n\r", "\r\n\r\n")]
    [InlineData("\r\n\n", "\r\n\r\n")]
    [InlineData("\na\r", "\r\na\r\n")]
    [InlineData("Héllo 👋 世界\t  \u2028\rline  ", "Héllo 👋 世界\t  \u2028\r\nline  ")]
    public void NormalizesOnlyLineEndings(string input, string expected) => Assert.Equal(expected, LineEndings.ToCrLf(input));

    [Theory]
    [InlineData("")]
    [InlineData("a\r\nb")]
    [InlineData("no breaks")]
    public void AlreadyNormalized_ReturnsSameInstance(string input) => Assert.Same(input, LineEndings.ToCrLf(input));

    [Fact]
    public void Null_Throws() => Assert.Throws<ArgumentNullException>(() => LineEndings.ToCrLf(null!));
}
