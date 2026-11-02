using SmartSnippets.Core.Settings;
using SmartSnippets.Presentation.Services;

namespace SmartSnippets.Uno.Platforms.Windows;

internal sealed class CandidateStartupRegistration : IStartupRegistration
{
    private const string Message = "Start at login is unavailable in this app; no registration was changed.";
    public bool IsEnabled() => false;
    public StartupResult Enable() => new(false, Message);
    public StartupResult Disable() => new(false, Message);
}
internal sealed class CandidateHotkeyManager(HotkeyGesture gesture) : IHotkeyManager
{
    private const string Message = "Global shortcuts are unavailable in this app. Use tray preview.";
    public HotkeyStatus Status => new(false, gesture, Message);
    public event EventHandler? StatusChanged { add { } remove { } }
    public HotkeyChangeResult TryChange(HotkeyGesture value) => new(false, Message);
    public HotkeyChangeResult RestoreDefault() => new(false, Message);
}
internal sealed class CandidateElevation : IElevation
{
    public bool IsElevated => false;
    public ElevationResult RestartAsAdministrator() => new(ElevationOutcome.Failed,
        "Administrator restart is unavailable in this app.");
}
