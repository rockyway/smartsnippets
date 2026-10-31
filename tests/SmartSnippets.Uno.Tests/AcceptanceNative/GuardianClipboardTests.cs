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

    private sealed class FakeClipboard : IGuardianClipboardOs
    {
        internal Dictionary<uint, byte[]> Data = new() { [13] = [65, 0, 0, 0] };
        internal List<ClipboardImage> ReadImages = [];
        internal int EmptyCalls, WriteCalls, ReadCalls, FailWrite;
        internal bool Complete = true, Open, Busy, ThrowWrite;
        internal Action<int>? BeforeRead;
        public nint Window => 20;
        public nint Owner { get; set; } = 10;
        public bool TryOpen() { Assert.False(Open); if (Busy) return false; Open = true; return true; }
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
