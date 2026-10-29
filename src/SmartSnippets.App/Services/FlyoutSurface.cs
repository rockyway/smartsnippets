using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

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
        if (sender is not FlyoutBase flyout || flyout.Target?.XamlRoot is not { } root) return;
        foreach (var popup in VisualTreeHelper.GetOpenPopupsForXamlRoot(root))
        {
            if (Find(popup.Child) is not { } presenter) continue;
            presenter.SystemBackdrop = null;
            // High contrast keeps the system window colour from the theme dictionaries.
            if (presenter.ActualTheme == ElementTheme.Dark) presenter.Background = Dark;
            else if (presenter.ActualTheme == ElementTheme.Light) presenter.Background = Light;
        }
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
