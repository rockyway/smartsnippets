using SmartSnippets.Core.Domain;
using SmartSnippets.Core.Settings;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Uno.Platforms.Windows;
using SmartSnippets.Uno.Tests.Composition;

namespace SmartSnippets.Uno.Tests.Flows;

public sealed class ManagementFlowTests
{
    [Fact]
    public async Task SnippetCrudSearchFolderDeleteToUnfiled_RoundtripsRealHostStore()
    {
        using var f = new HostFixture(); await f.Start();
        f.Host.Folders.NewFolderName = "Synthetic 日本語"; await f.Host.Folders.CreateAsync();
        var folder = Assert.Single(f.Host.Store.Folders);
        f.Host.Folders.BeginRename(f.Host.Folders.Rows.First(r => r.Id == folder.Id));
        var row = f.Host.Folders.Rows.First(r => r.Id == folder.Id); row.EditName = "Renamed";
        Assert.True(await f.Host.Folders.CommitRenameAsync(row));
        var snippet = f.Host.Store.CreateSnippet(new("first\nsecond 😀", "Label", true, folder.Id, "tag, 日本語")).Value!;
        Assert.True(await f.Host.Snippets.RequestEditAsync(snippet.Id));
        f.Host.Snippets.Editor!.Text = "edited\nbody 😀";
        Assert.True(await f.Host.Snippets.Editor.SaveAsync());
        f.Host.ShowPreview("edited");
        Assert.Contains(f.Host.Popup.Items, i => i.SnippetId == snippet.Id);
        await f.Host.Folders.DeleteAsync(f.Host.Folders.Rows.First(r => r.Id == folder.Id));
        Assert.Empty(f.Host.Store.Folders);
        Assert.Null(Assert.Single(f.Host.Store.Snippets).Snippet.FolderId);
        f.Host.Store.Load();
        Assert.Equal("edited\nbody 😀", Assert.Single(f.Host.Store.Snippets).Snippet.Text);
        Assert.True(await f.Host.Snippets.DeleteRowAsync(snippet.Id));
        f.Host.Store.Load(); Assert.Empty(f.Host.Store.Snippets);
    }
    [Theory]
    [InlineData(SaveChoice.Save, true, "changed")]
    [InlineData(SaveChoice.Discard, true, "original")]
    [InlineData(SaveChoice.Cancel, false, "original")]
    public async Task DirtyNavigation_SaveDiscardCancel(SaveChoice choice, bool allowed, string saved)
    {
        using var f = new HostFixture(); await f.Start();
        var a = f.Host.Store.CreateSnippet(new("original", "A", true, null, "")).Value!;
        var b = f.Host.Store.CreateSnippet(new("other", "B", true, null, "")).Value!;
        Assert.True(await f.Host.Snippets.RequestEditAsync(a.Id)); f.Host.Snippets.Editor!.Text = "changed";
        f.Boundary.Dialogs.Choices.Enqueue(choice);
        Assert.Equal(allowed, await f.Host.Snippets.RequestEditAsync(b.Id));
        f.Host.Store.Load(); Assert.Equal(saved, f.Host.Store.Snippets.First(s => s.Snippet.Id == a.Id).Snippet.Text);
    }
    [Fact]
    public async Task DirtyNavigation_SaveValidationFailureRetainsDraft()
    {
        using var f = new HostFixture(); await f.Start(); await f.Host.Snippets.NewAsync();
        f.Host.Snippets.Editor!.Text = " "; f.Boundary.Dialogs.Choices.Enqueue(SaveChoice.Save);
        Assert.False(await f.Host.Snippets.CanLeaveAsync());
        Assert.True(f.Host.Snippets.Editor.IsDirty); Assert.Empty(f.Host.Store.Snippets);
    }
    [Fact]
    public async Task Settings_ReloadsCompatibleValuesWithoutRegistration()
    {
        using var f = new HostFixture(); await f.Start();
        f.Host.SettingsPage.SelectedTheme = ThemeMode.Dark;
        f.Host.SettingsPage.SelectedPalette = ColorPalette.Ocean;
        f.Host.SettingsPage.SelectedTextSizePreset = TextSizePreset.Custom;
        f.Host.SettingsPage.CustomTextSize = 23;
        f.Host.SettingsPage.StartAtLogin = true;
        Assert.False(f.Host.SettingsPage.StartAtLogin);
        Assert.False(new CandidateStartupRegistration().Enable().Success);
        Assert.False(new CandidateStartupRegistration().Disable().Success);
        Assert.False(new CandidateHotkeyManager(f.Host.Settings.Hotkey).TryChange(f.Host.Settings.Hotkey).Success);
        Assert.Equal(ElevationOutcome.Failed, new CandidateElevation().RestartAsAdministrator().Outcome);
        var reloaded = AppSettings.Load(f.Host.SettingsStore);
        Assert.Equal(ThemeMode.Dark, reloaded.Theme); Assert.Equal(ColorPalette.Ocean, reloaded.ColorPalette);
        Assert.Equal(23, reloaded.CustomTextSize);
        Assert.Equal(23, f.Host.TextSize.FontSize);
    }
    [Fact]
    public Task ImportExport_RealJsonCancelAndFailedPublicationLeaveOriginal() => CandidateHostThread.RunAsync(async () =>
    {
        using var f = new HostFixture(); await f.Start();
        Assert.Null(await f.Host.Snippets.Porting.ImportAsync());
        Assert.False(await f.Host.Snippets.Porting.ExportAsync());
        f.Host.Store.CreateSnippet(new("synthetic\n😀", "Label", true, null, "test"));
        var path = Path.Combine(f.Root, "synthetic.json"); f.Boundary.Files.ExportPath = path;
        Assert.True(await f.Host.Snippets.Porting.ExportAsync());
        var original = File.ReadAllBytes(path);
        f.Boundary.Files.FailWrite = true;
        Assert.False(await f.Host.Snippets.Porting.ExportAsync());
        Assert.Equal(original, File.ReadAllBytes(path));
        f.Boundary.Files.FailWrite = false;
        if (OperatingSystem.IsWindows())
        {
            // Real Core atomic publication is denied after temporary-file writing, not just a fake exception.
            using var lockedOriginal = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            Assert.False(await f.Host.Snippets.Porting.ExportAsync());
            Assert.Equal(original, File.ReadAllBytes(path));
        }
        f.Boundary.Files.ImportPath = path;
        f.Host.Store.DeleteSnippet(Assert.Single(f.Host.Store.Snippets).Snippet.Id);
        var imported = await f.Host.Snippets.Porting.ImportAsync();
        Assert.NotNull(imported?.Summary); Assert.Equal("synthetic\n😀", Assert.Single(f.Host.Store.Snippets).Snippet.Text);
        File.WriteAllText(path, "{broken");
        var invalid = await f.Host.Snippets.Porting.ImportAsync(); Assert.NotNull(invalid?.Failure);
        Assert.Single(f.Host.Store.Snippets);
        var oversized = Path.Combine(f.Root, "oversized.json");
        using (var file = File.Create(oversized)) file.SetLength(SmartSnippets.Core.Porting.SnippetFileRules.MaxImportBytes + 1L);
        f.Boundary.Files.ImportPath = oversized;
        Assert.NotNull((await f.Host.Snippets.Porting.ImportAsync())?.Failure);
        Assert.Single(f.Host.Store.Snippets);
    });
}
