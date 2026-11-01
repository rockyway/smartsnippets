using SmartSnippets.Uno.Platforms.Windows;

namespace SmartSnippets.Uno;

internal static class PositiveProtocol
{
    // Independent review of this orchestration AND native input is required, not a command-line unlock.
    internal static readonly bool SourceReviewed = false;
    internal static readonly bool InputSourceReviewed = false;
    internal const string Permit = "owned_positive_row6_verified_operator_recopied_baseline\n";
    internal const string ControlPermit = "owned_clipboard_controls_only\n";
    internal const string PriorPositivePermit = "owned_positive_row6_verified_settled_baseline_v2\n";
    internal const string PriorControlPermit = "owned_clipboard_controls_settled_baseline_v2\n";
    internal static Guid? ReadPriorToken(byte[] bytes, bool positive)
    {
        var legacy = System.Text.Encoding.ASCII.GetBytes(positive ? Permit : ControlPermit);
        if (bytes.AsSpan().SequenceEqual(legacy)) return null;
        var header = System.Text.Encoding.ASCII.GetBytes(positive ? PriorPositivePermit : PriorControlPermit);
        if (bytes.Length != header.Length + 16 || !bytes.AsSpan(0, header.Length).SequenceEqual(header))
            throw new InvalidOperationException("native_baseline_permit");
        var token = new Guid(bytes.AsSpan(header.Length, 16));
        if (token == Guid.Empty) throw new InvalidOperationException("native_baseline_token");
        return token;
    }
    internal static bool IsPositive(string scene) => scene is "registered-enter" or "registered-row" or "registered-main" or "posted-enter" or "posted-row" or "posted-main" or "copy" or "preview" or "expired";
    internal static bool Inserts(string scene) => scene is "registered-enter" or "registered-row" or "registered-main" or "posted-enter" or "posted-row" or "posted-main";
    internal static bool RequiresInputReview(string scene) => Inserts(scene) || scene == "expired";
    internal static CandidateNativeOperation Permissions(string scene) => scene == "expired"
        ? Permissions("posted-enter") | CandidateNativeOperation.Copy
        : Inserts(scene)
        ? CandidateNativeOperation.Capture | CandidateNativeOperation.Insert | CandidateNativeOperation.Focus | CandidateNativeOperation.Input | CandidateNativeOperation.Clipboard
        : scene is "copy" or "preview" or "expired" ? CandidateNativeOperation.Copy | CandidateNativeOperation.Focus : throw new InvalidOperationException("native_scene");
    internal static void RequireReviewed(string scene)
    {
        if (!IsPositive(scene) || !SourceReviewed || (RequiresInputReview(scene) && !InputSourceReviewed))
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
    // Drain completion, even a false recovery result, precedes every independent guardian retry.
    internal async Task<bool> DrainAndSettleAsync(Action closeAdmission, Func<Task> drain,
        Func<Task> restore, Func<bool> pending, Func<Task> receipt)
    {
        try
        {
            closeAdmission();
            await drain(); // Never restore alongside an active dispatcher/VM operation.
            await restore();
            if (pending()) return false;
            await receipt();
            Settle();
            return true;
        }
        catch { return false; } // Lost receipts and recovery failures retain ownership, not inferred success.
    }
    internal async Task RetainAsync(Action pending, Func<Task> wait)
    {
        pending();
        while (Armed) await wait(); // No generic key-up, unsafe abort or timeout-based inferred settlement.
    }
}

/// <summary>Routes shortcut delivery only; operator keys never acquire an agent keyboard lifetime.</summary>
internal sealed class PositiveShortcutRoute
{
    internal const string ReadyFile = "registered-shortcut-ready";
    internal const string ReadyReceipt = "registered_os_shortcut_operator_ready\n";
    internal int Callbacks { get; private set; }
    internal bool AcceptCallback() => ++Callbacks == 1;
    internal static bool IsRegistered(string scene) => scene is "registered-enter" or "registered-row" or "registered-main";
    internal static bool MainRow(string scene) => scene is "registered-main" or "posted-main";
    internal static bool NativeEnter(string scene) => scene is "registered-enter" or "posted-enter";
    internal static async Task<TimeSpan> BeginAsync(string scene, Action post, Func<Task> ready)
    {
        if (!PositiveProtocol.Inserts(scene)) throw new InvalidOperationException("native_scene");
        if (IsRegistered(scene))
        {
            await ready();
            return TimeSpan.FromSeconds(25);
        }
        post();
        return TimeSpan.FromSeconds(5);
    }
    internal static async Task WriteReadyAsync(CancellationToken ct)
    {
        // Fixed names in the owner-private fixture cwd; exclusive publication, never an external path.
        var temporary = ReadyFile + ".tmp";
        await using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await stream.WriteAsync(System.Text.Encoding.ASCII.GetBytes(ReadyReceipt), ct);
            await stream.FlushAsync(ct);
        }
        File.Move(temporary, ReadyFile); // Atomic visibility, no overwrite of an existing readiness receipt.
    }
}
