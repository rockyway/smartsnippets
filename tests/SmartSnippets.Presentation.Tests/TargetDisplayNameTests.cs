using SmartSnippets.Presentation.Text;

namespace SmartSnippets.Presentation.Tests;

public sealed class TargetDisplayNameTests
{
    [Theory]
    [InlineData("Notepad", "notepad.exe", "Untitled - Notepad", "Notepad, Untitled")]
    [InlineData("Notepad", "notepad.exe", "Untitled — Notepad", "Notepad, Untitled")]
    [InlineData("Notepad", "notepad.exe", "", "Notepad")]
    [InlineData("", "notepad.exe", "", "notepad")]
    [InlineData("Application Frame Host", "ApplicationFrameHost.exe", "Calculator", "Calculator")]
    [InlineData("", "", "", "the target app")]
    public void Format_UsesFriendlyAndTitle(string friendly, string exe, string title, string expected) =>
        Assert.Equal(expected, TargetDisplayName.Format(friendly, exe, title));

    [Fact]
    public void Format_Truncates60_SurrogateSafe()
    {
        var result = TargetDisplayName.Format(new string('a', 58) + "😀abc", "", "");
        Assert.Equal(new string('a', 58) + "…", result);
    }
}
