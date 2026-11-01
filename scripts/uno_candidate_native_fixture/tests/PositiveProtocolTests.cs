using SmartSnippets.Presentation.Services;
using SmartSnippets.Uno.Platforms.Windows;
using Xunit;

namespace SmartSnippets.Uno;

public sealed class PositiveProtocolTests
{
    [Theory]
    [InlineData("registered-enter", 0, 1, 25)]
    [InlineData("posted-enter", 1, 0, 5)]
    [InlineData("posted-row", 1, 0, 5)]
    [InlineData("posted-main", 1, 0, 5)]
    public async Task PositiveShortcutRoute_OperatorSceneNeverPostsAndHasBoundedWait(string scene, int posts, int readies, int seconds)
    {
        var posted = 0; var ready = 0;
        var timeout = await PositiveShortcutRoute.BeginAsync(scene, () => posted++, () => { ready++; return Task.CompletedTask; });
        Assert.Equal(posts, posted); Assert.Equal(readies, ready);
        Assert.Equal(TimeSpan.FromSeconds(seconds), timeout);
        Assert.Equal(scene is "registered-enter" or "posted-enter", PositiveShortcutRoute.NativeEnter(scene));
    }
    [Fact]
    public void PositiveShortcutRoute_OnlyFirstCallbackCanReachAdmission()
    {
        var route = new PositiveShortcutRoute();
        Assert.True(route.AcceptCallback());
        Assert.False(route.AcceptCallback()); Assert.False(route.AcceptCallback());
        Assert.Equal(3, route.Callbacks);
    }
    [Fact]
    public void PositiveProtocol_RegisteredEnterUsesExactInsertionPermissionsAndInputGate()
    {
        Assert.True(PositiveProtocol.Inserts("registered-enter"));
        Assert.True(PositiveProtocol.RequiresInputReview("registered-enter"));
        Assert.Equal(PositiveProtocol.Permissions("posted-enter"), PositiveProtocol.Permissions("registered-enter"));
        Assert.Throws<InvalidOperationException>(() => PositiveProtocol.RequireReviewed("registered-enter"));
    }
    [Fact]
    public async Task PositiveShortcutRoute_ReadinessFailureNeverFallsBackToPosting()
    {
        Assert.Equal("registered-shortcut-ready", PositiveShortcutRoute.ReadyFile);
        Assert.Equal("registered_os_shortcut_operator_ready\n", PositiveShortcutRoute.ReadyReceipt);
        await Assert.ThrowsAsync<IOException>(() => PositiveShortcutRoute.BeginAsync("registered-enter",
            () => throw new Exception("must never post"), () => throw new IOException("publication refused")));
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PositiveProtocol_PriorPermitIsVersionedExactAndUsesDotNetGuidBytes(bool positive)
    {
        var token = new Guid("00112233-4455-6677-8899-aabbccddeeff");
        var legacy = System.Text.Encoding.ASCII.GetBytes(positive ? PositiveProtocol.Permit : PositiveProtocol.ControlPermit);
        Assert.Null(PositiveProtocol.ReadPriorToken(legacy, positive));
        var header = System.Text.Encoding.ASCII.GetBytes(positive ? PositiveProtocol.PriorPositivePermit : PositiveProtocol.PriorControlPermit);
        var frame = header.Concat(token.ToByteArray()).ToArray();
        Assert.Equal(token, PositiveProtocol.ReadPriorToken(frame, positive));
        foreach (var malformed in new[] { frame[..^1], frame.Concat(new byte[1]).ToArray(),
            header.Concat(new byte[16]).ToArray(), legacy.Concat(token.ToByteArray()).ToArray() })
            Assert.Throws<InvalidOperationException>(() => PositiveProtocol.ReadPriorToken(malformed, positive));
        Assert.Throws<InvalidOperationException>(() => PositiveProtocol.ReadPriorToken(frame, !positive));
    }

    [Theory]
    [InlineData("registered-enter")] [InlineData("posted-enter")] [InlineData("posted-row")] [InlineData("posted-main")]
    [InlineData("copy")] [InlineData("preview")] [InlineData("expired")]
    public void PositiveProtocol_ReviewGatesStayClosedEvenForKnownScenes(string scene)
    {
        Assert.True(PositiveProtocol.IsPositive(scene));
        Assert.False(PositiveProtocol.SourceReviewed);
        Assert.False(PositiveProtocol.InputSourceReviewed);
        Assert.Throws<InvalidOperationException>(() => PositiveProtocol.RequireReviewed(scene));
        Assert.Equal("native_result=" + scene + "_settled", PositiveProtocol.Result(scene));
        Assert.Equal((CandidateNativeOperation)0, PositiveProtocol.Permissions(scene) & CandidateNativeOperation.ClipboardLoss);
    }
    [Fact]
    public void PositiveProtocol_ExpiredHasFullBoundedPermissionsAndRequiresInputReview()
    {
        Assert.True(PositiveProtocol.RequiresInputReview("expired"));
        Assert.Equal(PositiveProtocol.Permissions("posted-enter") | CandidateNativeOperation.Copy,
            PositiveProtocol.Permissions("expired"));
        Assert.False(PositiveProtocol.RequiresInputReview("copy"));
        Assert.False(PositiveProtocol.RequiresInputReview("preview"));
    }
    [Theory]
    [InlineData("positive_surfaces 1 2 3 4", true)]
    [InlineData("positive_surfaces 1 1 3 4", false)]
    [InlineData("positive_surfaces 0 2 3 4", false)]
    [InlineData("positive_surfaces 1 2 3 4 extra", false)]
    [InlineData("positive_surfaces -1 2 3 4", false)]
    public void PositiveProtocol_ExactDistinctOwnedSurfacesOnly(string line, bool valid)
    {
        if (valid) PositiveProtocol.RequireSurfaces(line);
        else Assert.Throws<IOException>(() => PositiveProtocol.RequireSurfaces(line));
    }
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    public void PositiveInputPreflight_EveryGuardMustPassBeforeNativeDispatch(int refused)
    {
        var gates = Enumerable.Range(0, 4).Select(index => (Func<bool>)(() => index != refused)).ToArray();
        Assert.False(PositiveInputPreflight.Ready(gates[0], gates[1], gates[2], gates[3]));
    }
    [Fact]
    public void PositiveInputPreflight_RechecksLeaseThenExactPinAndForegroundLast()
    {
        var trace = new List<string>();
        Assert.True(PositiveInputPreflight.Ready(() => { trace.Add("lease"); return true; },
            () => { trace.Add("desktop"); return true; }, () => { trace.Add("quiet_all_vk_up"); return true; },
            () => { trace.Add("exact_pinned_foreground"); return true; }));
        Assert.Equal(new[] { "lease", "desktop", "quiet_all_vk_up", "lease", "exact_pinned_foreground" }, trace);
        var checks = 0;
        Assert.False(PositiveInputPreflight.Ready(() => ++checks == 1, () => true, () => true,
            () => throw new Exception("expired lease must refuse before dispatch")));
    }
    [Fact]
    public void PositiveEnterGuard_RefusalDoesNotDispatchAndThrowRetainsKeyboardObligation()
    {
        var guard = new PositiveEnterGuard();
        var calls = 0;
        Assert.Equal(DispatchState.NotDispatched, guard.Send(() => false, () => { calls++; return DispatchState.Dispatched; }));
        Assert.Equal(0, calls); Assert.False(guard.Pending);
        Assert.Throws<IOException>(() => guard.Send(() => true, () => throw new IOException()));
        Assert.True(guard.Pending);
        Assert.Equal(DispatchState.NotDispatched, guard.Send(() => true, () => throw new Exception("must not repeat")));
    }
    [Theory]
    [InlineData(0, DispatchState.NotDispatched, false)]
    [InlineData(1, DispatchState.Uncertain, true)]
    [InlineData(2, DispatchState.Dispatched, false)]
    [InlineData(3, DispatchState.Uncertain, true)]
    public void PositiveEnterGuard_PartialCountsNeverRepairKeys(uint count, DispatchState expected, bool pending)
    {
        var guard = new PositiveEnterGuard();
        Assert.Equal(expected, guard.Send(() => true, () => PositiveEnterGuard.Classify(count)));
        Assert.Equal(pending, guard.Pending);
    }
    [Theory]
    [InlineData(false, false)] // Known zero native Enter events.
    [InlineData(true, false)] // Enter delivered, insertion focus refused with no pending clipboard.
    [InlineData(true, true)] // Actual uncertain paste remains pending regardless of clipboard restoration.
    public async Task PositiveKeyboardLifetime_FailedReadbackSettlesOnlyKnownSafeInput(bool dispatched, bool pastePending)
    {
        var enter = new PositiveEnterGuard();
        enter.Send(() => true, () => dispatched ? DispatchState.Dispatched : DispatchState.NotDispatched);
        var keyboard = new PositiveKeyboardLifetime(); keyboard.Arm();
        var failed = false; var receipts = 0;
        try { throw new IOException("readback failed"); }
        catch
        {
            failed = true;
            var settled = await keyboard.DrainAndSettleAsync(() => { }, () => Task.CompletedTask,
                () => Task.CompletedTask, () => enter.Pending || pastePending,
                () => { receipts++; return Task.CompletedTask; });
            Assert.Equal(!pastePending, settled);
        }
        Assert.True(failed); Assert.Equal(pastePending, keyboard.Armed);
        Assert.Equal(pastePending ? 0 : 1, receipts);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task PositiveKeyboardLifetime_PartialOrThrowNeverSettlesAfterClipboardRecovery(bool throws)
    {
        var enter = new PositiveEnterGuard();
        try { enter.Send(() => true, () => throws ? throw new IOException() : DispatchState.Uncertain); }
        catch (IOException) { }
        var keyboard = new PositiveKeyboardLifetime(); keyboard.Arm();
        var restores = 0;
        for (var attempt = 0; attempt < 3; attempt++)
            Assert.False(await keyboard.DrainAndSettleAsync(() => { }, () => Task.CompletedTask,
                () => { restores++; return Task.CompletedTask; }, () => enter.Pending,
                () => throw new Exception("must not settle")));
        Assert.True(keyboard.Armed); Assert.Equal(3, restores);
    }

    [Fact]
    public async Task PositiveKeyboardLifetime_ThrowingDrainCannotAuthorizeGuardianRestore()
    {
        var keyboard = new PositiveKeyboardLifetime(); keyboard.Arm();
        Assert.False(await keyboard.DrainAndSettleAsync(() => { }, () => throw new IOException(),
            () => throw new Exception("must not restore before drain"), () => false,
            () => throw new Exception("must not settle")));
        Assert.True(keyboard.Armed);
    }

    [Fact]
    public async Task PositiveKeyboardLifetime_OnlyExplicitCompletionReleasesRetention()
    {
        var keyboard = new PositiveKeyboardLifetime();
        Assert.Throws<InvalidOperationException>(keyboard.Settle);
        keyboard.Arm();
        var waits = 0;
        var notifications = 0;
        await keyboard.RetainAsync(() => notifications++, () =>
        {
            Assert.True(keyboard.Armed);
            if (++waits == 3) keyboard.Settle();
            return Task.CompletedTask;
        });
        Assert.Equal(3, waits); Assert.Equal(1, notifications);
        Assert.Throws<InvalidOperationException>(keyboard.Arm);
    }
}
