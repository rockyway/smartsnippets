using System.Runtime.InteropServices;
using System.Security.Cryptography;
using SmartSnippets.Presentation.Services;
using SmartSnippets.WindowsInsertion018;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace SmartSnippets.App.Services;

// Prototype-only atomic seam. IPasteDispatcher cannot validate a separately
// retained guardian capture under the shipping method's internal clipboard lock.
// Do not nest OpenClipboard or claim these transactions are the shipping method.
// Snapshot, native representations, privacy markers and publication helpers below
// are the actual linked production implementation, not copies/reflection.
internal sealed partial class ClipboardPasteDispatcher
{
    internal sealed class GuardianBackup : IDisposable
    {
        private readonly Snapshot saved;
        internal nint OriginalOwner { get; }
        internal uint OriginalSequence { get; }
        internal bool Settled { get; private set; }
        private GuardianBackup(Snapshot saved, nint owner, uint sequence) =>
            (this.saved, OriginalOwner, OriginalSequence) = (saved, owner, sequence);
        public void Dispose()
        {
            foreach (var entry in saved.Entries) CryptographicOperations.ZeroMemory(entry.Data);
            saved.Entries.Clear();
            Settled = true;
        }

        internal static GuardianBackup? Capture(HWND window, OwnClipboardPublication publication)
        {
            if (!PInvoke.OpenClipboard(window)) return null;
            Snapshot? snapshot = null;
            try
            {
                var owner = NativeClipboardOwner();
                snapshot = TakeSnapshot();
                var sequence = GetClipboardSequenceNumber();
                // The shipping policy skips some handle/synthesized formats. This
                // experiment promises complete preservation and cannot use that waiver.
                if (snapshot.Entries.Any(e => e.Format == publication.MarkerFormat) || !Complete(snapshot, publication) ||
                    owner != NativeClipboardOwner())
                    return null;
                var result = new GuardianBackup(snapshot, owner, sequence);
                snapshot = null;
                return result;
            }
            finally
            {
                if (snapshot is not null) Zero(snapshot);
                PInvoke.CloseClipboard();
            }
        }

        // A failed or foreign-owner recovery retains this independent RAM backup.
        // Disposal is only authorized after a verified settlement, never on timeout.
        internal unsafe RestoreStatus RecoverStopped(HWND window, Func<bool> guardianPinned, bool workerSignaled,
            OwnClipboardPublication publication)
        {
            if (Settled || !guardianPinned() || !workerSignaled) return RestoreStatus.Failed;
            if (!PInvoke.OpenClipboard(window)) return RestoreStatus.Busy;
            try
            {
                var owner = NativeClipboardOwner();
                var sequence = GetClipboardSequenceNumber();
                if (owner == OriginalOwner && sequence == OriginalSequence) { Settled = true; return RestoreStatus.Restored; }
                if (owner != (nint)window.Value) return RestoreStatus.ClipboardChanged;
                var current = TakeSnapshot(); // One materialization under this lock; synthesis may advance sequence.
                bool recognizable;
                try
                {
                    if (MatchesOriginal(current, publication)) { Settled = true; return RestoreStatus.Restored; }
                    var entries = current.Entries.ToDictionary(e => e.Format, e => e.Data);
                    recognizable = Complete(current, publication) && (publication.IsRecognizable(entries) ||
                        ClipboardOwnershipPolicy.IsRecognizableRestore(entries, saved.Entries.ToDictionary(e => e.Format, e => e.Data),
                            publication.MarkerFormat, publication.Token, publication.PrivacyFormats));
                }
                finally { Zero(current); }
                if (!ClipboardOwnershipPolicy.CanRecoverStoppedWorker(guardianPinned(), (nint)window.Value, workerSignaled,
                    owner, recognizable)) return RestoreStatus.ClipboardChanged;
                var status = RestoreEntries(saved, publication);
                var restored = TakeSnapshot();
                try { if (status == RestoreStatus.Restored && MatchesOriginal(restored, publication)) Settled = true; }
                finally { Zero(restored); }
                return Settled ? RestoreStatus.Restored : RestoreStatus.Partial;
            }
            finally { PInvoke.CloseClipboard(); }
        }

        private bool MatchesOriginal(Snapshot current, OwnClipboardPublication publication)
        {
            if (!Complete(current, publication)) return false;
            var entries = current.Entries.ToDictionary(e => e.Format, e => e.Data);
            // Even a full original-byte match is not our publication without its run marker.
            if (!entries.Remove(publication.MarkerFormat, out var marker) ||
                !ClipboardOwnershipPolicy.ExactBytes(marker, publication.Token)) return false;
            foreach (var entry in saved.Entries)
                if (!entries.Remove(entry.Format, out var data) || !ClipboardOwnershipPolicy.ExactBytes(data, entry.Data)) return false;
            return entries.All(e => publication.PrivacyFormats.Contains(e.Key) && OwnClipboardPublication.IsCompatibleMarker(e.Value));
        }
    }

