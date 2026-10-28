using SmartSnippets.Core.Settings;
using SmartSnippets.Core.Porting;
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
    public const string PreviewGlobalDialog = "Preview global dialog";
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

    // Insertion (007).
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
        if (!restoreNotice) return message;
        return outcome == InsertionOutcome.Inserted ? InsertedRestoreNotice : $"{message} {RestoreNotice}";
    }

    // Import and export (010).
    public const string ImportButtonName = "Import snippets";
    public const string ExportButtonName = "Export snippets";
    public const string ImportButtonTooltip = "Import snippets from Smart Snippets, TermFlow, Rephlo or InkSpoke (.json)";
    public const string ExportButtonTooltip = "Export all snippets and folders to a .json file";
    public const string ImportCompleteTitle = "Import complete";
    public const string ImportNothingTitle = "Nothing imported";
    public const string ImportFailedTitle = "Couldn't import";
    public const string ImportUnexpectedFailure = "Couldn't import the file. Nothing was imported.";
    public const string ImportOutcomeUnknown = "Couldn't finish the import. Check your snippet list before importing this file again.";
    public const string ExportTooLarge = "Your library is too large to export as one file: it would be over 32 MB. No file was written.";
    public const string ExportUnexpectedFailure = "Couldn't export. No file was written.";
    public const string ExportCompleteTitle = "Export complete";
    public const string ExportFailedTitle = "Couldn't export";
    public const string ExportNotJsonFile = "Choose a file name ending in .json.";
    public static string FormatName(ImportFormat format) => format switch
    {
        ImportFormat.Native => "Smart Snippets", ImportFormat.TermFlow => "TermFlow",
        ImportFormat.Rephlo => "Rephlo", ImportFormat.InkSpoke => "InkSpoke",
        _ => throw new ArgumentOutOfRangeException(nameof(format))
    };
    public static string ImportSummary(ImportSummary s) =>
        $"Imported {s.Imported} {(s.Imported == 1 ? "snippet" : "snippets")} from {FormatName(s.Format)}.\n" +
        $"Skipped {s.SkippedDuplicates} {(s.SkippedDuplicates == 1 ? "duplicate" : "duplicates")}.\n" +
        $"Skipped {s.SkippedUnsupported} unsupported {(s.SkippedUnsupported == 1 ? "entry" : "entries")}.\n" +
        $"Rejected {s.Rejected} malformed {(s.Rejected == 1 ? "entry" : "entries")}.";
    public static string ImportRefusalMessage(ImportFailure f)
    {
        var format = f.Format ?? ImportFormat.Native;
        var supported = format switch { ImportFormat.Rephlo => "1.3", ImportFormat.InkSpoke => "2", _ => "1" };
        var entries = format switch { ImportFormat.Rephlo => "commands", ImportFormat.InkSpoke => "Mappings", _ => "snippets" };
        var message = f.Refusal switch
        {
            ImportRefusal.NotJsonFile => "Choose a .json file. Smart Snippets imports JSON export files only.",
            ImportRefusal.Unreadable => "Couldn't read the file. Check that it still exists and isn't open in another app.",
            ImportRefusal.TooLarge => "This file is larger than 32 MB, the import limit.",
            ImportRefusal.NotUtf8 => "This file isn't UTF-8 text. Save it as UTF-8 and try again.",
            ImportRefusal.InvalidJson => "This file isn't valid JSON.",
            ImportRefusal.UnknownFormat => "This file isn't a snippet export from Smart Snippets, TermFlow, Rephlo or InkSpoke.",
            ImportRefusal.UnsupportedVersion => f.FoundVersion is { } version
                ? $"This {FormatName(format)} file is version {version}. Smart Snippets can import version {supported}."
                : $"This {FormatName(format)} file has no version number. Smart Snippets can import version {supported}.",
            ImportRefusal.MissingEntries => $"This {FormatName(format)} file has no \"{entries}\" list.",
            ImportRefusal.TooManyEntries => $"This {FormatName(format)} file has {f.EntryCount:N0} entries. Smart Snippets can import up to {SnippetFileRules.MaxImportEntries:N0} entries from one file.",
            _ => throw new ArgumentOutOfRangeException(nameof(f))
        };
        return message + " Nothing was imported.";
    }
    public static string ImportStoreFailed(string storeMessage) => $"Couldn't save the imported snippets: {storeMessage} Nothing was imported.";
    public static string ExportTooManySnippets(int count) => $"Your library has {count:N0} snippets. One export file can hold up to {SnippetFileRules.MaxImportEntries:N0}, so that it can be imported again. No file was written.";
    public static string ExportTooManyEntries(int snippets, int folders) => $"Your library has {snippets:N0} snippets and {folders:N0} folders. One export file can hold up to {SnippetFileRules.MaxImportEntries:N0} entries (snippets and folders), so that it can be imported again. No file was written.";
    public static string ExportOutcomeUnknown(string fileName) => $"Couldn't finish the export. Check {fileName} before relying on it.";
    public static string ExportComplete(int snippets, int folders, string fileName) => $"Exported {snippets} {(snippets == 1 ? "snippet" : "snippets")} and {folders} {(folders == 1 ? "folder" : "folders")} to {fileName}.";
    public static string ExportWriteFailed(string fileName) => $"Couldn't write {fileName}. Choose another folder and try again. Any existing file was left unchanged.";

    // Tray.
    public const string TrayOpen = "Open Smart Snippets";
    public const string TrayShowPopup = "Show popup";
    public const string TraySettings = "Settings";
    public const string TrayExit = "Exit";
}
