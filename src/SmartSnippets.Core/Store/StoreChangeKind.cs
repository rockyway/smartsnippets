namespace SmartSnippets.Core.Store;

public enum StoreChangeKind
{
    SnippetCreated,
    SnippetUpdated,
    SnippetDeleted,
    FolderCreated,
    FolderRenamed,
    FolderDeleted,
    UsageRecorded,
    Reloaded,
    Imported,
}
