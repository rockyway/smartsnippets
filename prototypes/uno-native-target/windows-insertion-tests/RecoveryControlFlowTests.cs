using SmartSnippets.WindowsInsertion018;
using Xunit;

namespace SmartSnippets.WindowsInsertion018.Tests;

public sealed class RecoveryControlFlowTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RecoveryAfterWorkerStop_ReapFailureCannotSkipRecovery(bool stopFails)
    {
        var order = new List<string>();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var task = RecoveryAfterWorkerStop.RunAsync(async () =>
        {
            order.Add("stop");
            await release.Task;
            order.Add("reaped-or-failed");
            if (stopFails) throw new IOException("job termination race");
        }, () => { order.Add("recover"); return Task.FromResult(true); });
        Assert.Equal(new[] { "stop" }, order);
        Assert.False(task.IsCompleted);
        release.SetResult();
        Assert.True(await task);
        Assert.Equal(new[] { "stop", "reaped-or-failed", "recover" }, order);
    }

    [Fact]
    public async Task RecoveryAfterWorkerStop_HandshakeFailureRemainsPending()
    {
        Assert.False(await RecoveryAfterWorkerStop.RunAsync(() => Task.CompletedTask,
            () => Task.FromException<bool>(new IOException("closed guardian pipe"))));
        Assert.False(await RecoveryAfterWorkerStop.RunAsync(() => Task.CompletedTask, () => Task.FromResult(false)));
    }

    [Fact]
    public async Task FixedPipe_TimeoutCancelsAndDrainsActualReadBeforeReturning()
    {
        using var stream = new WaitingStream();
        using var reader = new StreamReader(stream);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            FixedPipe.ReadAsync(reader, TimeSpan.FromMilliseconds(30), CancellationToken.None));
        Assert.Equal(0, stream.Outstanding);
        Assert.True(stream.Canceled);
    }

    [Fact]
    public void StaTaskLoop_LatePostAfterClosureDoesNotThrowOrRun()
    {
        SynchronizationContext? closed = null;
        Assert.Equal(0, StaTaskLoop.Run(() =>
        {
            closed = SynchronizationContext.Current;
            return Task.FromResult(0);
        }));
        var ran = false;
        closed!.Post(_ => ran = true, null);
        Assert.False(ran);
    }

    [Fact]
    public void RestoreRecognition_BindsPartialOriginalToMarkerAndExactCapturedBytes()
    {
        Dictionary<uint, byte[]> original = new() { [400] = [1, 2, 3], [401] = [9] };
        Dictionary<uint, byte[]> partial = new() { [200] = [5, 6], [100] = new byte[4], [400] = [1, 2, 3] };
        HashSet<uint> privacy = [100, 101, 102];
        bool Recognize() => ClipboardOwnershipPolicy.IsRecognizableRestore(partial, original, 200, [5, 6], privacy);
        Assert.True(Recognize());
        partial.Remove(200);
        Assert.False(Recognize()); // Known original bytes without run marker are not our partial restore.
        partial[200] = [5, 7];
        Assert.False(Recognize());
        partial[200] = [5, 6];
        partial[400] = [1, 2, 4];
        Assert.False(Recognize());
        partial[400] = [1, 2, 3];
        partial[999] = [0];
        Assert.False(Recognize());
    }

    private sealed class WaitingStream : Stream
    {
        internal int Outstanding;
        internal bool Canceled;
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
        {
            Interlocked.Increment(ref Outstanding);
            try { await Task.Delay(Timeout.Infinite, ct); return 0; }
            catch (OperationCanceledException) { Canceled = true; throw; }
            finally { Interlocked.Decrement(ref Outstanding); }
        }
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() => throw new NotSupportedException();
        public override int Read(byte[] b, int o, int c) => throw new NotSupportedException();
        public override long Seek(long o, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] b, int o, int c) => throw new NotSupportedException();
    }
}
