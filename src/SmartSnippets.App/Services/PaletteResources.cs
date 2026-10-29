using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;
using SmartSnippets.App.Controls;
using Windows.UI.ViewManagement;
using SmartSnippets.Core.Settings;
using SmartSnippets.Presentation.Services;
using Windows.UI;

namespace SmartSnippets.App.Services;

/// <summary>
/// Stable application-scope brushes, one set per light/dark dictionary. Baselines retain their
/// original system resource bindings. Never mutate a framework brush or replace a realized brush.
/// </summary>
internal sealed class PaletteResources
{
    private sealed record Entry(string Source, SolidColorBrush Baseline, SolidColorBrush Live, PaletteRole Role);
    private readonly PaletteBaseline _darkBaseline = new(dark: true);
    private readonly PaletteBaseline _lightBaseline = new(dark: false);
    private bool _baselinesAttached;
    public event Action? BaselineChanged;
    private readonly Dictionary<bool, Dictionary<string, Entry>> _themes = [];
    private readonly SolidColorBrush _highContrastMenu = new();
    private readonly Dictionary<bool, AcrylicBrush> _acrylic = [];
    private readonly Dictionary<bool, LinearGradientBrush> _focusBorders = [];

    public PaletteResources()
    {
        Install("Default", dark: true);
        Install("Light", dark: false);
        _darkBaseline.ColorsChanged += OnBaselineChanged;
        _lightBaseline.ColorsChanged += OnBaselineChanged;
    }

    private void OnBaselineChanged() => BaselineChanged?.Invoke();

    public void InstallRootResources(FrameworkElement root)
    {
        // Per-root lookup takes precedence over WinUI template resources. Stable brush
        // instances are shared with application scope and are never replaced after load.
        foreach (var (dark, entries) in _themes)
        {
            var themed = new ResourceDictionary();
            foreach (var (key, entry) in entries) themed[key] = entry.Live;
            themed["NavigationViewDefaultPaneBackground"] = _acrylic[dark];
            themed["ComboBoxDropDownBackground"] = _acrylic[dark];
            themed["TextControlBorderBrushFocused"] = _focusBorders[dark];
            root.Resources.ThemeDictionaries[dark ? "Default" : "Light"] = themed;
        }
        // Do not shadow platform HighContrast state/foreground pairs. Only the explicit
        // surface brush is shared here; menus, navigation and control states resolve their
        // native high-contrast resources from the application/WinUI dictionaries.
        var highContrast = new ResourceDictionary
        {
            ["SolidBackgroundFillColorBaseBrush"] = _themes[false]["SolidBackgroundFillColorBaseBrush"].Live,
        };
        root.Resources.ThemeDictionaries["HighContrast"] = highContrast;
    }

    public void AttachBaselines(FrameworkElement root)
    {
        if (root is Grid grid && !_baselinesAttached)
        {
            grid.Children.Add(_darkBaseline);
            grid.Children.Add(_lightBaseline);
            _baselinesAttached = true;
        }
    }

