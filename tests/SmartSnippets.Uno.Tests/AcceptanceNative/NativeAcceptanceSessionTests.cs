using System;
using System.Threading.Tasks;
using SmartSnippets.Presentation.Services;
using SmartSnippets.TargetIdentity018;
using SmartSnippets.Uno.Platforms.Windows;
using SmartSnippets.Uno.Platforms.Windows.AcceptanceNative;
using Xunit;

namespace SmartSnippets.Uno.Tests.AcceptanceNative;

public sealed class NativeAcceptanceSessionTests
{
    [Fact]
    public void InsertAndFocusAdmission_AreNotPermissionToMutateBeforeIdleOrExactForeground()
    {
        using var fixture = new SessionFixture();
        fixture.Popup.Foreground = 30; fixture.Popup.Quiet = false;
        var session = fixture.Session;
        Assert.True(session.Validate(CandidateNativeOperation.Insert | CandidateNativeOperation.Focus,
            session.SessionId, session.Target.WindowHandle, session.Target.ProcessId));
        Assert.False(session.Validate(CandidateNativeOperation.Input, session.SessionId, session.Target.WindowHandle, session.Target.ProcessId));
        Assert.False(session.Validate(CandidateNativeOperation.Clipboard, session.SessionId, session.Target.WindowHandle, session.Target.ProcessId));
        Assert.Null(session.CaptureOwnedTarget(session.Target.WindowHandle, session.Target.ProcessId));
        Assert.False(session.TryFocusOwnedPopup());
        Assert.Equal(0, fixture.Popup.FocusCalls);
    }

    [Fact]
    public void CaptureAndCopy_ValidateFreshExactIdentityAndSeparateForegroundRules()
    {
        using var fixture = new SessionFixture(); var session = fixture.Session; var target = session.Target;
        Assert.Equal(target, session.CaptureOwnedTarget(target.WindowHandle, target.ProcessId));
        Assert.False(session.Validate(CandidateNativeOperation.Insert, Guid.NewGuid(), target.WindowHandle, target.ProcessId));
        Assert.False(session.Validate(CandidateNativeOperation.Insert, session.SessionId, target.WindowHandle + 1, target.ProcessId));
        Assert.False(session.Validate(CandidateNativeOperation.ClipboardLoss, session.SessionId, target.WindowHandle, target.ProcessId));
        fixture.Popup.Foreground = 30;
        Assert.Null(session.CaptureOwnedTarget(target.WindowHandle, target.ProcessId));
        Assert.True(session.Validate(CandidateNativeOperation.Copy, session.SessionId, target.WindowHandle, target.ProcessId));
        fixture.Popup.PopupPinned = false;
        Assert.False(session.Validate(CandidateNativeOperation.Copy, session.SessionId, target.WindowHandle, target.ProcessId));
    }

    [Fact]
    public async Task PendingForeignOwnership_BlocksDrainEvenAfterNoLocalSnapshotOrExpiredAdmission()
    {
        using var fixture = new SessionFixture();
        fixture.Guardian.Pending = true;
        fixture.Guardian.RestoreResult = RestoreStatus.ClipboardChanged;
        Assert.False(await fixture.Session.DrainOwnedRecoveryAsync());
        Assert.True(fixture.Session.RecoveryPending);
        Assert.True(fixture.Guardian.Closed);
        fixture.Guardian.RestoreResult = RestoreStatus.Restored;
        Assert.True(await fixture.Session.DrainOwnedRecoveryAsync());
    }

    [Fact]
    public async Task ClipboardControl_IsNoInputMandatoryRecoveryEvenWithForeignForeground()
    {
        using var fixture = new SessionFixture();
        fixture.Popup.Foreground = 999;
        Assert.Equal(SwapStatus.Swapped, fixture.Session.PublishClipboardControl());
        Assert.Equal(AcceptanceOperation.ClipboardControl, fixture.Guardian.LastOperation);
        Assert.Equal(0, fixture.Popup.FocusCalls);
        Assert.True(fixture.Session.RecoveryPending);
        Assert.Equal(SwapStatus.NotPreserved, fixture.Session.PublishClipboardControl());
        Assert.Equal(1, fixture.Guardian.Publications);
        Assert.True(await fixture.Session.DrainOwnedRecoveryAsync());
        Assert.False(fixture.Session.RecoveryPending);
        Assert.Equal(SwapStatus.NotPreserved, fixture.Session.PublishClipboardControl());
    }

