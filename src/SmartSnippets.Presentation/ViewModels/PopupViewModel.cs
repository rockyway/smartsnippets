using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartSnippets.Core.Logging;
using SmartSnippets.Core.Query;
using SmartSnippets.Core.Settings;
using SmartSnippets.Core.Store;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Presentation.Text;

namespace SmartSnippets.Presentation.ViewModels;

public sealed partial class PopupViewModel : ObservableObject, IDisposable
{
    private readonly ISnippetStore _store;
    private readonly ISettingsStore _settings;
    private readonly IInsertionService _insertion;
    private readonly IClipboardService _clipboard;
    private readonly IElevation _elevation;
    private readonly IPopupHost _host;
    private readonly IDialogService _dialogs;
    private readonly INotifier _notices;
    private readonly Action? _requestExit;
    private readonly ITextSizeService? _textSize;
    private SnippetSearch _search = new([]);
    private Dictionary<long, PopupItemViewModel> _cache = [];
    private long? _pendingId;
    private long? _failedId;
    private InsertionOutcome _failedOutcome;
    private string _editorQuery = string.Empty;
    private long? _editorSelection;
    private bool _disposed;
    private string _query = string.Empty;
    private List<PopupItemViewModel> _items = [];
    private int _selectedIndex = -1;
    private PopupLayout _layout;
    private SortField _sort;
    private string _statusText = string.Empty;
    private PopupFooterState _footerState;
    private string _footerMessage = string.Empty;
    private bool _isInserting;
    private SnippetEditorViewModel? _editor;

    public PopupViewModel(ISnippetStore store, ISettingsStore settingsStore, AppSettings initial,
        IInsertionService insertion, IClipboardService clipboard, IElevation elevation,
        IPopupHost host, IDialogService editorDialogs, INotifier notices, Action? requestExit = null, ITextSizeService? textSize = null)
    {
        _store = store; _settings = settingsStore; _insertion = insertion; _clipboard = clipboard;
        _elevation = elevation; _host = host; _dialogs = editorDialogs; _notices = notices; _requestExit = requestExit; _textSize = textSize;
        _layout = initial.PopupLayout; _sort = initial.PopupSort;
        RebuildSearch(); Refresh(null);
        _store.Changed += OnStoreChanged;
    }

    public InsertionTarget? Target { get; private set; }
    public bool IsPreview { get; private set; }
    public PopupOpenMode OpenPreview()
    {
        // Never replace an in-flight insertion or an editor's preserved target.
        if (IsInserting || IsEditorOpen) return PopupOpenMode.Ignored;
        Target = null;
        IsPreview = true;
        Query = string.Empty;
        ClearFooter(); StatusText = string.Empty;
        SelectFirst();
        AppLog.Info("popup_opened", "mode=Preview");
        return PopupOpenMode.Fresh;
    }
    public PopupOpenMode Open(InsertionTarget target)
    {
        if (IsInserting) return PopupOpenMode.Ignored;
        if (IsEditorOpen)
        {
            AppLog.Info("popup_opened", $"mode=Resumed pid={Target?.ProcessId}");
            return PopupOpenMode.Resumed;
        }
        Target = target;
        IsPreview = false;
        Query = string.Empty;
        ClearFooter(); StatusText = string.Empty;
        SelectFirst();
        AppLog.Info("popup_opened", $"mode=Fresh pid={target.ProcessId}");
        return PopupOpenMode.Fresh;
    }

