using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Uno.Platforms.Windows.AcceptanceNative;
using Xunit;

namespace SmartSnippets.Uno.Tests.AcceptanceNative;

public sealed class GuardianClipboardTests
{
    private const uint Marker = 500;
    private static readonly byte[] Token = Enumerable.Range(1, 16).Select(x => (byte)x).ToArray();
    private static readonly HashSet<uint> Privacy = [501, 502, 503];
    private static Dictionary<uint, byte[]> Publication() => new()
    {
        [Marker] = Token.ToArray(), [501] = new byte[4], [502] = new byte[4], [503] = new byte[4],
        [13] = Encoding.Unicode.GetBytes(GuardianClipboard.SyntheticText + "\0"), [16] = BitConverter.GetBytes(0x409)
    };
    private static GuardianClipboard Create(FakeClipboard os) => new(os, Marker, Token, Privacy, Publication());

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void IncompleteOrUnsupportedSnapshot_RefusesBeforeClear(bool unsupported)
    {
        var os = new FakeClipboard { Complete = !unsupported };
        if (!unsupported) os.Data[Marker] = Token.ToArray();
        var engine = Create(os);
        Assert.Equal(SwapStatus.NotPreserved, engine.Publish(() => true));
        Assert.Equal(0, os.EmptyCalls);
        Assert.False(engine.HasBackup);
    }

    [Fact]
    public void ActualOwnerAndRepresentationChange_RefusesWithoutSequenceArithmetic()
    {
        var os = new FakeClipboard();
        os.BeforeRead = count => { if (count == 2) os.Data[13] = [7, 0]; };
        Assert.Equal(SwapStatus.NotPreserved, Create(os).Publish(() => true));
        Assert.Equal(0, os.EmptyCalls);
    }

    [Fact]
    public void ExpiryAtFinalAdmission_RefusesBeforeClear()
    {
        var os = new FakeClipboard();
        var checks = 0;
        Assert.Equal(SwapStatus.NotPreserved, Create(os).Publish(() => ++checks < 3));
        Assert.Equal(0, os.EmptyCalls);
    }

    [Fact]
    public void IncompatiblePrivacyMarker_RefusesBeforeClear()
    {
        var os = new FakeClipboard(); os.Data[501] = [1, 0, 0, 0];
        Assert.Equal(SwapStatus.NotPreserved, Create(os).Publish(() => true));
        Assert.Equal(0, os.EmptyCalls);
    }

    [Fact]
    public void PublicationAndRestore_AreAtomicAndVerifyActualBytes()
    {
        var os = new FakeClipboard();
        var original = os.Data[13].ToArray();
        var engine = Create(os);
        Assert.Equal(SwapStatus.Swapped, engine.Publish(() => true));
        Assert.True(engine.RecoveryPending);
        Assert.Equal(Token, os.Data[Marker]);
        Assert.Equal(RestoreStatus.Restored, engine.Restore());
        Assert.Equal(original, os.Data[13]);
        Assert.True(Privacy.All(os.Data.ContainsKey));
        Assert.False(engine.HasBackup);
        Assert.True(engine.Settled);
        Assert.Equal(2, os.EmptyCalls);
        Assert.False(os.Open);
    }

    [Theory]
    [InlineData(1)] // Marker allocation failed after clear.
    [InlineData(3)] // Partial protected publication.
    public void FailedPublication_RetainsRamAndRecoversEvenUnmarkedEmptyOwnedState(int failAt)
    {
        var os = new FakeClipboard { FailWrite = failAt };
        var engine = Create(os);
        Assert.Equal(SwapStatus.Failed, engine.Publish(() => true));
        Assert.True(engine.RecoveryPending);
        os.FailWrite = 0;
        Assert.Equal(RestoreStatus.Restored, engine.Restore());
        Assert.False(engine.HasBackup);
    }

    [Fact]
    public void ThrowAfterClear_KeepsRecoveryBackup()
    {
        var os = new FakeClipboard { ThrowWrite = true };
        var engine = Create(os);
        Assert.Throws<InvalidOperationException>(() => engine.Publish(() => true));
        Assert.True(engine.RecoveryPending);
        Assert.False(os.Open);
        os.ThrowWrite = false;
        Assert.Equal(RestoreStatus.Restored, engine.Restore());
    }