    private void Install(string theme, bool dark)
    {
        var dictionary = (ResourceDictionary)Application.Current.Resources.ThemeDictionaries[theme];
        var entries = new Dictionary<string, Entry>(StringComparer.Ordinal);
        _themes.Add(dark, entries);

        void Add(string source, PaletteRole role, params string[] keys)
        {
            var baseline = source.StartsWith("Palette", StringComparison.Ordinal)
                ? (dark ? _darkBaseline : _lightBaseline).Brush(source) : (SolidColorBrush)dictionary[source];
            foreach (var key in keys)
            {
                var live = new SolidColorBrush(baseline.Color) { Opacity = baseline.Opacity };
                entries.Add(key, new(source, baseline, live, role));
                try
                {
                    // WinUI's IMap Insert rejects replacement of an existing XAML resource key.
                    // These dictionaries are prepared before any window is constructed.
                    dictionary.Remove(key);
                    dictionary[key] = live;
                }
                catch (System.Runtime.InteropServices.COMException)
                {
                    SmartSnippets.Core.Logging.AppLog.Info("palette_resource_failed", $"theme={theme} key={key}");
                    throw;
                }
            }
        }

        Add("PaletteBase", PaletteRole.Background, "SolidBackgroundFillColorBaseBrush");
        Add("PaletteSelection", PaletteRole.TextSelection, "TextControlSelectionHighlightColor", "AccentFillColorSelectedTextBackgroundBrush");
        Add("PaletteSurface", PaletteRole.Surface, "SolidBackgroundFillColorQuarternaryBrush");
        Add("PaletteLayer", PaletteRole.Layer, "LayerFillColorDefaultBrush", "NavigationViewContentBackground");
        Add("PaletteCard", PaletteRole.Surface, "CardBackgroundFillColorSecondaryBrush");
        Add("PaletteControl", PaletteRole.Surface, "ControlFillColorDefaultBrush", "ButtonBackground", "ComboBoxBackground",
            "ComboBoxBackgroundUnfocused", "ComboBoxBackgroundFocused", "TextControlBackground", "ToggleButtonBackground");
        Add("PaletteControlSecondary", PaletteRole.Hover, "ControlFillColorSecondaryBrush", "ButtonBackgroundPointerOver",
            "ComboBoxBackgroundPointerOver", "TextControlBackgroundPointerOver", "ToggleButtonBackgroundPointerOver");
        Add("PaletteControlTertiary", PaletteRole.Pressed, "ControlFillColorTertiaryBrush", "ButtonBackgroundPressed", "ComboBoxBackgroundPressed", "ToggleButtonBackgroundPressed");
        Add("PaletteInput", PaletteRole.Background, "ControlFillColorInputActiveBrush", "TextControlBackgroundFocused");
        Add("PaletteHover", PaletteRole.Hover, "SubtleFillColorSecondaryBrush", "ListViewItemBackgroundPointerOver",
            "MenuFlyoutItemBackgroundPointerOver", "ComboBoxItemBackgroundPointerOver", "ComboBoxItemBackgroundSelected",
            "ComboBoxItemBackgroundSelectedPressed", "ComboBoxDropDownBackgroundPointerOver", "NavigationViewItemBackgroundPointerOver",
            "NavigationViewItemBackgroundSelected", "NavigationViewItemBackgroundSelectedPressed");
        Add("PalettePressed", PaletteRole.Pressed, "SubtleFillColorTertiaryBrush", "ListViewItemBackgroundPressed",
            "MenuFlyoutItemBackgroundPressed", "ComboBoxItemBackgroundPressed", "ComboBoxItemBackgroundSelectedUnfocused",
            "ComboBoxItemBackgroundSelectedPointerOver", "ComboBoxDropDownBackgroundPointerPressed", "NavigationViewItemBackgroundPressed",
            "NavigationViewItemBackgroundSelectedPointerOver");
        Add("PaletteSurface", PaletteRole.Surface, "MenuFlyoutPresenterBackground");
        // Custom app tokens must be captured before replacing their dictionary entries.
        Add("MenuSolidBrush", PaletteRole.Surface, "MenuSolidBrush");
        Add("FolderTintBrush", PaletteRole.Accent, "FolderTintBrush");
        Add("FolderTintBackgroundBrush", PaletteRole.Layer, "FolderTintBackgroundBrush");
        Add("PaletteTint", PaletteRole.AccentTint, "AccentTintBrush");
        Add("PaletteBase", PaletteRole.Background, "ContentDialogBackground");
        Add("PaletteDialogTop", PaletteRole.Layer, "ContentDialogTopOverlay");

        Add("PaletteAccent", PaletteRole.Accent, "AccentFillColorDefaultBrush", "ListViewItemBackgroundSelected",
            "AccentButtonBackground", "NavigationViewSelectionIndicatorForeground", "RadioButtonOuterEllipseCheckedFill",
            "RadioButtonOuterEllipseCheckedStroke", "ToggleSwitchFillOn", "ToggleSwitchStrokeOn",
            "ToggleButtonBackgroundChecked", "CheckBoxCheckBackgroundFillChecked", "CheckBoxCheckBackgroundStrokeChecked");
        Add("PaletteAccentHover", PaletteRole.AccentHover, "AccentFillColorSecondaryBrush", "ListViewItemBackgroundSelectedPointerOver",
            "AccentButtonBackgroundPointerOver", "RadioButtonOuterEllipseCheckedFillPointerOver",
            "RadioButtonOuterEllipseCheckedStrokePointerOver", "ToggleSwitchFillOnPointerOver", "ToggleSwitchStrokeOnPointerOver",
            "ToggleButtonBackgroundCheckedPointerOver", "CheckBoxCheckBackgroundFillCheckedPointerOver", "CheckBoxCheckBackgroundStrokeCheckedPointerOver");
        Add("PaletteAccentPressed", PaletteRole.AccentPressed, "AccentFillColorTertiaryBrush", "ListViewItemBackgroundSelectedPressed",
            "AccentButtonBackgroundPressed", "RadioButtonOuterEllipseCheckedFillPressed",
            "RadioButtonOuterEllipseCheckedStrokePressed", "ToggleSwitchFillOnPressed", "ToggleSwitchStrokeOnPressed",
            "ToggleButtonBackgroundCheckedPressed", "CheckBoxCheckBackgroundFillCheckedPressed", "CheckBoxCheckBackgroundStrokeCheckedPressed");
        Add("PaletteOnAccent", PaletteRole.OnAccent, "TextOnAccentFillColorPrimaryBrush", "ListViewItemForegroundSelected",
            "AccentButtonForeground", "AccentButtonForegroundPointerOver", "RadioButtonCheckGlyphFill",
            "RadioButtonCheckGlyphFillPointerOver", "RadioButtonCheckGlyphFillPressed", "ToggleSwitchKnobFillOn",
            "ToggleSwitchKnobFillOnPointerOver", "ToggleSwitchKnobFillOnPressed", "ToggleButtonForegroundChecked",
            "ToggleButtonForegroundCheckedPointerOver", "CheckBoxCheckGlyphForegroundChecked", "CheckBoxCheckGlyphForegroundCheckedPointerOver");
        Add("PaletteOnAccentSecondary", PaletteRole.OnAccent, "TextOnAccentFillColorSecondaryBrush",
            "AccentButtonForegroundPressed", "ToggleButtonForegroundCheckedPressed", "CheckBoxCheckGlyphForegroundCheckedPressed");
        Add("PaletteAccentText", PaletteRole.AccentText, "AccentTextFillColorPrimaryBrush", "HyperlinkButtonForeground");
        Add("PaletteAccentTextSecondary", PaletteRole.AccentText, "AccentTextFillColorSecondaryBrush", "HyperlinkButtonForegroundPointerOver");
        Add("PaletteAccentTextTertiary", PaletteRole.AccentText, "AccentTextFillColorTertiaryBrush", "HyperlinkButtonForegroundPressed");

        // These template aliases originally use acrylic. Preserve its properties in Default;
        // custom palettes use an opaque fallback, without changing any window backdrop.
        var acrylic = new AcrylicBrush();
        _acrylic.Add(dark, acrylic);
        dictionary["NavigationViewDefaultPaneBackground"] = acrylic;
        dictionary["ComboBoxDropDownBackground"] = acrylic;

        // Preserve WinUI's two-stop focus elevation geometry; mutate the stops, not the resource.
        var focus = new LinearGradientBrush
        {
            MappingMode = BrushMappingMode.Absolute,
            StartPoint = new(0, 0), EndPoint = new(0, 2),
            RelativeTransform = new ScaleTransform { ScaleY = -1, CenterY = 0.5 },
        };
        focus.GradientStops.Add(new GradientStop { Offset = 1 });
        focus.GradientStops.Add(new GradientStop { Offset = 1 });
        _focusBorders.Add(dark, focus);
        dictionary["TextControlBorderBrushFocused"] = focus;
    }

