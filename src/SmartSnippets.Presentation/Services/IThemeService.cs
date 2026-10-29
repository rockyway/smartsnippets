using SmartSnippets.Core.Settings;

namespace SmartSnippets.Presentation.Services;

public interface IThemeService
{
    ThemeMode Current { get; }

    void Apply(ThemeMode mode);

    ColorPalette CurrentPalette { get; }

    void ApplyPalette(ColorPalette palette);
}