    [Fact]
    public void ForeignOwner_RetainsBackupWithoutClearOrOverwriting()
    {
        var os = new FakeClipboard(); var engine = Create(os);
        Assert.Equal(SwapStatus.Swapped, engine.Publish(() => true));
        os.Owner = 999; os.Data.Clear(); os.Data[13] = [9, 8];
        Assert.Equal(RestoreStatus.ClipboardChanged, engine.Restore());
        Assert.True(engine.HasBackup);
        Assert.Equal(1, os.EmptyCalls);
        Assert.Equal(new byte[] { 9, 8 }, os.Data[13]);
    }

    [Fact]
    public void ExactMarkerButForeignPayload_RefusesRecoveryAndKeepsBackup()
    {
        var os = new FakeClipboard(); var engine = Create(os);
        engine.Publish(() => true); os.Data[13] = [6, 6];
        Assert.Equal(RestoreStatus.ClipboardChanged, engine.Restore());
        Assert.True(engine.HasBackup);
        Assert.Equal(1, os.EmptyCalls);
    }

    [Fact]
    public void UnmarkedNonemptySubset_IsNotOwnedRecovery()
    {
        var os = new FakeClipboard(); var engine = Create(os);
        engine.Publish(() => true); os.Data.Remove(Marker);
        Assert.Equal(RestoreStatus.ClipboardChanged, engine.Restore());
        Assert.True(engine.HasBackup);
    }

    [Fact]
    public void FailedRestore_IsRetryableWithMarkerAndActualOriginalSubset()
    {
        var os = new FakeClipboard(); var engine = Create(os);
        engine.Publish(() => true);
        os.FailWrite = os.WriteCalls + 3;
        Assert.Equal(RestoreStatus.Partial, engine.Restore());
        Assert.True(engine.HasBackup);
        Assert.Equal(Token, os.Data[Marker]);
        os.FailWrite = 0;
        Assert.Equal(RestoreStatus.Restored, engine.Restore());
    }

    [Fact]
    public void SuccessfulCopy_RequiresSeparateCommitAndZerosOriginalRam()
    {
        var os = new FakeClipboard(); var engine = Create(os);
        engine.Publish(() => true);
        var savedBytes = os.ReadImages[0].Entries[13];
        Assert.False(engine.CommitCopy(() => false));
        Assert.True(engine.HasBackup);
        Assert.True(engine.CommitCopy(() => true));
        Assert.All(savedBytes, value => Assert.Equal(0, value));
        Assert.False(engine.HasBackup);
        Assert.Equal(Publication()[13], os.Data[13]);
    }

    [Fact]
    public void RestoreDoesNotDependOnAdmissionOrLifetime()
    {
        var os = new FakeClipboard(); var engine = Create(os);
        engine.Publish(() => true);
        Assert.False(engine.CommitCopy(() => false));
        Assert.Equal(RestoreStatus.Restored, engine.Restore());
        Assert.True(engine.Settled);
    }

    [Fact]
    public void BusyKeepsRamAndNeverClearsUntilRetry()
    {
        var os = new FakeClipboard(); var engine = Create(os);
        engine.Publish(() => true); os.Busy = true;
        Assert.Equal(RestoreStatus.Busy, engine.Restore());
        Assert.True(engine.HasBackup);
        os.Busy = false;
        Assert.Equal(RestoreStatus.Restored, engine.Restore());
    }

    [Fact]
    public void AllocationMismatch_RefusesBeforeClearAndReleasesSnapshot()
    {
        var os = new FakeClipboard { ExactAllocation = false }; var engine = Create(os);
        Assert.Equal(SwapStatus.NotPreserved, engine.Publish(() => true));
        Assert.Equal(0, os.EmptyCalls);
        Assert.False(engine.HasBackup);
    }

    [Fact]
    public void RevocationDuringAllocationProof_RefusesBeforeClear()
    {
        var allowed = true;
        var os = new FakeClipboard { AfterAllocation = () => allowed = false };
        Assert.Equal(SwapStatus.NotPreserved, Create(os).Publish(() => allowed));
        Assert.Equal(0, os.EmptyCalls);
    }

