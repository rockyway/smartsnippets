using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using SmartSnippets.Core.Logging;

namespace SmartSnippets.App.Services;

/// <summary>
/// Menus are solid. The default presenter background is a transparent brush that relies on a desktop-acrylic backdrop,
/// which does not render for flyouts constrained to the window, so the rows behind showed through. Style and theme
/// resources did not change that in practice, so the open presenter is set directly.
/// </summary>
internal static class FlyoutSurface
{
    private static readonly SolidColorBrush Dark = new(Color.FromArgb(0xFF, 0x2B, 0x2B, 0x2B));
    private static readonly SolidColorBrush Light = new(Color.FromArgb(0xFF, 0xF9, 0xF9, 0xF9));

    internal static void KeepSolid(MenuFlyout menu) => menu.Opened += OnOpened;

    private static void OnOpened(object? sender, object e)
    {
        MenuFlyoutPresenter? found = null;
        if (sender is FlyoutBase flyout && flyout.Target?.XamlRoot is { } root)
        {
            foreach (var popup in VisualTreeHelper.GetOpenPopupsForXamlRoot(root))
            {
                if (Find(popup.Child) is not { } presenter) continue;
                presenter.SystemBackdrop = null;
                // High contrast keeps the system window colour from the theme dictionaries.
                if (presenter.ActualTheme == ElementTheme.Dark) presenter.Background = Dark;
                else if (presenter.ActualTheme == ElementTheme.Light) presenter.Background = Light;
                found ??= presenter;
            }
        }
        LogSurface(found);
    }

    private static void LogSurface(MenuFlyoutPresenter? presenter)
    {
        // Only allowlisted type tokens and numeric values: never menu/row text, names or titles.
        // Exactly one receipt per Opened event, including a missing root/presenter.
        var brush = presenter?.Background;
        var type = brush switch
        {
            SolidColorBrush => "SolidColorBrush",
            AcrylicBrush => "AcrylicBrush",
            LinearGradientBrush => "LinearGradientBrush",
            null => "null",
            _ => "Other",
        };
        var color = brush is SolidColorBrush solid ? solid.Color : default;
        var backdrop = presenter?.SystemBackdrop switch
        {
            DesktopAcrylicBackdrop => "DesktopAcrylicBackdrop",
            MicaBackdrop => "MicaBackdrop",
            null => "null",
            _ => "Other",
        };
        AppLog.Info("menu_surface", FormattableString.Invariant(
            $"presenter={(presenter is null ? "missing" : "found")} bg={type}:{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2} backdrop={backdrop} opacity={presenter?.Opacity ?? -1} theme={(int)(presenter?.ActualTheme ?? ElementTheme.Default)}"));
    }

    private static MenuFlyoutPresenter? Find(DependencyObject? node)
    {
        if (node is null) return null;
        if (node is MenuFlyoutPresenter presenter) return presenter;
        var count = VisualTreeHelper.GetChildrenCount(node);
        for (var i = 0; i < count; i++)
        {
            if (Find(VisualTreeHelper.GetChild(node, i)) is { } found) return found;
        }
        return null;
    }
}
