using SmartSnippets.Core.Settings;

namespace SmartSnippets.Presentation.Text;

/// <summary>
/// User-facing wording from docs/design/001-smart-snippets-design.html (plan 009). Views and
/// view-models take text from here so tests can assert the exact design wording.
/// </summary>
public static class UiText
{
    // Browse pane and list.
    public const string BrowseAll = "All";
    public const string Unfiled = "Unfiled";
    public const string NoSnippets = "No snippets yet";
    public static string NoMatches(string query) => $"No matches for '{query}'";
    public static string NoSnippetsIn(string scope) => $"No snippets in {scope}";
    public static string NoMatchesIn(string query, string scope) => $"No matches for '{query}' in {scope}";

    /// <summary>"Support, used 31 times", "Email, used once", "Unfiled, never used".</summary>
    public static string RowMeta(string folder, int usageCount) => usageCount switch
    {
        <= 0 => $"{folder}, never used",
        1 => $"{folder}, used once",
        _ => $"{folder}, used {usageCount} times",
    };

    // Editor.
    public const string CustomLabel = "Custom label";
    public const string AutoLabelHint = "Edit the label to stop it following your tags.";
    public const string UnsavedChanges = "Unsaved changes";
    public static string UsedTimes(int usageCount) => usageCount switch
    {
        <= 0 => "Never",
        1 => "Once",
        _ => $"{usageCount} times",
    };

    // Snippet dialogs.
    public static string DeleteSnippetTitle(string label) => $"Delete \"{label}\"?";
    public const string DeleteSnippetBody = "This snippet and its usage history will be permanently removed.";
    public const string DeleteSnippetConfirm = "Delete snippet";
    public const string DiscardTitle = "Discard your changes?";
    public const string DiscardBody = "You've edited this snippet but haven't saved it.";
    public const string KeepEditing = "Keep editing";
    public const string Discard = "Discard";
    public const string Save = "Save";

    // Save failure, shown inline in the editor.
    public const string SaveFailed = "Couldn't save your snippet. Your text is still in the editor, so nothing is lost.";
    public const string TryAgain = "Try again";
    public const string CopyText = "Copy text";

    // Folders.
    public const string FolderNameTaken = "That folder name is already taken. Try another.";
    public const string FolderCreateFailed = "Couldn't create the folder. Try again.";
    public const string NewFolderPlaceholder = "Folder name";
    public static string SnippetCount(int count) => count == 1 ? "1 snippet" : $"{count} snippets";
    public static string UnfiledCount(int count) => $"{SnippetCount(count)}, built in";
    public const string FolderDeleteNote = "Deleting a folder moves its snippets to Unfiled. The snippets themselves are kept.";
    public static string DeleteFolderTitle(string name) => $"Delete folder \"{name}\"?";
    public static string DeleteFolderBody(int count) => count switch
    {
        0 => "It has no snippets.",
        1 => "Its 1 snippet will move to Unfiled. The snippet isn't deleted.",
        _ => $"Its {count} snippets will move to Unfiled. The snippets aren't deleted.",
    };
    public const string DeleteFolderConfirm = "Delete folder";

    // Settings.
    public const string HotkeyHint = "Press a new combination to capture it. Conflicts are explained here.";
    public static string HotkeyConflict(string attempted, string kept) =>
        $"Another app is already using {attempted}. Your hotkey is still {kept}. Try a different combination.";
    public static string TextSizeHint(double size) => $"{size:0.#} units. Custom accepts 10 to 32.";
    public const string StartAtLoginHint = "Starts in the background without opening the main window.";

    // Global popup.
    public const string PopupSearchPlaceholder = "Search label, text, folder, or tag";
    public const string PopupSearchName = "Search snippets";
    public const string PopupHint = "↑↓ move · Enter insert · Esc close";
    public static string CountOf(int n, int m) => $"{n} of {m} {(m == 1 ? "snippet" : "snippets")}";
    public static string FolderHeading(string name, int n) => $"{name} ({n})";
    public const string NoSnippetsMatch = "No snippets match";
    public const string NoSnippetsMatchBody = "Try fewer words, or add a new snippet with the plus button.";
    public static string LayoutTooltip(PopupLayout layout) => layout == PopupLayout.Folder ? "Layout: folder" : "Layout: flat";
    public static string SortName(SortField sort) => sort switch
    {
        SortField.UsageCount => "Usage count", SortField.Created => "Created date", SortField.Updated => "Updated date", _ => "Last used",
    };
    public static string SortTooltip(SortField sort) => "Sort: " + SortName(sort).ToLowerInvariant();
    public const string ToggleLayout = "Toggle layout";
    public const string SortButton = "Sort";
    public const string AddSnippet = "Add snippet";
    public const string OpenMainWindow = "Open main window";
    public static string TargetKept(string name) => $"Insertion target kept: {name}";
    public const string CtrlEnterSaves = "Ctrl+Enter saves";
    public const string Cancel = "Cancel";
    public const string SnippetAddedReady = "Snippet added and ready to insert";
    public const string RestartAsAdministrator = "Restart as administrator";
    public const string InsertFailedGeneric = "Couldn't insert the snippet. Nothing was pasted.";
    public const string ClipboardLossPrompt = "Some of what's on your clipboard can't be restored after pasting. Paste anyway?";
    public const string PasteAnyway = "Paste anyway";
    public const string ConfirmHint = "Enter choose · Esc cancel";
    public const string Dismiss = "Dismiss";
    public const string DismissHint = "Enter dismiss · Esc close";
    public const string MaybePastedFallback = "The snippet may already have been pasted. Check the target app before inserting it again.";
    public const string CopiedForManualPasteFallback = "The snippet is on your clipboard. Click in the target app and press Ctrl+V.";
    public const string InsertNoticeTitle = "Snippet inserted";

    // Tray.
    public const string TrayOpen = "Open Smart Snippets";
    public const string TrayShowPopup = "Show popup";
    public const string TraySettings = "Settings";
    public const string TrayExit = "Exit";
}
