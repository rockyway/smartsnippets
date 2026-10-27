using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SmartSnippets.Core.Query;
using SmartSnippets.Core.Store;

namespace SmartSnippets.Presentation.ViewModels;

/// <summary>Searchable snippet list: Label A to Z (case-insensitive), then Id. Rebuilds on every store change.</summary>
public sealed partial class SnippetListViewModel : ObservableObject, IDisposable
{
    public const string NoSnippetsMessage = "No snippets yet";

    private readonly ISnippetStore _store;
    private IReadOnlyList<SnippetView>? _searchSource;
    private SnippetSearch? _search;
    private bool _rebuilding;
    private bool _disposed;

    [ObservableProperty]
    private string _query = string.Empty;

    [ObservableProperty]
    private long? _selectedId;

    [ObservableProperty]
    private string _emptyState = string.Empty;

    [ObservableProperty]
    private bool _isEmpty;

    public SnippetListViewModel(ISnippetStore store)
    {
        _store = store;
        _store.Changed += OnStoreChanged;
        Rebuild();
    }

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

        var rows = _search.Filter(Query)
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
            : string.IsNullOrWhiteSpace(Query) ? NoSnippetsMessage : $"No matches for '{Query}'";
        Rebuilt?.Invoke(this, EventArgs.Empty);
    }
}
