using System;
using System.Threading;
using System.Threading.Tasks;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Uno.Platforms.Windows.AcceptanceNative;
using Xunit;

namespace SmartSnippets.Uno.Tests.AcceptanceNative;

public sealed class OwnedFocusTests
{
    private static readonly InsertionTarget A = new(11, 21, 0, "");
    private const int Popup = 30;

    [Fact]
    public async Task ForeignForeground_RefusesWithoutAnyFocusTransfer()
    {
        var os = new FocusOs { Foreground = 999 }; var probe = new Probe(os);
        var focus = new OwnedFocus(probe, A, Popup, os, () => true, () => true);
        Assert.False(await focus.ReadyForAdmissionAsync(default));
        Assert.False(await focus.RestoreTargetAsync(default));
        Assert.False(focus.TryFocusPopup());
        Assert.Equal(0, os.FocusCalls);
    }

    [Fact]
    public async Task ExactPinnedPopupAndFixture_CanTransferBothWaysWithoutAttachOrInput()
    {
        var os = new FocusOs { Foreground = Popup }; var probe = new Probe(os);
        var focus = new OwnedFocus(probe, A, Popup, os, () => true, () => true);
        Assert.True(await focus.RestoreTargetAsync(default));
        Assert.Equal(A.WindowHandle, os.Foreground);
        Assert.True(focus.TryFocusPopup());
        Assert.Equal((nint)Popup, os.Foreground);
        Assert.Equal(2, os.FocusCalls);
    }

    [Fact]
    public async Task DestroyedOrReusedPopup_RefusesBeforeFocus()
    {
        var os = new FocusOs { Foreground = Popup, PopupPinned = false }; var probe = new Probe(os);
        var focus = new OwnedFocus(probe, A, Popup, os, () => true, () => true);
        Assert.False(await focus.RestoreTargetAsync(default));
        Assert.False(focus.TryFocusPopup());
        Assert.Equal(0, os.FocusCalls);
    }

    [Fact]
    public async Task QuietWait_RechecksExpiryAndCancellationWithoutMutationRetry()
    {
        var allowed = true;
        var os = new FocusOs { Foreground = Popup, Quiet = false, AfterQuietCheck = () => allowed = false };
        var focus = new OwnedFocus(new Probe(os), A, Popup, os, () => allowed, () => allowed);
        Assert.False(await focus.RestoreTargetAsync(default));
        Assert.Equal(0, os.FocusCalls);
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        allowed = true;
        Assert.False(await focus.ReadyForAdmissionAsync(cancel.Token));
        Assert.Equal(0, os.FocusCalls);
    }

    [Fact]
    public async Task CopyOnlyAdmission_DoesNotGrantNativeFocus()
    {
        var os = new FocusOs { Foreground = Popup };
        var focus = new OwnedFocus(new Probe(os), A, Popup, os, () => true, () => false);
        Assert.True(focus.ReadyForAdmission());
        Assert.False(await focus.RestoreTargetAsync(default));
        Assert.False(focus.TryFocusPopup());
        Assert.Equal(0, os.FocusCalls);
    }

    [Fact]
    public async Task ExactPinnedMain_AllowsCopyAdmissionAndRestore_NotReusedMain()
    {
        var pinned = true;
        var os = new FocusOs { Foreground = 40 };
        var focus = new OwnedFocus(new Probe(os), A, Popup, os, () => true, () => true, 40, () => pinned);
        Assert.True(focus.ReadyForAdmission());
        Assert.True(await focus.RestoreTargetAsync(default));
        Assert.Equal(1, os.FocusCalls);
        os.Foreground = 40; pinned = false;
        Assert.False(focus.ReadyForAdmission());
        Assert.False(await focus.RestoreTargetAsync(default));
        Assert.Equal(1, os.FocusCalls);
    }

    [Fact]
    public async Task TransientZero_PollsUntilOwnedForeground_BeforeAnyTransfer()
    {
        var os = new FocusOs { Foreground = 0 };
        var focus = new OwnedFocus(new Probe(os), A, Popup, os, () => true, () => true);
        var restore = focus.RestoreTargetAsync(default);
        Assert.False(restore.IsCompleted);
        Assert.Equal(0, os.FocusCalls);
        os.Foreground = Popup;
        Assert.True(await restore);
        Assert.Equal(1, os.FocusCalls);
    }

    [Fact]
    public async Task PersistentZero_IsBoundedAndNeverFocuses_ForeignAfterZeroRefuses()
    {
        var os = new FocusOs { Foreground = 0 };
        var focus = new OwnedFocus(new Probe(os), A, Popup, os, () => true, () => true);
        var watch = System.Diagnostics.Stopwatch.StartNew();
        Assert.False(await focus.RestoreTargetAsync(default));
        Assert.InRange(watch.ElapsedMilliseconds, 2900, 6000);
        Assert.Equal(0, os.FocusCalls);
        var restore = focus.RestoreTargetAsync(default);
        os.Foreground = 999;
        Assert.False(await restore);
        Assert.Equal(0, os.FocusCalls);
    }

    private sealed class Probe(FocusOs os) : ITargetProbe
    {
        public TargetCheck Check(InsertionTarget target) => target == A ? TargetCheck.Ok : TargetCheck.Closed;
        public bool IsTargetForeground(InsertionTarget target) => target == A && os.Foreground == A.WindowHandle;
    }
    private sealed class FocusOs : IOwnedFocusOs
    {
        internal bool Quiet = true;
        internal int FocusCalls;
        internal Action? AfterQuietCheck;
        public bool PopupPinned { get; set; } = true;
        public nint Foreground { get; set; }
        public bool QuietDesktop() { AfterQuietCheck?.Invoke(); return Quiet; }
        public bool SetForeground(nint window) { FocusCalls++; Foreground = window; return true; }
    }
}
