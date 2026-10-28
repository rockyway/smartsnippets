using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartSnippets.Core.Domain;
using SmartSnippets.Core.Logging;
using SmartSnippets.Core.Store;
using SmartSnippets.Presentation.Navigation;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Presentation.Text;

namespace SmartSnippets.Presentation.ViewModels;

/// <summary>
/// Master-detail coordinator: the list plus the current editor. Every action that would lose unsaved edits
/// (select another row, New, delete another snippet, leaving the page) asks Save / Discard / Cancel first.
/// </summary>
public sealed partial class SnippetsPageViewModel : ObservableObject, IGuardedPage, IDisposable
{
    public const string EmptyEditorMessage = "Select a snippet or create a new one.";

    private readonly ISnippetStore _store;
    private readonly IDialogService _dialogs;
    private readonly IClipboardService? _clipboard;
    private readonly TimeProvider? _time;
    private readonly Func<long, Task>? _insertRow;
    private readonly Func<bool>? _canInsertRow;
    private int _suppressListSelection;
    private bool _disposed;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasEditor))]
    [NotifyCanExecuteChangedFor(nameof(DeleteSelectedCommand))]
    private SnippetEditorViewModel? _editor;

    public SnippetsPageViewModel(
        ISnippetStore store,
        IDialogService dialogs,
        IClipboardService? clipboard = null,
        TimeProvider? time = null,
        ISnippetFileService? files = null,
        ITextSizeService? textSize = null,
        Func<long, Task>? insertRow = null,
        Func<bool>? canInsertRow = null)
    {
        _store = store;
        _dialogs = dialogs;
        _clipboard = clipboard;
        _time = time;
        _insertRow = insertRow;
        _canInsertRow = canInsertRow;
        Porting = new SnippetPortingViewModel(store, dialogs, files, time);
        List = new SnippetListViewModel(store, textSize);
        List.PropertyChanged += OnListPropertyChanged;
        List.Rebuilt += OnListRebuilt;
    }

    public SnippetListViewModel List { get; }

    public SnippetPortingViewModel Porting { get; }

    public bool HasEditor => Editor is not null;

    public string EditorEmptyMessage => EmptyEditorMessage;

    /// <summary>The in-flight handling of the latest list click (for tests and for views that want to await it).</summary>
    public Task PendingSelection { get; private set; } = Task.CompletedTask;

    /// <summary>The in-flight delete started by the editor's Delete button (for tests and views that want to await it).</summary>
    public Task PendingDelete { get; private set; } = Task.CompletedTask;

    /// <summary>
    /// Raised when the list selection was put back after a cancelled or failed switch (argument: the id now selected,
    /// or null). <see cref="SnippetListViewModel.SelectedId"/> changes too; views bound to it need nothing else.
    /// </summary>
    public event EventHandler<long?>? SelectionRestored;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        List.PropertyChanged -= OnListPropertyChanged;
        List.Rebuilt -= OnListRebuilt;
        List.Dispose();
        SetEditor(null);
    }

    /// <summary>Opens the snippet in the editor after resolving unsaved edits. False when the switch did not happen.</summary>
    public async Task<bool> RequestSelectAsync(long id)
    {
        if (Editor is { IsDeleted: false } current && current.SnippetId == id)
        {
            SyncListToEditor();
            return true;
        }

        if (!await GuardAsync())
        {
            RestoreListSelection();
            return false;
        }

        if (!_store.Snippets.Any(v => v.Snippet.Id == id))
        {
            RestoreListSelection();
            return false;
        }

        SetEditor(NewEditorFor(id));
        SyncListToEditor();
        return true;
    }

    /// <summary>Starts a blank editor after resolving unsaved edits. False when the user cancelled.</summary>
    public async Task<bool> NewAsync()
    {
        if (!await GuardAsync())
        {
            return false;
        }

        SetEditor(NewEditorFor(null));
        SetListSelection(null);
        return true;
    }

    public bool CanInsertRow => _insertRow is not null && _canInsertRow?.Invoke() == true;

    public void CopyRow(long id)
    {
        var snippet = _store.Snippets.FirstOrDefault(v => v.Snippet.Id == id)?.Snippet;
        if (snippet is null || _clipboard is null) return;
        _clipboard.SetText(snippet.Text);
        AppLog.Info("snippet_row_copied", $"id={id}");
    }

    public Task InsertRowAsync(long id) => CanInsertRow && _store.Snippets.Any(v => v.Snippet.Id == id)
        ? _insertRow!(id) : Task.CompletedTask;

    public Task<bool> DeleteRowAsync(long id) => DeleteAsync(id);

    /// <summary>Deletes the selected snippet after confirming, then selects the next row. False when nothing was deleted.</summary>
    public Task<bool> DeleteSelectedAsync() => DeleteAsync(List.SelectedId ?? Editor?.SnippetId);

    private async Task<bool> DeleteAsync(long? targetId)
    {
        if (targetId is not { } id)
        {
            return false;
        }

        var view = _store.Snippets.FirstOrDefault(v => v.Snippet.Id == id);
        if (view is null)
        {
            return false;
        }

        var isCurrent = Editor?.SnippetId == id;
        if (!isCurrent && !await GuardAsync())
        {
            RestoreListSelection();
            return false;
        }

        if (!await _dialogs.ConfirmAsync(
                UiText.DeleteSnippetTitle(view.Snippet.Label), UiText.DeleteSnippetBody, UiText.DeleteSnippetConfirm))
        {
            return false;
        }

        var index = IndexInList(id);
        StoreResult result;
        try
        {
            result = _store.DeleteSnippet(id);
        }
        catch (StoreException ex)
        {
            AppLog.Info("delete_failed", $"kind={ex.Failure}");
            await _dialogs.ShowErrorAsync("Delete failed", $"Could not delete: {ex.Message}");
            return false;
        }

        if (!result.Success && result.Error != ValidationError.SnippetNotFound)
        {
            await _dialogs.ShowErrorAsync("Delete failed", $"Could not delete: {result.Message}");
            return false;
        }

        AppLog.Info("snippet_deleted", $"id={id}");
        if (!isCurrent)
        {
            SyncListToEditor();
            return true;
        }

        var items = List.Items;
        if (index < 0 || items.Count == 0)
        {
            SetEditor(null);
            SetListSelection(null);
            return true;
        }

        var next = items[Math.Min(index, items.Count - 1)];
        SetEditor(NewEditorFor(next.Id));
        SetListSelection(next.Id);
        return true;
    }

    /// <summary>Asks once when the editor is dirty. Discard drops the edits so a second ask does not repeat the prompt.</summary>
    public Task<bool> CanLeaveAsync() => GuardAsync();

    [RelayCommand]
    private Task New() => NewAsync();

    [RelayCommand(CanExecute = nameof(HasEditor))]
    private Task DeleteSelected() => DeleteSelectedAsync();

    private async Task<bool> GuardAsync()
    {
        if (Editor is not { IsDirty: true, IsDeleted: false } editor)
        {
            return true;
        }

        var choice = await _dialogs.AskSaveDiscardCancelAsync(UiText.DiscardTitle, UiText.DiscardBody);
        switch (choice)
        {
            case SaveChoice.Save:
                return await editor.SaveAsync();
            case SaveChoice.Discard:
                editor.ResetToSaved();
                return true;
            default:
                return false;
        }
    }

    private SnippetEditorViewModel NewEditorFor(long? id) => new(_store, _dialogs, id, _clipboard, _time);

    private void SetEditor(SnippetEditorViewModel? editor)
    {
        var old = Editor;
        if (old is not null)
        {
            old.Saved -= OnEditorSaved;
            old.Removed -= OnEditorRemoved;
            old.DeleteRequested -= OnEditorDeleteRequested;
        }

        if (editor is not null)
        {
            editor.Saved += OnEditorSaved;
            editor.Removed += OnEditorRemoved;
            editor.DeleteRequested += OnEditorDeleteRequested;
        }

        Editor = editor;
        old?.Dispose();
    }

    private void OnEditorDeleteRequested(object? sender, EventArgs e)
    {
        if (ReferenceEquals(sender, Editor) && Editor?.SnippetId is { } id)
        {
            PendingDelete = RunDeleteAsync(id);
        }
    }

    private async Task RunDeleteAsync(long id)
    {
        try
        {
            await DeleteAsync(id);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            AppLog.Error("delete_failed", ex);
        }
    }

    private void OnEditorSaved(object? sender, long id)
    {
        if (ReferenceEquals(sender, Editor))
        {
            SyncListToEditor();
        }
    }

    private void OnEditorRemoved(object? sender, EventArgs e)
    {
        if (ReferenceEquals(sender, Editor))
        {
            SetEditor(null);
            SetListSelection(null);
        }
    }

    private void OnListPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (_suppressListSelection > 0 || e.PropertyName != nameof(SnippetListViewModel.SelectedId))
        {
            return;
        }

        if (List.SelectedId is { } id)
        {
            PendingSelection = HandleListSelectionAsync(id);
        }
    }

    private async Task HandleListSelectionAsync(long id)
    {
        try
        {
            await RequestSelectAsync(id);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            AppLog.Error("select_failed", ex);
        }
    }

    private void OnListRebuilt(object? sender, EventArgs e)
    {
        if (_suppressListSelection == 0)
        {
            SyncListToEditor();
        }
    }

    private void SyncListToEditor() => SetListSelection(Editor?.SnippetId);

    private void RestoreListSelection()
    {
        SyncListToEditor();
        SelectionRestored?.Invoke(this, List.SelectedId);
    }

    private void SetListSelection(long? id)
    {
        var target = id is { } wanted && List.Items.Any(r => r.Id == wanted) ? id : null;
        _suppressListSelection++;
        try
        {
            List.SelectedId = target;
        }
        finally
        {
            _suppressListSelection--;
        }
    }

    private int IndexInList(long id)
    {
        for (var i = 0; i < List.Items.Count; i++)
        {
            if (List.Items[i].Id == id)
            {
                return i;
            }
        }

        return -1;
    }
}
