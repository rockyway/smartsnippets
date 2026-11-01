using System;
using System.Threading;
using System.Threading.Tasks;

namespace SmartSnippets.Uno.Platforms.Windows.AcceptanceNative;

internal enum ClipboardControlAdmission
{
    Ready, Closed, Permission, GuardianUnavailable, GuardianUnpinned, PopupUnpinned,
    TargetIdentity, Desktop, Unquiet, TimedOut, ForegroundChanged
}

internal static class ClipboardControlReadiness
{
    internal static async Task<ClipboardControlAdmission> WaitAsync(
        Func<ClipboardControlAdmission> classify, Func<bool> foregroundUnchanged,
        CancellationToken ct, TimeProvider? timeProvider = null, Action<string, int>? refused = null)
    {
        var clock = timeProvider ?? TimeProvider.System;
        var start = clock.GetTimestamp();
        var sampleOrder = 0;
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            sampleOrder++;
            if (!foregroundUnchanged())
            {
                NativeControlDiagnostics.Refused(refused, sampleOrder == 1 ? "initial" : "changed", sampleOrder);
                return ClipboardControlAdmission.ForegroundChanged;
            }
            var admission = classify();
            if (admission != ClipboardControlAdmission.Unquiet) return admission;
            var remaining = TimeSpan.FromSeconds(4) - clock.GetElapsedTime(start);
            if (remaining <= TimeSpan.Zero) return ClipboardControlAdmission.TimedOut;
            await Task.Delay(remaining < TimeSpan.FromMilliseconds(50) ? remaining : TimeSpan.FromMilliseconds(50), clock, ct);
        }
    }
}
