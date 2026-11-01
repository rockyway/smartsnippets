using System.Diagnostics;
using System.Text;
using System.Threading.Channels;
using SmartSnippets.TargetIdentity018;
using Xunit;

public sealed class OwnedFixtureLeaseTests
{
    [Fact]
    public async Task RealFactoryAndHarnessComposeSameLeaseAndStopOnlyTheirChild()
    {
        using var fixture = new ArtifactFixture();
        var child = new FakeChild();
        var native = new FakeIdentity(child);
        var receipts = new List<string>();
        var focusChecks = 0;
        await using (var lease = await OwnedFixtureLease.StartAsync(await fixture.OpenAsync(), (info, platform) =>
        {
            Assert.Equal(fixture.Entry, info.FileName);
            Assert.Equal(TargetPlatform.Windows, platform);
            Assert.False(info.UseShellExecute);
            Assert.True(info.RedirectStandardInput && info.RedirectStandardOutput && info.RedirectStandardError && info.CreateNoWindow);
            Assert.Equal(2, info.ArgumentList.Count);
            Assert.Equal("--authorized-probe-target", info.ArgumentList[0]);
            Assert.True(Guid.TryParseExact(info.ArgumentList[1], "D", out _));
            using var current = Process.GetCurrentProcess();
            Assert.Equal(current.Id.ToString(System.Globalization.CultureInfo.InvariantCulture), info.Environment["SS_TARGET_GRANTEE_PID"]);
            Assert.Equal(current.StartTime.ToUniversalTime().ToFileTimeUtc().ToString(System.Globalization.CultureInfo.InvariantCulture), info.Environment["SS_TARGET_GRANTEE_STARTED"]);
            Assert.DoesNotContain("DYLD_INSERT_LIBRARIES", info.Environment.Keys);
            Assert.DoesNotContain("DOTNET_STARTUP_HOOKS", info.Environment.Keys);
            return child;
        }, CancellationToken.None))
        {
            await IdentityHarness.ObserveAsync(lease, receipts.Add, _ => { focusChecks++; return Task.CompletedTask; }, CancellationToken.None,
                (sameLease, ready, _) => { Assert.Same(lease, sameLease); Assert.Equal((nint)11, ready.WindowA); return native; });
            Assert.Equal(4, focusChecks);
            Assert.Equal(new[] { "exact_focus_a", "exact_focus_b", "exact_focus_a", "closed_a_surviving_b" }, receipts);
            Assert.Equal(new[] { "empty_a", "empty_b", "focus_a", "focus_b", "focus_a", "close_a", "empty_b", "focus_b" }, child.Commands);
            Assert.Equal(1, native.DisposeCount);
            Assert.True(lease.IsAliveAndPinned());
        }
        Assert.Equal(1, child.DisposeCount);
        Assert.False(child.Alive);
    }

    [Theory]
    [InlineData(22, true)]
    [InlineData(21, false)]
    [InlineData(23, false)]
    public async Task VerifyMultilineARequiresExactAllowlistedReadbackCount(int count, bool accepted)
    {
        using var fixture = new ArtifactFixture();
        var child = new FakeChild { VerifyCount = count };
        await using var lease = await OwnedFixtureLease.StartAsync(await fixture.OpenAsync(), (_, _) => child, CancellationToken.None);
        if (accepted) await lease.VerifyMultilineAAsync(CancellationToken.None);
        else Assert.Equal("readback_failed", (await Assert.ThrowsAsync<ProbeFailure>(() => lease.VerifyMultilineAAsync(CancellationToken.None))).Token);
        Assert.Equal(new[] { "verify_multiline_a" }, child.Commands);
    }

