using System;
using System.Globalization;

namespace SmartSnippets.Uno.Platforms.Windows.AcceptanceNative;

internal static class NativeControlDiagnostics
{
    // Diagnostics must never change a refusal result or prevent its admission log.
    internal static void Refused(Action<string, int>? callback, string check, int order)
    {
        try { callback?.Invoke(check, order); } catch { }
    }

    internal static bool CheckIdle(Func<bool> unchanged, ref int sampleOrder, Action<string, int>? refused)
    {
        sampleOrder++;
        if (unchanged()) return true;
        Refused(refused, "idle", sampleOrder);
        return false;
    }

    internal static string Relation(nint foreground, nint windowA, nint windowB,
        uint pid, uint guardianPid, uint candidatePid, uint ownedPid)
    {
        if (foreground == 0) return "none";
        if (pid == 0) return "foreign";
        if (foreground == windowA) return "owned_target_a";
        if (foreground == windowB) return "owned_target_b";
        if (guardianPid != 0 && pid == guardianPid) return "guardian";
        if (candidatePid != 0 && pid == candidatePid) return "candidate";
        return ownedPid != 0 && pid == ownedPid ? "same_pid_other" : "foreign";
    }

    internal static string Record(nint foreground, nint windowA, nint windowB,
        uint pid, uint guardianPid, uint candidatePid, uint ownedPid,
        bool visible, bool enabled, uint thread, uint ownedThread, string check, int order)
    {
        if (check is not ("initial" or "changed" or "idle") || order < 1)
            throw new ArgumentOutOfRangeException(nameof(order));
        var relation = Relation(foreground, windowA, windowB, pid, guardianPid, candidatePid, ownedPid);
        return string.Create(CultureInfo.InvariantCulture,
            $"relation={relation} fgVisible={visible} fgEnabled={enabled} fgSameThreadAsOwned={thread != 0 && thread == ownedThread} check={check} sampleOrder={order}");
    }
}
