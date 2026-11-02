using SmartSnippets.Presentation.Services;
using SmartSnippets.Uno.Platforms.Windows;
using SmartSnippets.Uno.Tests.Composition;
using SmartSnippets.Uno.Tests.Wiring;

namespace SmartSnippets.Uno.Tests.Flows;

public sealed class AdministratorRestartFlowTests
{
    [Fact]
    public Task DirtyMainCancelPrecedesUacAndKeepsHostUsable() => CandidateHostThread.RunAsync(async () =>
    {
        using var f = new HostFixture(); var native = new ActionAuditNative(); var elevation = new Elevation(native.Trace);
        f.Boundary.DesktopFactory = (_, _) => new Desktop(native, elevation);
        await f.Start(); await f.Host.Snippets.NewAsync(); f.Host.Snippets.Editor!.Text = "synthetic draft";
        f.Boundary.Dialogs.Choices.Enqueue(SaveChoice.Cancel);
        var result = await f.Host.RestartAsAdministratorAsync();
        Assert.Equal(ElevationOutcome.Cancelled, result.Outcome);
        Assert.Equal(new[] { "drain" }, native.Trace);
        Assert.Equal(0, elevation.Calls); Assert.Equal(0, f.Boundary.Exits);
        Assert.True(f.Boundary.Main.InteractionEnabled); Assert.True(f.Host.Snippets.CanCopyRow);
        Assert.True(f.Host.Snippets.Editor.IsDirty); Assert.Equal("synthetic draft", f.Host.Snippets.Editor.Text);
    });

    [Fact]
    public Task PopupCancelRecoversEditorBeforeAnyUacAndCanRetryWithSave() => CandidateHostThread.RunAsync(async () =>
    {
        using var f = new HostFixture(); var native = new ActionAuditNative(); var elevation = new Elevation(native.Trace);
        f.Boundary.DesktopFactory = (_, _) => new Desktop(native, elevation);
        await f.Start(); f.Host.ShowPreview(); f.Host.Popup.AddSnippetCommand.Execute(null);
        f.Host.Popup.Editor!.Text = "synthetic popup draft";
        f.Boundary.Dialogs.Choices.Enqueue(SaveChoice.Cancel);
        await f.Host.RestartAsAdministratorAsync();
        Assert.Equal(0, elevation.Calls); Assert.Equal(0, f.Boundary.Exits);
        Assert.True(f.Host.Popup.Editor.IsDirty); Assert.Equal(2, f.Boundary.Popups[0].Shows);
        f.Boundary.Dialogs.Choices.Enqueue(SaveChoice.Save);
        var result = await f.Host.RestartAsAdministratorAsync();
        Assert.Equal(ElevationOutcome.Started, result.Outcome);
        Assert.Equal(new[] { "drain", "drain", "uac" }, native.Trace);
        Assert.Equal("synthetic popup draft", Assert.Single(f.Host.Store.Snippets).Snippet.Text);
        Assert.Equal(1, f.Boundary.Exits); Assert.Equal(1, f.Boundary.Main.Disposals);
        Assert.False(f.Host.Gate.IsAvailable);
    });

    [Theory]
    [InlineData(ElevationOutcome.Cancelled)]
    [InlineData(ElevationOutcome.Failed)]
    public Task UacCancelOrFailureResumesAdmissionWithoutExit(ElevationOutcome outcome) => CandidateHostThread.RunAsync(async () =>
    {
        using var f = new HostFixture(); var native = new ActionAuditNative(); var elevation = new Elevation(native.Trace) { Outcome = outcome };
        f.Boundary.DesktopFactory = (_, _) => new Desktop(native, elevation);
        await f.Start();
        await f.Host.SettingsPage.RestartAsAdministratorCommand.ExecuteAsync(null);
        Assert.Equal(new[] { "drain", "uac" }, native.Trace);
        Assert.Equal(0, f.Boundary.Exits); Assert.Equal(0, f.Boundary.TrayDisposals);
        Assert.True(f.Boundary.Main.InteractionEnabled); Assert.True(f.Host.Gate.IsAvailable);
        elevation.Outcome = ElevationOutcome.Started;
        await f.Host.SettingsPage.RestartAsAdministratorCommand.ExecuteAsync(null);
        f.Boundary.Drain(); // VM exit request after Started must not duplicate disposal.
        Assert.Equal(1, f.Boundary.Exits); Assert.Equal(1, f.Boundary.TrayDisposals);
        Assert.Equal(2, elevation.Calls);
    });

    [Fact]
    public Task PendingRecoveryPreventsUacAndProfileReleaseUntilRetrySettles() => CandidateHostThread.RunAsync(async () =>
    {
        using var f = new HostFixture(); var native = new ActionAuditNative { RecoveryResult = false };
        var elevation = new Elevation(native.Trace);
        f.Boundary.DesktopFactory = (_, _) => new Desktop(native, elevation);
        await f.Start(); await f.Host.RestartAsAdministratorAsync();
        Assert.Equal(0, elevation.Calls); Assert.Equal(0, f.Boundary.Main.Disposals); Assert.Equal(0, f.Boundary.Exits);
        native.RecoveryResult = true; await f.Host.RestartAsAdministratorAsync();
        Assert.Equal(1, elevation.Calls); Assert.Equal(1, f.Boundary.Exits);
    });

    [Fact]
    public void HandoffArgumentsPreserveBackgroundAndParseOnlyValidPid()
    {
        Assert.Equal("--after-pid 123 --background", AdministratorRestart.Arguments(123, true));
        Assert.Equal("--after-pid 123", AdministratorRestart.Arguments(123, false));
        Assert.Equal(123, AdministratorRestart.PreviousProcessId(["app", "--AFTER-PID", "123", "--background"]));
        foreach (var args in new[] { Array.Empty<string>(), new[] { "--after-pid" }, new[] { "--after-pid", "bad" },
            new[] { "--after-pid", "-1" }, new[] { "--after-pid", "0" }, new[] { "--after-pid", "9999999999999" } })
            Assert.Null(AdministratorRestart.PreviousProcessId(args));
    }

    private sealed class Elevation(List<string> trace) : IElevation
    {
        internal ElevationOutcome Outcome = ElevationOutcome.Started;
        internal int Calls;
        public bool IsElevated => false;
        public ElevationResult RestartAsAdministrator() { Calls++; trace.Add("uac"); return new(Outcome, "synthetic outcome"); }
    }

    private sealed class Desktop(ActionAuditNative native, IElevation elevation) : ICandidateDesktopServices
    {
        public ICandidateDailyOperations Operations => native;
        public IHotkeyManager Hotkeys { get; } = new CandidateHotkeyManager(SmartSnippets.Core.Settings.HotkeyGesture.Default);
        public IStartupRegistration Startup { get; } = new CandidateStartupRegistration();
        public IElevation Elevation => elevation;
        public void Dispose() { }
    }
}