    public void Update(ColorPalette palette, UISettings settings)
    {
        var highContrast = WindowSurface.IsHighContrast();
        _highContrastMenu.Color = WindowSurface.SystemColor(5); // COLOR_WINDOW
        foreach (var (dark, entries) in _themes)
        {
            var tones = PaletteTones.Resolve(palette, dark, highContrast);
            var baseline = dark ? _darkBaseline : _lightBaseline;
            var focus = _focusBorders[dark];
            focus.GradientStops[0].Color = highContrast ? WindowSurface.SystemColor(13)
                : tones is { } focusTones ? FromArgb(focusTones.Accent)
                : settings.GetColorValue(dark ? UIColorType.AccentLight2 : UIColorType.AccentDark1);
            focus.GradientStops[1].Color = highContrast ? WindowSurface.SystemColor(13) : baseline.Brush("PaletteStroke").Color;
            var acrylic = _acrylic[dark];
            var original = (dark ? _darkBaseline : _lightBaseline).Acrylic;
            if (highContrast || tones is not null)
            {
                acrylic.FallbackColor = highContrast ? WindowSurface.SystemColor(5) : FromArgb(tones!.Value.Surface);
                acrylic.AlwaysUseFallback = true;
            }
            else if (original is not null)
            {
                acrylic.TintColor = original.TintColor;
                acrylic.TintOpacity = original.TintOpacity;
                acrylic.TintLuminosityOpacity = original.TintLuminosityOpacity;
                acrylic.FallbackColor = original.FallbackColor;
                acrylic.Opacity = original.Opacity;
                acrylic.AlwaysUseFallback = original.AlwaysUseFallback;
            }
            foreach (var entry in entries.Values)
            {
                if (highContrast)
                {
                    entry.Live.Color = WindowSurface.SystemColor(entry.Role switch
                    {
                        PaletteRole.Accent or PaletteRole.AccentHover or PaletteRole.AccentPressed or PaletteRole.TextSelection => 13, // HIGHLIGHT
                        PaletteRole.OnAccent => 14, // HIGHLIGHTTEXT
                        PaletteRole.AccentText => 8, // WINDOWTEXT
                        _ => 5, // WINDOW
                    });
                    entry.Live.Opacity = 1;
                }
                else if (tones is { } colors)
                {
                    entry.Live.Color = FromArgb(colors.Color(entry.Role));
                    entry.Live.Opacity = entry.Role == PaletteRole.AccentTint ? 0.12 : 1;
                }
                else
                {
                    entry.Live.Color = BaselineColor(entry, dark, settings);
                    entry.Live.Opacity = entry.Baseline.Opacity;
                }
            }
        }
    }

