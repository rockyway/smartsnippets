using System.Security.Cryptography;

namespace SmartSnippets.WindowsInsertion018;

/// <summary>Metadata policy only. All production callers must hold the OS clipboard lock.</summary>
internal static class ClipboardOwnershipPolicy
{
    internal static bool CanBegin(bool guardianPinned, nint guardian, nint originalOwner, uint originalSequence,
        nint currentOwner, uint currentSequence, bool complete) => guardianPinned && guardian != 0 && complete &&
        originalOwner == currentOwner && originalSequence == currentSequence;

    internal static bool CanRestoreAcknowledged(bool guardianPinned, nint guardian, uint expectedSequence,
        nint currentOwner, uint currentSequence) => guardianPinned && guardian != 0 &&
        currentOwner == guardian && currentSequence == expectedSequence;

    internal static bool CanRecoverStoppedWorker(bool guardianPinned, nint guardian, bool workerSignaled,
        nint currentOwner, bool recognizablePublication) => guardianPinned && guardian != 0 && workerSignaled &&
        currentOwner == guardian && recognizablePublication;

    internal static bool IsRecognizablePublication(IReadOnlyDictionary<uint, byte[]> current,
        IReadOnlyDictionary<uint, byte[]> expected, uint markerFormat, IReadOnlySet<uint> privacyFormats)
    {
        foreach (var entry in current)
            if (!expected.TryGetValue(entry.Key, out var bytes) || !ExactBytes(entry.Value, bytes)) return false;
        if (current.Count == 0) return true;
        if (!current.ContainsKey(markerFormat)) return false;
        if (current.ContainsKey(13) && !privacyFormats.All(current.ContainsKey)) return false;
        return !(current.ContainsKey(1) || current.ContainsKey(7)) || current.ContainsKey(13);
    }

    // Restore states require the unguessable run marker AND exact captured bytes.
    // An unmarked subset of the original is never evidence of our restoration.
    internal static bool IsRecognizableRestore(IReadOnlyDictionary<uint, byte[]> current,
        IReadOnlyDictionary<uint, byte[]> original, uint markerFormat, byte[] token, IReadOnlySet<uint> privacyFormats)
    {
        if (!current.TryGetValue(markerFormat, out var marker) || !ExactBytes(marker, token)) return false;
        return current.All(e => e.Key == markerFormat ||
            (original.TryGetValue(e.Key, out var bytes) ? ExactBytes(e.Value, bytes) :
                privacyFormats.Contains(e.Key) && OwnClipboardPublication.IsCompatibleMarker(e.Value)));
    }

    internal static bool ExactBytes(ReadOnlySpan<byte> actual, ReadOnlySpan<byte> expected) =>
        actual.Length == expected.Length && CryptographicOperations.FixedTimeEquals(actual, expected);
}
