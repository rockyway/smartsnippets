using System;
using System.Runtime.InteropServices;
using SmartSnippets.Core.Settings;
using SmartSnippets.Presentation.Services;

namespace SmartSnippets.Uno.Platforms.Windows.AcceptanceNative;

internal interface IAcceptanceHotkeyOs
{
    bool Register(int id, HotkeyGesture gesture);
    bool Unregister(int id);
}

internal sealed class AcceptanceHotkey(HotkeyGesture selected, Func<bool> permitted,
    IAcceptanceHotkeyOs os, Action callback) : IHotkeyManager, IDisposable
{
    internal const int RegistrationId = 0x519;
    private bool registered, closed;
    public HotkeyStatus Status => new(registered && !closed && permitted(), selected,
        registered && !closed && permitted() ? "Owned acceptance shortcut registered." : "Acceptance shortcut unavailable.");
    public event EventHandler? StatusChanged;
    public HotkeyChangeResult TryChange(HotkeyGesture gesture)
    {
        if (closed || !permitted() || !gesture.IsValid || gesture != selected || gesture == HotkeyGesture.Default)
            return new(false, "Only the explicitly selected nonproduction acceptance shortcut is allowed.");
        if (registered) return new(true, "Owned acceptance shortcut registered.");
        // A conflict returns false. No unregister is attempted on failed registration.
        registered = os.Register(RegistrationId, selected);
        StatusChanged?.Invoke(this, EventArgs.Empty);
        return new(registered, registered ? "Owned acceptance shortcut registered." : "Shortcut conflict or unavailable.");
    }
    public HotkeyChangeResult RestoreDefault() => new(false, "Production shortcut is unavailable in acceptance.");
    internal bool HandleMessage(uint message, nuint id, nint data)
    {
        if (message != 0x0312 || id != RegistrationId) return false;
        var packed = unchecked((nuint)data);
        if (registered && !closed && permitted() && (uint)(packed & 0xffff) == selected.Modifiers &&
            (uint)((packed >> 16) & 0xffff) == selected.Key) callback();
        return true;
    }
    public void Dispose()
    {
        closed = true;
        if (registered)
        {
            if (!os.Unregister(RegistrationId)) throw new InvalidOperationException("Owned shortcut cleanup failed.");
            registered = false;
        }
        StatusChanged?.Invoke(this, EventArgs.Empty);
    }
}

internal sealed partial class NativeAcceptanceHotkey(nint window) : IAcceptanceHotkeyOs
{
    private bool OwnedWindow() => window != 0 && GetWindowThreadProcessId(window, out var pid) == GetCurrentThreadId() && pid == (uint)Environment.ProcessId;
    public bool Register(int id, HotkeyGesture gesture) => OwnedWindow() && RegisterHotKey(window, id,
        gesture.Modifiers | HotkeyGesture.ModNoRepeat, gesture.Key) != 0;
    public bool Unregister(int id) => OwnedWindow() && UnregisterHotKey(window, id) != 0;
    [LibraryImport("user32.dll")] private static partial int RegisterHotKey(nint window, int id, uint modifiers, uint key);
    [LibraryImport("user32.dll")] private static partial int UnregisterHotKey(nint window, int id);
    [LibraryImport("user32.dll")] private static partial uint GetWindowThreadProcessId(nint window, out uint pid);
    [LibraryImport("kernel32.dll")] private static partial uint GetCurrentThreadId();
}
