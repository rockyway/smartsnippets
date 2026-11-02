using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml;
using SmartSnippets.Core.Data;
using SmartSnippets.Core.Settings;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Uno.Platforms.Windows;
using SmartSnippets.Uno.Tests.Composition;
using Windows.System;

namespace SmartSnippets.Uno.Tests.Flows
{
    // D80 supersedes ordinary-path default denial. OS boundaries alone are substituted.
    [Collection("App XAML actions")]
    public sealed class DailyHotkeyFlowTests
    {
        [Fact]
        public void SavedHotkey_ConflictPreservesWorkingRegistration_AndRestoreDefault()
        {
            var os = new Registration(); var manager = new HotkeyManager(os);
            var saved = new HotkeyGesture(HotkeyGesture.ModControl | HotkeyGesture.ModAlt, 0x78);
            Assert.True(manager.Initialize(saved).IsRegistered);
            Assert.Equal(saved.Key, os.Calls[0].Key);
            Assert.Equal(saved.Modifiers | 0x4000u, os.Calls[0].Modifiers);
            os.Conflict = true;
            Assert.False(manager.TryChange(HotkeyGesture.Default).Success);
            Assert.Empty(os.Released); Assert.Equal(saved, manager.Status.Gesture);
            os.Conflict = false;
            Assert.True(manager.RestoreDefault().Success);
            Assert.Equal(HotkeyGesture.Default, manager.Status.Gesture);
            Assert.Equal(new[] { 1 }, os.Released);
            manager.UnregisterAll(); Assert.Empty(os.Active);
        }

        [Fact]
        public Task SavedHotkeyCallback_CapturesBeforeShow_RealEnterHandlerPastesAndRestoresOnce() => HostThread.RunAsync(async () =>
        {
            using var f = new HostFixture();
            var saved = new HotkeyGesture(HotkeyGesture.ModControl | HotkeyGesture.ModAlt, 0x78);
            using (var profile = AppProfile.OpenOwnedRoot(f.Root))
            {
                var factory = new SqliteConnectionFactory(profile.Paths.DatabasePath);
                new Migrator(factory).MigrateToLatest();
                var settings = new SettingsRepository(factory);
                new AppSettings().WithHotkey(saved).Save(settings);
            }
            Desktop? desktop = null;
            f.Boundary.DesktopFactory = (settings, pressed) => desktop = new(settings, pressed);
            await f.Start(background: true);
            var native = desktop!.Native;
            Assert.Equal(saved, desktop.Hotkeys.Status.Gesture);
            var id = f.Host.Store.CreateSnippet(new("first\nsecond", "synthetic", true, null, "")).Value!.Id;
            f.Boundary.Popups[0].CapturedShown = target =>
            {
                Assert.Equal(new[] { "capture" }, native.Trace);
                Assert.Same(native.Target, target); native.Trace.Add("show");
            };
            f.Boundary.Popups[0].Hidden += (_, _) => native.Trace.Add("hide");
            desktop.Press(); // Same direct callback as WM_HOTKEY; no queued recapture.
            Assert.Equal(new[] { "capture", "show" }, native.Trace);
            Assert.False(f.Host.Popup.IsPreview);
            Assert.True(f.Host.Gate.IsAvailable); // Ordinary Copy is now available without acceptance authorization.
            var window = new Popup.PopupWindow(f.Host.TextSize, () => { });
            window.ViewModel = f.Host.Popup;
            await window.PressEnterForDailyHotkeyTestAsync();
            Assert.Equal("first\r\nsecond", native.Text);
            Assert.Equal(1, native.Sends); Assert.Equal(1, native.Restores);
            Assert.Equal(1, Assert.Single(f.Host.Store.Snippets).Snippet.UsageCount);
            Assert.Equal(InsertionOutcome.Inserted, f.Host.Popup.LastInsertionOutcome);
            Assert.All(native.Checked, target => Assert.Same(native.Target, target));
            Assert.True(native.Trace.IndexOf("hide") < native.Trace.IndexOf("check"));
            Assert.Equal("restore", native.Trace[^1]);
            Assert.Equal(id, Assert.Single(f.Host.Store.Snippets).Snippet.Id);
            await f.Host.ExitAsync(); Assert.True(desktop.Disposed); Assert.Empty(desktop.Registration.Active);
        });