    [Fact]
    public void ClipboardControl_RefusesHeldInputRevocationAndUnpinnedPopupBeforePublication()
    {
        using var fixture = new SessionFixture();
        fixture.Popup.Quiet = false;
        Assert.Equal(SwapStatus.NotPreserved, fixture.Session.PublishClipboardControl());
        fixture.Popup.Quiet = true; fixture.Allowed = false;
        Assert.Equal(SwapStatus.NotPreserved, fixture.Session.PublishClipboardControl());
        fixture.Allowed = true; fixture.Popup.PopupPinned = false;
        Assert.Equal(SwapStatus.NotPreserved, fixture.Session.PublishClipboardControl());
        Assert.Equal(0, fixture.Guardian.Publications);
        Assert.Equal(0, fixture.Popup.FocusCalls);
    }

    private sealed class SessionFixture : IDisposable
    {
        internal bool Allowed = true;
        internal PopupOs Popup = new();
        internal Channel Guardian = new();
        internal NativeAcceptanceSession Session;
        internal SessionFixture()
        {
            var owned = new OwnedTargetProbe(new Lease(), new Identity(Popup));
            var closed = false;
            bool Permitted(AcceptanceOperation _) => Allowed && !closed;
            var probe = new GuardedTargetProbe(owned, Permitted);
            var target = probe.Capture("a");
            var focus = new OwnedFocus(probe, target, 30, Popup, () => !closed, () => !closed);
            var paste = new GuardedPasteDispatcher(probe, target, Permitted, Guardian, new Input(Popup), focus);
            Session = new(owned, probe, target, Guardian, paste, focus, Popup, Permitted, () => closed = true,
                () => { }, () => true, () => Popup.Quiet);
        }
        public void Dispose() => Session.Dispose();
    }
    private sealed class Lease : IOwnedFixtureLease
    {
        public uint ExpectedPid => checked((uint)Environment.ProcessId + 1);
        public bool IsAliveAndPinned() => true;
    }
    private sealed class Identity(PopupOs popup) : IExactWindowIdentity
    {
        public nint Capture(string fixtureWindow) => fixtureWindow == "a" ? 11 : 12;
        public bool Exists(nint identity) => identity is 11 or 12;
        public uint ProcessId(nint identity) => checked((uint)Environment.ProcessId + 1);
        public bool IsForeground(nint identity) => popup.Foreground == identity;
        public void Dispose() { }
    }
    private sealed class PopupOs : IOwnedFocusOs
    {
        internal bool Quiet = true;
        internal int FocusCalls;
        public bool PopupPinned { get; set; } = true;
        public nint Foreground { get; set; } = 11;
        public bool QuietDesktop() => Quiet;
        public bool SetForeground(nint window) { FocusCalls++; Foreground = window; return true; }
    }
    private sealed class Input(PopupOs popup) : IAcceptanceInput
    {
        public bool Quiet(ITargetProbe probe, InsertionTarget target) => popup.Quiet && probe.IsTargetForeground(target);
        public DispatchState SendPaste() => DispatchState.Dispatched;
    }
    private sealed class Channel : IGuardianChannel
    {
        internal bool Pending, Closed;
        internal int Publications;
        internal AcceptanceOperation LastOperation;
        internal RestoreStatus RestoreResult = RestoreStatus.Restored;
        public string RecoveryControl => "fake";
        public bool IsPinned => true;
        public bool RecoveryPending => Pending;
        public bool CanPublish => !Pending;
        public bool SnapshotAcknowledged => true;
        public int SavedCount => 1;
        public long ByteCount => 4;
        public SwapStatus Publish(AcceptanceOperation operation)
        { Publications++; LastOperation = operation; Pending = true; return SwapStatus.Swapped; }
        public bool CommitCopy() { Pending = false; return true; }
        public RestoreStatus Restore() { if (RestoreResult == RestoreStatus.Restored) Pending = false; return RestoreResult; }
        public void CloseAdmission() => Closed = true;
        public void Dispose() { }
    }
}
