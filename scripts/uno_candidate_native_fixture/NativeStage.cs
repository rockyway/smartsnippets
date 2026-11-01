namespace SmartSnippets.Uno;

/// <summary>
/// Last fixed orchestration stage reached by the native fixture worker, not a precise failure location.
/// target_foreground_grant is the last pre-trigger stage; it remains set for later posted-scene
/// readiness, keyboard-permit and insertion failures too. Only the allowlisted
/// tokens below are ever recorded or logged; exception messages, stacks and identities never are.
/// scripts/run_uno_candidate_native.py mirrors this list (NATIVE_STAGES) and a test keeps them equal.
/// </summary>
internal static class NativeStage
{
    internal const string Prefix = "native_positive_failed stage=";
    internal static readonly string[] Tokens =
    [
        "start", "review_gate", "fixture_start", "fixture_owned", "host_start", "host_ready",
        "session_create", "session_probe", "session_capture", "guardian_image", "guardian_spawn",
        "guardian_bootstrap", "guardian_wait", "session_focus", "broker_bind", "outside_job",
        "guardian_dup", "guardian_ready", "target_foreground_grant",
    ];
    private static volatile string last = "start";
    internal static string Last => last;
    /// <summary>Records a stage; any value outside the allowlist is ignored, never logged.</summary>
    internal static void Reach(string token)
    {
        if (Array.IndexOf(Tokens, token) >= 0) last = token;
    }
    /// <summary>True while the host (which opens app.log) may not have started yet.</summary>
    internal static bool BeforeHostLog => Array.IndexOf(Tokens, last) <= Array.IndexOf(Tokens, "host_start");
    internal static void Reset() => last = "start";
    /// <summary>Fixed event name for the sanitized failure log line (type and HResult are appended by AppLog).</summary>
    internal static string FailureEvent => Prefix + last;
}