    [Theory]
    [InlineData("target_foreground_grant result=granted", true)]
    [InlineData("target_foreground_grant result=refused", false)]
    [InlineData("target_foreground_grant result=granted extra", false)]
    [InlineData("target_command name=foreground_grant ok=1", false)]
    public async Task ForegroundGrantRequiresExactReceipt(string receipt, bool accepted)
    {
        using var fixture = new ArtifactFixture();
        var child = new FakeChild { GrantReceipt = receipt };
        await using var lease = await OwnedFixtureLease.StartAsync(await fixture.OpenAsync(), (_, _) => child, CancellationToken.None);
        if (accepted) await lease.GrantForegroundAsync(CancellationToken.None);
        else Assert.Equal("foreground_grant_refused", (await Assert.ThrowsAsync<ProbeFailure>(() => lease.GrantForegroundAsync(CancellationToken.None))).Token);
        Assert.Equal(new[] { "foreground_grant" }, child.Commands);
        child.Alive = false;
        await Assert.ThrowsAsync<ProbeFailure>(() => lease.GrantForegroundAsync(CancellationToken.None));
        Assert.Single(child.Commands); // Stale target identity cannot receive another request.
    }

    [Fact]
    public async Task SamePidOtherWindowCannotPassActualHarnessComposition()
    {
        using var fixture = new ArtifactFixture();
        var child = new FakeChild();
        await using var lease = await OwnedFixtureLease.StartAsync(await fixture.OpenAsync(), (_, _) => child, CancellationToken.None);
        var native = new FakeIdentity(child) { AcceptAnySamePidForeground = true };
        var failure = await Assert.ThrowsAsync<ProbeFailure>(() => IdentityHarness.ObserveAsync(lease, _ => { }, _ => Task.CompletedTask,
            CancellationToken.None, (_, _, _) => native));
        Assert.Equal("exact_foreground", failure.Token);
        Assert.Equal(1, native.DisposeCount);
    }

    [Theory]
    [InlineData("target_ready pid=999 a=11 b=12 edit_a=13 edit_b=14")]
    [InlineData("target_ready pid=424242 a=11 b=11 edit_a=13 edit_b=14")]
    [InlineData("target_ready pid=424242 a=11 b=12 edit_a=11 edit_b=14")]
    [InlineData("target_ready pid=424242 a=11 b=12")]
    [InlineData("target_ready pid=424242 a=011 b=12 edit_a=13 edit_b=14")]
    [InlineData("target_ready pid=424242 a=11 b=12 edit_a=13 edit_b=14 trailing")]
    public async Task SpoofedReadinessStopsOnlyCreatedChild(string readiness)
    {
        using var fixture = new ArtifactFixture();
        var child = new FakeChild(readiness);
        await Assert.ThrowsAsync<ProbeFailure>(async () => await OwnedFixtureLease.StartAsync(await fixture.OpenAsync(), (_, _) => child, CancellationToken.None));
        Assert.Equal(1, child.DisposeCount);
    }

    [Fact]
    public async Task ReadinessTimeoutAndEarlyExitPerformPartialInitializationCleanup()
    {
        using var fixture = new ArtifactFixture();
        foreach (var exited in new[] { false, true })
        {
            var child = new FakeChild(readiness: null) { Alive = !exited };
            var failure = await Assert.ThrowsAsync<ProbeFailure>(async () => await OwnedFixtureLease.StartAsync(await fixture.OpenAsync(), (_, _) => child,
                CancellationToken.None, TimeSpan.FromMilliseconds(20)));
            Assert.Equal(exited ? "child_identity" : "protocol_timeout", failure.Token);
            Assert.Equal(1, child.DisposeCount);
        }
    }

    [Fact]
    public async Task ChildExitAfterReadinessRefusesCommandsAndProbeCreation()
    {
        using var fixture = new ArtifactFixture();
        var child = new FakeChild();
        await using var lease = await OwnedFixtureLease.StartAsync(await fixture.OpenAsync(), (_, _) => child, CancellationToken.None);
        child.Alive = false;
        Assert.False(lease.IsAliveAndPinned());
        await Assert.ThrowsAsync<ProbeFailure>(() => lease.CommandAsync("focus_a", CancellationToken.None));
        Assert.Throws<ProbeFailure>(() => lease.CreateProbe());
        Assert.Empty(child.Commands);
    }

