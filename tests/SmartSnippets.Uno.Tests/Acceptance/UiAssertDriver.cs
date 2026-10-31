namespace SmartSnippets.Uno;

/// <summary>Read-only postconditions for observer-acknowledged rendered callbacks; never performs CRUD.</summary>
internal static class UiAssertDriver
{
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

    internal static void RequireSaved(UnoAppHost host)
    {
        if (host.Snippets.Editor is not { IsDirty: false, Text: SavedText }
            || host.Store.Snippets.Count != 1 || host.Store.Snippets.Single().Snippet.Text != SavedText)
            throw new InvalidOperationException("ui_save");
        // Reload from SQLite, not just the editor's in-memory projection.
        host.Store.Load();
        if (host.Store.Snippets.Count != 1 || host.Store.Snippets.Single().Snippet.Text != SavedText
            || host.Store.Snippets.Single().Snippet.UsageCount != 0 || host.Store.Folders.Count != 0)
            throw new InvalidOperationException("ui_reload");
    }

    internal static void RequireCancelled(UnoAppHost host)
    {
        if (host.Snippets.Editor is not { IsDirty: true, Text: DirtyText }
            || host.Store.Snippets.Count != 1 || host.Store.Snippets.Single().Snippet.Text != SavedText)
            throw new InvalidOperationException("ui_cancel");
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
