using SmartSnippets.Core.Domain;
using SmartSnippets.Core.Query;
using SmartSnippets.Core.Store;

namespace SmartSnippets.Presentation.Tests.Fakes;

/// <summary>
/// In-memory <see cref="ISnippetStore"/> applying the real Core rules (TagList, LabelSuggester, SnippetValidator,
/// FolderNameRules) and raising <c>Changed</c> like the real store. Scriptable to throw <see cref="StoreException"/>.
/// </summary>
internal sealed class FakeSnippetStore : ISnippetStore
{
    private readonly List<Folder> _folders = [];
    private readonly List<Snippet> _snippets = [];
    private long _nextId = 1;
    private DateTimeOffset _clock = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private IReadOnlyList<Folder> _foldersSnapshot = [];
    private IReadOnlyList<SnippetView> _viewsSnapshot = [];

    public event EventHandler<StoreChangedEventArgs>? Changed;

    public IReadOnlyList<Folder> Folders => _foldersSnapshot;

    public IReadOnlyList<SnippetView> Snippets => _viewsSnapshot;

    /// <summary>When set, every mutating call throws it (after any hook ran) until cleared.</summary>
    public StoreException? Failure { get; set; }

    /// <summary>Runs at the start of <see cref="CreateSnippet"/>/<see cref="UpdateSnippet"/> (re-entrancy tests).</summary>
    public Action? OnSaveCalled { get; set; }

    public Action<long>? OnRecordUsage { get; set; }

    public List<SnippetDraft> CreatedDrafts { get; } = [];

    public List<(long Id, SnippetDraft Draft)> UpdatedDrafts { get; } = [];

    public List<StoreChangedEventArgs> Raised { get; } = [];

    public int ChangedSubscriberCount => Changed?.GetInvocationList().Length ?? 0;

    public Snippet SeedSnippet(string text, string? label = null, string? folder = null, string tags = "")
    {
        long? folderId = folder is null ? null : (_folders.FirstOrDefault(f => f.Name == folder) ?? SeedFolder(folder)).Id;
        var result = CreateSnippet(new SnippetDraft(text, label ?? "", label is not null, folderId, tags));
        return result.Value ?? throw new InvalidOperationException(result.Message);
    }

    public Folder SeedFolder(string name) =>
        CreateFolder(name).Value ?? throw new InvalidOperationException("seed folder failed");

    public void Load() => Raise(StoreChangeKind.Reloaded, null, []);

    public StoreResult<Snippet> CreateSnippet(SnippetDraft draft)
    {
        OnSaveCalled?.Invoke();
        CreatedDrafts.Add(draft);
        ThrowIfFailing();
        if (Prepare(draft) is { } error)
        {
            return StoreResult<Snippet>.Fail(error);
        }

        var tags = TagList.Parse(draft.TagsInput);
        var (label, isCustom) = LabelSuggester.ResolveLabel(draft, tags);
        var now = Tick();
        var created = new Snippet(_nextId++, draft.Text, label, isCustom, draft.FolderId, tags, now, now, null, 0);
        _snippets.Add(created);
        Publish();
        Raise(StoreChangeKind.SnippetCreated, created.Id, [created.Id]);
        return StoreResult<Snippet>.Ok(created);
    }

    public StoreResult<Snippet> UpdateSnippet(long id, SnippetDraft draft)
    {
        OnSaveCalled?.Invoke();
        ThrowIfFailing();
        UpdatedDrafts.Add((id, draft));
        var index = _snippets.FindIndex(s => s.Id == id);
        if (index < 0)
        {
            return StoreResult<Snippet>.Fail(ValidationError.SnippetNotFound);
        }

        if (Prepare(draft) is { } error)
        {
            return StoreResult<Snippet>.Fail(error);
        }

        var tags = TagList.Parse(draft.TagsInput);
        var (label, isCustom) = LabelSuggester.ResolveLabel(draft, tags);
        var updated = _snippets[index] with
        {
            Text = draft.Text,
            Label = label,
            IsLabelCustom = isCustom,
            FolderId = draft.FolderId,
            Tags = tags,
            UpdatedAt = Tick(),
        };
        _snippets[index] = updated;
        Publish();
        Raise(StoreChangeKind.SnippetUpdated, id, [id]);
        return StoreResult<Snippet>.Ok(updated);
    }

