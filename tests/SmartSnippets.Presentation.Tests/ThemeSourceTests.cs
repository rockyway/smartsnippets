using SmartSnippets.Core.Settings;
using SmartSnippets.Presentation.Services;

namespace SmartSnippets.Presentation.Tests;

public sealed class ThemeSourceTests
{
    [Theory]
    [InlineData(ThemeMode.Dark, false, true)]
    [InlineData(ThemeMode.Light, true, false)]
    [InlineData(ThemeMode.System, false, false)]
    [InlineData(ThemeMode.System, true, true)]
    public void ForcedThemeWinsBeforeFirstLayout(ThemeMode requested, bool systemDark, bool expected)
        => Assert.Equal(expected, ThemeSource.IsDark(requested, systemDark));
}
