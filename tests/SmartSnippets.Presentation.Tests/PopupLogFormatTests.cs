using SmartSnippets.Core.Settings;
using SmartSnippets.Presentation.Services;

namespace SmartSnippets.Presentation.Tests;

public sealed class PopupLogFormatTests
{
    [Fact]
    public void SearchMs_Format_LayoutAndResults_NoQueryText()
    {
        Assert.Equal("42 layout=folder results=7", PopupLogFormat.SearchMs(42, PopupLayout.Folder, 7));
        Assert.Equal("1 layout=flat results=0", PopupLogFormat.SearchMs(1, PopupLayout.Flat, 0));
    }
}