    public string Query
    {
        get => _query;
        set
        {
            if (!SetProperty(ref _query, value ?? string.Empty)) return;
            ClearTransientFooter(); StatusText = string.Empty; Refresh(null);
        }
    }
    public List<PopupItemViewModel> Items { get => _items; private set => SetProperty(ref _items, value); }
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (value < 0 || value >= Items.Count || Items[value].Kind != PopupItemKind.Snippet) value = -1;
            SetProperty(ref _selectedIndex, value);
        }
    }
    private long? SelectedId => SelectedIndex >= 0 && SelectedIndex < Items.Count ? Items[SelectedIndex].SnippetId : null;
    public void MoveSelection(int delta)
    {
        ClearTransientFooter(); StatusText = string.Empty;
        if (delta == 0 || Items.Count == 0) return;
        var direction = Math.Sign(delta);
        var i = SelectedIndex < 0 ? (direction > 0 ? -1 : Items.Count) : SelectedIndex;
        for (i += direction; i >= 0 && i < Items.Count; i += direction)
            if (Items[i].Kind == PopupItemKind.Snippet) { SelectedIndex = i; break; }
    }
    public PopupLayout Layout => _layout;
    public bool IsFolderLayout => Layout == PopupLayout.Folder;
    public string LayoutTooltip => UiText.LayoutTooltip(Layout);
    public SortField Sort => _sort;
    public string SortTooltip => UiText.SortTooltip(Sort);
    [RelayCommand]
    private void ToggleLayout()
    {
        _layout = IsFolderLayout ? PopupLayout.Flat : PopupLayout.Folder;
        _settings.Set(SettingsKeys.PopupLayout, _layout.ToString());
        OnPropertyChanged(nameof(Layout)); OnPropertyChanged(nameof(IsFolderLayout)); OnPropertyChanged(nameof(LayoutTooltip));
        BuildCache(); Refresh(null); AppLog.Info("popup_layout", $"value={_layout}");
    }
    public void SetSort(SortField sort)
    {
        if (_sort == sort) return;
        _sort = sort; _settings.Set(SettingsKeys.PopupSort, sort.ToString());
        OnPropertyChanged(nameof(Sort)); OnPropertyChanged(nameof(SortTooltip));
        RebuildSearch(); Refresh(null); AppLog.Info("popup_sort", $"value={sort}");
    }
    public string HintText => FooterState switch { PopupFooterState.ConfirmClipboardLoss => UiText.ConfirmHint, PopupFooterState.MaybePasted => UiText.DismissHint, _ => string.IsNullOrEmpty(StatusText) ? UiText.PopupHint : StatusText };
    public string CountText => UiText.CountOf(Items.Count(i => i.Kind == PopupItemKind.Snippet), _store.Snippets.Count);
    public string StatusText { get => _statusText; private set { if (SetProperty(ref _statusText, value)) OnPropertyChanged(nameof(HintText)); } }
    public bool IsEmpty => Items.All(i => i.Kind != PopupItemKind.Snippet);
    public string EmptyTitle => UiText.NoSnippetsMatch;
    public string EmptyBody => UiText.NoSnippetsMatchBody;
    public PopupFooterState FooterState { get => _footerState; private set { if (SetProperty(ref _footerState, value)) NotifyFooter(); } }
    public string FooterMessage { get => _footerMessage; private set => SetProperty(ref _footerMessage, value); }
    public string FooterItemStatus => FooterState.ToString();
    public InsertionOutcome LastInsertionOutcome => _failedOutcome;
    public bool HasFooterMessage => FooterState != PopupFooterState.None;
    public bool IsFooterError => FooterState == PopupFooterState.Error;
    public bool IsFooterCaution => FooterState is PopupFooterState.ConfirmClipboardLoss or PopupFooterState.MaybePasted;
    public bool IsConfirmingClipboardLoss => FooterState == PopupFooterState.ConfirmClipboardLoss;
    public bool CanDismissFooter => FooterState == PopupFooterState.MaybePasted;
    public bool CanCopyFailedText => FooterState is PopupFooterState.Error or PopupFooterState.MaybePasted && _failedOutcome != InsertionOutcome.SnippetMissing && _store.Snippets.Any(s => s.Snippet.Id == _failedId);
    public bool CanRestartAsAdmin => FooterState == PopupFooterState.Error && _failedOutcome == InsertionOutcome.TargetElevated && !_elevation.IsElevated;
    public bool IsInserting { get => _isInserting; private set => SetProperty(ref _isInserting, value); }
    public SnippetEditorViewModel? Editor { get => _editor; private set { if (SetProperty(ref _editor, value)) { OnPropertyChanged(nameof(IsEditorOpen)); OnPropertyChanged(nameof(IsEditorClosed)); } } }
    public bool IsEditorOpen => Editor is not null;
    public bool IsEditorClosed => !IsEditorOpen;
    public string EditorTitle => UiText.AddSnippet;
    public string EditorTargetLine => UiText.TargetKept(Target?.DisplayName ?? "the target app");

    private void RebuildSearch()
    {
        _search = new SnippetSearch(SnippetSort.Apply(_store.Snippets, Sort));
        BuildCache();
    }
    private void BuildCache()
    {
        _cache = _store.Snippets.ToDictionary(v => v.Snippet.Id, v => new PopupItemViewModel(PopupItemKind.Snippet,
            v.Snippet.Id, v.Snippet.Label, SnippetRowViewModel.PreviewOf(v.Snippet.Text),
            IsFolderLayout ? string.Empty : v.FolderName ?? UiText.Unfiled,
            v.Snippet.Tags.ElementAtOrDefault(0) ?? string.Empty, v.Snippet.Tags.ElementAtOrDefault(1) ?? string.Empty, string.Empty) { TextSize = _textSize });
    }
    private void Refresh(long? keep)
    {
        var found = _search.Filter(Query);
        var rows = new List<PopupItemViewModel>();
        if (IsFolderLayout)
        {
            foreach (var group in SnippetGrouping.ByFolder(found))
            {
                var heading = UiText.FolderHeading(group.FolderName ?? UiText.Unfiled, group.Items.Count);
                rows.Add(new(PopupItemKind.Heading, 0, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, heading) { TextSize = _textSize });
                rows.AddRange(group.Items.Select(v => _cache[v.Snippet.Id]));
            }
        }
        else rows.AddRange(found.Select(v => _cache[v.Snippet.Id]));
        if (!Items.SequenceEqual(rows)) Items = rows;
        var index = keep is null ? -1 : Items.FindIndex(i => i.Kind == PopupItemKind.Snippet && i.SnippetId == keep);
        SelectedIndex = index >= 0 ? index : Items.FindIndex(i => i.Kind == PopupItemKind.Snippet);
        OnPropertyChanged(nameof(CountText)); OnPropertyChanged(nameof(IsEmpty)); OnPropertyChanged(nameof(CanCopyFailedText));
    }
    private void SelectFirst() => SelectedIndex = Items.FindIndex(i => i.Kind == PopupItemKind.Snippet);
    private void OnStoreChanged(object? sender, StoreChangedEventArgs e) { var id = SelectedId; RebuildSearch(); Refresh(id); }
    public void Dispose() { if (_disposed) return; _disposed = true; _store.Changed -= OnStoreChanged; CloseEditor(); }
    public void Close() { if (IsConfirmingClipboardLoss) { CancelPasteAnyway(); return; } _host.Hide(); AppLog.Info("popup_closed", "reason=esc"); }
    public void OnDeactivated() { if (!IsEditorOpen && !IsInserting) { _host.Hide(); AppLog.Info("popup_closed", "reason=deactivated"); } }
    [RelayCommand] private void OpenMainWindow() => _host.OpenMainWindow();
    [RelayCommand]
    private void AddSnippet()
    {
        if (IsEditorOpen || IsInserting) return;
        _editorQuery = Query; _editorSelection = SelectedId;
        Editor = new SnippetEditorViewModel(_store, _dialogs, null, _clipboard);
        Editor.Saved += OnEditorSaved;
        OnPropertyChanged(nameof(EditorTargetLine)); _host.Focus(PopupFocusTarget.Editor); AppLog.Info("popup_add_opened");
    }
    public async Task SaveEditorAsync() { var editor = Editor; if (editor is not null && !editor.IsSaving) await editor.SaveAsync(); }
    public async Task CancelEditorAsync(bool confirmIfDirty)
    {
        var editor = Editor;
        if (editor is null) return;
        var dirty = editor.IsDirty;
        if (confirmIfDirty && dirty && !await _dialogs.ConfirmAsync(UiText.DiscardTitle, UiText.DiscardBody, UiText.Discard)) return;
        CloseEditor(); Query = _editorQuery; Refresh(_editorSelection); _host.Focus(PopupFocusTarget.Search);
        AppLog.Info("popup_add_cancelled", $"dirty={dirty}");
    }
    private void OnEditorSaved(object? sender, long id)
    {
        CloseEditor();
        if (!Items.Any(i => i.Kind == PopupItemKind.Snippet && i.SnippetId == id)) Query = string.Empty;
        Refresh(id); StatusText = UiText.SnippetAddedReady; _host.Focus(PopupFocusTarget.Search);
        AppLog.Info("popup_add_saved", $"id={id}");
    }
    private void CloseEditor() { if (Editor is not { } editor) return; editor.Saved -= OnEditorSaved; Editor = null; editor.Dispose(); }
    public async Task InsertSelectedAsync()
    {
        if (FooterState == PopupFooterState.MaybePasted) { DismissFooter(); return; }
        if (IsInserting) { AppLog.Info("popup_insert_ignored", "reason=inserting source=keyboard"); return; }
        if (IsConfirmingClipboardLoss || SelectedId is not { } id) return;
        if (IsPreview) { _host.Hide(); AppLog.Info("popup_closed", "reason=preview_selection"); return; }
        if (Target is null) return;
        await InsertAsync(id, default);
    }
    public Task ActivateAsync(PopupItemViewModel item)
    {
        if (IsInserting) { AppLog.Info("popup_insert_ignored", "reason=inserting source=row"); return Task.CompletedTask; }
        if (item.Kind != PopupItemKind.Snippet) return Task.CompletedTask;
        if (FooterState == PopupFooterState.MaybePasted) { DismissFooter(); return Task.CompletedTask; }
        if (IsConfirmingClipboardLoss) return Task.CompletedTask;
        var index = Items.IndexOf(item); if (index < 0) return Task.CompletedTask;
        SelectedIndex = index; return InsertSelectedAsync();
    }
    private async Task InsertAsync(long id, InsertionOptions options)
    {
        if (IsInserting || Target is null) return;
        IsInserting = true; ClearFooter(); _host.Hide(); AppLog.Info("popup_closed", "reason=insert");
        InsertionResult result;
        try { result = await _insertion.InsertAsync(Target, id, options); }
        catch (Exception) { result = new(InsertionOutcome.PasteFailed, UiText.InsertFailedGeneric); }
        IsInserting = false;
        AppLog.Info("popup_insert", $"id={id} outcome={result.Outcome}");
        if (result.Outcome == InsertionOutcome.Inserted)
        {
            if (!string.IsNullOrEmpty(result.Message)) { _notices.Notify(UiText.InsertNoticeTitle, result.Message); AppLog.Info("popup_notice", "outcome=Inserted"); }
            return;
        }
        if (result.Outcome == InsertionOutcome.Busy) return;
        _failedId = id; _failedOutcome = result.Outcome;
        FooterMessage = result.Outcome switch
        {
            InsertionOutcome.ClipboardNotPreserved => UiText.ClipboardLossPrompt,
            InsertionOutcome.MaybePasted when string.IsNullOrEmpty(result.Message) => UiText.MaybePastedFallback,
            InsertionOutcome.CopiedForManualPaste when string.IsNullOrEmpty(result.Message) => UiText.CopiedForManualPasteFallback,
            _ => result.Message,
        };
        FooterState = result.Outcome switch
        {
            InsertionOutcome.ClipboardNotPreserved => PopupFooterState.ConfirmClipboardLoss,
            InsertionOutcome.MaybePasted => PopupFooterState.MaybePasted,
            InsertionOutcome.CopiedForManualPaste => PopupFooterState.ManualPaste,
            _ => PopupFooterState.Error,
        };
        if (IsConfirmingClipboardLoss) _pendingId = id;
        _host.ShowAgain();
        if (IsConfirmingClipboardLoss) _host.Focus(PopupFocusTarget.Footer);
    }
    [RelayCommand] private async Task PasteAnyway() { if (!IsConfirmingClipboardLoss || _pendingId is not { } id) return; AppLog.Info("popup_paste_anyway", $"id={id}"); await InsertAsync(id, new InsertionOptions(AllowClipboardLoss: true)); }
    [RelayCommand] private void CancelPasteAnyway() { if (!IsConfirmingClipboardLoss) return; AppLog.Info("popup_paste_cancelled", $"id={_pendingId}"); ClearFooter(); _host.Focus(PopupFocusTarget.Search); }
    [RelayCommand] private void DismissFooter() { if (FooterState != PopupFooterState.MaybePasted) return; AppLog.Info("popup_caution_dismissed", $"id={_failedId}"); ClearFooter(); _host.Focus(PopupFocusTarget.Search); }
    [RelayCommand] private void CopyFailedText() { if (!CanCopyFailedText) return; var snippet = _store.Snippets.FirstOrDefault(s => s.Snippet.Id == _failedId)?.Snippet; if (snippet is null) return; _clipboard.SetText(snippet.Text); AppLog.Info("popup_copy_text", $"id={snippet.Id}"); }
    [RelayCommand] private void RestartAsAdmin() { if (!CanRestartAsAdmin) return; var result = _elevation.RestartAsAdministrator(); if (result.Outcome == ElevationOutcome.Started) _requestExit?.Invoke(); else FooterMessage = result.Message; }
    private void ClearTransientFooter() { if (IsConfirmingClipboardLoss) CancelPasteAnyway(); else if (FooterState == PopupFooterState.MaybePasted) DismissFooter(); }
    private void ClearFooter() { FooterState = PopupFooterState.None; FooterMessage = string.Empty; _pendingId = null; }
    private void NotifyFooter()
    {
        OnPropertyChanged(nameof(HintText)); OnPropertyChanged(nameof(FooterItemStatus)); OnPropertyChanged(nameof(HasFooterMessage)); OnPropertyChanged(nameof(IsFooterError));
        OnPropertyChanged(nameof(IsFooterCaution)); OnPropertyChanged(nameof(IsConfirmingClipboardLoss));
        OnPropertyChanged(nameof(CanDismissFooter)); OnPropertyChanged(nameof(CanCopyFailedText)); OnPropertyChanged(nameof(CanRestartAsAdmin));
    }
}
