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

    internal static async Task<OwnedPreparationResult> PrepareAsync(IOwnedPreparationOs os,
        Func<IOwnedPreparationAutomation> automation, nint originalForeground, nint windowA, nint editorA, CancellationToken ct)
    {
        bool Guard() => !ct.IsCancellationRequested && os.Permitted() && os.PinnedIdentity() && os.VisibleDesktop();
        bool Initial() => Guard() && originalForeground != 0 &&
            (os.Foreground == originalForeground || os.Foreground == windowA) && os.QuietAllInput();
        if (!Initial()) return OwnedPreparationResult.Refused;
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
                var focused = client.SetFocus(); // Exactly once; a failure never falls back to manual activation.
                if (!Guard()) return OwnedPreparationResult.Refused;
                if (!focused) return OwnedPreparationResult.AutomationFailed;
                if (os.Foreground != windowA) return OwnedPreparationResult.ForegroundRefused;
            }
            catch { return OwnedPreparationResult.AutomationFailed; }
        }
        if (!Guard() || os.Foreground != windowA || !os.TryInputEpoch(out var after)) return OwnedPreparationResult.Refused;
        var changed = after != epoch;
        var start = os.Milliseconds;
        while (true)
        {
            if (!Guard() || os.Foreground != windowA) return OwnedPreparationResult.Refused;
            if ((!changed || os.Milliseconds - start >= 1500) && os.QuietAllInput()) break;
            if (os.Milliseconds - start >= 3000) return OwnedPreparationResult.QuietTimeout;
            await os.DelayAsync(ct);
        }
        if (!Guard() || os.Foreground != windowA || !os.QuietAllInput()) return OwnedPreparationResult.Refused;
        await os.FocusEditorAsync(ct); // Existing owned command, only after A is proven foreground.
        return Guard() && os.Foreground == windowA && os.TargetForeground() && os.QuietAllInput()
            ? alreadyA ? OwnedPreparationResult.AlreadyA : OwnedPreparationResult.Prepared
            : OwnedPreparationResult.Refused;
    }
}
