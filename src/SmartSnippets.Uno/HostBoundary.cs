using SmartSnippets.Core.Settings;
using SmartSnippets.Presentation.Navigation;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Presentation.ViewModels;

namespace SmartSnippets.Uno;

internal interface IMainWindow : IDisposable
{
    nint NativeHandle => 0; // Only the exact app-owned surface, never an external target.
    event EventHandler? HiddenByClose;
    Task NavigateAsync(PageKind page, bool show);
    Task<bool> CanExitAsync();
    void SetInteractionEnabled(bool enabled);
    void SetHotkeyStatus(HotkeyStatus status) { }
}
internal interface IPopupWindow : IPopupHost, IDisposable
{
    event EventHandler? Closed;
    event EventHandler? Hidden;
    event EventHandler<bool>? PresentationChanged;
    void Prewarm();
    void ShowPreview(string? query, bool openSortMenu);
    // A head without a concrete captured-target surface must not advertise native readiness.
    nint NativeHandle => 0; // Owned-surface metadata only; not capture or focus permission.
    bool SupportsCapturedPopup => false;
    bool IsCaptureSurfaceReady => false; // Read-only warmup metadata, never native permission.
    void ShowCaptured(InsertionTarget target) => throw new NotSupportedException("Captured popup is unavailable.");
}
internal interface ITray : INotifier, IDisposable
{
    bool TryNotify(string title, string message) { Notify(title, message); return true; }
    void SetHotkeyState(bool registered, bool popupVisible) { }
}

/// <summary>Only external UI/OS boundaries are substituted; UnoAppHost always composes the real store and VMs.</summary>
internal abstract class HostBoundary
{
    internal abstract IUiDispatcher Dispatcher { get; }
    internal abstract IThemeService CreateTheme(AppSettings settings);
    internal abstract IDisposable CreateMessageWindow();
    internal virtual IDisposable? CreateIdleMemoryTrimmer(Func<bool> idle) => null;
    internal virtual Platforms.Windows.IDesktopServices? CreateDesktopServices(UnoAppHost host, AppSettings settings, Action hotkey) => null;
    internal abstract IDialogService CreateDialogs(bool popup, ITextSizeService textSize);
    internal abstract ISnippetFileService CreateFiles();
    internal abstract IMainWindow CreateMain(UnoAppHost host);
    internal abstract IPopupWindow CreatePopup(UnoAppHost host);
    internal abstract ITray CreateTray(Action open, Action<nint> preview, Action settings, Action exit);
    internal abstract void ReportStartupFailure();
    internal abstract void Exit();
}
