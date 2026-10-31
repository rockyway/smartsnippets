using SmartSnippets.Presentation.Services;
using SmartSnippets.TargetIdentity018;

namespace SmartSnippets.WindowsInsertion018;

// Intentionally has no target, focus, insertion-service or input capability.
internal static class ClipboardOnlyWorkerFlow
{
    internal static async Task<bool> WaitForDesktopAsync(Func<bool> ready, Func<Task> delay)
    {
        for (var attempt = 0; attempt < 30; attempt++)
        {
            if (ready()) return true;
            await delay();
        }
        return ready(); // Still require the real guard; timeout never grants permission.
    }

    internal static void Execute(Func<ClipboardSwap> swap, Action awaitOwnedAbort)
    {
        var result = swap();
        if (result.Status != SwapStatus.Swapped || result.Snapshot is null)
            throw new ProbeFailure("clipboard_only_swap_refused");
        awaitOwnedAbort();
    }
}
