using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartSnippets.Core.Domain;
using SmartSnippets.Core.Logging;
using SmartSnippets.Core.Query;
using SmartSnippets.Core.Store;
using SmartSnippets.Presentation.Text;
using SmartSnippets.Presentation.Services;

namespace SmartSnippets.Presentation.ViewModels;

/// <summary>Searchable snippet list: Label A to Z (case-insensitive), then Id. Rebuilds on every store change.</summary>
public sealed partial class SnippetListViewModel : ObservableObject, IDisposable
{
    private readonly ISnippetStore _store;
    private readonly ITextSizeService? _textSize;
    private IReadOnlyList<SnippetView>? _searchSource;
    private IReadOnlyList<SnippetView>? _browseSource;
    private object? _browseFolders;
    private SnippetSearch? _search;
    private bool _rebuilding;
    private bool _syncingBrowse;
    private bool _disposed;

    [ObservableProperty]
    private string _query = string.Empty;

    [ObservableProperty]
    private long? _selectedId;

    [ObservableProperty]
    private string _emptyState = string.Empty;

    [ObservableProperty]
    private bool _isEmpty;

    /// <summary>The Browse scope. Two-way bindable; null means All. Re-pointed at the new record instance on every rebuild.</summary>
    [ObservableProperty]
    private BrowseItemViewModel? _selectedBrowse;

    /// <summary>Quick-add row in the Browse pane is open.</summary>
    [ObservableProperty]
    private bool _isCreatingFolder;

    [ObservableProperty]
    private string _newFolderName = string.Empty;

    [ObservableProperty]
    private string _newFolderError = string.Empty;

    public SnippetListViewModel(ISnippetStore store, ITextSizeService? textSize = null)
    {
        _store = store;
        _textSize = textSize;
        _store.Changed += OnStoreChanged;
        Rebuild();
    }

    /// <summary>All first, then each folder by name, then Unfiled last. Counts ignore the search query.</summary>
    public ObservableCollection<BrowseItemViewModel> BrowseItems { get; } = [];

    public ObservableCollection<SnippetRowViewModel> Items { get; } = [];

    /// <summary>Two-way friendly selection by row; null writes while the list is being rebuilt are ignored.</summary>
    public SnippetRowViewModel? SelectedItem
    {
        get => SelectedId is { } id ? Items.FirstOrDefault(r => r.Id == id) : null;
        set
        {
            if (_rebuilding)
            {
                return;
            }

            SelectedId = value?.Id;
        }
    }