    [Fact]
    public async Task BrokenPipeRefusesAndOwnedChildIsStillDisposed()
    {
        using var fixture = new ArtifactFixture();
        var child = new FakeChild();
        await using (var lease = await OwnedFixtureLease.StartAsync(await fixture.OpenAsync(), (_, _) => child, CancellationToken.None))
        {
            child.BreakInput = true;
            await Assert.ThrowsAsync<ProbeFailure>(() => lease.CommandAsync("empty_a", CancellationToken.None));
        }
        Assert.Equal(1, child.DisposeCount);
    }

    [Fact]
    public async Task DeniedFocusPreflightNeverReachesAnyFocusCommand()
    {
        using var fixture = new ArtifactFixture();
        var child = new FakeChild();
        await using var lease = await OwnedFixtureLease.StartAsync(await fixture.OpenAsync(), (_, _) => child, CancellationToken.None);
        await Assert.ThrowsAsync<ProbeFailure>(() => IdentityHarness.ObserveAsync(lease, _ => { }, _ => throw new ProbeFailure("focus_preflight"),
            CancellationToken.None, (_, _, _) => new FakeIdentity(child)));
        Assert.Equal(new[] { "empty_a", "empty_b" }, child.Commands);
    }

    [Theory]
    [InlineData("extra")]
    [InlineData("oversized")]
    [InlineData("stderr")]
    public async Task UnsolicitedOrOversizedOutputAndDiagnosticsRefuseReadiness(string kind)
    {
        using var fixture = new ArtifactFixture();
        var child = new FakeChild(kind == "oversized" ? new string('x', 257) : "target_ready pid=424242 a=11 b=12 edit_a=13 edit_b=14");
        if (kind == "extra") child.ExtraOutput();
        if (kind == "stderr") child.ErrorOutput();
        await Task.Delay(20); // Let the intentionally concurrent stream pumps observe planted negatives.
        await Assert.ThrowsAsync<ProbeFailure>(async () => await OwnedFixtureLease.StartAsync(await fixture.OpenAsync(), (_, _) => child, CancellationToken.None));
        Assert.Equal(1, child.DisposeCount);
    }

