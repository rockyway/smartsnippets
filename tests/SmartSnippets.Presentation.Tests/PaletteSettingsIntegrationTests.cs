using Microsoft.Data.Sqlite;
using SmartSnippets.Core.Data;
using SmartSnippets.Core.Settings;
using SmartSnippets.Core.Store;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Presentation.Tests.Fakes;
using SmartSnippets.Presentation.ViewModels;

namespace SmartSnippets.Presentation.Tests;

public sealed class PaletteSettingsIntegrationTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "ss-palette-" + Guid.NewGuid().ToString("N"));
    private readonly SqliteConnectionFactory _factory;

    public PaletteSettingsIntegrationTests()
    {
        _factory = new SqliteConnectionFactory(Path.Combine(_directory, "test.db"));
        new Migrator(_factory).MigrateToLatest();
    }

    [Theory]
    [InlineData(ColorPalette.Default)]
    [InlineData(ColorPalette.Ocean)]
    [InlineData(ColorPalette.Forest)]
    [InlineData(ColorPalette.Plum)]
    public void PaletteAndTheme_SurviveReloadAndPreservePopupPreferences(ColorPalette palette)
    {
        var store = new SettingsRepository(_factory);
        store.Set(SettingsKeys.PopupLayout, PopupLayout.Folder.ToString());
        store.Set(SettingsKeys.PopupSort, SortField.UsageCount.ToString());
        var theme = new FakeThemeService();
        using (var vm = new SettingsViewModel(AppSettings.Load(store), store, theme, new TextSizeService(),
            new FakeStartupRegistration(), new FakeHotkeyManager(), new FakeElevation()))
        {
            vm.SelectedPalette = palette == ColorPalette.Ocean ? ColorPalette.Plum : ColorPalette.Ocean;
            vm.SelectedPalette = palette;
            vm.SelectedTheme = ThemeMode.Dark;
        }

        var reloaded = AppSettings.Load(new SettingsRepository(_factory));
        Assert.Equal(palette, reloaded.ColorPalette);
        Assert.Equal(ThemeMode.Dark, reloaded.Theme);
        Assert.Equal(PopupLayout.Folder, reloaded.PopupLayout);
        Assert.Equal(SortField.UsageCount, reloaded.PopupSort);
        Assert.Equal(palette, theme.CurrentPalette);
        Assert.Equal(ThemeMode.Dark, theme.Current);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try { Directory.Delete(_directory, true); } catch (IOException) { }
    }
}
