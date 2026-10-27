using SmartSnippets.Core.Settings;

namespace SmartSnippets.Presentation.Services;

public sealed record HotkeyChangeResult(bool Success, string Message);

public sealed record HotkeyStatus(bool IsRegistered, HotkeyGesture Gesture, string Message);

/// <summary>Owns the global hotkey. A change registers the new gesture before releasing the old one.</summary>
public interface IHotkeyManager
{
    HotkeyStatus Status { get; }

    event EventHandler? StatusChanged;

    HotkeyChangeResult TryChange(HotkeyGesture gesture);

    HotkeyChangeResult RestoreDefault();
}
