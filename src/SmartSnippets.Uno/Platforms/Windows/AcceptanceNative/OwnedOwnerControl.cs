using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using SmartSnippets.Presentation.Services;

namespace SmartSnippets.Uno.Platforms.Windows.AcceptanceNative;

/// <summary>One-shot, control-only B ledger. Never admits a foreign owner or original bytes.</summary>
internal sealed class OwnedOwnerControl(GuardianClipboard clipboard, IGuardianClipboardOs ownerA,
    IGuardianClipboardOs ownerB, Func<bool> pinnedA, Func<bool> pinnedB,
    uint marker, IReadOnlyDictionary<uint, byte[]> publication)
{
    private bool attempted, transferred;

    internal bool VerifyRefusal(Func<bool> admitted)
    {
        if (attempted || !clipboard.RecoveryPending || !admitted() || !Pinned()) return false;
        attempted = true;
        if (!ownerB.TryOpen()) return false;
        try
        {
            using var current = ownerB.Read();
            if (ownerB.Owner != ownerA.Window || !Full(current) || !Pinned() || !admitted() ||
                publication.Values.Any(b => !ownerB.CanAllocateExact(b.Length)) || !Pinned() || !admitted()) return false;
            if (!ownerB.Empty()) return false;
            // Record before any write can fail. Partial B publication is NOT permission to overwrite it.
            transferred = true;
            if (ownerB.Owner != ownerB.Window) return false;
            foreach (var entry in publication.OrderBy(e => e.Key == marker ? 0 : 1))
                if (!ownerB.Write(entry.Key, entry.Value)) return false;
            using var published = ownerB.Read();
            if (ownerB.Owner != ownerB.Window || !Pinned() || !Full(published)) return false;
        }
        finally { ownerB.Close(); }
        // This is the production guardian refusal, not a simulated status.
        return clipboard.Restore() == RestoreStatus.ClipboardChanged && clipboard.HasBackup;
    }

    // Explicit OWN-test cleanup, independent of expired admission. On abort call only after worker death.
    internal RestoreStatus CleanupAndRestore()
    {
        if (!clipboard.HasBackup) return clipboard.Restore();
        if (!transferred) return clipboard.Restore();
        if (!Pinned()) return RestoreStatus.ClipboardChanged;
        if (!ownerA.TryOpen()) return RestoreStatus.Busy;
        try
        {
            // A partial return is already in the unchanged guardian's recognizable-state domain.
            if (ownerA.Owner != ownerA.Window)
            {
                using var current = ownerA.Read();
                if (ownerA.Owner != ownerB.Window || !Full(current) || !Pinned() ||
                    publication.Values.Any(b => !ownerA.CanAllocateExact(b.Length)) || !Pinned())
                    return RestoreStatus.ClipboardChanged;
                if (!ownerA.Empty()) return RestoreStatus.Failed;
                transferred = false; // A now owns any partial write; normal Restore can recover it.
                if (ownerA.Owner != ownerA.Window) return RestoreStatus.Partial;
                foreach (var entry in publication.OrderBy(e => e.Key == marker ? 0 : 1))
                    if (!ownerA.Write(entry.Key, entry.Value)) return RestoreStatus.Partial;
                using var returned = ownerA.Read();
                if (ownerA.Owner != ownerA.Window || !Full(returned)) return RestoreStatus.Partial;
            }
        }
        finally { ownerA.Close(); }
        return clipboard.Restore();
    }

    private bool Pinned() => ownerA.Window != 0 && ownerB.Window != 0 && ownerA.Window != ownerB.Window && pinnedA() && pinnedB();
    private bool Full(ClipboardImage image) => image.Complete && image.Entries.Count == publication.Count &&
        publication.All(e => image.Entries.TryGetValue(e.Key, out var bytes) && bytes.Length == e.Value.Length &&
            CryptographicOperations.FixedTimeEquals(bytes, e.Value));
}
