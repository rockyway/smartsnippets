using SmartSnippets.Core.Settings;

namespace SmartSnippets.Core.Tests.Settings;

public sealed class AppSettingsTests
{
    private sealed class MemoryStore : ISettingsStore
    {
        public Dictionary<string, string> Values { get; } = [];

        public string? Get(string key) => Values.TryGetValue(key, out var v) ? v : null;

        public void Set(string key, string value) => Values[key] = value;
    }

    [Fact]
    public void EmptyDb_ReturnsDefaults()
    {
        var s = AppSettings.Load(new MemoryStore());

        Assert.Equal(ThemeMode.System, s.Theme);
        Assert.Equal(TextSizePreset.Medium, s.TextSizePreset);
        Assert.Equal(14, s.CustomTextSize);
        Assert.Equal(0x0006u, s.HotkeyModifiers);
        Assert.Equal(0x20u, s.HotkeyKey);
        Assert.Equal(PopupLayout.Flat, s.PopupLayout);
        Assert.Equal(SortField.LastUsed, s.PopupSort);
        Assert.Equal(new AppSettings(), s);
    }

    [Fact]
    public void SaveLoad_RoundTrips()
    {
        var store = new MemoryStore();
        var saved = new AppSettings
        {
            Theme = ThemeMode.Dark,
            TextSizePreset = TextSizePreset.Custom,
            CustomTextSize = 17.5,
            HotkeyModifiers = 0x0003,
            HotkeyKey = 0x41,
            PopupLayout = PopupLayout.Folder,
            PopupSort = SortField.UsageCount,
        };

        saved.Save(store);

        Assert.Equal(saved, AppSettings.Load(store));
        Assert.Equal("Dark", store.Values[SettingsKeys.Theme]);
        Assert.Equal("17.5", store.Values[SettingsKeys.TextSizeCustom]);
        Assert.Equal("Folder", store.Values[SettingsKeys.PopupLayout]);
        Assert.Equal("UsageCount", store.Values[SettingsKeys.PopupSort]);
    }

    [Fact]
    public void AppSettings_SaveShell_WritesNoPopupKeys()
    {
        var store = new MemoryStore();
        store.Set(SettingsKeys.PopupLayout, "Folder");
        store.Set(SettingsKeys.PopupSort, "UsageCount");

        new AppSettings { Theme = ThemeMode.Dark }.SaveShell(store);

        Assert.Equal("Folder", store.Get(SettingsKeys.PopupLayout));
        Assert.Equal("UsageCount", store.Get(SettingsKeys.PopupSort));
        Assert.Equal("Dark", store.Get(SettingsKeys.Theme));
        var empty = new MemoryStore();
        new AppSettings().SaveShell(empty);
        Assert.DoesNotContain(SettingsKeys.PopupLayout, empty.Values.Keys);
        Assert.DoesNotContain(SettingsKeys.PopupSort, empty.Values.Keys);
    }

    [Fact]
    public void Keys_AreExact()
    {
        var store = new MemoryStore();
        new AppSettings().Save(store);

        Assert.Equal(
            ["hotkey.key", "hotkey.modifiers", "popup.layout", "popup.sort", "text_size.custom", "text_size.preset", "theme"],
            store.Values.Keys.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void InvalidValue_FallsBackToDefault()
    {
        var store = new MemoryStore();
        store.Values[SettingsKeys.Theme] = "Purple";
        store.Values[SettingsKeys.TextSizePreset] = "99";
        store.Values[SettingsKeys.TextSizeCustom] = "big";
        store.Values[SettingsKeys.HotkeyModifiers] = "-1";
        store.Values[SettingsKeys.HotkeyKey] = "abc";
        store.Values[SettingsKeys.PopupLayout] = "";
        store.Values[SettingsKeys.PopupSort] = "Random";

        Assert.Equal(new AppSettings(), AppSettings.Load(store));
    }

    [Fact]
    public void ValidValuesSurviveNeighbouringInvalidOnes()
    {
        var store = new MemoryStore();
        store.Values[SettingsKeys.Theme] = "light";
        store.Values[SettingsKeys.PopupSort] = "nope";

        var s = AppSettings.Load(store);

        Assert.Equal(ThemeMode.Light, s.Theme);
        Assert.Equal(SortField.LastUsed, s.PopupSort);
    }

    [Theory]
    [InlineData("5", 10)]
    [InlineData("10", 10)]
    [InlineData("20.25", 20.25)]
    [InlineData("32", 32)]
    [InlineData("1000", 32)]
    [InlineData("-3", 10)]
    public void CustomTextSize_Clamped(string stored, double expected)
    {
        var store = new MemoryStore();
        store.Values[SettingsKeys.TextSizeCustom] = stored;

        Assert.Equal(expected, AppSettings.Load(store).CustomTextSize);
    }

    [Theory]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    public void CustomTextSize_NonFinite_UsesDefault(string stored)
    {
        var store = new MemoryStore();
        store.Values[SettingsKeys.TextSizeCustom] = stored;

        Assert.Equal(14, AppSettings.Load(store).CustomTextSize);
    }

    [Fact]
    public void Save_ClampsOutOfRangeCustomSize()
    {
        var store = new MemoryStore();

        new AppSettings { CustomTextSize = 99 }.Save(store);

        Assert.Equal("32", store.Values[SettingsKeys.TextSizeCustom]);
    }

    [Fact]
    public void Numbers_UseInvariantCulture()
    {
        var old = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
            var store = new MemoryStore();
            new AppSettings { CustomTextSize = 12.5 }.Save(store);

            Assert.Equal("12.5", store.Values[SettingsKeys.TextSizeCustom]);
            Assert.Equal(12.5, AppSettings.Load(store).CustomTextSize);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = old;
        }
    }
}
