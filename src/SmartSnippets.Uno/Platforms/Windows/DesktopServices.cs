using SmartSnippets.Presentation.Services;

namespace SmartSnippets.Uno.Platforms.Windows;

/// <summary>D80 ordinary Windows product services, independent of the closed acceptance harness.</summary>
internal interface IDesktopServices : IDisposable
{
    IDailyOperations Operations { get; }
    IHotkeyManager Hotkeys { get; }
    IStartupRegistration Startup { get; }
    IElevation Elevation { get; }
    bool StartupMigrationFailed => false;
}

internal interface IClipboardOperations : ITargetProbe, IPasteDispatcher, IClipboardService
{
    Task<bool> DrainOwnedRecoveryAsync();
    Task<bool> WaitForCopyAdmissionAsync(CancellationToken ct) => Task.FromResult(false);
    bool TryFocusOwnedPopup() => false;
}

internal interface IDailyOperations : IClipboardOperations
{
    bool HasPendingRecovery => false;
    InsertionTarget? CaptureForeground();
    InsertionTarget? CaptureFrom(nint window) => null;
}
