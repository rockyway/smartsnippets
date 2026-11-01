using SmartSnippets.Core.Porting;
using SmartSnippets.Core.Settings;
using SmartSnippets.Presentation.Navigation;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Uno;

namespace SmartSnippets.Uno.Tests.Composition;

internal sealed class HostFixture : IDisposable
{
    internal string Root { get; } = Path.Combine(Path.GetTempPath(), "SmartSnippets.Uno019.Tests", Guid.NewGuid().ToString("N"));
    internal TestBoundary Boundary { get; } = new();
    internal UnoAppHost Host { get; }
    internal HostFixture() => Host = new(Boundary, () => CandidateProfile.OpenOwnedRoot(Root));
    internal Task Start(bool background = false, LaunchOptions? options = null) => Host.StartAsync(background, options ?? new(null, false, null));
    public void Dispose()
    {
        Host.Dispose();
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (Directory.Exists(Root)) Directory.Delete(Root, true);
    }
}
internal sealed class TestBoundary : CandidateHostBoundary
{
    internal Platforms.Windows.CandidateNativeCapability? Capability;
    internal override Platforms.Windows.CandidateNativeCapability? NativeCapability => Capability;
    internal TestDialogs Dialogs { get; } = new();
    internal TestFiles Files { get; } = new();
    internal MainSurface Main { get; private set; } = null!;
    internal List<PopupSurface> Popups { get; } = [];
    internal int Reports, Exits, Messages, MessageDisposals, TrayDisposals;
    internal bool FailWindow;
    internal bool PopupAutoReady = true;
    internal Action? TrayPreview, TrayOpen, TraySettings, TrayExit;
    internal Queue<Action> Posted { get; } = new();
    internal void Drain() { while (Posted.TryDequeue(out var action)) action(); }
    internal override IUiDispatcher Dispatcher => new TestDispatcher(Posted);
    internal override IThemeService CreateTheme(AppSettings settings) => new TestTheme(settings);
    internal override IDisposable CreateMessageWindow() { Messages++; return new Release(() => MessageDisposals++); }
    internal override IDialogService CreateDialogs(bool popup, ITextSizeService textSize) => Dialogs;
    internal override ISnippetFileService CreateFiles() => Files;
    internal override ICandidateMainWindow CreateMain(UnoAppHost host)
    {
        if (FailWindow) throw new IOException("synthetic startup failure");
        return Main = new MainSurface(host);
    }
    internal override ICandidatePopupWindow CreatePopup(UnoAppHost host)
    {
        var surface = new PopupSurface(host) { AutoReady = PopupAutoReady }; Popups.Add(surface); return surface;
    }
    internal override ICandidateTray CreateTray(Action open, Action preview, Action settings, Action exit)
    {
        TrayOpen = open; TrayPreview = preview; TraySettings = settings; TrayExit = exit;
        return new TestTray(() => TrayDisposals++);
    }
    internal override void ReportStartupFailure() => Reports++;
    internal override void Exit() => Exits++;
    private sealed class TestDispatcher(Queue<Action> queue) : IUiDispatcher { public void Post(Action action) => queue.Enqueue(action); }
    private sealed class TestTray(Action dispose) : ICandidateTray
    { public void Dispose() => dispose(); public void Notify(string title, string message) { } }
    private sealed class Release(Action dispose) : IDisposable { public void Dispose() => dispose(); }
}
internal sealed class MainSurface(UnoAppHost host) : ICandidateMainWindow
{
    internal PageKind Current = PageKind.Snippets;
    internal int Navigations, Disposals;
    internal bool Visible;
    internal bool InteractionEnabled = true;
    internal Func<Task<bool>>? ExitGuard;
    public void SetInteractionEnabled(bool enabled) => InteractionEnabled = enabled;
    public event EventHandler? HiddenByClose;
    public async Task NavigateAsync(PageKind page, bool show)
    {
        await CandidateUiPolicy.NavigateAsync(() => Current != page ? CanExitAsync() : Task.FromResult(true),
            () => { Current = page; Navigations++; }, show, () => Visible = true);
    }
    public Task<bool> CanExitAsync() => ExitGuard?.Invoke() ?? Current switch
    { PageKind.Snippets => host.Snippets.CanLeaveAsync(), PageKind.Folders => host.Folders.CanLeaveAsync(), _ => Task.FromResult(true) };
    internal async Task CloseAsync()
    { if (await CanExitAsync()) { Visible = false; HiddenByClose?.Invoke(this, EventArgs.Empty); } }
    public void Dispose() => Disposals++;
}
internal sealed class PopupSurface : ICandidatePopupWindow
{
    private readonly UnoAppHost host;
    internal CandidatePopupLifecycle Lifecycle { get; }
    internal bool AutoReady = true;
    internal PopupSurface(UnoAppHost host)
    {
        this.host = host;
        Lifecycle = new(action => action(), _ => { }, () => { }, () => { });
        Lifecycle.PresentationChanged += (_, shown) => PresentationChanged?.Invoke(this, shown);
    }
    internal void Ready() => Lifecycle.Shows.CompleteWarmup(Lifecycle.Shows.WarmupGeneration);
    private void CompleteIfReady() { if (AutoReady) Ready(); }
    internal string? PreviewQuery;
    internal bool PreviewSortMenu;
    internal int Prewarms, Shows, Hides, Disposals;
    internal bool ClosedAlready;
    public event EventHandler? Closed;
    public event EventHandler? Hidden;
    public event EventHandler<bool>? PresentationChanged;
    public void Prewarm() { Prewarms++; Lifecycle.Shows.BeginWarmup(); CompleteIfReady(); }
    public void ShowPreview(string? query, bool openSortMenu)
    {
        PreviewQuery = query; PreviewSortMenu = openSortMenu;
        Lifecycle.Preview(host.Popup, query, () => { Shows++; return new(true, null); }, () => host.CanPresentPopup);
        CompleteIfReady();
    }
    public bool SupportsCapturedPopup => true;
    internal Action<InsertionTarget>? CapturedShown;
    public void ShowCaptured(InsertionTarget target)
    {
        Lifecycle.Captured(host.Popup, target, () => host.CanPresentPopup && host.Gate.CanInsert,
            () => { CapturedShown?.Invoke(target); Shows++; return new(true, null); });
        CompleteIfReady();
    }
    public void Hide() { Hides++; Lifecycle.Shows.Hide(); Lifecycle.Hidden(); Hidden?.Invoke(this, EventArgs.Empty); }
    public void ShowAgain() { Lifecycle.Reopen(host.Popup, () => { Shows++; return new(true, null); }, () => host.CanPresentPopup); CompleteIfReady(); }
    public void OpenMainWindow() { _ = host.ApplyLaunchOptionsAsync(new(PageKind.Snippets, false, null)); Hide(); }
    public void EditSnippet(long id) { var row = host.Popup.Items.First(r => r.SnippetId == id); host.Popup.EditRow(row); }
    public void Focus(PopupFocusTarget target) { }
    public void Dispose() { if (ClosedAlready) return; ClosedAlready = true; Lifecycle.Shows.Close(); Disposals++; Closed?.Invoke(this, EventArgs.Empty); }
}
internal sealed class TestDialogs : IDialogService
{
    internal Queue<SaveChoice> Choices { get; } = [];
    internal int Errors;
    internal Task<SaveChoice>? PendingChoice;
    public Task<bool> ConfirmAsync(string title, string message, string confirmText) => Task.FromResult(true);
    public Task<SaveChoice> AskSaveDiscardCancelAsync(string title, string message) => PendingChoice ?? Task.FromResult(Choices.Count > 0 ? Choices.Dequeue() : SaveChoice.Cancel);
    public Task ShowErrorAsync(string title, string message) { Errors++; return Task.CompletedTask; }
    public Task ShowInfoAsync(string title, string message) => Task.CompletedTask;
}
internal sealed class TestFiles : ISnippetFileService
{
    internal string? ImportPath, ExportPath;
    internal bool FailWrite;
    internal Task<string?>? DelayedExportPicker;
    public Task<string?> PickImportFileAsync() => Task.FromResult(ImportPath);
    public Task<string?> PickExportFileAsync(string suggestedBaseName) => DelayedExportPicker ?? Task.FromResult(ExportPath);
    public Stream OpenRead(string path) => File.OpenRead(path);
    public Task WriteAtomicallyAsync(string path, ReadOnlyMemory<byte> content) => FailWrite
        ? Task.FromException(new IOException("synthetic write refusal")) : AtomicFileWriter.WriteAsync(path, content);
}
internal sealed class TestTheme(AppSettings settings) : IThemeService
{
    public ThemeMode Current { get; private set; } = settings.Theme;
    public ColorPalette CurrentPalette { get; private set; } = settings.ColorPalette;
    public void Apply(ThemeMode mode) => Current = mode;
    public void ApplyPalette(ColorPalette palette) => CurrentPalette = palette;
}
