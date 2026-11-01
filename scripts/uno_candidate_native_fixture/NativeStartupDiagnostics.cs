namespace SmartSnippets.Uno;

/// <summary>Read-only samples, not foreground authority. No identities leave this recorder.</summary>
internal sealed class NativeStartupDiagnostics(Func<nint> foreground, Func<nint, uint> processId,
    uint launcherPid, Func<nint> windowA, Func<nint> windowB)
{
    private int order;
    internal string Sample(string phase)
    {
        // Only fixed call-site tokens; refuse arbitrary text before sampling.
        if (phase is not ("target_launch" or "host_ready" or "preparation_start" or "before_focus" or "after_focus"))
            throw new ArgumentException("diagnostic_phase");
        var window = foreground(); // Exactly one foreground read per sample.
        var pid = window == 0 ? 0 : processId(window);
        var launcher = pid != 0 && pid == launcherPid;
        var relation = window == 0 ? "none" : window == windowA() ? "owned_a" :
            window == windowB() ? "owned_b" : launcher ? "launcher" : "foreign";
        return $"phase={phase} relation={relation} launcher_foreground={launcher} sample_order={++order}";
    }
}

/// <summary>Bounded read-only trace. PID/HWND/class strings never leave the sampler.</summary>
internal sealed class OwnedForegroundTrace(Func<nint> foreground, Func<nint, uint> processId,
    nint windowA, uint targetPid, uint workerPid, uint guardianPid, uint coordinatorPid, uint launcherPid,
    nint initial, Func<nint, string> windowKind, Func<long> milliseconds, Func<Task> delay)
{
    internal string Sample(int order, long elapsed)
    {
        var window = foreground();
        var pid = window == 0 ? 0 : processId(window);
        var relation = window == 0 ? "none" : pid == 0 ? "foreign" :
            pid == targetPid ? window == windowA ? "owned_a" : "a_other_window" :
            pid == workerPid ? "worker" : pid == guardianPid ? "guardian" :
            pid == coordinatorPid ? "coordinator" : pid == launcherPid ? "launcher" : "foreign";
        var kind = relation == "a_other_window" ? windowKind(window) : "not_applicable";
        if (kind is not ("owned_by_a" or "ime" or "tooltip" or "toplevel_other" or "not_applicable"))
            kind = "toplevel_other";
        return $"relation={relation} kind={kind} initial_foreground={window != 0 && window == initial} sample_order={order} elapsed_ms={elapsed}";
    }
    internal async Task<List<string>> RecordAsync()
    {
        var records = new List<string>();
        var start = milliseconds();
        // Count cap also bounds a stalled/nonadvancing clock. Scheduling is best effort, not a 5ms guarantee.
        for (var order = 1; order <= 51; order++)
        {
            var elapsed = milliseconds() - start;
            if (elapsed > 250) break;
            records.Add(Sample(order, elapsed));
            if (elapsed >= 250 || order == 51) break;
            await delay().ConfigureAwait(false);
        }
        return records;
    }
}

internal static class NativeHostReadiness
{
    // Same short-circuit order as the host's original compound check. Report the FIRST refusal.
    internal static bool Check(Func<bool> popup, Func<bool> popupHandle, Func<bool> main,
        Func<bool> mainHandle, Func<bool> store, Func<bool> popupModel, Func<bool> denied,
        Func<bool> foregroundUnchanged, Action<string> receipt)
    {
        var reason = !popup() ? "popup_missing" : !popupHandle() ? "popup_handle_missing" :
            !main() ? "main_missing" : !mainHandle() ? "main_handle_missing" :
            !store() ? "store_missing" : !popupModel() ? "popup_model_missing" :
            !denied() ? "insert_admitted" : !foregroundUnchanged() ? "foreground_changed" : "ready";
        receipt(reason);
        return reason == "ready";
    }
}