    private static bool Complete(Snapshot snapshot, OwnClipboardPublication publication) =>
        snapshot.UnsavedCount == 0 && snapshot.SkippedByDesignCount == 0 && !snapshot.CapExceeded &&
        snapshot.Entries.All(e => !publication.PrivacyFormats.Contains(e.Format) || OwnClipboardPublication.IsCompatibleMarker(e.Data));

    internal unsafe ClipboardSwap TryGuardedSwap(string text, Func<bool> guardianPinned, nint originalOwner,
        uint originalSequence, OwnClipboardPublication publication)
    {
        if (!guardianPinned() || clipboardOwner.IsNull || publication.MarkerFormat == 0 || publication.Token.Length != 16)
            return new(SwapStatus.Failed, null);
        if (!PInvoke.OpenClipboard(clipboardOwner)) return new(SwapStatus.Busy, null);
        Snapshot? snapshot = null;
        var cleared = false;
        try
        {
            if (!ClipboardOwnershipPolicy.CanBegin(guardianPinned(), (nint)clipboardOwner.Value, originalOwner, originalSequence,
                NativeClipboardOwner(), GetClipboardSequenceNumber(), true)) return new(SwapStatus.NotPreserved, null);
            snapshot = TakeSnapshot();
            // The entry epoch was checked before materialization under this same
            // lock. GetClipboardData may synthesize formats and advance it itself.
            if (!guardianPinned() || NativeClipboardOwner() != originalOwner || !Complete(snapshot, publication) ||
                snapshot.Entries.Any(e => e.Format == publication.MarkerFormat)) return new(SwapStatus.NotPreserved, null);
            if (!PInvoke.EmptyClipboard()) return new(SwapStatus.Failed, null);
            cleared = true;
            _pending = snapshot; // Every post-clear failure carries the original snapshot.
            // EmptyClipboard, not the OpenClipboard HWND parameter alone, must have
            // assigned the exact pinned guardian window. Check the actual native API.
            if (NativeClipboardOwner() != (nint)clipboardOwner.Value || !guardianPinned())
                return new(SwapStatus.Failed, snapshot);
            var written = SetData(publication.MarkerFormat, publication.Token) &&
                SetData(16, BitConverter.GetBytes(OwnClipboardPublication.PublishedLocale)) && SetText(text);
            return new(written ? SwapStatus.Swapped : SwapStatus.Failed, snapshot);
        }
        catch (Exception error) when (cleared && snapshot is not null)
        {
            throw new ClipboardSwapException(snapshot, error);
        }
        finally
        {
            if (cleared && snapshot is not null)
                snapshot.Sequence = ClipboardSafety.CaptureBeforeClose(GetClipboardSequenceNumber, () => PInvoke.CloseClipboard());
            else
            {
                if (snapshot is not null) Zero(snapshot);
                PInvoke.CloseClipboard();
            }
        }
    }

    internal unsafe RestoreStatus TryGuardedRestore(IClipboardSnapshot saved, Func<bool> guardianPinned, OwnClipboardPublication publication)
    {
        if (saved is not Snapshot snapshot || _pending != snapshot || !guardianPinned()) return RestoreStatus.ClipboardChanged;
        if (!PInvoke.OpenClipboard(clipboardOwner)) return RestoreStatus.Busy;
        var attempted = false;
        try
        {
            if (!ClipboardOwnershipPolicy.CanRestoreAcknowledged(guardianPinned(), (nint)clipboardOwner.Value, snapshot.Sequence,
                NativeClipboardOwner(), GetClipboardSequenceNumber())) return RestoreStatus.ClipboardChanged;
            attempted = true;
            var status = RestoreEntries(snapshot, publication);
            // Independent guardian backup remains armed even when worker restore fails.
            if (status == RestoreStatus.Restored) { _pending = null; Zero(snapshot); }
            return status;
        }
        finally
        {
            // A failed restore remains retryable by this worker at its actual locked epoch.
            if (attempted) snapshot.Sequence = ClipboardSafety.CaptureBeforeClose(GetClipboardSequenceNumber, () => PInvoke.CloseClipboard());
            else PInvoke.CloseClipboard();
        }
    }

    private static RestoreStatus RestoreEntries(Snapshot snapshot, OwnClipboardPublication publication)
    {
        if (!PInvoke.EmptyClipboard()) return RestoreStatus.Failed;
        // Marker FIRST: death/failure after clear is empty or an explicitly bound
        // subset of original bytes. Never publish an unmarked partial original.
        if (!SetData(publication.MarkerFormat, publication.Token)) return RestoreStatus.Partial;
        var failed = SetMarkers(out var markers) ? 0 : 1;
        foreach (var entry in snapshot.Entries)
            if (!markers.Contains(entry.Format) && !SetEntry(entry)) failed++;
        return failed == 0 ? RestoreStatus.Restored : RestoreStatus.Partial;
    }

    private static void Zero(Snapshot snapshot)
    {
        foreach (var entry in snapshot.Entries) CryptographicOperations.ZeroMemory(entry.Data);
        snapshot.Entries.Clear();
    }

    [LibraryImport("user32.dll", EntryPoint = "GetClipboardOwner")]
    private static partial nint NativeClipboardOwner();
}
