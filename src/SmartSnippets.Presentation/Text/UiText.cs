using SmartSnippets.Presentation.Services;

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

    // Insertion (007).
    public const string RestartAsAdministrator = "Restart as administrator";
    public const string PasteAnyway = "Paste anyway";
    public const string InsertedRestoreNotice = "Pasted, but your previous clipboard couldn't be fully restored.";
    public const string RestoreNotice = "Your previous clipboard couldn't be fully restored.";

    public static string InsertionMessage(InsertionOutcome outcome, string displayName, bool restoreNotice)
    {
        var app = string.IsNullOrWhiteSpace(displayName) ? "the target app" : displayName;
        var message = outcome switch
        {
            InsertionOutcome.Inserted => "",
            InsertionOutcome.TargetClosed => $"Couldn't insert. {app} was closed. Nothing was pasted.",
            InsertionOutcome.TargetElevated => "The target app is running as administrator. Smart Snippets can't insert text into it.",
            InsertionOutcome.FocusFailed => $"Couldn't switch back to {app}. Nothing was pasted. Click in {app} and try again.",
            InsertionOutcome.ClipboardBusy => "Couldn't insert because another app is using the clipboard. Nothing was pasted. Try again in a moment.",
            InsertionOutcome.PasteFailed => $"Couldn't paste into {app}. Use Copy text and paste it yourself.",
            InsertionOutcome.SnippetMissing => "Couldn't insert. This snippet was deleted. Nothing was pasted.",
            InsertionOutcome.Busy => "Already inserting a snippet.",
            InsertionOutcome.ClipboardNotPreserved => "Your clipboard holds something Smart Snippets can't save and put back. Nothing was pasted. Paste anyway? What's on your clipboard now may be lost.",
            InsertionOutcome.MaybePasted => $"Smart Snippets may have pasted into {app}. Check it before trying again.",
            InsertionOutcome.CopiedForManualPaste => $"Couldn't paste into {app}. The snippet is on your clipboard. Paste it yourself with Ctrl+V.",
            _ => throw new ArgumentOutOfRangeException(nameof(outcome)),
        };
        if (!restoreNotice || outcome == InsertionOutcome.MaybePasted) return message;
        return outcome == InsertionOutcome.Inserted ? InsertedRestoreNotice : $"{message} {RestoreNotice}";
    }

    // Tray.
    public const string TrayOpen = "Open Smart Snippets";
    public const string TrayShowPopup = "Show popup";
    public const string TraySettings = "Settings";
    public const string TrayExit = "Exit";
}