        [Theory]
        [InlineData(TargetCheck.Closed, true, DispatchState.Dispatched, 0, 0)]
        [InlineData(TargetCheck.Elevated, true, DispatchState.Dispatched, 0, 0)]
        [InlineData(TargetCheck.Ok, false, DispatchState.Dispatched, 0, 1)]
        [InlineData(TargetCheck.Ok, true, DispatchState.Uncertain, 1, 1)]
        [InlineData(TargetCheck.Ok, true, DispatchState.NotDispatched, 1, 1)]
        public Task RefusedOrUncertainInsert_RestoresWhenSwapped_AndNeverCreditsUsage(
            TargetCheck check, bool foreground, DispatchState dispatch, int sends, int restores) => HostThread.RunAsync(async () =>
        {
            using var f = new HostFixture(); Desktop? desktop = null;
            f.Boundary.DesktopFactory = (settings, pressed) => desktop = new(settings, pressed);
            await f.Start(); var native = desktop!.Native;
            f.Host.Store.CreateSnippet(new("synthetic", "synthetic", true, null, ""));
            native.CheckResult = check; native.Foreground = foreground; native.Dispatch = dispatch;
            desktop.Press(); await f.Host.Popup.InsertSelectedAsync();
            Assert.Equal(sends, native.Sends); Assert.Equal(restores, native.Restores);
            Assert.Equal(0, Assert.Single(f.Host.Store.Snippets).Snippet.UsageCount);
            Assert.NotEqual(InsertionOutcome.Inserted, f.Host.Popup.LastInsertionOutcome);
        });

        [Fact]
        public Task PreviewAndShutdown_DoNotAdmitHotkeyInsertion() => HostThread.RunAsync(async () =>
        {
            using var f = new HostFixture(); Desktop? desktop = null;
            f.Boundary.DesktopFactory = (settings, pressed) => desktop = new(settings, pressed);
            await f.Start(); f.Host.ShowPreview(); desktop!.Press();
            Assert.Equal(0, desktop.Native.Captures);
            await f.Host.Popup.InsertSelectedAsync(); Assert.Equal(0, desktop.Native.Sends);
            f.Boundary.Popups[0].Hide();
            await f.Host.ExitAsync(); desktop.Press();
            Assert.Equal(0, desktop.Native.Captures); Assert.Equal(0, desktop.Native.Sends);
        });

        [Fact]
        public Task HotkeyUi_UsesRegisteredGestureAndTracksPopupAndSettingsChanges() => HostThread.RunAsync(async () =>
        {
            using var f = new HostFixture(); Desktop? desktop = null;
            f.Boundary.DesktopFactory = (settings, pressed) => desktop = new(settings, pressed);
            await f.Start();
            Assert.True(f.Boundary.Main.HotkeyStatus!.IsRegistered);
            Assert.Equal(TrayState.Ready, f.Boundary.TrayState);
            var views = new WindowsHostBoundary(); views.CreateTheme(f.Host.Settings);
            using var main = views.CreateMain(f.Host);
            Assert.Equal("Smart Snippets", views.MainWindow!.Title);
            main.SetHotkeyStatus(desktop!.Hotkeys.Status);
            Assert.Contains(desktop.Hotkeys.Status.Message, views.MainWindow!.NativeStatusText.Text);
            Assert.DoesNotContain("insertion unavailable", views.MainWindow.NativeStatusText.Text);
            var changed = new HotkeyGesture(HotkeyGesture.ModControl | HotkeyGesture.ModAlt, 0x78);
            Assert.True(desktop.Hotkeys.TryChange(changed).Success); f.Boundary.Drain();
            Assert.Equal(changed, f.Boundary.Main.HotkeyStatus.Gesture);
            desktop.Press(); Assert.Equal(TrayState.PopupOpen, f.Boundary.TrayState);
            f.Boundary.Popups[0].Hide(); Assert.Equal(TrayState.Ready, f.Boundary.TrayState);
            main.SetHotkeyStatus(new(false, changed, "Synthetic registration conflict"));
            Assert.Equal("Synthetic registration conflict", views.MainWindow.NativeStatusText.Text);
        });

