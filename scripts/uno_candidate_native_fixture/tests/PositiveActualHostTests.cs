using SmartSnippets.Core.Settings;
using SmartSnippets.Presentation.Navigation;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Uno.Platforms.Windows;
using SmartSnippets.Uno.Tests.Composition;
using Xunit;

namespace SmartSnippets.Uno;

[CollectionDefinition("Fixture host", DisableParallelization = true)]
public sealed class FixtureHostCollection { }

[Collection("Fixture host")]
public sealed class PositiveActualHostTests
{
    [Theory]
    [InlineData("enter-callback")] [InlineData("popup-row")] [InlineData("main-row")]
    public Task ActualHost_LateBoundSessionCaptureSingleFlightAndPersistedUsage(string action) => CandidateHostThread.RunAsync(async () =>
    {
        using var f = new Fixture();
        await f.Host.StartAsync(true, new(null, false, null));
        var id = f.Host.Store.CreateSnippet(new("AΩ中🙂e\u0301\nsecond line\n", "Synthetic", true, null, "")).Value!.Id;
        Assert.False(f.Host.ShowCapturedPopup());
        f.Broker.Bind(f.Operations, f.Operations.Id);
        Assert.False(f.Host.ShowCapturedPopup()); // Guardian/session binding is not post-Job admission.
        f.Broker.Admit();
        Assert.True(f.Host.ShowCapturedPopup());
        Assert.Equal(f.Operations.Id, f.Operations.ValidatedSession); // Late session-ID translation.
        Assert.Same(f.Operations.Target, f.Host.Popup.Target);
        var first = action == "main-row" ? f.Host.Snippets.InsertRowAsync(id) : action == "popup-row"
            ? f.Host.Popup.InsertRowAsync(f.Host.Popup.Items.Single(row => row.SnippetId == id)) : f.Host.Popup.InsertSelectedAsync();
        Assert.True(f.Host.Popup.IsInserting);
        Assert.False(f.Host.ShowCapturedPopup());
        await f.Host.Snippets.InsertRowAsync(id);
        Assert.Equal(InsertionOutcome.Busy, (await f.Host.Gate.InsertAsync(f.Operations.Target, id)).Outcome);
        f.Operations.Focus.SetResult(FocusPath.Direct);
        await first;
        f.Host.Store.Load();
        Assert.Equal(1, f.Host.Store.Snippets.Single().Snippet.UsageCount);
        Assert.Equal(1, f.Broker.CaptureCalls); Assert.Equal(1, f.Broker.PasteCalls);
        Assert.Equal("AΩ中🙂e\u0301\r\nsecond line\r\n", f.Operations.Text);
        Assert.Equal(1, f.Operations.Restores);
        await f.Host.ExitAsync();
        Assert.Equal(1, f.Boundary.Exits); Assert.True(f.Operations.Drained);
    });

    [Fact]
    public Task ActualHost_CopyWaitAndFocusReachBoundSessionButPreviewAndRevocationNeverMutate() => CandidateHostThread.RunAsync(async () =>
    {
        using var f = new Fixture();
        await f.Host.StartAsync(true, new(null, false, null));
        var id = f.Host.Store.CreateSnippet(new("synthetic", "Synthetic", true, null, "")).Value!.Id;
        f.Broker.Bind(f.Operations, f.Operations.Id); f.Broker.Admit();
        await f.Host.RunCopyOperationAsync(() => f.Host.Snippets.CopyRow(id));
        Assert.Equal(1, f.Operations.CopyWaits); Assert.Equal(1, f.Broker.CopyCommits);
        Assert.True(f.Host.ShowCapturedPopup());
        Assert.True(f.Host.Gate.TryFocusOwnedPopup()); Assert.Equal(1, f.Operations.PopupFocus);
        f.Host.ShowPreview();
        await f.Host.RunCopyOperationAsync(() => f.Host.Snippets.CopyRow(id));
        f.Host.Popup.CopyRow(f.Host.Popup.Items.Single(row => row.SnippetId == id));
        await f.Host.Snippets.InsertRowAsync(id);
        await f.Host.Popup.InsertSelectedAsync();
        Assert.Equal(1, f.Broker.CopyCommits); Assert.Equal(0, f.Broker.PasteCalls);
        Assert.Equal(0, f.Host.Store.Snippets.Single().Snippet.UsageCount);
        f.Capability.Revoke(); f.Boundary.Popup.Hide();
        Assert.False(f.Host.ShowCapturedPopup());
        await f.Host.RunCopyOperationAsync(() => f.Host.Snippets.CopyRow(id));
        Assert.Equal(1, f.Broker.CopyCommits);
        await f.Host.ExitAsync();
    });

