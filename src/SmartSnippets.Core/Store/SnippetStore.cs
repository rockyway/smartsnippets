using Microsoft.Data.Sqlite;
using SmartSnippets.Core.Data;
using SmartSnippets.Core.Domain;
using SmartSnippets.Core.Logging;
using SmartSnippets.Core.Query;

namespace SmartSnippets.Core.Store;

public sealed class SnippetStore : ISnippetStore
{
    private const int SqliteBusy = 5;
    private const int SqliteLocked = 6;
    private const int SqliteCorrupt = 11;
    private const int SqliteNotADatabase = 26;

    private readonly SqliteConnectionFactory _factory;
    private readonly TimeProvider _time;
    private readonly FolderRepository _folderRepository;
    private readonly SnippetRepository _snippetRepository;
    private readonly int _ownerThreadId = Environment.CurrentManagedThreadId;

    private IReadOnlyList<Folder> _folders = [];
    private IReadOnlyList<Snippet> _snippets = [];
    private IReadOnlyList<SnippetView> _views = [];

    public SnippetStore(SqliteConnectionFactory factory, TimeProvider time)
    {
        _factory = factory;
        _time = time;
        _folderRepository = new FolderRepository(factory);
        _snippetRepository = new SnippetRepository(factory);
    }

    public event EventHandler<StoreChangedEventArgs>? Changed;

    public IReadOnlyList<Folder> Folders => _folders;

    public IReadOnlyList<SnippetView> Snippets => _views;

    public void Load()
    {
        AssertOwnerThread();
        var (folders, snippets) = Guarded("load", () =>
        {
            using var connection = _factory.Open();
            return (_folderRepository.LoadAll(connection), _snippetRepository.LoadAll(connection));
        });

        Publish(folders, snippets);
        AppLog.Info("store_loaded", $"count={snippets.Count}");
        Raise(StoreChangeKind.Reloaded, null, []);
    }

    public StoreResult<Snippet> CreateSnippet(SnippetDraft draft)
    {
        AssertOwnerThread();
        var prepared = Prepare(draft);
        if (prepared.Error is { } error)
        {
            return StoreResult<Snippet>.Fail(error);
        }

        var (label, isCustom, tags, _) = prepared;
        var now = _time.GetUtcNow();
        var created = Write("create_snippet", (c, tx) =>
            _snippetRepository.Insert(c, tx, draft.Text, label, isCustom, draft.FolderId, tags, now));

        Publish(_folders, [.. _snippets, created]);
        Raise(StoreChangeKind.SnippetCreated, created.Id, [created.Id]);
        return StoreResult<Snippet>.Ok(created);
    }

    public StoreResult<Snippet> UpdateSnippet(long id, SnippetDraft draft)
    {
        AssertOwnerThread();
        var existing = FindSnippet(id);
        if (existing is null)
        {
            return StoreResult<Snippet>.Fail(ValidationError.SnippetNotFound);
        }

        var prepared = Prepare(draft);
        if (prepared.Error is { } error)
        {
            return StoreResult<Snippet>.Fail(error);
        }

        var (label, isCustom, tags, _) = prepared;
        var now = Stored(_time.GetUtcNow());
        var found = Write("update_snippet", (c, tx) =>
            _snippetRepository.Update(c, tx, id, draft.Text, label, isCustom, draft.FolderId, tags, now));
        if (!found)
        {
            return StoreResult<Snippet>.Fail(ValidationError.SnippetNotFound);
        }

        var updated = existing with
        {
            Text = draft.Text,
            Label = label,
            IsLabelCustom = isCustom,
            FolderId = draft.FolderId,
            Tags = tags,
            UpdatedAt = now,
        };
        Publish(_folders, ReplaceSnippet(updated));
        Raise(StoreChangeKind.SnippetUpdated, id, [id]);
        return StoreResult<Snippet>.Ok(updated);
    }

    public StoreResult DeleteSnippet(long id)
    {
        AssertOwnerThread();
        if (FindSnippet(id) is null)
        {
            return StoreResult.Fail(ValidationError.SnippetNotFound);
        }

        var found = Write("delete_snippet", (c, tx) => _snippetRepository.Delete(c, tx, id));
        if (!found)
        {
            return StoreResult.Fail(ValidationError.SnippetNotFound);
        }

        Publish(_folders, [.. _snippets.Where(s => s.Id != id)]);
        Raise(StoreChangeKind.SnippetDeleted, id, [id]);
        return StoreResult.Ok();
    }

