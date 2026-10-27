using SmartSnippets.Core.Domain;
using SmartSnippets.Core.Query;

namespace SmartSnippets.Core.Store;

/// <summary>
/// The only write path to snippets and folders. Single-threaded: call on the UI thread (a Debug build asserts it).
/// Expected failures return a failed <see cref="StoreResult"/>; database failures throw <see cref="StoreException"/>.
/// After any failure the snapshots are unchanged and no event is raised.
/// </summary>
public interface ISnippetStore
{
    /// <summary>Snapshot ordered by name (OrdinalIgnoreCase); replaced, never mutated, on change.</summary>
    IReadOnlyList<Folder> Folders { get; }

    /// <summary>Snapshot ordered by id with folder names joined; replaced, never mutated, on change.</summary>
    IReadOnlyList<SnippetView> Snippets { get; }

    /// <summary>Raised once per committed mutation, after the snapshots are replaced.</summary>
    event EventHandler<StoreChangedEventArgs>? Changed;

    /// <summary>Full reload from the database (startup); raises Changed(Reloaded).</summary>
    void Load();

    StoreResult<Snippet> CreateSnippet(SnippetDraft draft);

    StoreResult<Snippet> UpdateSnippet(long id, SnippetDraft draft);

    StoreResult DeleteSnippet(long id);

    StoreResult<Folder> CreateFolder(string name);

    StoreResult<Folder> RenameFolder(long id, string name);

    /// <summary>Moves the folder's snippets to Unfiled, then deletes the folder.</summary>
    StoreResult DeleteFolder(long id);

    /// <summary>Counts a use; leaves updated_at untouched.</summary>
    StoreResult RecordUsage(long id);
}
