using Xunit;
using SmartSnippets.Presentation.Services;
using SmartSnippets.TargetIdentity018;
using SmartSnippets.WindowsInsertion018;

namespace SmartSnippets.WindowsInsertion018.Tests;

public sealed class ClipboardOnlyWorkerFlowTests
{
    [Fact]
    public async Task DesktopWaitNeverGrantsPermissionWhenGuardRemainsFalse()
    {
        var probes = 0; var waits = 0;
        Assert.False(await ClipboardOnlyWorkerFlow.WaitForDesktopAsync(
            () => { probes++; return false; }, () => { waits++; return Task.CompletedTask; }));
        Assert.Equal(31, probes); Assert.Equal(30, waits);
    }

    [Fact]
    public async Task DesktopWaitStopsAsSoonAsRealGuardPasses()
    {
        var probes = 0; var waits = 0;
        Assert.True(await ClipboardOnlyWorkerFlow.WaitForDesktopAsync(
            () => ++probes == 3, () => { waits++; return Task.CompletedTask; }));
        Assert.Equal(3, probes); Assert.Equal(2, waits);
    }

    [Fact]
    public async Task DesktopWaitDoesNotDelayAnAlreadyReadyDesktop()
    {
        Assert.True(await ClipboardOnlyWorkerFlow.WaitForDesktopAsync(
            () => true, () => throw new InvalidOperationException("Unexpected delay")));
    }

    [Fact]
    public void FullSwapPrecedesAbortReadinessExactlyOnce()
    {
        var calls = new List<string>();
        ClipboardOnlyWorkerFlow.Execute(
            () => { calls.Add("swap"); return new(SwapStatus.Swapped, new Snapshot()); },
            () => calls.Add("await-owned-abort"));
        Assert.Equal(new[] { "swap", "await-owned-abort" }, calls);
    }

    [Theory]
    [InlineData(SwapStatus.Busy)]
    [InlineData(SwapStatus.Failed)]
    [InlineData(SwapStatus.NotPreserved)]
    public void RefusedOrPartialSwapNeverAdvertisesReady(SwapStatus status)
    {
        var ready = false;
        Assert.Throws<ProbeFailure>(() => ClipboardOnlyWorkerFlow.Execute(
            () => new(status, new Snapshot()), () => ready = true));
        Assert.False(ready);
    }

    [Fact]
    public void MissingSnapshotNeverAdvertisesReady()
    {
        var ready = false;
        Assert.Throws<ProbeFailure>(() => ClipboardOnlyWorkerFlow.Execute(
            () => new(SwapStatus.Swapped, null), () => ready = true));
        Assert.False(ready);
    }

    [Fact]
    public void NativeSwapFailurePropagatesToExistingWatchdogRecovery()
    {
        var failure = new ClipboardSwapException(new Snapshot(), new InvalidOperationException());
        var ready = false;
        Assert.Same(failure, Assert.Throws<ClipboardSwapException>(() => ClipboardOnlyWorkerFlow.Execute(
            () => throw failure, () => ready = true)));
        Assert.False(ready);
    }

    [Fact]
    public void ChildTempEnvironmentUsesOnlyTheOwnedCoordinatorRoot()
    {
        using var privateRoot = new PrivateDiagnostics();
        var environment = OwnedRoleProcess.RoleTemporaryEnvironment(privateRoot.DirectoryPath);
        Assert.Equal(2, environment.Count);
        Assert.Equal(privateRoot.DirectoryPath, environment["TEMP"]);
        Assert.Equal(privateRoot.DirectoryPath, environment["TMP"]);
    }

    [Fact]
    public void ChildTempEnvironmentRejectsRelativePaths()
    {
        Assert.Throws<ProbeFailure>(() => OwnedRoleProcess.RoleTemporaryEnvironment("relative-root"));
    }

    private sealed class Snapshot : IClipboardSnapshot
    {
        public int SavedCount => 1;
        public int SkippedByDesignCount => 0;
        public int UnsavedCount => 0;
        public bool CapExceeded => false;
        public long ByteCount => 8;
    }
}