        [Fact]
        public Task DailyCopy_AllVmConsumersNeverPasteOrCredit_AndPreviewDenies() => HostThread.RunAsync(async () =>
        {
            using var f = new HostFixture(); Desktop? desktop = null;
            f.Boundary.DesktopFactory = (settings, pressed) => desktop = new(settings, pressed);
            await f.Start();
            var id = f.Host.Store.CreateSnippet(new("synthetic copy", "synthetic", true, null, "")).Value!.Id;
            var native = desktop!.Native;
            await f.Host.RunCopyOperationAsync(() => f.Host.Snippets.CopyRow(id));
            Assert.Equal(1, native.Copies);
            desktop.Press();
            await f.Host.RunCopyOperationAsync(() => f.Host.Popup.CopyRow(f.Host.Popup.Items[0]));
            Assert.Equal(2, native.Copies);
            f.Host.Popup.EditRow(f.Host.Popup.Items[0]);
            await f.Host.RunCopyOperationAsync(() => f.Host.Popup.Editor!.CopyTextCommand.Execute(null));
            Assert.Equal(3, native.Copies);
            await f.Host.Popup.CancelEditorAsync(false);
            native.CheckResult = TargetCheck.Closed;
            await f.Host.Popup.InsertSelectedAsync();
            Assert.True(f.Host.Popup.CanCopyFailedText);
            await f.Host.RunCopyOperationAsync(() => f.Host.Popup.CopyFailedTextCommand.Execute(null));
            Assert.Equal(4, native.Copies);
            Assert.Equal("synthetic copy", native.Text);
            Assert.Equal(0, native.Sends);
            Assert.Equal(0, Assert.Single(f.Host.Store.Snippets).Snippet.UsageCount);
            f.Host.ShowPreview();
            Assert.False(f.Host.Gate.IsAvailable);
            await f.Host.RunCopyOperationAsync(() => f.Host.Snippets.CopyRow(id));
            f.Host.Popup.CopyRow(f.Host.Popup.Items[0]);
            Assert.Equal(4, native.Copies);
        });

        [Fact]
        public Task Tray_CapturesSuppliedWindowBeforeShow_WithoutRecapturingForeground() => HostThread.RunAsync(async () =>
        {
            using var f = new HostFixture(); Desktop? desktop = null;
            f.Boundary.DesktopFactory = (settings, pressed) => desktop = new(settings, pressed);
            await f.Start();
            f.Host.Store.CreateSnippet(new("synthetic", "synthetic", true, null, ""));
            f.Boundary.Popups[0].CapturedShown = target =>
            {
                Assert.Equal((nint)123, desktop!.Native.TrayWindow);
                Assert.Equal(0, desktop.Native.Captures);
                Assert.Same(desktop.Native.Target, target);
            };
            f.Boundary.TrayCaptured!(123);
            Assert.False(f.Host.Popup.IsPreview);
            Assert.Equal((nint)123, desktop!.Native.TrayWindow); // Already captured before any queued work.
            await f.Host.Popup.InsertSelectedAsync();
            Assert.Equal(1, desktop!.Native.Sends);
            Assert.Equal(1, Assert.Single(f.Host.Store.Snippets).Snippet.UsageCount);
        });

        [Fact]
        public Task DailyStartup_SettingsReflectsRegistryAndToggleChangesOnlyAppValue() => HostThread.RunAsync(async () =>
        {
            using var f = new HostFixture();
            var values = new Dictionary<string, object?> { ["UnrelatedApp"] = "sentinel" };
            var startup = new Services.StartupRegistration(() => @"C:\synthetic\app.exe",
                name => values.GetValueOrDefault(name), (name, command) => values[name] = command,
                name => values.Remove(name));
            Assert.True(startup.Enable().Success);
            f.Boundary.DesktopFactory = (settings, pressed) => new Desktop(settings, pressed) { Startup = startup };
            await f.Start();
            Assert.True(f.Host.SettingsPage.StartAtLogin);
            f.Host.SettingsPage.StartAtLogin = false;
            Assert.False(startup.IsEnabled());
            f.Host.SettingsPage.StartAtLogin = true;
            Assert.True(startup.IsEnabled());
            Assert.Equal("sentinel", values["UnrelatedApp"]);
        });

        [Fact]
        public Task CopyFailure_CanRetryWithoutPasteUsageOrStuckAdmission() => HostThread.RunAsync(async () =>
        {
            using var f = new HostFixture(); Desktop? desktop = null;
            f.Boundary.DesktopFactory = (settings, pressed) => desktop = new(settings, pressed);
            await f.Start();
            var id = f.Host.Store.CreateSnippet(new("synthetic", "synthetic", true, null, "")).Value!.Id;
            desktop!.Native.CopySucceeds = false;
            await f.Host.RunCopyOperationAsync(() => f.Host.Snippets.CopyRow(id));
            Assert.True(f.Host.Gate.IsAvailable);
            Assert.Equal(0, desktop.Native.Copies);
            desktop.Native.CopySucceeds = true;
            await f.Host.RunCopyOperationAsync(() => f.Host.Snippets.CopyRow(id));
            Assert.Equal(1, desktop.Native.Copies);
            Assert.Equal(0, desktop.Native.Sends);
            Assert.Equal(0, Assert.Single(f.Host.Store.Snippets).Snippet.UsageCount);
            await f.Host.ExitAsync();
            Assert.False(f.Host.Gate.TrySetText("synthetic"));
            Assert.Equal(1, desktop.Native.Copies);
        });

