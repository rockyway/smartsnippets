using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using SmartSnippets.Presentation.Services;

namespace SmartSnippets.Uno.Platforms.Windows.AcceptanceNative;

/// <summary>All methods except TryOpen/Close require the same OS clipboard lock.</summary>
internal interface IGuardianClipboardOs
{
    nint Window { get; }
    bool TryOpen();
    void Close();
    nint Owner { get; }
    ClipboardImage Read();
    bool Empty();
    bool CanAllocateExact(int byteCount);
    bool Write(uint format, byte[] bytes);
}

internal sealed class ClipboardImage(Dictionary<uint, byte[]> entries, bool complete) : IDisposable
{
    internal Dictionary<uint, byte[]> Entries { get; } = entries;
    internal bool Complete { get; } = complete;
    public void Dispose()
    {
        foreach (var bytes in Entries.Values) CryptographicOperations.ZeroMemory(bytes);
        Entries.Clear();
    }
}

/// <summary>Runs only in the independent guardian. RAM is never disposed on an unsettled exit.</summary>
internal sealed class GuardianClipboard(IGuardianClipboardOs os, uint marker, byte[] token,
    IReadOnlySet<uint> privacy, IReadOnlyDictionary<uint, byte[]> publication, byte[]? knownPriorToken = null)
{
    internal const string SyntheticText = "AΩ中🙂e\u0301\r\nsecond line\r\n";
    private ClipboardImage? original;
    private nint originalOwner;
    private bool mutated;
    internal bool RecoveryPending => original is not null && mutated;
    internal bool HasBackup => original is not null;
    internal int SavedCount => original?.Entries.Count ?? 0;
    internal long ByteCount => original?.Entries.Sum(e => (long)e.Value.Length) ?? 0;
    internal bool Settled { get; private set; }

    internal SwapStatus Publish(Func<bool> admitted)
    {
        if (original is not null || Settled || marker == 0 || token.Length != 16 || privacy.Count != 3 ||
            privacy.Contains(0) || publication.Count == 0 ||
            (knownPriorToken is not null && (knownPriorToken.Length != 16 || knownPriorToken.All(b => b == 0) || Exact(knownPriorToken, token))) ||
            !admitted()) return SwapStatus.NotPreserved;
        if (!os.TryOpen()) return SwapStatus.Busy;
        try
        {
            originalOwner = os.Owner;
            original = os.Read();
            var hasPriorMarker = original.Entries.TryGetValue(marker, out var oldMarker);
            if (!original.Complete || (hasPriorMarker && (knownPriorToken is null || originalOwner != 0 ||
                !Exact(oldMarker!, knownPriorToken) || !original.Entries.Keys.Any(k => k != marker && !privacy.Contains(k)))) ||
                original.Entries.Any(e => privacy.Contains(e.Key) && (e.Value.Length != 4 || e.Value.Any(b => b != 0))) ||
                os.Owner != originalOwner || !admitted())
            { Settle(); return SwapStatus.NotPreserved; }
            // Validate the actual materialized representations, not sequence arithmetic.
            using (var current = os.Read())
                if (!Same(current, original) || os.Owner != originalOwner || !admitted())
                { Settle(); return SwapStatus.NotPreserved; }
            // Both reads include the old marker. It must never enter the new recovery image.
            if (hasPriorMarker)
            {
                CryptographicOperations.ZeroMemory(oldMarker!);
                original.Entries.Remove(marker);
            }
            // Refuse allocator rounding before destroying the baseline. Write rechecks every handle.
            if (publication.Values.Concat(original.Entries.Values).Select(b => b.Length).Append(token.Length).Append(4)
                .Distinct().Any(length => !os.CanAllocateExact(length)))
            { Settle(); return SwapStatus.NotPreserved; }
            if (os.Owner != originalOwner || !admitted()) { Settle(); return SwapStatus.NotPreserved; }
            if (!os.Empty()) { Settle(); return SwapStatus.Failed; }
            mutated = true; // Set before any subsequent native call can fail.
            if (os.Owner != os.Window || !os.Write(marker, token)) return SwapStatus.Failed;
            foreach (var entry in publication)
                if (entry.Key != marker && !os.Write(entry.Key, entry.Value)) return SwapStatus.Failed;
            using var published = os.Read();
            return IsFullPublication(published) && os.Owner == os.Window ? SwapStatus.Swapped : SwapStatus.Failed;
        }
        finally { os.Close(); }
    }

    // Copy is a separate, explicit commit. Before commit, worker death restores it like insertion.
    internal bool CommitCopy(Func<bool> admitted)
    {
        if (!RecoveryPending || !admitted() || !os.TryOpen()) return false;
        try
        {
            using var current = os.Read();
            if (os.Owner != os.Window || !IsFullPublication(current) || !admitted()) return false;
            Settle();
            return true;
        }
        finally { os.Close(); }
    }

    // Cleanup is deliberately independent of permission, cancellation, expiry and worker admission.
    internal RestoreStatus Restore()
    {
        if (Settled) return RestoreStatus.Restored;
        if (original is null) return RestoreStatus.Failed;
        if (!mutated) { Settle(); return RestoreStatus.Restored; }
        if (!os.TryOpen()) return RestoreStatus.Busy;
        try
        {
            if (os.Owner != os.Window) return RestoreStatus.ClipboardChanged;
            using (var current = os.Read())
            {
                if (!current.Complete || !Recognizable(current)) return RestoreStatus.ClipboardChanged;
                if (MatchesRestored(current)) { Settle(); return RestoreStatus.Restored; }
            }
            if (!os.Empty()) return RestoreStatus.Failed;
            if (os.Owner != os.Window || !os.Write(marker, token)) return RestoreStatus.Partial;
            foreach (var format in privacy)
                if (!os.Write(format, new byte[4])) return RestoreStatus.Partial;
            foreach (var entry in original.Entries)
                if (!privacy.Contains(entry.Key) && !os.Write(entry.Key, entry.Value)) return RestoreStatus.Partial;
            using var restored = os.Read();
            if (os.Owner != os.Window || !MatchesRestored(restored)) return RestoreStatus.Partial;
            Settle();
            return RestoreStatus.Restored;
        }
        finally { os.Close(); }
    }

    private bool Recognizable(ClipboardImage image)
    {
        // A verified EmptyClipboard by this sole writer can leave an empty owned state
        // if marker allocation failed. No other unmarked subset is accepted.
        if (image.Entries.Count == 0) return mutated;
        if (!image.Entries.TryGetValue(marker, out var value) || !Exact(value, token)) return false;
        return MatchesPublicationSubset(image) || (original is not null && MatchesSubset(image, original.Entries));
    }

    private bool MatchesSubset(ClipboardImage image, IReadOnlyDictionary<uint, byte[]> expected) => image.Entries.All(e =>
        e.Key == marker ? Exact(e.Value, token) : expected.TryGetValue(e.Key, out var value) ? Exact(e.Value, value) :
            privacy.Contains(e.Key) && e.Value.Length == 4 && e.Value.All(b => b == 0));

    private bool MatchesPublicationSubset(ClipboardImage image) => image.Entries.All(e =>
        publication.TryGetValue(e.Key, out var value) && Exact(e.Value, value));

    private bool IsFullPublication(ClipboardImage image) => image.Complete && MatchesPublicationSubset(image) &&
        publication.All(e => image.Entries.TryGetValue(e.Key, out var value) && Exact(value, e.Value));

    private bool MatchesRestored(ClipboardImage image) => image.Complete && original is not null &&
        image.Entries.Count == original.Entries.Keys.Concat(privacy).Distinct().Count() + 1 && MatchesSubset(image, original.Entries) &&
        image.Entries.ContainsKey(marker) && original.Entries.All(e => image.Entries.TryGetValue(e.Key, out var value) && Exact(value, e.Value));

    private static bool Same(ClipboardImage a, ClipboardImage b) => a.Complete && b.Complete &&
        a.Entries.Count == b.Entries.Count && a.Entries.All(e => b.Entries.TryGetValue(e.Key, out var value) && Exact(value, e.Value));
    private static bool Exact(byte[] a, byte[] b) => a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
    private void Settle() { original?.Dispose(); original = null; Settled = true; }
}
