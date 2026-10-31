using SmartSnippets.Core;
using SmartSnippets.Core.Data;
using SmartSnippets.Core.Settings;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Presentation.ViewModels;

namespace SmartSnippets.Uno;

/// <summary>Read-only postconditions for observer-acknowledged rendered callbacks; never performs CRUD.</summary>
internal static class UiAssertDriver
{
    internal static void RequireSettingsFresh(UnoAppHost host)
    {
        RequireFresh(host);
        RequireSettingsState(host, ThemeMode.System, TextSizePreset.Medium, TextSizeService.MediumSize);
        if (host.SettingsStore.Get(SettingsKeys.Theme) is not null
            || host.SettingsStore.Get(SettingsKeys.TextSizePreset) is not null)
            throw new InvalidOperationException("ui_settings_not_fresh");
    }

    internal static void RequireSettingsTheme(UnoAppHost host) =>
        RequireSettingsState(host, ThemeMode.Dark, TextSizePreset.Medium, TextSizeService.MediumSize);

    internal static void RequireSettingsTextSize(UnoAppHost host) =>
        RequireSettingsState(host, ThemeMode.Dark, TextSizePreset.Large, TextSizeService.LargeSize);

    internal static void RequireSettingsReloaded(UnoAppHost host, string profileRoot)
    {
        RequireSettingsTextSize(host);
        // Fresh repository/connection read only: no Save, Set, migration or VM mutation.
        var database = new AppPaths(profileRoot).DatabasePath;
        if (!File.Exists(database)) throw new InvalidOperationException("ui_settings_reload_missing");
        var repository = new SettingsRepository(new SqliteConnectionFactory(database));
        var loaded = AppSettings.Load(repository);
        if (repository.Get(SettingsKeys.Theme) != "Dark" || repository.Get(SettingsKeys.TextSizePreset) != "Large"
            || loaded.Theme != ThemeMode.Dark || loaded.TextSizePreset != TextSizePreset.Large
            || loaded.CustomTextSize != 14 || loaded.ColorPalette != ColorPalette.Default)
            throw new InvalidOperationException("ui_settings_reload");
    }

    private static void RequireSettingsState(UnoAppHost host, ThemeMode theme, TextSizePreset size, double fontSize)
    {
        var loaded = AppSettings.Load(host.SettingsStore);
        if (host.SettingsPage.SelectedTheme != theme || host.SettingsPage.SelectedTextSizePreset != size
            || host.TextSize.FontSize != fontSize || host.SettingsPage.TextSize.FontSize != fontSize
            || loaded.Theme != theme || loaded.TextSizePreset != size || host.Gate.IsAvailable
            || host.Store.Snippets.Count != 0 || host.Store.Folders.Count != 0)
            throw new InvalidOperationException("ui_settings_state");
    }

    internal const string FolderName = "ui019 synthetic folder";
    internal const string RenamedFolderName = "ui019 synthetic renamed";
    internal const string FolderSnippetText = "ui019 synthetic attached";

    internal static long RequireFolderCreated(UnoAppHost host)
    {
        if (host.Store.Folders.Count != 1 || host.Store.Folders.Single().Name != FolderName
            || host.Store.Snippets.Count != 0 || host.Folders.NewFolderName != "")
            throw new InvalidOperationException("ui_folder_create");
        var id = host.Store.Folders.Single().Id;
        host.Store.Load();
        if (host.Store.Folders.Count != 1 || host.Store.Folders.Single().Id != id
            || host.Store.Folders.Single().Name != FolderName)
            throw new InvalidOperationException("ui_folder_create_reload");
        return id;
    }

    internal static void RequireFolderAttached(UnoAppHost host, long id)
    {
        if (host.Store.Snippets.Count != 1 || host.Store.Snippets.Single().Snippet.FolderId != id
            || host.Store.Snippets.Single().Snippet.Text != FolderSnippetText
            || host.Store.Snippets.Single().Snippet.UsageCount != 0)
            throw new InvalidOperationException("ui_folder_seed");
    }

    internal static void RequireFolderRenamed(UnoAppHost host, long id)
    {
        host.Store.Load();
        if (host.Store.Folders.Count != 1 || host.Store.Folders.Single().Id != id
            || host.Store.Folders.Single().Name != RenamedFolderName || host.Folders.IsRenameInProgress)
            throw new InvalidOperationException("ui_folder_rename");
        RequireFolderAttached(host, id);
    }