    public StoreResult<Folder> CreateFolder(string name)
    {
        AssertOwnerThread();
        var validation = FolderNameRules.Validate(name);
        if (validation is { } error)
        {
            return StoreResult<Folder>.Fail(error);
        }

        var normalized = FolderNameRules.Normalize(name);
        var key = FolderNameRules.Key(name);
        if (_folders.Any(f => FolderNameRules.Key(f.Name) == key))
        {
            return StoreResult<Folder>.Fail(ValidationError.FolderNameDuplicate);
        }

        var now = _time.GetUtcNow();
        var created = Write<Folder?>("create_folder", (c, tx) =>
            _folderRepository.NameKeyExists(c, tx, key, null)
                ? null
                : _folderRepository.Insert(c, tx, normalized, key, now));
        if (created is null)
        {
            return StoreResult<Folder>.Fail(ValidationError.FolderNameDuplicate);
        }

        Publish([.. _folders, created], _snippets);
        Raise(StoreChangeKind.FolderCreated, created.Id, []);
        return StoreResult<Folder>.Ok(created);
    }

    public StoreResult<Folder> RenameFolder(long id, string name)
    {
        AssertOwnerThread();
        var existing = _folders.FirstOrDefault(f => f.Id == id);
        if (existing is null)
        {
            return StoreResult<Folder>.Fail(ValidationError.FolderNotFound);
        }

        var validation = FolderNameRules.Validate(name);
        if (validation is { } error)
        {
            return StoreResult<Folder>.Fail(error);
        }

        var normalized = FolderNameRules.Normalize(name);
        var key = FolderNameRules.Key(name);
        if (_folders.Any(f => f.Id != id && FolderNameRules.Key(f.Name) == key))
        {
            return StoreResult<Folder>.Fail(ValidationError.FolderNameDuplicate);
        }

        var now = Stored(_time.GetUtcNow());
        var outcome = Write("rename_folder", (c, tx) =>
        {
            if (_folderRepository.NameKeyExists(c, tx, key, id))
            {
                return ValidationError.FolderNameDuplicate;
            }

            return _folderRepository.Rename(c, tx, id, normalized, key, now) ? (ValidationError?)null : ValidationError.FolderNotFound;
        });
        if (outcome is { } failed)
        {
            return StoreResult<Folder>.Fail(failed);
        }

        var renamed = existing with { Name = normalized, UpdatedAt = now };
        Publish([.. _folders.Select(f => f.Id == id ? renamed : f)], _snippets);
        Raise(StoreChangeKind.FolderRenamed, id, []);
        return StoreResult<Folder>.Ok(renamed);
    }

    public StoreResult DeleteFolder(long id)
    {
        AssertOwnerThread();
        if (_folders.All(f => f.Id != id))
        {
            return StoreResult.Fail(ValidationError.FolderNotFound);
        }

        var now = Stored(_time.GetUtcNow());
        var moved = Write("delete_folder", (c, tx) => _folderRepository.Delete(c, tx, id, now));
        if (moved < 0)
        {
            return StoreResult.Fail(ValidationError.FolderNotFound);
        }

        var affected = new List<long>();
        var snippets = new List<Snippet>(_snippets.Count);
        foreach (var snippet in _snippets)
        {
            if (snippet.FolderId == id)
            {
                affected.Add(snippet.Id);
                snippets.Add(snippet with { FolderId = null, UpdatedAt = now });
            }
            else
            {
                snippets.Add(snippet);
            }
        }

        Publish([.. _folders.Where(f => f.Id != id)], snippets);
        Raise(StoreChangeKind.FolderDeleted, id, affected);
        return StoreResult.Ok();
    }

    public StoreResult RecordUsage(long id)
    {
        AssertOwnerThread();
        var existing = FindSnippet(id);
        if (existing is null)
        {
            return StoreResult.Fail(ValidationError.SnippetNotFound);
        }

        var now = Stored(_time.GetUtcNow());
        var found = Write("record_usage", (c, tx) => _snippetRepository.RecordUsage(c, tx, id, now));
        if (!found)
        {
            return StoreResult.Fail(ValidationError.SnippetNotFound);
        }

        Publish(_folders, ReplaceSnippet(existing with { LastUsedAt = now, UsageCount = existing.UsageCount + 1 }));
        Raise(StoreChangeKind.UsageRecorded, id, [id]);
        return StoreResult.Ok();
    }

