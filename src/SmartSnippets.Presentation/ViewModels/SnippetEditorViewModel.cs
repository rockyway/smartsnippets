using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartSnippets.Core.Domain;
using SmartSnippets.Core.Logging;
using SmartSnippets.Core.Store;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Presentation.Text;

namespace SmartSnippets.Presentation.ViewModels;

/// <summary>
/// Edit session for one snippet or a new one. Host-agnostic: the main window and the popup both host it.
/// The store, not this class, finalizes the label; the view-model only previews it with the same function.
/// </summary>
public sealed partial class SnippetEditorViewModel : ObservableObject, IDisposable
{
    public const string NeverDisplay = "Never";

    private readonly ISnippetStore _store;
    private readonly IDialogService _dialogs;
    private readonly IClipboardService? _clipboard;
    private readonly TimeProvider _time;

    private long? _snippetId;
    private Snippet? _current;
    private Baseline _baseline = new(string.Empty, string.Empty, false, null, []);
    private bool _loading;
    private bool _updatingChoices;
    private bool _settingLabel;
    private string? _labelBeforeAuto;
    private bool _disposed;

    [ObservableProperty]
    private string _text = string.Empty;

    [ObservableProperty]
    private string _label = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLabelAuto))]
    [NotifyPropertyChangedFor(nameof(LabelPlaceholder))]
    [NotifyPropertyChangedFor(nameof(AutoLabelHint))]
    private bool _isLabelCustom;

    private FolderChoice _selectedFolder = FolderChoice.Unfiled;

    /// <summary>Null is transient control feedback, not a request to choose Unfiled.</summary>
    public FolderChoice SelectedFolder
    {
        get => _selectedFolder;
        set
        {
            if (value is null || ReferenceEquals(_selectedFolder, value)) return;
            _selectedFolder = value;
            OnPropertyChanged();
            OnSelectedFolderChanged(value);
        }
    }

    /// <summary>Only explicit, current choices are edits; collection reset/null feedback is not.</summary>
    public void SelectFolder(FolderChoice? choice)
    {
        if (_loading || _updatingChoices || choice is null) return;
        var current = FolderChoices.FirstOrDefault(c => c.Id == choice.Id);
        if (current is not null) SelectedFolder = current;
    }

    [ObservableProperty]
    private string _tagsInput = string.Empty;

    [ObservableProperty]
    private List<string> _tagChips = [];

    [ObservableProperty]
    private string _textError = string.Empty;

    [ObservableProperty]
    private string _labelError = string.Empty;

    [ObservableProperty]
    private string _folderError = string.Empty;

    [ObservableProperty]
    private string _newFolderError = string.Empty;

    [ObservableProperty]
    private string _generalError = string.Empty;

    [ObservableProperty]
    private string _newFolderName = string.Empty;

    /// <summary>True while the inline new-folder box (name, Create, Cancel) is open.</summary>
    [ObservableProperty]
    private bool _isCreatingFolder;

    [ObservableProperty]
    private bool _isDirty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(RetrySaveCommand))]
    private bool _isSaving;

    /// <summary>True once the snippet was deleted elsewhere; the host should close the editor.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(RetrySaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    private bool _isDeleted;

    /// <summary>True after the store failed to save; shown inline with Try again and Copy text. Text and dirty state are kept.</summary>
    [ObservableProperty]
    private bool _hasSaveFailure;

    [ObservableProperty]
    private string _saveFailureMessage = string.Empty;

    [ObservableProperty]
    private long? _savedSnippetId;

    [ObservableProperty]
    private bool _hasMetadata;

    [ObservableProperty]
    private string _createdDisplay = string.Empty;

    [ObservableProperty]
    private string _updatedDisplay = string.Empty;

    [ObservableProperty]
    private string _lastUsedDisplay = string.Empty;

    [ObservableProperty]
    private string _usageCountDisplay = string.Empty;

    public SnippetEditorViewModel(
        ISnippetStore store,
        IDialogService dialogs,
        long? snippetId,
        IClipboardService? clipboard = null,
        TimeProvider? time = null)
    {
        _store = store;
        _dialogs = dialogs;
        _clipboard = clipboard;
        _time = time ?? TimeProvider.System;
        _snippetId = snippetId;

        RefreshChoices(null);
        var existing = snippetId is { } id ? FindInStore(id) : null;
        if (snippetId is not null && existing is null)
        {
            IsDeleted = true;
            GeneralError = ValidationError.SnippetNotFound.ToMessage();
        }

        Load(existing);
        _store.Changed += OnStoreChanged;
    }

    /// <summary>Folders to pick from: Unfiled first, then by name.</summary>
    public ObservableCollection<FolderChoice> FolderChoices { get; } = [];

    /// <summary>The snippet being edited; null until a new snippet is first saved.</summary>
    public long? SnippetId => _snippetId;

    public bool IsNew => _snippetId is null;

    public bool IsLabelAuto => !IsLabelCustom;

    /// <summary>"Auto: &lt;suggestion&gt;" while the label is automatic, otherwise "Label".</summary>
    public string LabelPlaceholder => IsLabelCustom ? "Label" : $"Auto: {Suggestion}";

    /// <summary>"Edit the label to stop it following your tags." while the label is automatic, otherwise empty.</summary>
    public string AutoLabelHint => IsLabelCustom ? string.Empty : UiText.AutoLabelHint;

    /// <summary>The label the store would assign automatically for the current text and tags.</summary>
    public string Suggestion => LabelSuggester.Suggest(Text, TagList.Parse(TagsInput));

    /// <summary>Raised after a successful save with the saved snippet's id (the popup host closes on it).</summary>
    public event EventHandler<long>? Saved;

    /// <summary>Raised once when the snippet being edited is deleted elsewhere.</summary>
    public event EventHandler? Removed;

    /// <summary>Raised by <see cref="DeleteCommand"/>; the page runs its single confirm-and-delete flow.</summary>
    public event EventHandler? DeleteRequested;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _store.Changed -= OnStoreChanged;
    }

    /// <summary>Saves the draft. Returns true when stored; false on validation failure, store failure, or when already saving.</summary>
    public Task<bool> SaveAsync() => Task.FromResult(SaveCore());

    private bool SaveCore()
    {
        if (IsSaving || IsDeleted)
        {
            return false;
        }

        IsSaving = true;
        try
        {
            ClearErrors();
            var draft = new SnippetDraft(Text, Label, IsLabelCustom, SelectedFolder.Id, TagsInput);
            StoreResult<Snippet> result;
            try
            {
                result = _snippetId is { } id ? _store.UpdateSnippet(id, draft) : _store.CreateSnippet(draft);
            }
            catch (StoreException ex)
            {
                AppLog.Info("save_failed", $"kind={ex.Failure}");
                SaveFailureMessage = UiText.SaveFailed;
                HasSaveFailure = true;
                return false;
            }

            ClearSaveFailure();
            if (!result.Success)
            {
                ApplyError(result);
                return false;
            }

            var saved = result.Value!;
            _snippetId = saved.Id;
            OnPropertyChanged(nameof(SnippetId));
            OnPropertyChanged(nameof(IsNew));
            DeleteCommand.NotifyCanExecuteChanged();
            ApplySaved(FindInStore(saved.Id) ?? saved);
            AppLog.Info("snippet_saved", $"id={saved.Id}");
            SavedSnippetId = saved.Id;
            Saved?.Invoke(this, saved.Id);
            return true;
        }
        finally
        {
            IsSaving = false;
        }
    }

    /// <summary>Drops unsaved edits without asking (the Discard button, or the caller has already asked).</summary>
    public void ResetToSaved()
    {
        if (IsDeleted)
        {
            return;
        }

        ClearSaveFailure();
        var snippet = _snippetId is { } id ? FindInStore(id) ?? _current : null;
        RefreshChoices(null);
        Load(snippet);
    }

    /// <summary>Creates <see cref="NewFolderName"/> through the store and selects it. Errors show in <see cref="NewFolderError"/>.</summary>
    public async Task<bool> CreateFolderAsync()
    {
        NewFolderError = string.Empty;
        StoreResult<Folder> result;
        try
        {
            result = _store.CreateFolder(NewFolderName);
        }
        catch (StoreException ex)
        {
            AppLog.Info("save_failed", $"kind={ex.Failure}");
            await _dialogs.ShowErrorAsync("Folder not created", $"Could not create the folder: {ex.Message}");
            return false;
        }

        if (!result.Success)
        {
            NewFolderError = FolderMessages.For(result.Error!.Value, NewFolderName);
            return false;
        }

        var folder = result.Value!;
        AppLog.Info("folder_created", $"id={folder.Id}");
        RefreshChoices(folder.Id);
        NewFolderName = string.Empty;
        IsCreatingFolder = false;
        return true;
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private Task Save() => SaveAsync();

    [RelayCommand(CanExecute = nameof(CanSave))]
    private Task RetrySave() => SaveAsync();

    /// <summary>The "Unsaved changes" bar's Discard button: back to the last saved values, no dialog.</summary>
    [RelayCommand]
    private void Discard() => ResetToSaved();

    /// <summary>Esc path: asks before throwing away unsaved edits; the Discard button skips the question.</summary>
    public async Task<bool> ConfirmDiscardAsync()
    {
        if (!IsDirty)
        {
            return false;
        }

        if (!await _dialogs.ConfirmAsync(UiText.DiscardTitle, UiText.DiscardBody, UiText.Discard))
        {
            return false;
        }

        ResetToSaved();
        return true;
    }

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private void Delete() => DeleteRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand(CanExecute = nameof(CanCopyText))]
    private void CopyText() => _clipboard?.SetText(Text);

    [RelayCommand]
    private Task CreateFolder() => CreateFolderAsync();

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

    private bool CanSave() => !IsSaving && !IsDeleted;

    private bool CanDelete() => !IsNew && !IsDeleted;

    private bool CanCopyText() => _clipboard is not null;

    private void ClearSaveFailure()
    {
        HasSaveFailure = false;
        SaveFailureMessage = string.Empty;
    }

    partial void OnTextChanged(string value)
    {
        if (_loading)
        {
            return;
        }

        TextError = string.Empty;
        OnInputChanged();
    }

    partial void OnTagsInputChanged(string value)
    {
        TagChips = TagList.Parse(value).ToList();
        if (_loading)
        {
            return;
        }

        OnInputChanged();
    }

    partial void OnLabelChanged(string value)
    {
        if (_loading || _settingLabel)
        {
            return;
        }

        LabelError = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            IsLabelCustom = false;
            SetLabel(Suggestion);
        }
        else
        {
            // Typing replaces any custom label remembered from switching to automatic.
            _labelBeforeAuto = null;
            IsLabelCustom = true;
        }

        RecomputeDirty();
    }

    partial void OnIsLabelCustomChanged(bool value)
    {
        if (_loading)
        {
            return;
        }

        if (!value)
        {
            // Remember the custom label so switching back restores it rather than freezing the suggestion.
            _labelBeforeAuto = Label;
            LabelError = string.Empty;
            SetLabel(Suggestion);
        }
        else if (_labelBeforeAuto is { } previous)
        {
            _labelBeforeAuto = null;
            SetLabel(previous);
        }

        RecomputeDirty();
    }

    private void OnSelectedFolderChanged(FolderChoice value)
    {
        if (_loading || _updatingChoices)
        {
            return;
        }

        FolderError = string.Empty;
        RecomputeDirty();
    }

    partial void OnNewFolderNameChanged(string value) => NewFolderError = string.Empty;

    private void OnInputChanged()
    {
        OnPropertyChanged(nameof(LabelPlaceholder));
        if (!IsLabelCustom)
        {
            SetLabel(Suggestion);
        }

        RecomputeDirty();
    }

    private void SetLabel(string value)
    {
        _settingLabel = true;
        try
        {
            Label = value;
        }
        finally
        {
            _settingLabel = false;
        }
    }

    private void RecomputeDirty()
    {
        var tags = TagList.Parse(TagsInput);
        IsDirty = !string.Equals(Text, _baseline.Text, StringComparison.Ordinal)
            || IsLabelCustom != _baseline.IsLabelCustom
            || (IsLabelCustom && !string.Equals(Label, _baseline.Label, StringComparison.Ordinal))
            || SelectedFolder.Id != _baseline.FolderId
            || !tags.SequenceEqual(_baseline.Tags, StringComparer.Ordinal);
    }

    private void Load(Snippet? snippet)
    {
        _current = snippet;
        _labelBeforeAuto = null;
        _loading = true;
        try
        {
            Text = snippet?.Text ?? string.Empty;
            TagsInput = snippet is null ? string.Empty : TagList.Format(snippet.Tags);
            IsLabelCustom = snippet?.IsLabelCustom ?? false;
            SetLabel(IsLabelCustom ? snippet!.Label : Suggestion);
            SelectedFolder = ChoiceFor(snippet?.FolderId);
            ClearErrors();
        }
        finally
        {
            _loading = false;
        }

        OnPropertyChanged(nameof(LabelPlaceholder));
        _baseline = CaptureBaseline();
        RecomputeDirty();
        RefreshMetadata();
    }

    private void ApplySaved(Snippet saved)
    {
        _current = saved;
        _loading = true;
        try
        {
            TagsInput = TagList.Format(saved.Tags);
            IsLabelCustom = saved.IsLabelCustom;
            SetLabel(saved.Label);
            SelectedFolder = ChoiceFor(saved.FolderId);
        }
        finally
        {
            _loading = false;
        }

        OnPropertyChanged(nameof(LabelPlaceholder));
        _baseline = CaptureBaseline();
        RecomputeDirty();
        RefreshMetadata();
    }

    private Baseline CaptureBaseline() =>
        new(Text, Label, IsLabelCustom, SelectedFolder.Id, TagList.Parse(TagsInput));

    private void RefreshMetadata()
    {
        var snippet = _current;
        HasMetadata = snippet is not null;
        CreatedDisplay = snippet is null ? string.Empty : FormatDate(snippet.CreatedAt);
        UpdatedDisplay = snippet is null ? string.Empty : FormatDate(snippet.UpdatedAt);
        LastUsedDisplay = snippet?.LastUsedAt is { } used ? FormatLastUsed(used) : NeverDisplay;
        UsageCountDisplay = UiText.UsedTimes(snippet?.UsageCount ?? 0);
    }

    private DateTimeOffset ToLocal(DateTimeOffset value) => TimeZoneInfo.ConvertTime(value, _time.LocalTimeZone);

    private string FormatDate(DateTimeOffset value) =>
        ToLocal(value).ToString("d MMM yyyy", CultureInfo.InvariantCulture);

    private string FormatLastUsed(DateTimeOffset value)
    {
        var local = ToLocal(value);
        return local.Date == _time.GetLocalNow().Date
            ? "Today, " + local.ToString("HH:mm", CultureInfo.InvariantCulture)
            : FormatDate(value);
    }

    private void ClearErrors()
    {
        TextError = string.Empty;
        LabelError = string.Empty;
        FolderError = string.Empty;
        GeneralError = string.Empty;
    }

    private void ApplyError(StoreResult result)
    {
        switch (result.Error)
        {
            case ValidationError.TextRequired:
                TextError = result.Message;
                break;
            case ValidationError.LabelRequired:
                LabelError = result.Message;
                break;
            case ValidationError.FolderNotFound:
                FolderError = result.Message;
                break;
            case ValidationError.SnippetNotFound:
                MarkDeleted();
                break;
            default:
                GeneralError = result.Message;
                break;
        }
    }

    private void MarkDeleted()
    {
        if (IsDeleted)
        {
            return;
        }

        IsDeleted = true;
        GeneralError = ValidationError.SnippetNotFound.ToMessage();
        Removed?.Invoke(this, EventArgs.Empty);
    }

    private Snippet? FindInStore(long id)
    {
        foreach (var view in _store.Snippets)
        {
            if (view.Snippet.Id == id)
            {
                return view.Snippet;
            }
        }

        return null;
    }

    private FolderChoice ChoiceFor(long? folderId) =>
        FolderChoices.FirstOrDefault(c => c.Id == folderId) ?? FolderChoice.Unfiled;

    /// <summary>Rebuilds the choice list from the store and selects <paramref name="select"/> (or keeps the current pick).</summary>
    private void RefreshChoices(long? select)
    {
        var keep = select ?? SelectedFolder.Id;
        var choices = new List<FolderChoice>(_store.Folders.Count + 1) { FolderChoice.Unfiled };
        choices.AddRange(_store.Folders.Select(f => new FolderChoice(f.Id, f.Name)));

        _updatingChoices = true;
        try
        {
            if (!choices.SequenceEqual(FolderChoices))
            {
                FolderChoices.Clear();
                foreach (var choice in choices)
                {
                    FolderChoices.Add(choice);
                }
            }

            SelectedFolder = ChoiceFor(keep);
        }
        finally
        {
            _updatingChoices = false;
        }

        if (_baseline.FolderId is { } baselineFolder && FolderChoices.All(c => c.Id != baselineFolder))
        {
            _baseline = _baseline with { FolderId = null };
        }

        if (select is not null)
        {
            FolderError = string.Empty;
        }

        RecomputeDirty();
    }

    private void OnStoreChanged(object? sender, StoreChangedEventArgs e)
    {
        if (_disposed)
        {
            return;
        }

        RefreshChoices(null);
        if (_snippetId is not { } id || IsSaving)
        {
            return;
        }

        var snippet = FindInStore(id);
        if (snippet is null)
        {
            MarkDeleted();
            return;
        }

        var affected = e.Kind == StoreChangeKind.Reloaded || e.Id == id || e.SnippetIds.Contains(id);
        if (affected && !IsDirty)
        {
            Load(snippet);
        }
        else
        {
            _current = snippet;
            RefreshMetadata();
        }
    }

    private readonly record struct Baseline(
        string Text,
        string Label,
        bool IsLabelCustom,
        long? FolderId,
        IReadOnlyList<string> Tags);
}
