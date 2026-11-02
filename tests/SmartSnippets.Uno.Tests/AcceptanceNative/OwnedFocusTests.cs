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

    [Theory]
    [InlineData(false, true, true, "permission", 0)]
    [InlineData(true, false, true, "admission", 0)]
    [InlineData(true, true, false, "foreground_refused", 1)]
    [InlineData(true, true, true, "foreground_accepted", 1)]
    public void PopupFocusReceiptsPreserveSingleAttemptAndGuardOrder(bool permission, bool quiet, bool accepted, string reason, int calls)
    {
        var os = new FocusOs { Foreground = A.WindowHandle, Quiet = quiet, FocusAccepted = accepted };
        var receipts = new System.Collections.Generic.List<string>();
        var focus = new OwnedFocus(new Probe(os), A, Popup, os, () => true, () => permission, focusReceipt: receipts.Add);
        Assert.Equal(permission && quiet && accepted, focus.TryFocusPopup());
        Assert.Equal(new[] { reason }, receipts);
        Assert.Equal(calls, os.FocusCalls);
    }

    [Theory]
    [InlineData(11, true)] [InlineData(30, true)] [InlineData(40, true)]
    [InlineData(12, false)] [InlineData(99, false)]
    public async Task ChildForegroundUsesExactPinnedSurfaceForAdmissionPopupAndRestore(int root, bool admitted)
    {
        var os = new FocusOs { Foreground = 101, Normalize = raw =>
            SmartSnippets.TargetIdentity018.ForegroundIdentity.Normalize(raw,
                h => h == 101 ? 0x40000000 : 0, _ => root, _ => (7u, 21u), out _) };
        var focus = new OwnedFocus(new Probe(os), A, Popup, os, () => true, () => true, 40, () => true);
        Assert.Equal(admitted, focus.ReadyForAdmission());
        Assert.Equal(admitted, await focus.ReadyForAdmissionAsync(default));
        Assert.Equal(admitted, await focus.RestoreTargetAsync(default));
        Assert.Equal(admitted, focus.TryFocusPopup());
        Assert.Equal(admitted ? (root == 30 ? 2 : root == 11 ? 1 : 2) : 0, os.FocusCalls);
    }

    private sealed class Probe(FocusOs os) : ITargetProbe
    {
        public TargetCheck Check(InsertionTarget target) => target == A ? TargetCheck.Ok : TargetCheck.Closed;
        public bool IsTargetForeground(InsertionTarget target) => target == A && os.Foreground == A.WindowHandle;
    }
    private sealed class FocusOs : IOwnedFocusOs
    {
        internal bool Quiet = true, FocusAccepted = true;
        internal int FocusCalls;
        internal Action? AfterQuietCheck;
        public bool PopupPinned { get; set; } = true;
        private nint rawForeground;
        internal Func<nint, nint> Normalize = h => h;
        public nint Foreground { get => Normalize(rawForeground); set => rawForeground = value; }
        public bool QuietDesktop() { AfterQuietCheck?.Invoke(); return Quiet; }
        public bool SetForeground(nint window) { FocusCalls++; if (FocusAccepted) Foreground = window; return FocusAccepted; }
    }
}