    [Fact]
    public void ZeroPaddedPublication_IsNotExactAndCannotCommitOrRestoreAsOwned()
    {
        var os = new FakeClipboard(); var engine = Create(os);
        Assert.Equal(SwapStatus.Swapped, engine.Publish(() => true));
        os.Data[13] = os.Data[13].Concat(new byte[1]).ToArray();
        Assert.False(engine.CommitCopy(() => true));
        Assert.Equal(RestoreStatus.ClipboardChanged, engine.Restore());
        Assert.True(engine.HasBackup);
        Assert.Equal(1, os.EmptyCalls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NextSession_RejectsPriorMarkerEvenAfterSettlement_RequiresOperatorRecopy(bool copy)
    {
        var os = new FakeClipboard(); var first = Create(os);
        Assert.Equal(SwapStatus.Swapped, first.Publish(() => true));
        if (copy) Assert.True(first.CommitCopy(() => true));
        else Assert.Equal(RestoreStatus.Restored, first.Restore());
        Assert.True(first.Settled);
        var clears = os.EmptyCalls;
        Assert.Equal(SwapStatus.NotPreserved, Create(os).Publish(() => true));
        Assert.Equal(clears, os.EmptyCalls);
    }

    private static readonly byte[] Prior = new Guid("00112233-4455-6677-8899-aabbccddeeff").ToByteArray();

    [Theory]
    [InlineData(0)] // Wrong token.
    [InlineData(1)] // Guid network byte order is not ToByteArray order.
    [InlineData(2)] // Live prior owner, even if it disappears later.
    [InlineData(3)] // Second read changed marker.
    [InlineData(4)] // Unsupported representation.
    [InlineData(5)] // Metadata without user representation.
    [InlineData(6)] // Nonzero privacy.
    [InlineData(7)] // Malformed privacy.
    [InlineData(8)] // Representation changed on second read.
    public void VerifiedPriorBaseline_RejectsUnprovenImageWithoutEmpty(int failure)
    {
        var os = new FakeClipboard { Owner = 0 };
        os.Data[Marker] = Prior.ToArray();
        byte[] known = Prior.ToArray();
        switch (failure)
        {
            case 0: known[0] ^= 1; break;
            case 1: known = Convert.FromHexString("00112233445566778899aabbccddeeff"); break;
            case 2: os.Owner = 10; os.BeforeRead = n => { if (n == 2) os.Owner = 0; }; break;
            case 3: os.BeforeRead = n => { if (n == 2) os.Data[Marker][0] ^= 1; }; break;
            case 4: os.Data[999] = [1]; os.Complete = false; break;
            case 5: os.Data.Remove(13); break;
            case 6: os.Data[501] = [1, 0, 0, 0]; break;
            case 7: os.Data[501] = [0]; break;
            case 8: os.BeforeRead = n => { if (n == 2) os.Data[13] = [9]; }; break;
        }
        var engine = new GuardianClipboard(os, Marker, Token, Privacy, Publication(), known);
        Assert.Equal(SwapStatus.NotPreserved, engine.Publish(() => true));
        Assert.Equal(0, os.EmptyCalls);
        Assert.False(engine.HasBackup);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void InvalidPriorToken_RefusesBeforeOpen(int invalid)
    {
        var os = new FakeClipboard();
        var prior = invalid == 0 ? new byte[15] : invalid == 1 ? new byte[16] : Token.ToArray();
        Assert.Equal(SwapStatus.NotPreserved, new GuardianClipboard(os, Marker, Token, Privacy, Publication(), prior).Publish(() => true));
        Assert.Equal(0, os.OpenCalls);
        Assert.Equal(0, os.EmptyCalls);
    }

    [Fact]
    public void VerifiedPriorBaseline_StripsOnlyOldMarkerAndRestoresEveryOriginalByteWithCurrentToken()
    {
        var os = new FakeClipboard { Owner = 0 };
        os.Data[Marker] = Prior.ToArray();
        os.Data[1] = [65, 0]; os.Data[7] = [66, 0]; os.Data[16] = [9, 4, 0, 0];
        foreach (var format in Privacy) os.Data[format] = new byte[4];
        var expected = os.Data.Where(e => e.Key != Marker).ToDictionary(e => e.Key, e => e.Value.ToArray());
        var engine = new GuardianClipboard(os, Marker, Token, Privacy, Publication(), Prior);
        Assert.Equal(SwapStatus.Swapped, engine.Publish(() => true));
        Assert.False(os.ReadImages[0].Entries.ContainsKey(Marker));
        Assert.Equal(expected.Count, engine.SavedCount);
        // Neither owner zero nor the prior token grants recovery or copy commit authority.
        os.Owner = 0;
        Assert.False(engine.CommitCopy(() => true));
        Assert.Equal(RestoreStatus.ClipboardChanged, engine.Restore());
        os.Owner = os.Window; os.Data[Marker] = Prior.ToArray();
        Assert.False(engine.CommitCopy(() => true));
        Assert.Equal(RestoreStatus.ClipboardChanged, engine.Restore());
        os.Data[Marker] = Token.ToArray();
        Assert.Equal(RestoreStatus.Restored, engine.Restore());
        Assert.Equal(Token, os.Data[Marker]);
        Assert.Equal(expected.Count + 1, os.Data.Count);
        foreach (var entry in expected) Assert.Equal(entry.Value, os.Data[entry.Key]);
        Assert.Equal(Prior, new Guid("00112233-4455-6677-8899-aabbccddeeff").ToByteArray());
    }

    [Theory]
    [InlineData(0)] // Exact publication.
    [InlineData(1)] // Foreign owner with identical bytes.
    [InlineData(2)] // Changed text with retained marker/owner.
    [InlineData(3)] // Missing representation.
    [InlineData(4)] // Additional representation.
    [InlineData(5)] // Incomplete read.
    [InlineData(6)] // Busy.
    [InlineData(7)] // Revoked before read.
    [InlineData(8)] // Revoked after read.
    public void VerifyPublication_RequiresExactOwnedImageWithoutMutatingRecovery(int change)
    {
        var os = new FakeClipboard(); var engine = Create(os);
        Assert.False(engine.VerifyPublication(() => true));
        Assert.Equal(SwapStatus.Swapped, engine.Publish(() => true));
        var saved = os.ReadImages[0].Entries[13];
        var original = saved.ToArray();
        var count = engine.SavedCount; var bytes = engine.ByteCount;
        switch (change)
        {
            case 1: os.Owner = 999; break;
            case 2: os.Data[13] = [9, 8]; break;
            case 3: os.Data.Remove(16); break;
            case 4: os.Data[999] = [1]; break;
            case 5: os.Complete = false; break;
            case 6: os.Busy = true; break;
        }
        var expected = os.Data.ToDictionary(e => e.Key, e => e.Value.ToArray());
        var writes = os.WriteCalls; var checks = 0;
        Assert.Equal(change == 0, engine.VerifyPublication(() => change != 7 && (change != 8 || ++checks == 1)));
        Assert.True(engine.RecoveryPending);
        Assert.True(engine.HasBackup);
        Assert.False(engine.Settled);
        Assert.Equal(count, engine.SavedCount); Assert.Equal(bytes, engine.ByteCount);
        Assert.Equal(original, saved);
        Assert.Equal(1, os.EmptyCalls); Assert.Equal(writes, os.WriteCalls);
        Assert.False(os.Open);
        Assert.Equal(expected.Count, os.Data.Count);
        foreach (var entry in expected) Assert.Equal(entry.Value, os.Data[entry.Key]);
    }

    private sealed class FakeClipboard : IGuardianClipboardOs
    {
        internal Dictionary<uint, byte[]> Data = new() { [13] = [65, 0, 0, 0] };
        internal List<ClipboardImage> ReadImages = [];
        internal int EmptyCalls, WriteCalls, ReadCalls, FailWrite, OpenCalls;
        internal bool Complete = true, Open, Busy, ThrowWrite;
        internal Action<int>? BeforeRead;
        internal bool ExactAllocation = true;
        internal Action? AfterAllocation;
        public bool CanAllocateExact(int byteCount) { AfterAllocation?.Invoke(); return ExactAllocation; }
        public nint Window => 20;
        public nint Owner { get; set; } = 10;
        public bool TryOpen() { OpenCalls++; Assert.False(Open); if (Busy) return false; Open = true; return true; }
        public void Close() { Assert.True(Open); Open = false; }
        public ClipboardImage Read()
        {
            Assert.True(Open); BeforeRead?.Invoke(++ReadCalls);
            var result = new ClipboardImage(Data.ToDictionary(e => e.Key, e => e.Value.ToArray()), Complete);
            ReadImages.Add(result); return result;
        }
        public bool Empty() { Assert.True(Open); EmptyCalls++; Owner = Window; Data.Clear(); return true; }
        public bool Write(uint format, byte[] bytes)
        {
            Assert.True(Open); WriteCalls++;
            if (ThrowWrite) throw new InvalidOperationException();
            if (WriteCalls == FailWrite) return false;
            Data[format] = bytes.ToArray(); return true;
        }
    }
}
