using SmartSnippets.Core;

namespace SmartSnippets.Core.Tests;

public class AppPathsTests
{
    [Fact]
    public void DatabasePath_IsUnderOverriddenRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "ss-test-root");
        var paths = new AppPaths(root);

        Assert.Equal(Path.Combine(root, "smartsnippets.db"), paths.DatabasePath);
        Assert.Equal(Path.Combine(root, "logs"), paths.LogDirectory);
    }

    [Fact]
    public void DefaultRoot_EndsWithSmartSnippets()
    {
        Assert.Equal("SmartSnippets", Path.GetFileName(new AppPaths().Root));
    }
}
