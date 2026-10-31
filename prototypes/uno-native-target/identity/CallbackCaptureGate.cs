using SmartSnippets.Presentation.Services;

namespace SmartSnippets.TargetIdentity018;

// Native ID is generation-tagged. No activation or async continuation in this gate.
internal sealed class CallbackCaptureGate(OwnedTargetProbe probe, InsertionTarget original, uint generation)
{
    internal uint Generation { get; } = generation;
    internal int Deliveries { get; private set; }
    internal int Captures { get; private set; }
    internal bool Failed { get; private set; }
    internal bool Active { get; set; } = true;
    internal InsertionTarget? Captured { get; private set; }

    internal void Deliver(uint signature, uint id)
    {
        try
        {
            if (!Active || signature != NativeCallbackLease.Signature || id != Generation) return;
            Deliveries++;
            if (probe.Check(original) != TargetCheck.Ok || !probe.IsTargetForeground(original)) return;
            var captured = probe.Capture("a");
            if (captured != original || !probe.IsTargetForeground(captured)) { Failed = true; return; }
            Captured = captured;
            Captures++;
        }
        catch { Failed = true; } // Never unwind through WndProc/Carbon.
    }
}
