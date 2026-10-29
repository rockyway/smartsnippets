using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace SmartSnippets.App.Controls;

/// <summary>Live consumers of unmodified WinUI Color resources, with explicit light/dark context.</summary>
public sealed partial class PaletteBaseline : UserControl
{
    public event Action? ColorsChanged;

    public PaletteBaseline(bool dark)
    {
        RequestedTheme = dark ? ElementTheme.Dark : ElementTheme.Light;
        InitializeComponent();
        foreach (var border in ((StackPanel)Content).Children.OfType<Border>())
        {
            if (border.Background is SolidColorBrush solid)
                solid.RegisterPropertyChangedCallback(SolidColorBrush.ColorProperty, (_, _) => ColorsChanged?.Invoke());
            border.RegisterPropertyChangedCallback(Border.BackgroundProperty, (_, _) => ColorsChanged?.Invoke());
        }
        Loaded += (_, _) => ColorsChanged?.Invoke();
    }

    internal AcrylicBrush? Acrylic => PaletteAcrylic.Background as AcrylicBrush;

    internal SolidColorBrush Brush(string name) => (SolidColorBrush)((Border)FindName(name)).Background;
}
