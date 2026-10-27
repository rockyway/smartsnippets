namespace SmartSnippets.Core.Store;

/// <summary>
/// <see cref="Id"/> is the snippet or folder the change is about (null for Reloaded).
/// <see cref="SnippetIds"/> lists every snippet whose row changed (for FolderDeleted: those moved to Unfiled).
/// </summary>
public sealed class StoreChangedEventArgs : EventArgs
{
    public StoreChangedEventArgs(StoreChangeKind kind, long? id, IReadOnlyList<long> snippetIds)
    {
        Kind = kind;
        Id = id;
        SnippetIds = snippetIds;
    }

    public StoreChangeKind Kind { get; }

    public long? Id { get; }

    public IReadOnlyList<long> SnippetIds { get; }
}