    [Fact]
    public async Task CancelledReadinessReleasesOwnedChildAndPinnedImage()
    {
        using var fixture = new ArtifactFixture();
        var child = new FakeChild(readiness: null);
        using var stop = new CancellationTokenSource();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await OwnedFixtureLease.StartAsync(await fixture.OpenAsync(), (_, _) =>
        {
            stop.CancelAfter(TimeSpan.FromMilliseconds(20));
            return child;
        }, stop.Token));
        Assert.Equal(1, child.DisposeCount);
        File.Delete(fixture.Entry);
    }

    [Fact]
    public async Task AlreadyCancelledFactoryNeverStartsAnyChild()
    {
        using var fixture = new ArtifactFixture();
        using var stop = new CancellationTokenSource();
        stop.Cancel();
        var starts = 0;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await OwnedFixtureLease.StartAsync(await fixture.OpenAsync(), (_, _) =>
        {
            starts++;
            throw new InvalidOperationException();
        }, stop.Token));
        Assert.Equal(0, starts);
        File.Delete(fixture.Entry);
    }

    [Fact]
    public async Task FailedStartReleasesPinnedFile()
    {
        using var fixture = new ArtifactFixture();
        await Assert.ThrowsAsync<ProbeFailure>(async () => await OwnedFixtureLease.StartAsync(await fixture.OpenAsync(), (_, _) => throw new ProbeFailure("child_start"), CancellationToken.None));
        File.Delete(fixture.Entry);
        Assert.False(File.Exists(fixture.Entry));
    }

    [Theory]
    [InlineData("--pid", "424242")]
    [InlineData("--executable", "fixture")]
    public void NoAttachOrUnauthenticatedArgumentsAreAccepted(string key, string value) =>
        Assert.Throws<ProbeFailure>(() => HarnessOptions.Parse([key, value]));

    [Fact]
    public void BothExplicitAuthorizationAndOuterWatchdogGateAreRequired()
    {
        Assert.Throws<ProbeFailure>(() => HarnessOptions.Parse(["--authorized-screen-slot", "--executable", "fixture", "--sha256", new string('a', 64)]));
        var options = HarnessOptions.Parse(["--authorized-screen-slot", "--watchdog-stdin", "--executable", "fixture", "--sha256", new string('a', 64)]);
        Assert.Equal("fixture", options.Executable);
    }

    private sealed class FakeIdentity(FakeChild child) : IExactWindowIdentity
    {
        internal bool AcceptAnySamePidForeground;
        internal int DisposeCount;
        public nint Capture(string fixtureWindow) => fixtureWindow == "a" ? 1 : 2;
        public bool Exists(nint identity) => child.Alive && (identity == 2 || (identity == 1 && !child.ClosedA));
        public uint ProcessId(nint identity) => child.Id;
        public bool IsForeground(nint identity) => AcceptAnySamePidForeground || child.Foreground == identity;
        public void Dispose() => DisposeCount++;
    }

    private sealed class FakeChild : IOwnedChild
    {
        private readonly FeedStream output = new(), error = new();
        internal readonly List<string> Commands = [];
        internal bool Alive = true, BreakInput, ClosedA;
        internal nint Foreground;
        internal int DisposeCount;
        internal int VerifyCount = 22;
        internal string GrantReceipt = "target_foreground_grant result=refused";
        public uint Id => 424242;
        public ChildProtocol Protocol { get; }

        internal FakeChild(string? readiness = "target_ready pid=424242 a=11 b=12 edit_a=13 edit_b=14")
        {
            Protocol = new(new StreamReader(output), new StreamReader(error), new StreamWriter(new CommandStream(Command), new UTF8Encoding(false)));
            if (readiness is not null) output.Feed(readiness + "\n");
        }

        private void Command(string command)
        {
            if (BreakInput) throw new IOException();
            Commands.Add(command);
            if (command == "foreground_grant") { output.Feed(GrantReceipt + "\n"); return; }
            if (command == "focus_a") Foreground = 1;
            if (command == "focus_b") Foreground = 2;
            if (command == "close_a") ClosedA = true;
            output.Feed("target_command name=" + command + " ok=1" + (command == "verify_multiline_a" ? " utf16_count=" + VerifyCount : "") + "\n");
        }

        internal void ExtraOutput() => output.Feed("extra\n");
        internal void ErrorOutput() => error.Feed("private diagnostic\n");
        public bool IsIdentityAlive() => Alive;
        public async ValueTask DisposeAsync() { DisposeCount++; Alive = false; output.Complete(); error.Complete(); await Protocol.DisposeAsync(); }
    }

    private sealed class FeedStream : Stream
    {
        private readonly Channel<byte> bytes = Channel.CreateUnbounded<byte>();
        internal void Feed(string text) { foreach (var value in Encoding.UTF8.GetBytes(text)) bytes.Writer.TryWrite(value); }
        internal void Complete() => bytes.Writer.TryComplete();
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
        {
            if (!await bytes.Reader.WaitToReadAsync(ct)) return 0;
            var count = 0;
            while (count < buffer.Length && bytes.Reader.TryRead(out var value)) buffer.Span[count++] = value;
            return count;
        }
        protected override void Dispose(bool disposing) { Complete(); base.Dispose(disposing); }
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] b, int o, int c) => throw new NotSupportedException();
        public override void Flush() { }
        public override long Seek(long o, SeekOrigin s) => throw new NotSupportedException();
        public override void SetLength(long v) => throw new NotSupportedException();
        public override void Write(byte[] b, int o, int c) => throw new NotSupportedException();
    }

    private sealed class CommandStream(Action<string> command) : Stream
    {
        private readonly StringBuilder text = new();
        public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            text.Append(Encoding.UTF8.GetString(buffer));
            var line = text.ToString();
            if (line.EndsWith('\n')) { text.Clear(); command(line.TrimEnd('\n', '\r')); }
        }
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct = default) { Write(buffer.Span); return ValueTask.CompletedTask; }
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] b, int o, int c) => throw new NotSupportedException();
        public override void Flush() { }
        public override Task FlushAsync(CancellationToken ct) => Task.CompletedTask;
        public override long Seek(long o, SeekOrigin s) => throw new NotSupportedException();
        public override void SetLength(long v) => throw new NotSupportedException();
    }
}
