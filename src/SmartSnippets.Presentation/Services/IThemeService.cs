using SmartSnippets.Core.Settings;

namespace SmartSnippets.Presentation.Services;

public interface IThemeService
{
    ThemeMode Current { get; }

    void Apply(ThemeMode mode);
}
