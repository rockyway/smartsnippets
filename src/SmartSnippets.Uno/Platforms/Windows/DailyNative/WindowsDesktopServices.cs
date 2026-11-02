using SmartSnippets.Core.Settings;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Uno.Platforms.Windows;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace SmartSnippets.Uno.Services;

/// <summary>D80 product composition. Never constructs or unlocks an acceptance session.</summary>
internal sealed class WindowsDesktopServices : ICandidateDesktopServices
{
    private readonly HotkeyService _registration;
    private readonly Action<long> _pressed;
    public ICandidateDailyOperations Operations { get; }
    public IHotkeyManager Hotkeys { get; }
    public IStartupRegistration Startup { get; } = new CandidateStartupRegistration();
    public IElevation Elevation { get; } = new CandidateElevation();

    internal WindowsDesktopServices(MessageWindow message, AppSettings settings, Func<nint> popup, Action hotkey)
    {
        Operations = new DesktopOperations(message.Handle, popup);
        _registration = new HotkeyService(message);
        _pressed = _ => hotkey();
        _registration.HotkeyPressed += _pressed;
        var manager = new HotkeyManager(_registration);
        Hotkeys = manager;
        manager.Initialize(settings.Hotkey);
    }
    public void Dispose()
    {
        _registration.HotkeyPressed -= _pressed;
        _registration.Dispose();
    }

    private sealed class DesktopOperations(HWND owner, Func<nint> popup) : ICandidateDailyOperations
    {
        private readonly Win32TargetProbe _probe = new();
        private readonly ClipboardPasteDispatcher _paste = new(owner);
        private bool _keysPending;
        public bool HasPendingRecovery => _keysPending;
        public bool IsAvailable => false; // Explicit Copy remains outside the hotkey-only slice.
        public void SetText(string text) { }
        public bool TrySetText(string text) => false;
        public InsertionTarget? CaptureForeground() => TargetCapture.CaptureForeground();
        public TargetCheck Check(InsertionTarget target) => _probe.Check(target);
        public bool IsTargetForeground(InsertionTarget target) => _probe.IsTargetForeground(target);
        public bool TryFocusOwnedPopup()
        {
            var handle = new HWND(popup());
            return !handle.IsNull && PInvoke.IsWindow(handle)
                && PInvoke.GetWindowThreadProcessId(handle, out var pid) != 0
                && pid == PInvoke.GetCurrentProcessId() && ForegroundActivation.TryActivate(handle, out _);
        }
        public Task<FocusPath?> RestoreFocusAsync(InsertionTarget target, CancellationToken ct) => _paste.RestoreFocusAsync(target, ct);
        public bool ReleaseHeldModifiers() => _paste.ReleaseHeldModifiers();
        public ClipboardSwap TrySwapClipboard(string text, bool allowIncomplete) => _paste.TrySwapClipboard(text, allowIncomplete);
        public SwapStatus TryPlaceText(string text) => _paste.TryPlaceText(text);
        public DispatchState SendPaste()
        {
            _keysPending = true; // A throwing/partial native send still needs key-up recovery.
            var state = _paste.SendPaste();
            if (state != DispatchState.Uncertain) _keysPending = false;
            return state;
        }
        public bool ReleasePasteKeys()
        {
            var released = _paste.ReleasePasteKeys();
            if (released) _keysPending = false;
            return released;
        }
        public RestoreStatus TryRestoreClipboard(IClipboardSnapshot snapshot) => _paste.TryRestoreClipboard(snapshot);
        public void DiscardClipboardSnapshot(IClipboardSnapshot snapshot) => _paste.DiscardClipboardSnapshot(snapshot);
        public async Task<bool> DrainOwnedRecoveryAsync()
        {
            if (_keysPending && !ReleasePasteKeys()) return false;
            var restored = await _paste.RestorePendingAsync();
            return restored is RestoreStatus.Restored or RestoreStatus.ClipboardChanged;
        }
    }
}