    /// <summary>Raised after every rebuild, whether or not the visible rows changed.</summary>
    public event EventHandler? Rebuilt;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _store.Changed -= OnStoreChanged;
    }

    partial void OnQueryChanged(string value) => Rebuild();

    partial void OnNewFolderNameChanged(string value) => NewFolderError = string.Empty;

    [RelayCommand]
    private void StartNewFolder()
    {
        NewFolderName = string.Empty;
        NewFolderError = string.Empty;
        IsCreatingFolder = true;
    }

    [RelayCommand]
    private void CancelNewFolder()
    {
        IsCreatingFolder = false;
        NewFolderName = string.Empty;
        NewFolderError = string.Empty;
    }

    /// <summary>Creates the folder from the Browse pane and selects it as the scope. Errors stay inline.</summary>
    [RelayCommand]
    public void CreateFolder()
    {
        StoreResult<Folder> result;
        try
        {
            result = _store.CreateFolder(NewFolderName);
        }
        catch (StoreException ex)
        {
            AppLog.Info("save_failed", $"kind={ex.Failure}");
            NewFolderError = UiText.FolderCreateFailed;
            return;
        }

        if (!result.Success)
        {
            NewFolderError = FolderMessages.For(result.Error!.Value, NewFolderName);
            return;
        }

        var folder = result.Value!;
        AppLog.Info("folder_created", $"id={folder.Id}");
        IsCreatingFolder = false;
        NewFolderName = string.Empty;
        NewFolderError = string.Empty;
        SelectedBrowse = BrowseItems.FirstOrDefault(b => b.Kind == BrowseKind.Folder && b.FolderId == folder.Id) ?? SelectedBrowse;
    }

    partial void OnSelectedBrowseChanged(BrowseItemViewModel? value)
    {
        if (_syncingBrowse)
        {
            return;
        }

        if (value is null)
        {
            SelectedBrowse = BrowseItems.FirstOrDefault();
            return;
        }

        Rebuild();
    }

    partial void OnSelectedIdChanged(long? value) => OnPropertyChanged(nameof(SelectedItem));

    private void OnStoreChanged(object? sender, StoreChangedEventArgs e) => Rebuild();

    private void Rebuild()
    {
        var source = _store.Snippets;
        if (!ReferenceEquals(source, _searchSource) || _search is null)
        {
            _search = new SnippetSearch(source);
            _searchSource = source;
        }

        // Browse counts ignore the query, so they are rebuilt only when the store's snapshots change.
        if (!ReferenceEquals(source, _browseSource) || !ReferenceEquals(_store.Folders, _browseFolders))
        {
            SyncBrowse(source);
        }

        var scope = SelectedBrowse;

        var rows = _search.Filter(Query)
            .Where(v => InScope(v, scope))
            .OrderBy(v => v.Snippet.Label, StringComparer.OrdinalIgnoreCase)
            .ThenBy(v => v.Snippet.Id)
            .Select(view => SnippetRowViewModel.From(view, _textSize))
            .ToList();

        if (!rows.SequenceEqual(Items))
        {
            _rebuilding = true;
            try
            {
                Items.Clear();
                foreach (var row in rows)
                {
                    Items.Add(row);
                }
            }
            finally
            {
                _rebuilding = false;
            }
        }

        if (SelectedId is { } id && rows.All(r => r.Id != id))
        {
            SelectedId = null;
        }
        else
        {
            OnPropertyChanged(nameof(SelectedItem));
        }

        IsEmpty = rows.Count == 0;
        EmptyState = rows.Count > 0
            ? string.Empty
            : EmptyText(Query, scope);
        Rebuilt?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Names the Browse scope when it is narrower than All, so an empty folder never reads as an empty library.</summary>
    private static string EmptyText(string query, BrowseItemViewModel? scope)
    {
        var scoped = scope is { Kind: not BrowseKind.All };
        if (string.IsNullOrWhiteSpace(query))
        {
            return scoped ? UiText.NoSnippetsIn(scope!.Name) : UiText.NoSnippets;
        }

        return scoped ? UiText.NoMatchesIn(query, scope!.Name) : UiText.NoMatches(query);
    }

    private static bool InScope(SnippetView view, BrowseItemViewModel? scope) => scope?.Kind switch
    {
        BrowseKind.Folder => view.Snippet.FolderId == scope.FolderId,
        BrowseKind.Unfiled => view.Snippet.FolderId is null,
        _ => true,
    };

    private void SyncBrowse(IReadOnlyList<SnippetView> source)
    {
        var folders = _store.Folders;
        _browseSource = source;
        _browseFolders = folders;

        var counts = new Dictionary<long, int>();
        var unfiled = 0;
        foreach (var view in source)
        {
            if (view.Snippet.FolderId is { } folderId)
            {
                counts[folderId] = counts.GetValueOrDefault(folderId) + 1;
            }
            else
            {
                unfiled++;
            }
        }

        var items = new List<BrowseItemViewModel>(folders.Count + 2)
        {
            new(BrowseKind.All, null, UiText.BrowseAll, source.Count),
        };
        foreach (var folder in folders)
        {
            items.Add(new BrowseItemViewModel(BrowseKind.Folder, folder.Id, folder.Name, counts.GetValueOrDefault(folder.Id)));
        }

        items.Add(new BrowseItemViewModel(BrowseKind.Unfiled, null, UiText.Unfiled, unfiled));

        var previous = SelectedBrowse;
        _syncingBrowse = true;
        try
        {
            if (!items.SequenceEqual(BrowseItems))
            {
                BrowseItems.Clear();
                foreach (var item in items)
                {
                    BrowseItems.Add(item);
                }
            }

            SelectedBrowse = previous is null
                ? BrowseItems[0]
                : BrowseItems.FirstOrDefault(b => b.Kind == previous.Kind && b.FolderId == previous.FolderId) ?? BrowseItems[0];
        }
        finally
        {
            _syncingBrowse = false;
        }
    }
}