    private sealed class Fixture : IDisposable
    {
        private readonly string root = Path.Combine(Path.GetTempPath(), "ss019-positive-tests-" + Guid.NewGuid().ToString("N"));
        internal readonly LateNativeOperations Broker = new();
        internal readonly Operations Operations = new();
        internal readonly Boundary Boundary;
        internal readonly CandidateNativeCapability Capability;
        internal readonly UnoAppHost Host;
        internal Fixture()
        {
            Capability = new(Guid.NewGuid(), 123, 456, PositiveProtocol.Permissions("posted-enter") | CandidateNativeOperation.Copy,
                TimeSpan.FromSeconds(30), TimeProvider.System, Broker);
            Boundary = new(Capability);
            Host = new(Boundary, () => CandidateProfile.OpenOwnedRoot(root));
        }
        public void Dispose()
        {
            Host.Dispose(); Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
    private sealed class Operations : ICandidateNativeOperations
    {
        internal readonly Guid Id = Guid.NewGuid();
        internal readonly InsertionTarget Target = new(123, 456, 789, "synthetic");
        internal readonly TaskCompletionSource<FocusPath?> Focus = new();
        internal Guid ValidatedSession;
        internal int Restores, CopyWaits, PopupFocus;
        internal bool Drained;
        internal string? Text;
        public bool Validate(CandidateNativeOperation operation, Guid id, nint window, uint pid)
        { ValidatedSession = id; return id == Id && window == 123 && pid == 456; }
        public InsertionTarget? CaptureOwnedTarget(nint window, uint pid) => Target;
        public TargetCheck Check(InsertionTarget target) => target == Target ? TargetCheck.Ok : TargetCheck.Closed;
        public bool IsTargetForeground(InsertionTarget target) => target == Target;
        public Task<FocusPath?> RestoreFocusAsync(InsertionTarget target, CancellationToken ct) => Focus.Task;
        public bool ReleaseHeldModifiers() => true;
        public ClipboardSwap TrySwapClipboard(string text, bool allowIncomplete) { Text = text; return new(SwapStatus.Swapped, new Snapshot()); }
        public SwapStatus TryPlaceText(string text) => SwapStatus.NotPreserved;
        public DispatchState SendPaste() => DispatchState.Dispatched;
        public bool ReleasePasteKeys() => true;
        public RestoreStatus TryRestoreClipboard(IClipboardSnapshot snapshot) { Restores++; return RestoreStatus.Restored; }
        public void DiscardClipboardSnapshot(IClipboardSnapshot snapshot) => throw new InvalidOperationException();
        public bool IsAvailable => true;
        public bool TrySetText(string text) => true;
        public void SetText(string text) => _ = TrySetText(text);
        public Task<bool> WaitForCopyAdmissionAsync(CancellationToken ct) { CopyWaits++; return Task.FromResult(true); }
        public bool TryFocusOwnedPopup() { PopupFocus++; return true; }
        public Task<bool> DrainOwnedRecoveryAsync() { Drained = true; return Task.FromResult(true); }
    }
    private sealed class Snapshot : IClipboardSnapshot
    {
        public int SavedCount => 1; public int SkippedByDesignCount => 0; public int UnsavedCount => 0;
        public bool CapExceeded => false; public long ByteCount => 1;
    }
    private sealed class Boundary(CandidateNativeCapability capability) : CandidateHostBoundary
    {
        internal int Exits;
        internal Surface Popup = null!;
        internal override CandidateNativeCapability NativeCapability => capability;
        internal override IUiDispatcher Dispatcher => new Dispatcher();
        internal override IThemeService CreateTheme(AppSettings settings) => new Theme();
        internal override IDisposable CreateMessageWindow() => new Main();
        internal override IDialogService CreateDialogs(bool popup, ITextSizeService textSize) => new Dialogs();
        internal override ISnippetFileService CreateFiles() => new Files();
        internal override ICandidateMainWindow CreateMain(UnoAppHost host) => new Main();
        internal override ICandidatePopupWindow CreatePopup(UnoAppHost host) => Popup = new(host);
        internal override ICandidateTray CreateTray(Action open, Action preview, Action settings, Action exit) => new Tray();
        internal override void ReportStartupFailure() => throw new InvalidOperationException();
        internal override void Exit() => Exits++;
    }
    private sealed class Surface(UnoAppHost host) : ICandidatePopupWindow
    {
        public event EventHandler? Closed { add { } remove { } }
        public event EventHandler? Hidden;
        public event EventHandler<bool>? PresentationChanged;
        public bool SupportsCapturedPopup => true;
        public void Prewarm() { }
        public void ShowCaptured(InsertionTarget target) { host.Popup.Open(target); PresentationChanged?.Invoke(this, true); }
        public void ShowPreview(string? query, bool openSortMenu) { host.Popup.OpenPreview(); if (query is not null) host.Popup.Query = query; PresentationChanged?.Invoke(this, true); }
        public void Hide() => Hidden?.Invoke(this, EventArgs.Empty);
        public void ShowAgain() { }
        public void OpenMainWindow() { }
        public void EditSnippet(long id) { }
        public void Focus(PopupFocusTarget target) { }
        public void Dispose() { }
    }
    private sealed class Main : ICandidateMainWindow
    {
        public event EventHandler? HiddenByClose { add { } remove { } }
        public Task NavigateAsync(PageKind page, bool show) => Task.CompletedTask;
        public Task<bool> CanExitAsync() => Task.FromResult(true);
        public void SetInteractionEnabled(bool enabled) { }
        public void Dispose() { }
    }
    private sealed class Dispatcher : IUiDispatcher { public void Post(Action action) => action(); }
    private sealed class Theme : IThemeService
    {
        public ThemeMode Current => ThemeMode.System; public ColorPalette CurrentPalette => ColorPalette.Default;
        public void Apply(ThemeMode mode) { } public void ApplyPalette(ColorPalette palette) { }
    }
    private sealed class Tray : ICandidateTray { public void Notify(string title, string message) { } public void Dispose() { } }
    private sealed class Files : ISnippetFileService
    {
        public Task<string?> PickImportFileAsync() => Task.FromResult<string?>(null);
        public Task<string?> PickExportFileAsync(string suggestedBaseName) => Task.FromResult<string?>(null);
        public Stream OpenRead(string path) => throw new InvalidOperationException();
        public Task WriteAtomicallyAsync(string path, ReadOnlyMemory<byte> content) => throw new InvalidOperationException();
    }
    private sealed class Dialogs : IDialogService
    {
        public Task<bool> ConfirmAsync(string title, string message, string confirmText) => Task.FromResult(false);
        public Task<SaveChoice> AskSaveDiscardCancelAsync(string title, string message) => Task.FromResult(SaveChoice.Cancel);
        public Task ShowErrorAsync(string title, string message) => Task.CompletedTask;
        public Task ShowInfoAsync(string title, string message) => Task.CompletedTask;
    }
}
