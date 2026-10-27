using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartSnippets.Core.Domain;
using SmartSnippets.Core.Logging;
using SmartSnippets.Core.Store;
using SmartSnippets.Presentation.Navigation;
using SmartSnippets.Presentation.Services;

namespace SmartSnippets.Presentation.ViewModels;

/// <summary>Folders page: create, rename in place and delete folders. Follows the store through <c>Changed</c>.</summary>
public sealed partial class FoldersPageViewModel : ObservableObject, IGuardedPage, IDisposable
{
    public const string NoFoldersMessage = "No folders yet";

    private readonly ISnippetStore _store;
    private readonly IDialogService _dialogs;
    private bool _disposed;

    [ObservableProperty]
    private string _newFolderName = string.Empty;

    [ObservableProperty]
    private string _newFolderError = string.Empty;

    [ObservableProperty]
    private bool _isEmpty;

    public FoldersPageViewModel(ISnippetStore store, IDialogService dialogs)
    {
        _store = store;
        _dialogs = dialogs;
        Refresh();
        _store.Changed += OnStoreChanged;
    }

    public ObservableCollection<FolderRowViewModel> Rows { get; } = [];

    public string EmptyState => NoFoldersMessage;

    public bool IsRenameInProgress => Rows.Any(r => r.IsRenaming);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _store.Changed -= OnStoreChanged;
    }

    /// <summary>Creates the folder named in <see cref="NewFolderName"/>; failures show in <see cref="NewFolderError"/>.</summary>
    [RelayCommand]
    public async Task CreateAsync()
    {
        NewFolderError = string.Empty;
        StoreResult<Folder> result;
        try
        {
            result = _store.CreateFolder(NewFolderName);
        }
        catch (StoreException ex)
        {
            await ShowStoreFailureAsync("create the folder", ex);
            return;
        }

        if (!result.Success)
        {
            NewFolderError = FolderMessages.For(result.Error!.Value, NewFolderName);
            return;
        }

        AppLog.Info("folder_created", $"id={result.Value!.Id}");
        NewFolderName = string.Empty;
    }

    /// <summary>Starts renaming <paramref name="row"/>; any other rename in progress is cancelled.</summary>
    [RelayCommand]
    public void BeginRename(FolderRowViewModel row)
    {
        foreach (var other in Rows.Where(r => r.IsRenaming && !ReferenceEquals(r, row)).ToList())
        {
            CancelRename(other);
        }

        row.EditName = row.Name;
        row.RenameError = string.Empty;
        row.IsRenaming = true;
        OnPropertyChanged(nameof(IsRenameInProgress));
    }

    /// <summary>Enter: stores the new name. False (and the row stays in rename mode with an error) on failure.</summary>
    public async Task<bool> CommitRenameAsync(FolderRowViewModel row)
    {
        if (!row.IsRenaming)
        {
            return true;
        }

        if (string.Equals(FolderNameRules.Normalize(row.EditName), row.Name, StringComparison.Ordinal))
        {
            CancelRename(row);
            return true;
        }

        StoreResult<Folder> result;
        try
        {
            result = _store.RenameFolder(row.Id, row.EditName);
        }
        catch (StoreException ex)
        {
            await ShowStoreFailureAsync("rename the folder", ex);
            return false;
        }

        if (!result.Success)
        {
            row.RenameError = FolderMessages.For(result.Error!.Value, row.EditName);
            return false;
        }

        AppLog.Info("folder_renamed", $"id={row.Id}");
        row.IsRenaming = false;
        row.RenameError = string.Empty;
        OnPropertyChanged(nameof(IsRenameInProgress));
        return true;
    }

    /// <summary>Esc: abandons the rename.</summary>
    [RelayCommand]
    public void CancelRename(FolderRowViewModel row)
    {
        row.IsRenaming = false;
        row.EditName = row.Name;
        row.RenameError = string.Empty;
        OnPropertyChanged(nameof(IsRenameInProgress));
    }

    /// <summary>Deletes the folder after confirming; its snippets move to Unfiled.</summary>
    [RelayCommand]
    public async Task DeleteAsync(FolderRowViewModel row)
    {
        var count = row.SnippetCount;
        var noun = count == 1 ? "snippet" : "snippets";
        if (!await _dialogs.ConfirmAsync(
                "Delete folder", $"Delete '{row.Name}'? {count} {noun} will move to Unfiled.", "Delete"))
        {
            return;
        }

        StoreResult result;
        try
        {
            result = _store.DeleteFolder(row.Id);
        }
        catch (StoreException ex)
        {
            await ShowStoreFailureAsync("delete the folder", ex);
            return;
        }

        if (!result.Success && result.Error != ValidationError.FolderNotFound)
        {
            await _dialogs.ShowErrorAsync("Delete failed", $"Could not delete the folder: {result.Message}");
            return;
        }

        AppLog.Info("folder_deleted", $"id={row.Id}");
    }

    /// <summary>
    /// Leaving the page with a rename in progress: unchanged renames are dropped silently; otherwise
    /// Save commits (staying on failure), Discard abandons it, Cancel stays.
    /// </summary>
    public async Task<bool> CanLeaveAsync()
    {
        foreach (var row in Rows.Where(r => r.IsRenaming).ToList())
        {
            if (string.Equals(FolderNameRules.Normalize(row.EditName), row.Name, StringComparison.Ordinal))
            {
                CancelRename(row);
                continue;
            }

            var choice = await _dialogs.AskSaveDiscardCancelAsync(
                "Unsaved rename", $"Save the new name for '{row.Name}' before continuing?");
            switch (choice)
            {
                case SaveChoice.Save:
                    if (!await CommitRenameAsync(row))
                    {
                        return false;
                    }

                    break;
                case SaveChoice.Discard:
                    CancelRename(row);
                    break;
                default:
                    return false;
            }
        }

        return true;
    }

    partial void OnNewFolderNameChanged(string value) => NewFolderError = string.Empty;

    private void OnStoreChanged(object? sender, StoreChangedEventArgs e) => Refresh();

    private async Task ShowStoreFailureAsync(string action, StoreException ex)
    {
        AppLog.Info("save_failed", $"kind={ex.Failure}");
        await _dialogs.ShowErrorAsync("Folder error", $"Could not {action}: {ex.Message}");
    }

    private void Refresh()
    {
        var counts = new Dictionary<long, int>();
        foreach (var view in _store.Snippets)
        {
            if (view.Snippet.FolderId is { } folderId)
            {
                counts[folderId] = counts.GetValueOrDefault(folderId) + 1;
            }
        }

        var existing = Rows.ToDictionary(r => r.Id);
        var ordered = new List<FolderRowViewModel>(_store.Folders.Count);
        foreach (var folder in _store.Folders
                     .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(f => f.Id))
        {
            var count = counts.GetValueOrDefault(folder.Id);
            if (existing.TryGetValue(folder.Id, out var row))
            {
                row.Name = folder.Name;
                row.SnippetCount = count;
            }
            else
            {
                row = new FolderRowViewModel(folder.Id, folder.Name, count);
            }

            ordered.Add(row);
        }

        if (!ordered.SequenceEqual(Rows))
        {
            Rows.Clear();
            foreach (var row in ordered)
            {
                Rows.Add(row);
            }
        }

        IsEmpty = Rows.Count == 0;
        OnPropertyChanged(nameof(IsRenameInProgress));
    }
}