    private static DateTimeOffset Stored(DateTimeOffset value) => DbTime.FromDb(DbTime.ToDb(value));

    private static StoreException ToStoreException(Exception ex)
    {
        if (ex is SqliteException sqlite)
        {
            return sqlite.SqliteErrorCode switch
            {
                SqliteBusy or SqliteLocked => new StoreException(
                    StoreFailure.DatabaseBusy, "The snippet database is busy. Please try again.", ex),
                SqliteCorrupt or SqliteNotADatabase => new StoreException(
                    StoreFailure.Corrupt, "The snippet database appears to be damaged.", ex),
                _ => new StoreException(StoreFailure.DatabaseUnavailable, "The snippet database could not be accessed.", ex),
            };
        }

        return new StoreException(StoreFailure.DatabaseUnavailable, "The snippet database could not be accessed.", ex);
    }

    [System.Diagnostics.Conditional("DEBUG")]
    private void AssertOwnerThread() =>
        System.Diagnostics.Debug.Assert(Environment.CurrentManagedThreadId == _ownerThreadId, "SnippetStore must be used on the thread that created it.");

    private T Guarded<T>(string op, Func<T> action)
    {
        try
        {
            return action();
        }
        catch (Exception ex) when (ex is SqliteException or IOException)
        {
            // Only the operation name is logged: never text, labels, tags or folder names.
            AppLog.Error("store_write_failed", ex, $"op={op}");
            throw ToStoreException(ex);
        }
    }

    /// <summary>Runs <paramref name="work"/> in one transaction; any database failure rolls back and throws.</summary>
    private T Write<T>(string op, Func<SqliteConnection, SqliteTransaction, T> work) => Guarded(op, () =>
    {
        using var connection = _factory.Open();
        using var transaction = connection.BeginTransaction();
        var result = work(connection, transaction);
        transaction.Commit();
        return result;
    });

    private (string Label, bool IsCustom, IReadOnlyList<string> Tags, ValidationError? Error) Prepare(SnippetDraft draft)
    {
        var tags = TagList.Parse(draft.TagsInput);
        var (label, isCustom) = LabelSuggester.ResolveLabel(draft, tags);
        var errors = SnippetValidator.Validate(draft.Text, label);
        if (errors.Count > 0)
        {
            return (label, isCustom, tags, errors[0]);
        }

        if (draft.FolderId is { } folderId && _folders.All(f => f.Id != folderId))
        {
            return (label, isCustom, tags, ValidationError.FolderNotFound);
        }

        return (label, isCustom, tags, null);
    }

    private Snippet? FindSnippet(long id)
    {
        foreach (var snippet in _snippets)
        {
            if (snippet.Id == id)
            {
                return snippet;
            }
        }

        return null;
    }

    private List<Snippet> ReplaceSnippet(Snippet replacement) =>
        [.. _snippets.Select(s => s.Id == replacement.Id ? replacement : s)];

    private void Publish(IReadOnlyList<Folder> folders, IReadOnlyList<Snippet> snippets)
    {
        var sortedFolders = folders
            .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(f => f.Id)
            .ToArray();
        var names = new Dictionary<long, string>(sortedFolders.Length);
        foreach (var folder in sortedFolders)
        {
            names[folder.Id] = folder.Name;
        }

        var views = new SnippetView[snippets.Count];
        for (var i = 0; i < views.Length; i++)
        {
            var snippet = snippets[i];
            views[i] = new SnippetView(
                snippet,
                snippet.FolderId is { } id && names.TryGetValue(id, out var name) ? name : null);
        }

        _folders = Array.AsReadOnly(sortedFolders);
        _snippets = snippets;
        _views = Array.AsReadOnly(views);
    }

    private void Raise(StoreChangeKind kind, long? id, IReadOnlyList<long> snippetIds) =>
        Changed?.Invoke(this, new StoreChangedEventArgs(kind, id, snippetIds));
}
