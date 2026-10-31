using System.Security.AccessControl;
using System.Security.Principal;
using SmartSnippets.Core.Domain;
using SmartSnippets.Core.Logging;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Presentation.ViewModels;
using SmartSnippets.UnoProbe;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace SmartSnippets.UnoFlow.Tests;

public sealed class ProbePopupDriverTests
{
    [Fact]
    public void RealDriverCloaksBeforeWarmupAndUncloaksOnlyAfterHideAndReady()
    {
        using var h = new Harness();
        h.Flow.OnPreview(); var surface = h.Surfaces.Single();
        Assert.Equal(new[] { "cloak:True", "watch", "invalidate", "warmup" }, surface.Events);
        Assert.Single(h.Timers); Assert.True(h.Timers[0].Started);
        surface.Ready();
        Assert.Equal(new[] { "cloak:True", "watch", "invalidate", "warmup", "unwatch", "hide", "cloak:False", "cloak:False", "warmup" }, surface.Events);
        Assert.True(h.Timers[0].Disposed);
    }
    [Fact]
    public void RetryIsBoundedStaleCallbacksCannotStopCurrentTimerOrDetachLayout()
    {
        using var h = new Harness(); h.Flow.OnPreview();
        var surface = h.Surfaces.Single(); var staleReady = surface.ReadyCallback!;
        var firstTimer = h.Timers.Single(); firstTimer.Fire();
        Assert.True(firstTimer.Disposed); Assert.Equal(2, h.Timers.Count);
        Assert.Equal(2, surface.Events.Count(e => e == "invalidate"));
        var current = h.Timers.Last(); var events = surface.Events.Count;
        staleReady(); firstTimer.Fire();
        Assert.False(current.Disposed); Assert.Equal(events, surface.Events.Count);
        current.Fire();
        Assert.False(h.Flow.IsWarming); Assert.True(current.Disposed);
        Assert.Equal(2, h.Timers.Count); Assert.DoesNotContain("show", surface.Events);
        h.Flow.OnPreview(); Assert.Equal(3, h.Timers.Count); // A fresh explicit request can restart.
    }
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ClosedNativeWindowIsInvalidatedBeforePolicyAndRecreatedCold(bool warming)
    {
        using var h = new Harness(); h.Flow.OnPreview();
        var dead = h.Surfaces.Single(); if (!warming) dead.Ready();
        var generation = h.Flow.WarmupGeneration; var stale = dead.ReadyCallback!;
        dead.Kill(); // Any later native method on this fake dead handle throws.
        Assert.False(h.Flow.IsWarming); Assert.True(h.Timers[0].Disposed);
        h.Flow.OnPreview(); Assert.Equal(2, h.Surfaces.Count);
        var next = h.Surfaces.Last(); Assert.True(h.Flow.WarmupGeneration > generation);
        stale(); h.Timers[0].Fire(); h.Flow.OnLayoutReady(generation);
        Assert.True(h.Flow.IsWarming);
        next.Ready(); Assert.False(h.Flow.IsWarming);
    }
    [Fact]
    public async Task DefaultDriverRefusesEveryNativeOperationIncludingConsentedClipboardLoss()
    {
        using var h = new Harness(); var os = h.Driver;
        var target = new InsertionTarget(1, 1, 0, "SYNTHETIC_TITLE_018");
        Assert.Null(os.CaptureTarget()); Assert.Equal(TargetCheck.Closed, os.Check(target));
        Assert.False(os.IsTargetForeground(target)); Assert.Null(await os.RestoreFocusAsync(target, default));
        Assert.False(os.ReleaseHeldModifiers()); Assert.False(os.ReleasePasteKeys());
        foreach (var consent in new[] { false, true })
        {
            var swap = os.TrySwapClipboard("synthetic", consent);
            Assert.Equal(SwapStatus.NotPreserved, swap.Status); Assert.Null(swap.Snapshot);
        }
        Assert.Equal(SwapStatus.Failed, os.TryPlaceText("synthetic"));
        Assert.Equal(DispatchState.NotDispatched, os.SendPaste());
        Assert.Equal(RestoreStatus.Failed, os.TryRestoreClipboard(new Snapshot()));
        Assert.Throws<InvalidOperationException>(() => os.DiscardClipboardSnapshot(new Snapshot()));
        Assert.Throws<InvalidOperationException>(() => os.SetText("synthetic"));
        Assert.Equal(InsertionOutcome.PasteFailed, (await h.Flow.Insertion.InsertAsync(target, h.Flow.SnippetId, new(true))).Outcome);
        Assert.Empty(h.Surfaces);
        h.Flow.OnPreview(); h.Surfaces.Single().Ready(); h.Flow.OnHide();
        Assert.Throws<InvalidOperationException>(() => h.Flow.OnMainCopy(h.Flow.SnippetId));
    }
    [Fact]
    public async Task DeferredStoreDirtyFlushAndFocusCancellationExecuteThroughDriver()
    {
        var queue = new Queue<Action>(); using var h = new Harness(schedule: queue.Enqueue, native: new Native());
        h.Flow.OnShortcut(); h.Surfaces.Single().Ready();
        h.Flow.OnHide(); while (queue.TryDequeue(out var action)) action();
        Assert.Empty(h.Surfaces.Single().Focuses);
        h.Flow.Store.CreateSnippet(new SnippetDraft("new synthetic", "new row", true, null, "newtag"));
        Assert.Single(h.Flow.Popup.Items, i => i.Kind == PopupItemKind.Snippet);
        h.Flow.ShowAgain();
        Assert.Equal(2, h.Flow.Popup.Items.Count(i => i.Kind == PopupItemKind.Snippet));
        h.Flow.OnPreview(); while (queue.TryDequeue(out var action)) action();
        Assert.Empty(h.Surfaces.Single().Focuses);
        await h.Flow.OnEnterAsync();
    }
    [Fact]
    public void CloseCancelsDeferredFocusFromTheOldCoordinator()
    {
        var queue = new Queue<Action>(); using var h = new Harness(native: new Native(), schedule: queue.Enqueue);
        h.Flow.OnShortcut(); h.Surfaces.Single().Ready(); h.Surfaces.Single().Kill();
        h.Flow.OnShortcut();
        while (queue.TryDequeue(out var action)) action();
        Assert.Empty(h.Surfaces.Last().Focuses);
        Assert.True(h.Flow.IsWarming);
    }
    [Fact]
    public async Task ShutdownClosesAdmissionHidesAndDrainsActualInsertionBeforeProfileRemoval()
    {
        var native = new Native { FocusWait = new() }; var queue = new Queue<Action>();
        using var h = new Harness(native: native, schedule: queue.Enqueue);
        h.Flow.OnShortcut(); h.Surfaces.Single().Ready();
        var insertion = h.Flow.OnEnterAsync();
        var root = h.Flow.ProfileRoot; var shutdown = h.Flow.ShutdownAsync();
        Assert.False(shutdown.IsCompleted); Assert.True(Directory.Exists(root));
        var calls = native.Calls.Count;
        h.Flow.OnShortcut(); h.Flow.OnMenu(); h.Flow.OnPreview(); h.Flow.OnOpenMain(); h.Flow.OnMainCopy(h.Flow.SnippetId);
        h.Flow.OnFooterDismiss(); h.Flow.OnPopupCopy(h.Flow.Popup.Items[h.Flow.Popup.SelectedIndex]);
        await h.Flow.OnMainInsertAsync(h.Flow.SnippetId); await h.Flow.OnEnterAsync();
        Assert.Equal(calls, native.Calls.Count);
        Assert.Equal(InsertionOutcome.Busy, (await h.Flow.Insertion.InsertAsync(native.Target, h.Flow.SnippetId)).Outcome);
        while (queue.TryDequeue(out var action)) action();
        Assert.Empty(h.Surfaces.Single().Focuses);
        native.FocusWait.SetResult(FocusPath.Direct);
        await insertion; await shutdown;
        Assert.False(Directory.Exists(root)); Assert.Equal(1, native.Dispatches); Assert.Equal(1, native.Restores);
        await h.Flow.ShutdownAsync(); h.Flow.Dispose();
        var surfaceEvents = h.Surfaces.Single().Events.Count;
        h.Flow.OnPreview(); h.Flow.OnLayoutReady(h.Flow.WarmupGeneration); h.Flow.ShowAgain();
        Assert.Equal(surfaceEvents, h.Surfaces.Single().Events.Count);
    }
    [Fact]
    public async Task ShutdownWhileVisibleHidesAndClosedPopupThenMainNeverTouchesDeadHandle()
    {
        using var h = new Harness(); h.Flow.OnPreview(); h.Surfaces.Single().Ready();
        var live = h.Surfaces.Single(); var hides = live.Events.Count(e => e == "hide");
        await h.Flow.ShutdownAsync(); Assert.True(live.Events.Count(e => e == "hide") > hides);
        live.Kill(); h.Driver.Dispose(); await h.Flow.ShutdownAsync();
    }
    [Fact]
    public void OwnerPrivateProfileIsMarkedRemovedAndCollisionNeverAdopted()
    {
        var root = TempRoot();
        using (var h = new Harness(root: root))
        {
            Assert.True(File.Exists(Path.Combine(root, ".probe-owned")));
            AssertPrivate(root);
            Assert.Throws<IOException>(() => ProbeFlowComposition.CreateAtRoot(h.Driver, a => a(), root));
        }
        Assert.False(Directory.Exists(root));
        Directory.CreateDirectory(root); var canary = Path.Combine(root, "retain"); File.WriteAllText(canary, "owned elsewhere");
        Assert.Throws<IOException>(() => new ProbeFlowProfile(root));
        Assert.Equal("owned elsewhere", File.ReadAllText(canary)); Directory.Delete(root, true);
    }
    [Fact]
    public void ConstructorFailureCleansOwnedDirectoryAndDeletionRefusesWrongMarker()
    {
        var root = TempRoot();
        Assert.Throws<ArgumentNullException>(() => ProbeFlowComposition.CreateAtRoot(null!, a => a(), root));
        Assert.False(Directory.Exists(root));
        var profile = new ProbeFlowProfile(root); var marker = Path.Combine(root, ".probe-owned");
        var token = File.ReadAllText(marker); File.WriteAllText(marker, "wrong owner");
        Assert.Throws<IOException>(() => profile.Dispose()); Assert.True(Directory.Exists(root));
        File.WriteAllText(marker, token); profile.Dispose(); profile.Dispose(); Assert.False(Directory.Exists(root));
    }
    [Fact]
    public async Task SyntheticTitleAppearsInRefusalContextButNotActualAppLog()
    {
        var native = new Native { CheckResult = TargetCheck.Closed }; using var h = new Harness(native: native);
        var logRoot = Path.Combine(h.Flow.ProfileRoot, "logs"); AppLog.Initialize(logRoot);
        try
        {
            h.Flow.OnShortcut(); h.Surfaces.Single().Ready(); await h.Flow.OnEnterAsync();
            Assert.Contains(native.Target.DisplayName, h.Flow.Popup.FooterMessage);
            var log = File.ReadAllText(Path.Combine(logRoot, "app.log"));
            Assert.Contains("insert_result", log);
            foreach (var canary in new[] { native.Target.DisplayName, "Synthetic flow", "日本語", "#synthetic" }) Assert.DoesNotContain(canary, log);
        }
        finally { AppLog.Shutdown(); }
    }
    private static string TempRoot() => Path.Combine(Path.GetTempPath(), "SmartSnippets.Uno018-test-" + Guid.NewGuid().ToString("N"));
    private static void AssertPrivate(string root)
    {
        if (OperatingSystem.IsWindows())
        {
            var acl = new DirectoryInfo(root).GetAccessControl(); Assert.True(acl.AreAccessRulesProtected);
            var user = WindowsIdentity.GetCurrent().User;
            var rules = acl.GetAccessRules(true, true, typeof(SecurityIdentifier)).Cast<FileSystemAccessRule>();
            foreach (var rule in rules) { Assert.Equal(user, rule.IdentityReference); Assert.Equal(AccessControlType.Allow, rule.AccessControlType); }
            Assert.NotEmpty(rules);
        }
        else Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute, File.GetUnixFileMode(root));
    }
    internal sealed class Harness : IDisposable
    {
        internal List<Surface> Surfaces { get; } = [];
        internal List<Timer> Timers { get; } = [];
        internal ProbePopupDriver Driver { get; }
        internal ProbeFlowComposition Flow { get; }
        internal int MainShows;
        internal Harness(Native? native = null, Action<Action>? schedule = null, string? root = null)
        {
            Driver = new ProbePopupDriver(() => { var s = new Surface(); Surfaces.Add(s); return s; },
                () => { var t = new Timer(); Timers.Add(t); return t; }, () => MainShows++, native);
            Flow = root is null ? ProbeFlowComposition.Create(Driver, schedule ?? (a => a())) :
                ProbeFlowComposition.CreateAtRoot(Driver, schedule ?? (a => a()), root);
            Driver.Attach(Flow);
        }
        public void Dispose() { Flow.Dispose(); Driver.Dispose(); }
    }
    internal sealed class Surface : IProbePopupSurface
    {
        public event Action? Closed;
        internal bool Dead;
        internal List<string> Events { get; } = [];
        internal List<PopupFocusTarget> Focuses { get; } = [];
        internal Action? ReadyCallback;
        private void Native(string token) { Assert.False(Dead, "Use-after-close: " + token); Events.Add(token); }
        public void WatchLayout(Action ready) { Native("watch"); ReadyCallback = ready; }
        public void UnwatchLayout() => Events.Add("unwatch"); // Managed detach allowed after native close.
        public void InvalidateMeasure() => Native("invalidate");
        public void Cloak(bool value) => Native("cloak:" + value);
        public void ShowWarmup() => Native("warmup");
        public void Show() => Native("show");
        public void Hide() => Native("hide");
        public void Focus(PopupFocusTarget target) { Native("focus"); Focuses.Add(target); }
        public void Close() { Native("close"); Kill(); }
        internal void Ready() => ReadyCallback!();
        internal void Kill() { Dead = true; Closed?.Invoke(); }
    }
    internal sealed class Timer : IProbeFlowTimer
    {
        internal bool Started, Disposed;
        private Action? _elapsed;
        public void Start(Action elapsed) { _elapsed = elapsed; Started = true; }
        public void Stop() => Started = false;
        public void Dispose() => Disposed = true;
        internal void Fire() => _elapsed!(); // Deliberately delivers stale callbacks, even after Dispose.
    }
    internal sealed class Native : IGuardedProbeNativeAdapter
    {
        internal InsertionTarget Target { get; } = new(101, 77, 0, "SYNTHETIC_TITLE_018");
        internal List<string> Calls { get; } = [];
        internal TargetCheck CheckResult = TargetCheck.Ok;
        internal TaskCompletionSource<FocusPath?>? FocusWait;
        internal int Dispatches, Restores;
        internal SwapStatus Swap = SwapStatus.Swapped;
        internal DispatchState Dispatch = DispatchState.Dispatched;
        public InsertionTarget CaptureTarget() { Calls.Add("capture"); return Target; }
        public TargetCheck Check(InsertionTarget target) { Calls.Add("check"); Assert.Equal(Target, target); return CheckResult; }
        public bool IsTargetForeground(InsertionTarget target) { Calls.Add("foreground"); return target == Target; }
        public Task<FocusPath?> RestoreFocusAsync(InsertionTarget target, CancellationToken ct) { Calls.Add("restore-focus"); return FocusWait?.Task ?? Task.FromResult<FocusPath?>(FocusPath.Direct); }
        public bool ReleaseHeldModifiers() { Calls.Add("modifiers"); return true; }
        public ClipboardSwap TrySwapClipboard(string text, bool allowIncomplete) { Calls.Add("swap"); return new(Swap, Swap == SwapStatus.Swapped ? new Snapshot() : null); }
        public SwapStatus TryPlaceText(string text) { Calls.Add("place"); return SwapStatus.Swapped; }
        public DispatchState SendPaste() { Calls.Add("paste"); Dispatches++; return Dispatch; }
        public bool ReleasePasteKeys() { Calls.Add("keys"); return true; }
        public RestoreStatus TryRestoreClipboard(IClipboardSnapshot snapshot) { Calls.Add("restore"); Restores++; return RestoreStatus.Restored; }
        public void DiscardClipboardSnapshot(IClipboardSnapshot snapshot) => Calls.Add("discard");
        public void SetText(string text) => Calls.Add("copy");
    }
    internal sealed class Snapshot : IClipboardSnapshot
    {
        public int SavedCount => 1;
        public int SkippedByDesignCount => 0;
        public int UnsavedCount => 0;
        public bool CapExceeded => false;
        public long ByteCount => 0;
    }
}
