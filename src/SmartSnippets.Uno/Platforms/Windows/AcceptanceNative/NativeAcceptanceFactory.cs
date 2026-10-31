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

    internal static NativeAcceptanceSession Create(AcceptanceFixture fixture, string selectedWindow,
        nint ownedPopupWindow, string guardianPath, string guardianDigest, DateTimeOffset expires,
        bool allowInsert, bool allowCopy, Func<AcceptanceOperation, bool> permitted)
    {
        RequireReviewed();
        if (expires <= DateTimeOffset.UtcNow || expires > DateTimeOffset.UtcNow.AddMinutes(2) ||
            !fixture.Lease.IsAliveAndPinned() || (!allowInsert && !allowCopy)) throw new InvalidOperationException("Acceptance session refused.");
        var started = Stopwatch.GetTimestamp();
        var lifetime = expires - DateTimeOffset.UtcNow;
        var closed = false;
        bool Permission(AcceptanceOperation operation) => !closed && Stopwatch.GetElapsedTime(started) < lifetime &&
            DateTimeOffset.UtcNow < expires && permitted(operation) &&
            (operation is not (AcceptanceOperation.Insert or AcceptanceOperation.Input or AcceptanceOperation.Clipboard) || allowInsert) &&
            (operation != AcceptanceOperation.Copy || allowCopy);
        var owned = fixture.Lease.CreateProbe();
        try
        {
            var probe = new GuardedTargetProbe(owned, Permission);
            var target = probe.Capture(selectedWindow);
            var popup = new NativeOwnedPopup(ownedPopupWindow);
            try
            {
                var channel = GuardianClient.Start(fixture, target, popup, guardianPath, guardianDigest, expires, allowInsert, allowCopy);
                var focus = new OwnedFocus(probe, target, ownedPopupWindow, popup,
                    () => Permission(AcceptanceOperation.Capture) || Permission(AcceptanceOperation.Copy) || Permission(AcceptanceOperation.Insert),
                    () => Permission(AcceptanceOperation.Focus));
                return new(owned, probe, target, channel, new(probe, target, Permission, channel, new NativeAcceptanceInput(), focus),
                    focus, popup, Permission, () => closed = true, popup.Dispose);
            }
            catch { popup.Dispose(); throw; }
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
