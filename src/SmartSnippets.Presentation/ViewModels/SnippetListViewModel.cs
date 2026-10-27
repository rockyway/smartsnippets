using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SmartSnippets.Core.Query;
using SmartSnippets.Core.Store;
using SmartSnippets.Presentation.Text;

namespace SmartSnippets.Presentation.ViewModels;

/// <summary>Searchable snippet list: Label A to Z (case-insensitive), then Id. Rebuilds on every store change.</summary>
public sealed partial class SnippetListViewModel : ObservableObject, IDisposable
{
    private readonly ISnippetStore _store;
    private IReadOnlyList<SnippetView>? _searchSource;
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

    public SnippetListViewModel(ISnippetStore store)
    {
        _store = store;
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

        SyncBrowse(source);
        var scope = SelectedBrowse;

        var rows = _search.Filter(Query)
            .Where(v => InScope(v, scope))
            .OrderBy(v => v.Snippet.Label, StringComparer.OrdinalIgnoreCase)
            .ThenBy(v => v.Snippet.Id)
            .Select(SnippetRowViewModel.From)
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
            : string.IsNullOrWhiteSpace(Query) ? UiText.NoSnippets : UiText.NoMatches(Query);
        Rebuilt?.Invoke(this, EventArgs.Empty);
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
        var items = new List<BrowseItemViewModel>(folders.Count + 2)
        {
            new(BrowseKind.All, null, UiText.BrowseAll, source.Count),
        };
        foreach (var folder in folders)
        {
            items.Add(new BrowseItemViewModel(
                BrowseKind.Folder, folder.Id, folder.Name, source.Count(v => v.Snippet.FolderId == folder.Id)));
        }

        items.Add(new BrowseItemViewModel(BrowseKind.Unfiled, null, UiText.Unfiled, source.Count(v => v.Snippet.FolderId is null)));

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
