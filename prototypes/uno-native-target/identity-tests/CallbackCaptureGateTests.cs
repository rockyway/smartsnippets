using SmartSnippets.TargetIdentity018;
using Xunit;

public sealed class CallbackCaptureGateTests
{
    [Theory]
    [InlineData(0u, 101u)]
    [InlineData(NativeCallbackLease.Signature, 100u)]
    [InlineData(NativeCallbackLease.Signature, 999u)]
    public void UnknownIdOrStaleGenerationNeverCaptures(uint signature, uint generation)
    {
        var native = new Identity();
        using var probe = new OwnedTargetProbe(new Lease(), native);
        var gate = new CallbackCaptureGate(probe, probe.Capture("a"), 101);
        native.CaptureCount = 0;
        gate.Deliver(signature, generation);
        CallbackHarness.RequireCapture(gate, 0, 0);
        Assert.Equal(0, native.CaptureCount);
    }

    [Fact]
    public void OriginalExactAIsCapturedButSamePidSiblingBIsNot()
    {
        var native = new Identity();
        using var probe = new OwnedTargetProbe(new Lease(), native);
        var a = probe.Capture("a");
        var gate = new CallbackCaptureGate(probe, a, 101);
        gate.Deliver(NativeCallbackLease.Signature, 101);
        CallbackHarness.RequireCapture(gate, 1, 1);
        Assert.Equal(a, gate.Captured);
        native.Foreground = 2;
        gate.Deliver(NativeCallbackLease.Signature, 101);
        CallbackHarness.RequireCapture(gate, 2, 1);
        gate.Active = false;
        native.Foreground = 1;
        gate.Deliver(NativeCallbackLease.Signature, 101);
        CallbackHarness.RequireCapture(gate, 2, 1);
    }

    [Fact]
    public void MissingCallbackIsAFailureNotSuccessfulCapture()
    {
        using var probe = new OwnedTargetProbe(new Lease(), new Identity());
        var gate = new CallbackCaptureGate(probe, probe.Capture("a"), 101);
        Assert.Throws<ProbeFailure>(() => CallbackHarness.RequireCapture(gate, 1, 1));
    }

    [Fact]
    public void NativeCallbackBoundaryDoesNotAllowExceptionsToEscape()
    {
        var native = new Identity();
        using var probe = new OwnedTargetProbe(new Lease(), native);
        var gate = new CallbackCaptureGate(probe, probe.Capture("a"), 101);
        native.Throw = true;
        gate.Deliver(NativeCallbackLease.Signature, 101);
        Assert.True(gate.Failed);
        Assert.Throws<ProbeFailure>(() => CallbackHarness.RequireCapture(gate, 1, 1));
    }

    [Fact]
    public void CallbackModeRequiresSeparateAuthorizationAndDefaultRemainsIdentity()
    {
        string[] basic = ["--authorized-screen-slot", "--watchdog-stdin", "--executable", "fixture", "--sha256", "hash"];
        Assert.False(HarnessOptions.Parse(basic).NativeCallback);
        Assert.Throws<ProbeFailure>(() => HarnessOptions.Parse([.. basic, "--inprocess-native-callback"]));
        Assert.Throws<ProbeFailure>(() => HarnessOptions.Parse([.. basic, "--authorized-native-hotkey"]));
        Assert.True(HarnessOptions.Parse([.. basic, "--inprocess-native-callback", "--authorized-native-hotkey"]).NativeCallback);
    }

    private sealed class Lease : IOwnedFixtureLease
    {
        public uint ExpectedPid => 42;
        public bool IsAliveAndPinned() => true;
    }
    private sealed class Identity : IExactWindowIdentity
    {
        internal nint Foreground = 1;
        internal int CaptureCount;
        internal bool Throw;
        public nint Capture(string window) { CaptureCount++; return window == "a" ? 1 : 2; }
        public bool Exists(nint identity) => Throw ? throw new InvalidOperationException() : identity is 1 or 2;
        public uint ProcessId(nint identity) => 42;
        public bool IsForeground(nint identity) => identity == Foreground;
        public void Dispose() { }
    }
}
