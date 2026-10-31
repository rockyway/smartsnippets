using System;
using SmartSnippets.Core.Settings;
using SmartSnippets.Presentation.Services;
using SmartSnippets.TargetIdentity018;
using SmartSnippets.Uno.Platforms.Windows.AcceptanceNative;
using Xunit;

namespace SmartSnippets.Uno.Tests.AcceptanceNative;

public sealed class AcceptanceHotkeyAndTargetTests
{
    private static readonly HotkeyGesture Selected = new(HotkeyGesture.ModControl | HotkeyGesture.ModAlt, 0x78);

    [Fact]
    public void DefaultOrUnselectedGesture_NeverRegistersOrUnregisters()
    {
        var os = new HotkeyOs();
        using var hotkey = new AcceptanceHotkey(Selected, () => true, os, () => { });
        Assert.False(hotkey.RestoreDefault().Success);
        Assert.False(hotkey.TryChange(HotkeyGesture.Default).Success);
        Assert.False(hotkey.TryChange(new(HotkeyGesture.ModAlt, 0x79)).Success);
        Assert.Equal(0, os.Registers);
        Assert.Equal(0, os.Unregisters);
    }

    [Fact]
    public void ConflictingRegistration_NeverReleasesAnothersShortcut()
    {
        var os = new HotkeyOs { RegisterResult = false };
        using (var hotkey = new AcceptanceHotkey(Selected, () => true, os, () => { }))
        {
            Assert.False(hotkey.TryChange(Selected).Success);
            Assert.False(hotkey.Status.IsRegistered);
        }
        Assert.Equal(1, os.Registers);
        Assert.Equal(0, os.Unregisters);
    }

    [Fact]
    public void CallbackChecksOwnedIdExactGestureAndFreshPermission()
    {
        var allowed = true; var callbacks = 0; var os = new HotkeyOs();
        var hotkey = new AcceptanceHotkey(Selected, () => allowed, os, () => callbacks++);
        Assert.True(hotkey.TryChange(Selected).Success);
        var data = (nint)((Selected.Key << 16) | Selected.Modifiers);
        Assert.False(hotkey.HandleMessage(0x312, 1, data));
        Assert.True(hotkey.HandleMessage(0x312, AcceptanceHotkey.RegistrationId, data + 1));
        Assert.Equal(0, callbacks);
        hotkey.HandleMessage(0x312, AcceptanceHotkey.RegistrationId, data);
        Assert.Equal(1, callbacks);
        allowed = false;
        hotkey.HandleMessage(0x312, AcceptanceHotkey.RegistrationId, data);
        Assert.Equal(1, callbacks);
        Assert.False(hotkey.Status.IsRegistered);
        hotkey.Dispose(); hotkey.Dispose();
        Assert.Equal(1, os.Unregisters); // Cleanup is not blocked by permission expiry.
    }

    [Fact]
    public void FailedUnregister_IsReportedAndRetryableNotForgotten()
    {
        var os = new HotkeyOs { UnregisterResult = false };
        var hotkey = new AcceptanceHotkey(Selected, () => true, os, () => { });
        hotkey.TryChange(Selected);
        Assert.Throws<InvalidOperationException>(hotkey.Dispose);
        os.UnregisterResult = true;
        hotkey.Dispose();
        Assert.Equal(2, os.Unregisters);
    }

    [Fact]
    public void OwnedProbe_RejectsArbitraryWindowSamePidAndRechecksRetainedLease()
    {
        var lease = new Lease(); var windows = new Windows();
        using var owned = new OwnedTargetProbe(lease, windows);
        var allowed = true;
        var guarded = new GuardedTargetProbe(owned, _ => allowed);
        var a = guarded.Capture("a"); var b = guarded.Capture("b");
        Assert.Equal(TargetCheck.Ok, guarded.Check(a));
        Assert.Equal(TargetCheck.Ok, guarded.Check(b));
        Assert.Equal(TargetCheck.Closed, guarded.Check(new(99, lease.ExpectedPid, 0, "")));
        Assert.Throws<InvalidOperationException>(() => guarded.Capture("arbitrary title"));
        lease.Pinned = false;
        Assert.Equal(TargetCheck.Closed, guarded.Check(a));
        lease.Pinned = true; allowed = false;
        var count = windows.Captures;
        Assert.Throws<InvalidOperationException>(() => guarded.Capture("a"));
        Assert.Equal(count, windows.Captures);
        Assert.Equal(TargetCheck.Closed, guarded.Check(a));
    }

    private sealed class HotkeyOs : IAcceptanceHotkeyOs
    {
        internal bool RegisterResult = true, UnregisterResult = true;
        internal int Registers, Unregisters;
        public bool Register(int id, HotkeyGesture gesture) { Registers++; return RegisterResult; }
        public bool Unregister(int id) { Unregisters++; return UnregisterResult; }
    }
    private sealed class Lease : IOwnedFixtureLease
    {
        internal bool Pinned = true;
        public uint ExpectedPid => checked((uint)Environment.ProcessId + 1);
        public bool IsAliveAndPinned() => Pinned;
    }
    private sealed class Windows : IExactWindowIdentity
    {
        internal int Captures;
        public nint Capture(string fixtureWindow) { Captures++; return fixtureWindow == "a" ? 11 : 12; }
        public bool Exists(nint identity) => identity is 11 or 12;
        public uint ProcessId(nint identity) => checked((uint)Environment.ProcessId + 1);
        public bool IsForeground(nint identity) => identity == 11;
        public void Dispose() { }
    }
}
