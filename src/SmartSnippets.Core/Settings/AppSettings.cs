using System.Globalization;
using SmartSnippets.Core.Logging;

namespace SmartSnippets.Core.Settings;

public sealed record AppSettings
{
    public const double MinCustomTextSize = 10;
    public const double MaxCustomTextSize = 32;

    public ThemeMode Theme { get; init; } = ThemeMode.System;

    public TextSizePreset TextSizePreset { get; init; } = TextSizePreset.Medium;

    public double CustomTextSize { get; init; } = 14;

    /// <summary>Win32 MOD_* flags; default MOD_CONTROL | MOD_SHIFT.</summary>
    public uint HotkeyModifiers { get; init; } = 0x0006;

    /// <summary>Virtual-key code; default VK_SPACE.</summary>
    public uint HotkeyKey { get; init; } = 0x20;

    public HotkeyGesture Hotkey => new(HotkeyModifiers, HotkeyKey);

    public PopupLayout PopupLayout { get; init; } = PopupLayout.Flat;

    public SortField PopupSort { get; init; } = SortField.LastUsed;

    public AppSettings WithHotkey(HotkeyGesture gesture) =>
        this with { HotkeyModifiers = gesture.Modifiers, HotkeyKey = gesture.Key };

    public static AppSettings Load(ISettingsStore store)
    {
        var d = new AppSettings();
        return new AppSettings
        {
            Theme = ReadEnum(store, SettingsKeys.Theme, d.Theme),
            TextSizePreset = ReadEnum(store, SettingsKeys.TextSizePreset, d.TextSizePreset),
            CustomTextSize = ReadSize(store, d.CustomTextSize),
            HotkeyModifiers = ReadUInt(store, SettingsKeys.HotkeyModifiers, d.HotkeyModifiers),
            HotkeyKey = ReadUInt(store, SettingsKeys.HotkeyKey, d.HotkeyKey),
            PopupLayout = ReadEnum(store, SettingsKeys.PopupLayout, d.PopupLayout),
            PopupSort = ReadEnum(store, SettingsKeys.PopupSort, d.PopupSort),
        };
    }

    public void Save(ISettingsStore store)
    {
        store.Set(SettingsKeys.Theme, Theme.ToString());
        store.Set(SettingsKeys.TextSizePreset, TextSizePreset.ToString());
        store.Set(SettingsKeys.TextSizeCustom, Clamp(CustomTextSize, 14).ToString("R", CultureInfo.InvariantCulture));
        store.Set(SettingsKeys.HotkeyModifiers, HotkeyModifiers.ToString(CultureInfo.InvariantCulture));
        store.Set(SettingsKeys.HotkeyKey, HotkeyKey.ToString(CultureInfo.InvariantCulture));
        store.Set(SettingsKeys.PopupLayout, PopupLayout.ToString());
        store.Set(SettingsKeys.PopupSort, PopupSort.ToString());
    }

    private static double Clamp(double value, double fallback) =>
        double.IsFinite(value) ? Math.Clamp(value, MinCustomTextSize, MaxCustomTextSize) : fallback;

    private static T ReadEnum<T>(ISettingsStore store, string key, T fallback)
        where T : struct, Enum
    {
        var raw = store.Get(key);
        if (raw is null)
        {
            return fallback;
        }

        if (Enum.TryParse<T>(raw, ignoreCase: true, out var value) && Enum.IsDefined(value))
        {
            return value;
        }

        AppLog.Info("settings_invalid", $"key={key}");
        return fallback;
    }

    private static uint ReadUInt(ISettingsStore store, string key, uint fallback)
    {
        var raw = store.Get(key);
        if (raw is null)
        {
            return fallback;
        }

        if (uint.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }

        AppLog.Info("settings_invalid", $"key={key}");
        return fallback;
    }

    private static double ReadSize(ISettingsStore store, double fallback)
    {
        var raw = store.Get(SettingsKeys.TextSizeCustom);
        if (raw is null)
        {
            return fallback;
        }

        if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            && double.IsFinite(value))
        {
            return Clamp(value, fallback);
        }

        AppLog.Info("settings_invalid", $"key={SettingsKeys.TextSizeCustom}");
        return fallback;
    }
}
