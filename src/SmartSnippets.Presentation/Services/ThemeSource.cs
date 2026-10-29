using SmartSnippets.Core.Settings;

namespace SmartSnippets.Presentation.Services;

public static class ThemeSource
{
    public static bool IsDark(ThemeMode requested, bool systemDark) => requested switch
    {
        ThemeMode.Dark => true,
        ThemeMode.Light => false,
        _ => systemDark,
    };
}