        private sealed class Registration : IHotkeyRegistration
        {
            internal bool Conflict;
            internal List<(int Id, uint Modifiers, uint Key)> Calls = [];
            internal List<int> Released = [];
            internal HashSet<int> Active = [];
            public HotkeyRegistration Register(int id, uint modifiers, uint virtualKey)
            {
                Calls.Add((id, modifiers, virtualKey));
                if (Conflict) return new(false, 1409);
                Active.Add(id); return new(true, 0);
            }
            public void Unregister(int id) { Released.Add(id); Active.Remove(id); }
            public void UnregisterAll() => Active.Clear();
        }
        private sealed class Desktop : IDesktopServices
        {
            internal readonly Registration Registration = new();
            internal readonly Native Native = new();
            private readonly Action _pressed;
            internal bool Disposed;
            internal Desktop(AppSettings settings, Action pressed)
            {
                _pressed = pressed;
                var manager = new HotkeyManager(Registration); manager.Initialize(settings.Hotkey); Hotkeys = manager;
            }
            internal void Press() => _pressed();
            public IDailyOperations Operations => Native;
            public IHotkeyManager Hotkeys { get; }
            public IStartupRegistration Startup { get; init; } = new UnavailableStartupRegistration();
            public IElevation Elevation { get; } = new UnavailableElevation();
            public void Dispose() { Disposed = true; Registration.UnregisterAll(); }
        }
        private sealed class Native : IDailyOperations
        {
            internal InsertionTarget Target = new(123, 456, 789, "synthetic");
            internal List<string> Trace = [];
            internal List<InsertionTarget> Checked = [];
            internal int Captures, Sends, Restores, Copies;
            internal nint TrayWindow;
            internal string? Text;
            internal TargetCheck CheckResult = TargetCheck.Ok;
            internal bool Foreground = true, CopySucceeds = true;
            internal DispatchState Dispatch = DispatchState.Dispatched;
            public InsertionTarget? CaptureForeground() { Captures++; Trace.Add("capture"); return Target; }
            public InsertionTarget? CaptureFrom(nint window) { TrayWindow = window; return Target; }
            public TargetCheck Check(InsertionTarget target) { Checked.Add(target); Trace.Add("check"); return CheckResult; }
            public bool IsTargetForeground(InsertionTarget target) { Checked.Add(target); return Foreground; }
            public Task<FocusPath?> RestoreFocusAsync(InsertionTarget target, CancellationToken ct) { Checked.Add(target); return Task.FromResult<FocusPath?>(FocusPath.Direct); }
            public bool ReleaseHeldModifiers() => true;
            public ClipboardSwap TrySwapClipboard(string text, bool allowIncomplete) { Text = text; return new(SwapStatus.Swapped, new Snapshot()); }
            public SwapStatus TryPlaceText(string text) => throw new InvalidOperationException("Unexpected fallback");
            public DispatchState SendPaste() { Sends++; return Dispatch; }
            public bool ReleasePasteKeys() => true;
            public RestoreStatus TryRestoreClipboard(IClipboardSnapshot snapshot) { Restores++; Trace.Add("restore"); return RestoreStatus.Restored; }
            public void DiscardClipboardSnapshot(IClipboardSnapshot snapshot) => throw new InvalidOperationException("Unexpected discard");
            public bool IsAvailable => true;
            public void SetText(string text) { Copies++; Text = text; }
            public bool TrySetText(string text) { if (!CopySucceeds) return false; SetText(text); return true; }
            public Task<bool> WaitForCopyAdmissionAsync(CancellationToken ct) => Task.FromResult(true);
            public Task<bool> DrainOwnedRecoveryAsync() => Task.FromResult(true);
        }
        private sealed class Snapshot : IClipboardSnapshot
        {
            public int SavedCount => 1;
            public int SkippedByDesignCount => 0;
            public int UnsavedCount => 0;
            public bool CapExceeded => false;
            public long ByteCount => 8;
        }
    }
}

namespace SmartSnippets.Uno.Popup
{
    public sealed partial class PopupWindow
    {
        internal Task PressEnterForDailyHotkeyTestAsync()
        {
            Task last = Task.CompletedTask;
            RunOperation = operation => last = operation();
            FocusManager.Focused = SearchBox;
            OnKeyDown(this, new KeyRoutedEventArgs { Key = VirtualKey.Enter });
            return last;
        }
    }
}
