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
