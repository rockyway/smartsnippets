using System.Text;
using System.Threading.Channels;

namespace SmartSnippets.TargetIdentity018;

/// <summary>Bounded in-memory protocol; never persists or prints untrusted child output.</summary>
internal sealed class ChildProtocol : IAsyncDisposable
{
    private readonly StreamReader stdout, stderr;
    private readonly StreamWriter stdin;
    private readonly CancellationTokenSource stop = new();
    private readonly Channel<string> lines = Channel.CreateBounded<string>(new BoundedChannelOptions(32) { FullMode = BoundedChannelFullMode.Wait });
    private readonly Task outputPump, errorPump;
    private volatile bool refused;
    internal bool HasUnexpectedOutput => refused || lines.Reader.TryPeek(out _);

    internal ChildProtocol(StreamReader stdout, StreamReader stderr, StreamWriter stdin)
    {
        (this.stdout, this.stderr, this.stdin) = (stdout, stderr, stdin);
        stdin.NewLine = "\n";
        outputPump = PumpAsync();
        errorPump = WatchErrorAsync();
    }

    private async Task PumpAsync()
    {
        try
        {
            var character = new char[1];
            var line = new StringBuilder(256);
            var count = 0;
            while (await stdout.ReadAsync(character.AsMemory(), stop.Token) != 0)
            {
                var c = character[0];
                if (c == '\n')
                {
                    if (++count > 32 || !lines.Writer.TryWrite(line.ToString().TrimEnd('\r'))) throw new ProbeFailure("protocol_limit");
                    line.Clear();
                }
                else
                {
                    if (line.Length == 256 || c is < ' ' or > '~' && c != '\r') throw new ProbeFailure("protocol_limit");
                    line.Append(c);
                }
            }
            if (line.Length != 0) refused = true;
        }
        catch (Exception) { if (!stop.IsCancellationRequested) refused = true; }
        finally { lines.Writer.TryComplete(); }
    }

    private async Task WatchErrorAsync()
    {
        try { if (await stderr.ReadAsync(new char[1].AsMemory(), stop.Token) != 0) refused = true; }
        catch (Exception) { if (!stop.IsCancellationRequested) refused = true; }
    }

    internal async Task<string> ReadAsync(TimeSpan timeout, CancellationToken ct)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(timeout);
        try
        {
            var result = await lines.Reader.ReadAsync(deadline.Token);
            if (refused) throw new ProbeFailure("child_diagnostic");
            return result;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { throw new ProbeFailure("protocol_timeout"); }
        catch (ChannelClosedException) { throw new ProbeFailure("child_eof"); }
    }

    internal async Task SendAsync(string command, TimeSpan timeout, CancellationToken ct)
    {
        if (command is not ("empty_a" or "empty_b" or "focus_a" or "focus_b" or "close_a")) throw new ProbeFailure("command_refused");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(timeout);
        try
        {
            await stdin.WriteLineAsync(command.AsMemory(), deadline.Token);
            await stdin.FlushAsync(deadline.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { throw new ProbeFailure("protocol_timeout"); }
        catch (IOException) { throw new ProbeFailure("child_pipe"); }
    }

    private static void ClosePipe(IDisposable pipe)
    {
        try { pipe.Dispose(); }
        catch (IOException) { } // A broken pipe must not prevent closing the other owned streams.
    }

    public async ValueTask DisposeAsync()
    {
        stop.Cancel();
        ClosePipe(stdin);
        ClosePipe(stdout);
        ClosePipe(stderr);
        // Disposed pipe streams release the read pumps; outer watchdog still bounds
        // a platform pipe implementation that fails to honor cancellation/disposal.
        try { await Task.WhenAll(outputPump, errorPump).WaitAsync(TimeSpan.FromSeconds(2)); }
        catch (Exception) { }
        stop.Dispose();
    }
}
