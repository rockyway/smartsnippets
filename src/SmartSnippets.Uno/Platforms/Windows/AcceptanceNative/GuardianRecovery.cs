using System;
using System.Threading.Tasks;
using SmartSnippets.Presentation.Services;

namespace SmartSnippets.Uno.Platforms.Windows.AcceptanceNative;

internal static class GuardianRecovery
{
    /// <summary>EOF/timeout is not worker death. Exceptions and foreign ownership never authorize releasing RAM.</summary>
    internal static async Task RetainUntilSettledAsync(Func<bool> stopped, Func<bool> hasBackup,
        Func<RestoreStatus> restore, Func<Task> wait, Func<RestoreStatus, Task<RestoreStatus>> serveRecovery)
    {
        if (!hasBackup()) return;
        while (true)
        {
            var confirmedStopped = false;
            try { confirmedStopped = stopped(); }
            catch (Exception) { } // Unknown handle status is not death.
            if (confirmedStopped) break;
            await wait();
        }
        var status = RestoreStatus.Failed;
        try { status = restore(); }
        catch (Exception) { } // Still retain backup and expose retry control.
        // Clipboard contention is transient and has not authorized any ownership change.
        // Retry only Busy after verified worker death, with the same guarded restore.
        // The native wait is 100ms: at most 30 waits before retaining manual recovery.
        for (var retry = 0; retry < 30 && hasBackup() && status == RestoreStatus.Busy; retry++)
        {
            await wait();
            try { status = restore(); }
            catch (Exception) { status = RestoreStatus.Failed; }
        }
        while (hasBackup())
        {
            try { status = await serveRecovery(status); }
            catch (Exception) { } // Disconnected/rejected control clients cannot destroy the backup.
        }
    }
}
