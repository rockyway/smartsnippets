using SmartSnippets.Uno.Platforms.Windows;

namespace SmartSnippets.Uno;

internal static class PositiveProtocol
{
    // Independent review of this orchestration AND native input is required, not a command-line unlock.
    internal static readonly bool SourceReviewed = false;
    internal static readonly bool InputSourceReviewed = false;
    internal const string Permit = "owned_positive_row6_verified_operator_recopied_baseline\n";
    internal static bool IsPositive(string scene) => scene is "posted-enter" or "posted-row" or "posted-main" or "copy" or "preview" or "expired";
    internal static bool Inserts(string scene) => scene is "posted-enter" or "posted-row" or "posted-main";
    internal static CandidateNativeOperation Permissions(string scene) => Inserts(scene)
        ? CandidateNativeOperation.Capture | CandidateNativeOperation.Insert | CandidateNativeOperation.Focus | CandidateNativeOperation.Input | CandidateNativeOperation.Clipboard
        : scene is "copy" or "preview" or "expired" ? CandidateNativeOperation.Copy | CandidateNativeOperation.Focus : throw new InvalidOperationException("native_scene");
    internal static void RequireReviewed(string scene)
    {
        if (!IsPositive(scene) || !SourceReviewed || (Inserts(scene) && !InputSourceReviewed))
            throw new InvalidOperationException("native_positive_unreviewed");
    }
    internal static string Result(string scene) => IsPositive(scene) ? "native_result=" + scene + "_settled" : throw new InvalidOperationException("native_scene");
    internal static void RequireSurfaces(string line)
    {
        var fields = line.Split(' ');
        if (fields.Length != 5 || fields[0] != "positive_surfaces") throw new IOException("native_surfaces");
        var handles = fields.Skip(1).Select(BootstrapProtocol.Handle).ToArray();
        if (handles.Distinct().Count() != 4) throw new IOException("native_surfaces");
    }
}

/// <summary>Loss of a keyboard completion receipt is NOT permission to abort a Job containing possible downs.</summary>
internal static class PositiveInputPreflight
{
    internal static bool Ready(Func<bool> lease, Func<bool> visibleCurrentDesktop, Func<bool> quietAllVkUp, Func<bool> exactPinnedForeground) =>
        lease() && visibleCurrentDesktop() && quietAllVkUp() && lease() && exactPinnedForeground();
}

internal sealed class PositiveEnterGuard
{
    internal bool Pending { get; private set; }
    internal static SmartSnippets.Presentation.Services.DispatchState Classify(uint count) => count == 2
        ? SmartSnippets.Presentation.Services.DispatchState.Dispatched : count == 0
        ? SmartSnippets.Presentation.Services.DispatchState.NotDispatched : SmartSnippets.Presentation.Services.DispatchState.Uncertain;
    internal SmartSnippets.Presentation.Services.DispatchState Send(Func<bool> permitted, Func<SmartSnippets.Presentation.Services.DispatchState> send)
    {
        if (Pending || !permitted()) return SmartSnippets.Presentation.Services.DispatchState.NotDispatched;
        Pending = true; // A throw is ambiguous; do not infer which downs were delivered.
        var result = send();
        Pending = result == SmartSnippets.Presentation.Services.DispatchState.Uncertain;
        return result;
    }
}

internal sealed class PositiveKeyboardLifetime
{
    internal bool Armed { get; private set; }
    private bool used;
    internal void Arm() { if (used) throw new InvalidOperationException("native_keyboard_order"); used = true; Armed = true; }
    internal void Settle() { if (!Armed) throw new InvalidOperationException("native_keyboard_order"); Armed = false; }
    internal async Task RetainAsync(Action pending, Func<Task> wait)
    {
        pending();
        while (Armed) await wait(); // No generic key-up, unsafe abort or timeout-based inferred settlement.
    }
}
