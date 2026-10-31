using System.Globalization;
using System.Text;

namespace SmartSnippets.Uno;

internal enum NativeScene { Restore, Abort }

/// <summary>Bounded, content-free private-pipe receipts. Original clipboard bytes never enter this protocol.</summary>
internal static class BootstrapProtocol
{
    internal const int MaximumLine = 512;
    internal static async Task<string> ReadAsync(Stream stream, CancellationToken ct)
    {
        var bytes = new List<byte>();
        var one = new byte[1];
        while (bytes.Count <= MaximumLine)
        {
            if (await stream.ReadAsync(one, ct) != 1) throw new IOException("native_pipe_eof");
            if (one[0] == 10) return Encoding.ASCII.GetString(bytes.ToArray());
            if (one[0] < 32 || one[0] > 126) throw new IOException("native_pipe_encoding");
            bytes.Add(one[0]);
        }
        throw new IOException("native_pipe_limit");
    }
    internal static async Task WriteAsync(Stream stream, string line, CancellationToken ct)
    {
        if (line.Length > MaximumLine || line.Any(c => c < 32 || c > 126)) throw new IOException("native_pipe_limit");
        await stream.WriteAsync(Encoding.ASCII.GetBytes(line + "\n"), ct);
        await stream.FlushAsync(ct);
    }
    internal static nint Handle(string field) => long.TryParse(field, NumberStyles.None, CultureInfo.InvariantCulture, out var value) && value > 0
        ? checked((nint)value) : throw new IOException("native_handle");
    internal static string[] Ready(string line)
    {
        var fields = line.Split(' ');
        if (fields.Length != 4 || fields[0] != "guardian_ready") throw new IOException("native_ready");
        _ = Handle(fields[1]); _ = Handle(fields[2]);
        if (!System.Text.RegularExpressions.Regex.IsMatch(fields[3], "^ss-uno019-recovery-[0-9a-f]{32}$")) throw new IOException("native_recovery");
        return fields;
    }
}

/// <summary>No success or forced recovery cleanup can be inferred from EOF, elapsed time or worker death.</summary>
internal sealed class NativeSceneState(NativeScene scene)
{
    internal bool GuardianReady { get; private set; }
    internal bool JobAssigned { get; private set; }
    internal bool Published { get; private set; }
    internal bool Settled { get; private set; }
    internal void Ready() { if (GuardianReady) throw new InvalidOperationException("native_order"); GuardianReady = true; }
    internal void Assign() { if (!GuardianReady || JobAssigned) throw new InvalidOperationException("native_order"); JobAssigned = true; }
    internal void Publish() { if (!JobAssigned || Published) throw new InvalidOperationException("native_order"); Published = true; }
    internal void Restored() { if (scene != NativeScene.Restore || !Published) throw new InvalidOperationException("native_order"); Settled = true; }
    internal void GuardianStopped(uint exitCode)
    {
        // Reviewed helper exits zero after its known HasBackup loop terminates, never on pending failure.
        if (scene != NativeScene.Abort || !Published || exitCode != 0) throw new InvalidOperationException("native_recovery_pending");
        Settled = true;
    }
}

internal static class NativeRecoveryLifetime
{
    internal static async Task CleanupAsync(Func<Task> stopWorkers, Func<Task> retainGuardian, Action workerPending)
    {
        try { await stopWorkers(); }
        catch { workerPending(); }
        finally { await retainGuardian(); }
    }
    internal static async Task<uint> RetainAsync(Func<bool> stopped, Func<uint> exitCode, Func<Task> wait, Action pending, int pendingAfterChecks = 40)
    {
        var checks = 0;
        while (!stopped())
        {
            if (++checks == pendingAfterChecks) pending();
            await wait();
        }
        return exitCode();
    }
}
