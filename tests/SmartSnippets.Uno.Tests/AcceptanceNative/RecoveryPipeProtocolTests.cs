using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Uno.Platforms.Windows.AcceptanceNative;
using SmartSnippets.WindowsInsertion018;
using Xunit;

namespace SmartSnippets.Uno.Tests.AcceptanceNative;

public sealed class RecoveryPipeProtocolTests
{
    [Theory]
    [InlineData("status", true, "RECOVERY_PENDING ClipboardChanged", 0)]
    [InlineData("retry", false, "settled", 1)]
    public async Task ResponseIsReadableBeforeDisconnect_AndServerWaitsForAck(string command, bool backup, string expected, int calls)
    {
        await using var pair = await PipePair.ConnectAsync();
        var restores = 0;
        var response = RecoveryPipeProtocol.RespondAsync(pair.Server, pair.ServerReader, RestoreStatus.ClipboardChanged,
            () => { restores++; return RestoreStatus.Restored; }, () => backup);
        await FixedPipe.WriteAsync(pair.Client, command);
        Assert.Equal(expected, await FixedPipe.ReadAsync(pair.ClientReader, TimeSpan.FromSeconds(2), default));
        Assert.False(response.IsCompleted); // Flush alone is not a peer acknowledgement.
        await FixedPipe.WriteAsync(pair.Client, "ack");
        await response;
        Assert.Equal(calls, restores);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("retry extra")]
    [InlineData("STATUS")]
    [InlineData("ack")]
    public async Task UnknownCommand_DoesNotRestoreOrSendStatus(string command)
    {
        await using var pair = await PipePair.ConnectAsync();
        var restores = 0;
        var response = RecoveryPipeProtocol.RespondAsync(pair.Server, pair.ServerReader, RestoreStatus.Busy,
            () => { restores++; return RestoreStatus.Restored; }, () => true);
        await FixedPipe.WriteAsync(pair.Client, command);
        Assert.Equal(RestoreStatus.Busy, await response);
        Assert.Equal(0, restores);
        pair.Server.Disconnect();
        Assert.Null(await FixedPipe.ReadOrEofAsync(pair.ClientReader));
    }

    [Fact]
    public async Task MissingAck_IsBounded_AndDoesNotRetryRestore()
    {
        await using var pair = await PipePair.ConnectAsync();
        var restores = 0;
        var watch = Stopwatch.StartNew();
        var response = RecoveryPipeProtocol.RespondAsync(pair.Server, pair.ServerReader, RestoreStatus.Busy,
            () => { restores++; return RestoreStatus.Restored; }, () => true);
        await FixedPipe.WriteAsync(pair.Client, "status");
        Assert.Equal("RECOVERY_PENDING Busy", await FixedPipe.ReadAsync(pair.ClientReader, TimeSpan.FromSeconds(2), default));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await response);
        Assert.InRange(watch.ElapsedMilliseconds, 2800, 6000);
        Assert.Equal(0, restores);
    }

    [Fact]
    public async Task InvalidAck_IsRejected()
    {
        await using var pair = await PipePair.ConnectAsync();
        var response = RecoveryPipeProtocol.RespondAsync(pair.Server, pair.ServerReader, RestoreStatus.Busy,
            () => RestoreStatus.Restored, () => true);
        await FixedPipe.WriteAsync(pair.Client, "status");
        await FixedPipe.ReadAsync(pair.ClientReader, TimeSpan.FromSeconds(2), default);
        await FixedPipe.WriteAsync(pair.Client, "retry");
        await Assert.ThrowsAsync<IOException>(async () => await response);
    }

    private sealed class PipePair : IAsyncDisposable
    {
        internal NamedPipeServerStream Server { get; }
        internal NamedPipeClientStream Client { get; }
        internal StreamReader ServerReader { get; }
        internal StreamReader ClientReader { get; }
        private PipePair(string name)
        {
            Server = new(name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            Client = new(".", name, PipeDirection.InOut, PipeOptions.Asynchronous);
            ServerReader = new(Server, leaveOpen: true);
            ClientReader = new(Client, leaveOpen: true);
        }
        internal static async Task<PipePair> ConnectAsync()
        {
            var pair = new PipePair("ss-uno019-test-" + Guid.NewGuid().ToString("N"));
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await Task.WhenAll(pair.Server.WaitForConnectionAsync(deadline.Token), pair.Client.ConnectAsync(deadline.Token));
            return pair;
        }
        public async ValueTask DisposeAsync()
        {
            ServerReader.Dispose(); ClientReader.Dispose();
            await Client.DisposeAsync(); await Server.DisposeAsync();
        }
    }
}