    private static Color BaselineColor(Entry entry, bool dark, UISettings settings)
    {
        // Read current accent shades explicitly on every refresh: Default must follow OS changes
        // even when no original framework brush has a realized consumer. Other colors have
        // explicit light/dark visual consumers in PaletteBaseline, never static cross-theme aliases.
        UIColorType? accent = entry.Source switch
        {
            "PaletteAccent" or "PaletteAccentHover" or "PaletteAccentPressed" => dark ? UIColorType.AccentLight2 : UIColorType.AccentDark1,
            "PaletteAccentText" => dark ? UIColorType.AccentLight3 : UIColorType.AccentDark2,
            "PaletteAccentTextSecondary" => dark ? UIColorType.AccentLight3 : UIColorType.AccentDark3,
            "PaletteAccentTextTertiary" => dark ? UIColorType.AccentLight2 : UIColorType.AccentDark1,
            "PaletteTint" or "PaletteSelection" => UIColorType.Accent,
            _ => null,
        };
        return accent is { } type ? settings.GetColorValue(type) : entry.Baseline.Color;
    }

    public SolidColorBrush BackgroundBrush(ElementTheme theme) =>
        _themes[!WindowSurface.IsHighContrast() && theme == ElementTheme.Dark]["SolidBackgroundFillColorBaseBrush"].Live;

    public SolidColorBrush MenuBrush(ElementTheme theme) => WindowSurface.IsHighContrast()
        ? _highContrastMenu : _themes[theme == ElementTheme.Dark]["MenuSolidBrush"].Live;

    private static Color FromArgb(uint color) => Color.FromArgb((byte)(color >> 24), (byte)(color >> 16), (byte)(color >> 8), (byte)color);
}
