using Xunit;

namespace SmartSnippets.Uno;

public sealed class OwnedTargetPreparationTests
{
    [Theory]
    [InlineData("restore")] [InlineData("abort")] [InlineData("owner-refusal")]
    [InlineData("posted-enter")] [InlineData("copy")] [InlineData("preview")] [InlineData("expired")]
    public void SourceGateIsClosedIncludingControlScenes(string scene)
    {
        Assert.False(OwnedTargetPreparation.SourceReviewed);
        Assert.False(OwnedTargetPreparation.ShouldPrepare(scene));
        if (scene is "restore" or "abort" or "owner-refusal") Assert.False(PositiveProtocol.IsPositive(scene));
    }

    [Theory]
    [InlineData("foreign")] [InlineData("dead")] [InlineData("sibling")] [InlineData("editor")]
    [InlineData("class")] [InlineData("held")] [InlineData("recent")] [InlineData("expired")] [InlineData("desktop")]
    public async Task UnsafeInitialStateNeverCreatesCom(string reason)
    {
        var os = new Os();
        switch (reason)
        {
            case "foreign": os.Foreground = 99; break;
            case "held": case "recent": os.Quiet = false; break;
            case "expired": os.Permission = false; break;
            case "desktop": os.Desktop = false; break;
            default: os.Identity = false; break;
        }
        var calls = 0;
        var result = await Prepare(os, () => { calls++; return new Com(os); });
        Assert.Equal(OwnedPreparationResult.Refused, result);
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task AlreadyAStillChecksGuardsButNeverCreatesCom()
    {
        var os = new Os { Foreground = 2 };
        Assert.Equal(OwnedPreparationResult.AlreadyA, await Prepare(os, () => throw new Exception()));
        Assert.Equal(1, os.EditorCommands);
    }

    [Theory]
    [InlineData("create")] [InlineData("element")] [InlineData("focus")]
    public async Task PinRecheckedBeforeElementBeforeFocusAndAfterFocus(string phase)
    {
        var os = new Os(); var com = new Com(os);
        if (phase == "element") com.ElementAction = () => os.Identity = false;
        if (phase == "focus") com.FocusAction = () => os.Identity = false;
        Assert.Equal(OwnedPreparationResult.Refused, await Prepare(os, () =>
        {
            if (phase == "create") os.Identity = false;
            return com;
        }));
        Assert.Equal(phase == "create" ? 0 : 1, com.Elements);
        Assert.Equal(phase == "focus" ? 1 : 0, com.Focuses);
        Assert.True(com.Released); Assert.Equal(0, os.EditorCommands);
    }

    [Theory]
    [InlineData("element")] [InlineData("focus")] [InlineData("throw")]
    public async Task AutomationFailureReleasesAndNeverFallsBack(string phase)
    {
        var os = new Os(); var com = new Com(os) { ElementOk = phase != "element", FocusOk = false };
        if (phase == "throw") com.FocusAction = () => throw new InvalidOperationException();
        Assert.Equal(OwnedPreparationResult.AutomationFailed, await Prepare(os, () => com));
        Assert.True(com.Released); Assert.InRange(com.Focuses, 0, 1); Assert.Equal(0, os.EditorCommands);
    }

    [Fact]
    public async Task WrongPostFocusForegroundRefusesWithoutOwnedCommand()
    {
        var os = new Os(); var com = new Com(os) { FocusAction = () => os.Foreground = 99 };
        Assert.Equal(OwnedPreparationResult.ForegroundRefused, await Prepare(os, () => com));
        Assert.True(com.Released); Assert.Equal(1, com.Focuses); Assert.Equal(0, os.EditorCommands);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ChangedEpochRequiresFreshQuietAndRemainsBounded(bool neverQuiet)
    {
        var os = new Os(); var com = new Com(os) { FocusAction = () => { os.Epoch++; os.Quiet = false; } };
        os.OnDelay = () => { if (!neverQuiet && os.Milliseconds >= 1500) os.Quiet = true; };
        Assert.Equal(neverQuiet ? OwnedPreparationResult.QuietTimeout : OwnedPreparationResult.Prepared,
            await Prepare(os, () => com));
        Assert.InRange(os.Milliseconds, 1500, 3000); Assert.True(com.Released);
        Assert.Equal(1, com.Focuses); Assert.Equal(neverQuiet ? 0 : 1, os.EditorCommands);
    }

    [Theory]
    [InlineData("expired")] [InlineData("desktop")] [InlineData("held")] [InlineData("foreign")]
    public async Task ProviderResolutionCannotBypassFinalPreFocusGuard(string change)
    {
        var os = new Os();
        var com = new Com(os) { ElementAction = () =>
        {
            if (change == "expired") os.Permission = false;
            if (change == "desktop") os.Desktop = false;
            if (change == "held") os.Quiet = false;
            if (change == "foreign") os.Foreground = 99;
        } };
        Assert.Equal(OwnedPreparationResult.Refused, await Prepare(os, () => com));
        Assert.Equal(0, com.Focuses); Assert.True(com.Released);
    }

    [Fact]
    public async Task LostQuietOrPermissionDuringFreshWaitCannotContinue()
    {
        var os = new Os();
        var com = new Com(os) { FocusAction = () => os.Epoch++ };
        os.OnDelay = () => os.Permission = false;
        Assert.Equal(OwnedPreparationResult.Refused, await Prepare(os, () => com));
        Assert.Equal(0, os.EditorCommands); Assert.True(com.Released);
    }

    [Fact]
    public async Task FailedFinalSessionCheckRefuses()
    {
        var os = new Os();
        os.OnEditor = () => os.Identity = false;
        Assert.Equal(OwnedPreparationResult.Refused, await Prepare(os, () => new Com(os)));
    }

    [Fact]
    public async Task UnchangedEpochSuccessSingleCallAndFinalChecks()
    {
        var os = new Os(); var com = new Com(os);
        Assert.Equal(OwnedPreparationResult.Prepared, await Prepare(os, () => com));
        Assert.Equal(1, com.Elements); Assert.Equal(1, com.Focuses); Assert.True(com.Released);
        Assert.Equal(1, os.EditorCommands); Assert.Equal(0, os.Milliseconds);
    }

    private static Task<OwnedPreparationResult> Prepare(Os os, Func<IOwnedPreparationAutomation> com) =>
        OwnedTargetPreparation.PrepareAsync(os, com, 1, 2, 3, CancellationToken.None);
    private sealed class Os : IOwnedPreparationOs
    {
        internal bool Identity = true, Permission = true, Desktop = true, Quiet = true;
        internal uint Epoch = 10;
        internal int EditorCommands;
        internal Action? OnDelay, OnEditor;
        public nint Foreground { get; set; } = 1;
        public long Milliseconds { get; private set; }
        public bool PinnedIdentity() => Identity;
        public bool Permitted() => Permission;
        public bool VisibleDesktop() => Desktop;
        public bool QuietAllInput() => Quiet;
        public bool TryInputEpoch(out uint epoch) { epoch = Epoch; return true; }
        public Task DelayAsync(CancellationToken ct) { Milliseconds += 25; OnDelay?.Invoke(); return Task.CompletedTask; }
        public Task FocusEditorAsync(CancellationToken ct) { EditorCommands++; OnEditor?.Invoke(); return Task.CompletedTask; }
        public bool TargetForeground() => Foreground == 2;
    }
    private sealed class Com(Os os) : IOwnedPreparationAutomation
    {
        internal bool Released, ElementOk = true, FocusOk = true;
        internal int Elements, Focuses;
        internal Action? ElementAction, FocusAction;
        public bool ElementFromHandle(nint editor) { Assert.Equal(3, editor); Elements++; ElementAction?.Invoke(); return ElementOk; }
        public bool SetFocus() { Focuses++; os.Foreground = 2; FocusAction?.Invoke(); return FocusOk; }
        public void Dispose() => Released = true;
    }
}