    public StoreResult DeleteSnippet(long id)
    {
        ThrowIfFailing();
        if (_snippets.RemoveAll(s => s.Id == id) == 0)
        {
            return StoreResult.Fail(ValidationError.SnippetNotFound);
        }

        Publish();
        Raise(StoreChangeKind.SnippetDeleted, id, [id]);
        return StoreResult.Ok();
    }

    public StoreResult<Folder> CreateFolder(string name)
    {
        ThrowIfFailing();
        if (FolderNameRules.Validate(name) is { } error)
        {
            return StoreResult<Folder>.Fail(error);
        }

        if (_folders.Any(f => FolderNameRules.Key(f.Name) == FolderNameRules.Key(name)))
        {
            return StoreResult<Folder>.Fail(ValidationError.FolderNameDuplicate);
        }

        var now = Tick();
        var created = new Folder(_nextId++, FolderNameRules.Normalize(name), now, now);
        _folders.Add(created);
        Publish();
        Raise(StoreChangeKind.FolderCreated, created.Id, []);
        return StoreResult<Folder>.Ok(created);
    }

    public StoreResult<Folder> RenameFolder(long id, string name)
    {
        ThrowIfFailing();
        var index = _folders.FindIndex(f => f.Id == id);
        if (index < 0)
        {
            return StoreResult<Folder>.Fail(ValidationError.FolderNotFound);
        }

        if (FolderNameRules.Validate(name) is { } error)
        {
            return StoreResult<Folder>.Fail(error);
        }

        if (_folders.Any(f => f.Id != id && FolderNameRules.Key(f.Name) == FolderNameRules.Key(name)))
        {
            return StoreResult<Folder>.Fail(ValidationError.FolderNameDuplicate);
        }

        var renamed = _folders[index] with { Name = FolderNameRules.Normalize(name), UpdatedAt = Tick() };
        _folders[index] = renamed;
        Publish();
        Raise(StoreChangeKind.FolderRenamed, id, []);
        return StoreResult<Folder>.Ok(renamed);
    }

    public StoreResult DeleteFolder(long id)
    {
        ThrowIfFailing();
        if (_folders.RemoveAll(f => f.Id == id) == 0)
        {
            return StoreResult.Fail(ValidationError.FolderNotFound);
        }

        var moved = new List<long>();
        for (var i = 0; i < _snippets.Count; i++)
        {
            if (_snippets[i].FolderId == id)
            {
                moved.Add(_snippets[i].Id);
                _snippets[i] = _snippets[i] with { FolderId = null, UpdatedAt = Tick() };
            }
        }

        Publish();
        Raise(StoreChangeKind.FolderDeleted, id, moved);
        return StoreResult.Ok();
    }

    public StoreResult RecordUsage(long id)
    {
        OnRecordUsage?.Invoke(id);
        ThrowIfFailing();
        var index = _snippets.FindIndex(s => s.Id == id);
        if (index < 0)
        {
            return StoreResult.Fail(ValidationError.SnippetNotFound);
        }

        _snippets[index] = _snippets[index] with { LastUsedAt = Tick(), UsageCount = _snippets[index].UsageCount + 1 };
        Publish();
        Raise(StoreChangeKind.UsageRecorded, id, [id]);
        return StoreResult.Ok();
    }

    private ValidationError? Prepare(SnippetDraft draft)
    {
        var tags = TagList.Parse(draft.TagsInput);
        var (label, _) = LabelSuggester.ResolveLabel(draft, tags);
        var errors = SnippetValidator.Validate(draft.Text, label);
        if (errors.Count > 0)
        {
            return errors[0];
        }

        return draft.FolderId is { } folderId && _folders.All(f => f.Id != folderId)
            ? ValidationError.FolderNotFound
            : null;
    }

    private void ThrowIfFailing()
    {
        if (Failure is { } failure)
        {
            throw failure;
        }
    }

    private DateTimeOffset Tick() => _clock = _clock.AddMinutes(1);

    private void Publish()
    {
        var sorted = _folders.OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase).ThenBy(f => f.Id).ToArray();
        var names = sorted.ToDictionary(f => f.Id, f => f.Name);
        _foldersSnapshot = sorted;
        _viewsSnapshot = _snippets
            .OrderBy(s => s.Id)
            .Select(s => new SnippetView(s, s.FolderId is { } id && names.TryGetValue(id, out var n) ? n : null))
            .ToArray();
    }

    private void Raise(StoreChangeKind kind, long? id, IReadOnlyList<long> snippetIds)
    {
        var args = new StoreChangedEventArgs(kind, id, snippetIds);
        Raised.Add(args);
        Changed?.Invoke(this, args);
    }
}
