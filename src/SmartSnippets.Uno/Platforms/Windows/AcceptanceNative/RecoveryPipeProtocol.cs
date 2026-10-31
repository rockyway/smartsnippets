using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SmartSnippets.Presentation.Services;
using SmartSnippets.WindowsInsertion018;

namespace SmartSnippets.Uno.Platforms.Windows.AcceptanceNative;

internal static class RecoveryPipeProtocol
{
    // The coordinator must read the response and send "ack" before disconnecting.
    // Never WaitForPipeDrain: an unresponsive peer cannot retain this connection forever.
    internal static async Task<RestoreStatus> RespondAsync(Stream pipe, StreamReader reader,
        RestoreStatus status, Func<RestoreStatus> restore, Func<bool> hasBackup)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var command = await FixedPipe.ReadAsync(reader, TimeSpan.FromSeconds(3), deadline.Token);
        if (command is not ("status" or "retry")) return status;
        if (command == "retry") status = restore();
        var response = hasBackup() ? "RECOVERY_PENDING " + status : "settled";
        await FixedPipe.WriteAsync(pipe, response).WaitAsync(deadline.Token);
        if (await FixedPipe.ReadAsync(reader, TimeSpan.FromSeconds(3), deadline.Token) != "ack")
            throw new IOException("Recovery acknowledgement refused.");
        return status;
    }
}
