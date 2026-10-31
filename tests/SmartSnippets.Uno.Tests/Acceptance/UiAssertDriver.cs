namespace SmartSnippets.Uno;

/// <summary>Read-only postconditions for observer-acknowledged rendered callbacks; never performs CRUD.</summary>
internal static class UiAssertDriver
{
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
