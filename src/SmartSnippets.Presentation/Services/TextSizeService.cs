using CommunityToolkit.Mvvm.ComponentModel;
using SmartSnippets.Core.Settings;

namespace SmartSnippets.Presentation.Services;

public sealed class TextSizeService : ObservableObject, ITextSizeService
{
    public const double SmallSize = 12;
    public const double MediumSize = 14;
    public const double LargeSize = 18;

    private double _fontSize = MediumSize;

    public double FontSize
    {
        get => _fontSize;
        private set => SetProperty(ref _fontSize, value);
    }

    public void Apply(TextSizePreset preset, double customSize) =>
        FontSize = preset switch
        {
            TextSizePreset.Small => SmallSize,
            TextSizePreset.Large => LargeSize,
            TextSizePreset.Custom => Clamp(customSize),
            _ => MediumSize,
        };

    public static double Clamp(double value) =>
        double.IsFinite(value)
            ? Math.Clamp(value, AppSettings.MinCustomTextSize, AppSettings.MaxCustomTextSize)
            : MediumSize;
}
