namespace SmartSnippets.Uno;

internal interface IOwnedPreparationOs
{
    bool PinnedIdentity();
    bool Permitted();
    bool VisibleDesktop();
    bool QuietAllInput();
    nint Foreground { get; }
    bool TryInputEpoch(out uint epoch);
    long Milliseconds { get; }
    Task DelayAsync(CancellationToken ct);
    Task FocusEditorAsync(CancellationToken ct);
    bool TargetForeground();
}
internal interface IOwnedPreparationAutomation : IDisposable
{
    bool ElementFromHandle(nint editor);
    bool SetFocus();
}
internal enum OwnedPreparationResult { Prepared, AlreadyA, Refused, AutomationFailed, ForegroundRefused, QuietTimeout }

/// <summary>Intentional owned activation, never nonactivation QA evidence. No keyboard or clipboard operations.</summary>
internal static class OwnedTargetPreparation
{
    internal static readonly bool SourceReviewed = false;
    internal static bool ShouldPrepare(string scene) => SourceReviewed && PositiveProtocol.IsPositive(scene);
    internal static bool IsExpectedEditorClass(ReadOnlySpan<char> value) => value.SequenceEqual("Edit");

    internal static async Task<OwnedPreparationResult> PrepareAsync(IOwnedPreparationOs os,
        Func<IOwnedPreparationAutomation> automation, nint originalForeground, nint windowA, nint editorA, CancellationToken ct,
        Action<string>? initialReceipt = null, Action<string>? sample = null)
    {
        sample?.Invoke("preparation_start");
        bool Guard() => !ct.IsCancellationRequested && os.Permitted() && os.PinnedIdentity() && os.VisibleDesktop();
        bool Initial() => Guard() && originalForeground != 0 &&
            (os.Foreground == originalForeground || os.Foreground == windowA) && os.QuietAllInput();
        string InitialReason()
        {
            if (ct.IsCancellationRequested) return "cancelled";
            if (!os.Permitted()) return "permission";
            if (!os.PinnedIdentity()) return "identity";
            if (!os.VisibleDesktop()) return "desktop";
            if (originalForeground == 0 || (os.Foreground != originalForeground && os.Foreground != windowA)) return "foreground";
            return os.QuietAllInput() ? "ready" : "unquiet";
        }
        // Startup can refresh the input timestamp. Wait read-only for quiet; never retry
        // automation or accept a foreground transfer while waiting for admission.
        var initialForeground = os.Foreground;
        var initialStart = os.Milliseconds;
        while (true)
        {
            var reason = InitialReason();
            if (os.Foreground != initialForeground) reason = "foreground";
            if (reason == "ready") break;
            if (reason != "unquiet" || os.Milliseconds - initialStart >= 4000)
            {
                initialReceipt?.Invoke(reason == "unquiet" ? "quiet_timeout" : reason);
                return OwnedPreparationResult.Refused;
            }
            await os.DelayAsync(ct);
        }
        initialReceipt?.Invoke("ready");
        if (!Initial() || os.Foreground != initialForeground) return OwnedPreparationResult.Refused;
        var alreadyA = os.Foreground == windowA;
        uint epoch;
        if (!os.TryInputEpoch(out epoch)) return OwnedPreparationResult.Refused;
        if (!alreadyA)
        {
            try
            {
                using var client = automation();
                if (!Initial()) return OwnedPreparationResult.Refused;
                if (!client.ElementFromHandle(editorA)) return OwnedPreparationResult.AutomationFailed;
                // Element resolution may run provider code: repeat every guard and capture epoch right at SetFocus.
                if (!Initial() || os.Foreground != originalForeground || !os.TryInputEpoch(out epoch))
                    return OwnedPreparationResult.Refused;
                sample?.Invoke("before_focus");
                var focused = client.SetFocus(); // Exactly once; a failure never falls back to manual activation.
                sample?.Invoke("after_focus");
                if (!Guard()) return OwnedPreparationResult.Refused;
                if (!focused) return OwnedPreparationResult.AutomationFailed;
                if (os.Foreground != windowA) return OwnedPreparationResult.ForegroundRefused;
            }
            catch { return OwnedPreparationResult.AutomationFailed; }
        }
        if (!Guard() || os.Foreground != windowA || !os.TryInputEpoch(out var after)) return OwnedPreparationResult.Refused;
        var changed = after != epoch;
        if (alreadyA && changed) return OwnedPreparationResult.Refused;
        bool EpochStable() => os.TryInputEpoch(out var current) && current == after;
        var start = os.Milliseconds;
        while (true)
        {
            if (!Guard() || os.Foreground != windowA || !EpochStable()) return OwnedPreparationResult.Refused;
            if ((!changed || os.Milliseconds - start >= 1500) && os.QuietAllInput()) break;
            if (os.Milliseconds - start >= 3000) return OwnedPreparationResult.QuietTimeout;
            await os.DelayAsync(ct);
        }
        if (!Guard() || os.Foreground != windowA || !os.QuietAllInput() || !EpochStable()) return OwnedPreparationResult.Refused;
        await os.FocusEditorAsync(ct); // Existing owned command, only after A is proven foreground.
        return Guard() && os.Foreground == windowA && os.TargetForeground() && os.QuietAllInput() && EpochStable()
            ? alreadyA ? OwnedPreparationResult.AlreadyA : OwnedPreparationResult.Prepared
            : OwnedPreparationResult.Refused;
    }
}