    internal static void RequireFolderDeleted(UnoAppHost host, long snippetId)
    {
        host.Store.Load();
        if (host.Store.Folders.Count != 0 || host.Store.Snippets.Count != 1
            || host.Store.Snippets.Single().Snippet.Id != snippetId
            || host.Store.Snippets.Single().Snippet.FolderId is not null
            || host.Store.Snippets.Single().Snippet.Text != FolderSnippetText
            || host.Store.Snippets.Single().Snippet.UsageCount != 0
            || !host.Folders.IsEmpty || host.Folders.Rows.Count != 1
            || host.Folders.Rows.Single().Id != 0 || host.Folders.Rows.Single().SnippetCount != 1)
            throw new InvalidOperationException("ui_folder_delete_unfiled");
    }

    internal const string SavedText = "ui019 synthetic saved";
    internal const string DirtyText = "ui019 synthetic dirty";
    internal const string DiscardText = "ui019 synthetic discarded";

    internal static void RequireFresh(UnoAppHost host)
    {
        if (host.Store.Snippets.Count != 0 || host.Store.Folders.Count != 0 || host.Snippets.Editor is not null)
            throw new InvalidOperationException("ui_not_fresh");
    }

    internal static void RequireNew(UnoAppHost host)
    {
        if (host.Snippets.Editor is not { Text: "" } || host.Store.Snippets.Count != 0)
            throw new InvalidOperationException("ui_new");
    }

    internal static long RequireSaved(UnoAppHost host)
    {
        if (host.Snippets.Editor is not { IsDirty: false, Text: SavedText }
            || host.Store.Snippets.Count != 1 || host.Store.Snippets.Single().Snippet.Text != SavedText)
            throw new InvalidOperationException("ui_save");
        // Reload from SQLite, not just the editor's in-memory projection.
        host.Store.Load();
        if (host.Store.Snippets.Count != 1 || host.Store.Snippets.Single().Snippet.Text != SavedText
            || host.Store.Snippets.Single().Snippet.UsageCount != 0 || host.Store.Folders.Count != 0)
            throw new InvalidOperationException("ui_reload");
        return host.Store.Snippets.Single().Snippet.Id;
    }

    internal static void RequireCancelled(UnoAppHost host)
    {
        host.Store.Load();
        if (host.Snippets.Editor is not { IsDirty: true, Text: DirtyText, IsNew: false } editor
            || host.Store.Snippets.Count != 1 || host.Store.Snippets.Single().Snippet.Text != SavedText
            || host.Store.Snippets.Single().Snippet.Id != editor.SnippetId
            || host.Store.Snippets.Single().Snippet.UsageCount != 0)
            throw new InvalidOperationException("ui_cancel");
    }

    internal static void RequireDirtySaved(UnoAppHost host, long savedId)
    {
        RequireGuardedNewPersistence(host, savedId);
        RequireCleanNewEditor(host);
    }

    internal static SnippetEditorViewModel RequireDiscardDirty(UnoAppHost host, long savedId)
    {
        RequireGuardedNewPersistence(host, savedId);
        if (host.Snippets.Editor is not { IsDirty: true, IsNew: true, SnippetId: null, Text: DiscardText })
            throw new InvalidOperationException("ui_discard_not_dirty");
        return host.Snippets.Editor;
    }

    internal static void RequireDiscarded(UnoAppHost host, long savedId, SnippetEditorViewModel discardedEditor)
    {
        RequireGuardedNewPersistence(host, savedId);
        RequireCleanNewEditor(host);
        if (ReferenceEquals(host.Snippets.Editor, discardedEditor))
            throw new InvalidOperationException("ui_discard_missing_new_transition");
    }

    private static void RequireCleanNewEditor(UnoAppHost host)
    {
        if (host.Snippets.Editor is not { IsDirty: false, IsNew: true, SnippetId: null, Text: "" })
            throw new InvalidOperationException("ui_guarded_new_editor");
    }

    private static void RequireGuardedNewPersistence(UnoAppHost host, long savedId)
    {
        host.Store.Load();
        if (host.Store.Snippets.Count != 1 || host.Store.Folders.Count != 0
            || host.Store.Snippets.Single().Snippet.Id != savedId
            || host.Store.Snippets.Single().Snippet.Text != DirtyText
            || host.Store.Snippets.Single().Snippet.UsageCount != 0
            || host.Store.Snippets.Single().Snippet.FolderId is not null || host.Gate.IsAvailable)
            throw new InvalidOperationException("ui_guarded_new_persistence");
    }
}

/// <summary>Explicit fixture setup, never counted as rendered assignment evidence.</summary>
internal static class FolderSceneSeed
{
    internal static long AttachSyntheticSnippet(UnoAppHost host, long folderId)
    {
        var result = host.Store.CreateSnippet(new(UiAssertDriver.FolderSnippetText, "", false, folderId, ""));
        if (!result.Success) throw new InvalidOperationException("ui_folder_seed_failed");
        return result.Value!.Id;
    }
}
