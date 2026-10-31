using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SmartSnippets.Core.Data;
using SmartSnippets.Core.Domain;
using SmartSnippets.Core.Settings;
using SmartSnippets.Core.Store;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Presentation.ViewModels;

namespace SmartSnippets.UnoProbe;

/// <summary>Actual host and CI share real store/VM/coordinator/insertion composition.
/// UI-thread owned. Shutdown closes admission synchronously, then asynchronously
/// drains insertion and VM continuations before deleting the owned profile.</summary>
public sealed class ProbeFlowComposition : IPopupHost, IDisposable, IAsyncDisposable
{
    private readonly ProbeFlowProfile _profile;
    private readonly ProbeFlowBoundary _os;
    private readonly Action<Action> _schedule;
    private readonly HashSet<Task> _operations = [];
    private PopupShowCoordinator _shows;
    private bool _visible, _stopping;
    private int _generationBase;
    private Task? _shutdown;
    internal string ProfileRoot => _profile.Root;
    public SnippetStore Store { get; }
    public PopupViewModel Popup { get; }
    public SnippetsPageViewModel Main { get; }
    public InsertionShutdownCoordinator Insertion { get; }
    public long SnippetId { get; }
    public int WarmupGeneration => _generationBase + _shows.WarmupGeneration;
    public bool IsWarming => !_stopping && _shows.IsWarming;

    public static ProbeFlowComposition Create(ProbeFlowBoundary os, Action<Action> schedule) =>
        CreateAtRoot(os, schedule, Path.Combine(Path.GetTempPath(), "SmartSnippets.Uno018-flow-" + Guid.NewGuid().ToString("N")));
    internal static ProbeFlowComposition CreateAtRoot(ProbeFlowBoundary os, Action<Action> schedule, string root)
    {
        var profile = new ProbeFlowProfile(root);
        try { return new(os, schedule, profile); }
        catch { profile.Dispose(); throw; }
    }
    private ProbeFlowComposition(ProbeFlowBoundary os, Action<Action> schedule, ProbeFlowProfile profile)
    {
        ArgumentNullException.ThrowIfNull(os);
        ArgumentNullException.ThrowIfNull(schedule);
        _os = os; _profile = profile;
        _schedule = action => schedule(() => { if (!_stopping) action(); });
        var factory = profile.Factory;
        new Migrator(factory).MigrateToLatest();
        Store = new SnippetStore(factory, TimeProvider.System); Store.Load();
        SnippetId = (Store.CreateSnippet(new SnippetDraft("Synthetic flow λ 日本語 😀\nsecond line", "Synthetic flow", true, null, "synthetic")).Value
            ?? throw new InvalidOperationException("Flow fixture failed.")).Id;
        Insertion = new InsertionShutdownCoordinator(new InsertionService(Store, os, os));
        Popup = new PopupViewModel(Store, new SettingsRepository(factory), new AppSettings(),
            Insertion, os, os, this, os, os, schedule: _schedule);
        Main = new SnippetsPageViewModel(Store, os, os,
            insertRow: Popup.InsertFromMainAsync, canInsertRow: () => !_stopping && Popup.CanInsertFromMain);
        _shows = NewCoordinator();
    }
    private PopupShowCoordinator NewCoordinator() => new(_schedule, _os.Focus, _os.StartWarmup, _os.StopWarmup);
    public void OnShortcut() => CaptureAndShow();
    public void OnMenu() => CaptureAndShow();
    private void CaptureAndShow()
    {
        if (_stopping) return;
        var target = _os.CaptureTarget(); // Must precede even native warmup.
        if (target is null || Popup.Open(target) == PopupOpenMode.Ignored) return;
        RequestShow();
    }
    public void OnPreview()
    {
        if (!_stopping && Popup.OpenPreview() != PopupOpenMode.Ignored) RequestShow();
    }
    public void OnLayoutReady(int generation)
    {
        if (!_stopping && generation == WarmupGeneration) _shows.CompleteWarmup(generation - _generationBase);
    }
    public void OnWarmupTimeout(int generation)
    {
        if (!_stopping && generation == WarmupGeneration) _shows.WarmupTimedOut(generation - _generationBase);
    }
    public void OnPopupWindowClosed()
    {
        if (_stopping) return;
        _visible = false;
        var nextBase = WarmupGeneration + 1;
        _shows.Close(); // Driver has already invalidated the native surface.
        _generationBase = nextBase;
        _shows = NewCoordinator();
    }
    public Task OnEnterAsync() => !_stopping && _visible ? Track(Popup.InsertSelectedAsync()) : Task.CompletedTask;
    public Task OnRowAsync(PopupItemViewModel row) => !_stopping && _visible ? Track(Popup.InsertRowAsync(row)) : Task.CompletedTask;
    public void OnOpenMain() { if (!_stopping) Popup.OpenMainWindowCommand.Execute(null); }
    public Task OnMainInsertAsync(long id) => !_stopping ? Track(Main.InsertRowAsync(id)) : Task.CompletedTask;
    public void OnMainCopy(long id) { if (!_stopping) Main.CopyRow(id); }
    public void OnPopupCopy(PopupItemViewModel row) { if (!_stopping && _visible) Popup.CopyRow(row); }
    public void OnHide() { if (!_stopping) Popup.Close(); }
    public void OnFooterDismiss()
    {
        if (_stopping) return;
        if (Popup.IsConfirmingClipboardLoss) Popup.CancelPasteAnywayCommand.Execute(null);
        else if (Popup.FooterState == PopupFooterState.MaybePasted) Popup.DismissFooterCommand.Execute(null);
    }
    private async Task Track(Task operation)
    {
        _operations.Add(operation);
        try { await operation; } finally { _operations.Remove(operation); }
    }
    private void RequestShow()
    {
        if (_stopping) return;
        _shows.RequestShow(() =>
        {
            Popup.EnsureCurrent(); _os.ShowPopup(); _visible = true;
            return new(true, Popup.IsPreview ? null : PopupFocusTarget.Search);
        });
    }
    public void Hide() { if (_stopping) return; HideCore(); }
    private void HideCore() { _visible = false; _shows.Hide(); _os.HidePopup(); }
    public void ShowAgain() => RequestShow();
    public void OpenMainWindow() { if (!_stopping) { HideCore(); _os.ShowMain(); } }
    public void EditSnippet(long snippetId) { if (!_stopping) { OpenMainWindow(); _ = Track(Main.RequestEditAsync(snippetId)); } }
    public void Focus(PopupFocusTarget target) { if (!_stopping) _shows.Focus(target); }
    public Task ShutdownAsync()
    {
        if (_shutdown is not null) return _shutdown;
        _stopping = true;
        Insertion.TryBeginShutdown();
        _shutdown = ShutdownCoreAsync();
        return _shutdown;
    }
    private async Task ShutdownCoreAsync()
    {
        try { HideCore(); _shows.Close(); }
        finally
        {
            await Insertion.WaitForIdleAsync();
            try { await Task.WhenAll(_operations.ToArray()); }
            finally
            {
                try { Main.Dispose(); }
                finally { try { Popup.Dispose(); } finally { _profile.Dispose(); } }
            }
        }
    }
    // Never synchronously block the UI dispatcher while an insertion is draining.
    public void Dispose() => _ = ShutdownAsync();
    public ValueTask DisposeAsync() => new(ShutdownAsync());
}
