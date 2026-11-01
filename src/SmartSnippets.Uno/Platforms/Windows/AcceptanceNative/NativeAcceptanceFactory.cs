using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using SmartSnippets.Core.Settings;
using SmartSnippets.Presentation.Services;
using SmartSnippets.TargetIdentity018;
using SmartSnippets.WindowsInsertion018;

namespace SmartSnippets.Uno.Platforms.Windows.AcceptanceNative;

internal static class NativeAcceptanceFactory
{
    // Independent source review must explicitly clear this gate. No CLI/env/settings unlock.
    internal static readonly bool SourceReviewed = false;
    internal static void RequireReviewed()
    {
        if (!SourceReviewed || !OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763) || RuntimeInformation.ProcessArchitecture != Architecture.X64)
            throw new InvalidOperationException("Native acceptance is not reviewed or supported.");
    }

    internal static bool AllowsOperation(AcceptanceOperation operation, bool allowInsert, bool allowCopy, bool allowClipboardControl) => operation switch
    {
        AcceptanceOperation.Capture or AcceptanceOperation.Insert or AcceptanceOperation.Input or
            AcceptanceOperation.Clipboard or AcceptanceOperation.Shortcut => allowInsert,
        AcceptanceOperation.Copy => allowCopy,
        AcceptanceOperation.Focus => allowInsert || allowCopy,
        AcceptanceOperation.ClipboardControl => allowClipboardControl,
        AcceptanceOperation.MetadataCapture => allowInsert || allowCopy || allowClipboardControl,
        _ => false
    };

    internal static NativeAcceptanceSession Create(AcceptanceFixture fixture, string selectedWindow,
        nint ownedPopupWindow, string guardianPath, string guardianDigest, DateTimeOffset expires,
        bool allowInsert, bool allowCopy, Func<AcceptanceOperation, bool> permitted, bool allowClipboardControl = false, nint ownedMainWindow = 0, Guid? knownPriorToken = null)
    {
        RequireReviewed();
        if (expires <= DateTimeOffset.UtcNow || expires > DateTimeOffset.UtcNow.AddMinutes(2) ||
            !fixture.Lease.IsAliveAndPinned() || (!allowInsert && !allowCopy && !allowClipboardControl)) throw new InvalidOperationException("Acceptance session refused.");
        var started = Stopwatch.GetTimestamp();
        var lifetime = expires - DateTimeOffset.UtcNow;
        var closed = false;
        bool Permission(AcceptanceOperation operation) => !closed && Stopwatch.GetElapsedTime(started) < lifetime &&
            DateTimeOffset.UtcNow < expires && permitted(operation) &&
            AllowsOperation(operation, allowInsert, allowCopy, allowClipboardControl);
        var owned = fixture.Lease.CreateProbe();
        try
        {
            var probe = new GuardedTargetProbe(owned, Permission);
            var target = owned.Capture(selectedWindow);
            var popup = new NativeOwnedPopup(ownedPopupWindow);
            NativeOwnedPopup? main = null;
            try
            {
                main = ownedMainWindow == 0 ? null : new NativeOwnedPopup(ownedMainWindow);
                var channel = GuardianClient.Start(fixture, target, popup, guardianPath, guardianDigest, expires, allowInsert, allowCopy, allowClipboardControl, main, knownPriorToken);
                var focus = new OwnedFocus(probe, target, ownedPopupWindow, popup,
                    () => Permission(AcceptanceOperation.Capture) || Permission(AcceptanceOperation.Copy) || Permission(AcceptanceOperation.Insert),
                    () => Permission(AcceptanceOperation.Focus), ownedMainWindow, () => main?.PopupPinned == true,
                    reason => SmartSnippets.Core.Logging.AppLog.Info("native_popup_focus", $"reason={reason}"));
                return new(owned, probe, target, channel, new(probe, target, Permission, channel, new NativeAcceptanceInput(), focus),
                    focus, popup, Permission, () => closed = true, () => { try { main?.Dispose(); } finally { popup.Dispose(); } });
            }
            catch { try { main?.Dispose(); } finally { popup.Dispose(); } throw; }
        }
        catch { owned.Dispose(); throw; }
    }
}

internal sealed partial class NativeAcceptanceInput : IAcceptanceInput
{
    public bool Quiet(ITargetProbe probe, InsertionTarget target) => InputPreflight.QuietOwnedTarget(probe, target);
    public unsafe DispatchState SendPaste()
    {
        Input* inputs = stackalloc Input[4];
        inputs[0] = new() { Type = 1, Key = 0x11 };
        inputs[1] = new() { Type = 1, Key = 0x56 };
        inputs[2] = new() { Type = 1, Key = 0x56, Flags = 2 };
        inputs[3] = new() { Type = 1, Key = 0x11, Flags = 2 };
        var count = SendInput(4, inputs, sizeof(Input));
        return ClassifySendCount(count);
    }
    internal static DispatchState ClassifySendCount(uint count) =>
        count == 4 ? DispatchState.Dispatched : count == 0 ? DispatchState.NotDispatched : DispatchState.Uncertain;
    [StructLayout(LayoutKind.Explicit, Size = 40)] private struct Input
    { [FieldOffset(0)] public uint Type; [FieldOffset(8)] public ushort Key; [FieldOffset(12)] public uint Flags; }
    [LibraryImport("user32.dll")] private static unsafe partial uint SendInput(uint count, Input* inputs, int size);
}
