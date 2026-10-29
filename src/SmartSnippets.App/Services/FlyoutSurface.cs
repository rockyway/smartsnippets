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

    private static readonly List<WeakReference<MenuFlyoutPresenter>> Presenters = [];
    internal static Func<ElementTheme, SolidColorBrush>? ResolveMenuBrush { get; set; }
    internal static Func<ElementTheme>? ResolveRequestedTheme { get; set; }

    internal static void Refresh()
    {
        for (var i = Presenters.Count - 1; i >= 0; i--)
        {
            if (Presenters[i].TryGetTarget(out var presenter)) MakeSolid(presenter);
            else Presenters.RemoveAt(i);
        }
    }

    internal static void KeepSolid(MenuFlyout menu) => menu.Opened += OnOpened;

    private static void OnOpened(object? sender, object e)
    {
        // The menu may open in its own popup surface with its own XamlRoot, so the presenter is located through the menu
        // items it hosts (their ancestors) first; the open-popups lookup is only a fallback.
        var found = sender is MenuFlyout menu ? FromItems(menu) : null;
        if (found is null && sender is FlyoutBase flyout && flyout.Target?.XamlRoot is { } root)
        {
            foreach (var popup in VisualTreeHelper.GetOpenPopupsForXamlRoot(root))
            {
                if (Find(popup.Child) is { } presenter) { found = presenter; break; }
            }
        }
        if (found is not null)
        {
            if (!Presenters.Any(reference => reference.TryGetTarget(out var existing) && ReferenceEquals(existing, found)))
            {
                Presenters.Add(new(found));
                found.ActualThemeChanged += OnPresenterThemeChanged;
            }
            MakeSolid(found);
        }
        LogSurface(found);
    }

    private static MenuFlyoutPresenter? FromItems(MenuFlyout menu)
    {
        foreach (var item in menu.Items)
        {
            DependencyObject? node = item;
            for (var depth = 0; node is not null && depth < 64; depth++)
            {
                if (node is MenuFlyoutPresenter presenter) return presenter;
                node = VisualTreeHelper.GetParent(node);
            }
        }
        return null;
    }

    private static void OnPresenterThemeChanged(FrameworkElement sender, object args)
    {
        if (sender is MenuFlyoutPresenter presenter) MakeSolid(presenter);
    }

    private static void MakeSolid(MenuFlyoutPresenter presenter)
    {
        presenter.SystemBackdrop = null;
        if (ResolveRequestedTheme is { } resolveTheme && presenter.RequestedTheme != resolveTheme())
            presenter.RequestedTheme = resolveTheme();
        presenter.Background = ResolveMenuBrush?.Invoke(presenter.ActualTheme)
            ?? (WindowSurface.IsHighContrast() ? new SolidColorBrush(WindowSurface.SystemColor(5))
                : presenter.ActualTheme == ElementTheme.Dark ? Dark : Light);
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
