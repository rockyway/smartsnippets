using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Uno.Platforms.Windows.AcceptanceNative;
using Xunit;

namespace SmartSnippets.Uno.Tests.AcceptanceNative;

public sealed class OwnedOwnerControlTests
{
    [Fact]
    public void ValidB_UsesRealGuardianRefusalThenExplicitCleanupRestoresOriginal()
    {
        var f = new Fixture();
        Assert.True(f.Control.VerifyRefusal(() => true));
        Assert.Equal((nint)30, f.State.Owner);
        Assert.True(f.Guardian.HasBackup);
        Assert.Equal(RestoreStatus.ClipboardChanged, f.Guardian.Restore());
        Assert.Equal(2, f.State.Clears);
        Assert.False(f.Control.VerifyRefusal(() => true));
        Assert.Equal(RestoreStatus.Restored, f.Control.CleanupAndRestore());
        Assert.Equal(new byte[] { 65, 0, 0, 0 }, f.State.Data[13]);
        Assert.False(f.Guardian.HasBackup);
        Assert.Equal(4, f.State.Clears);
    }

    [Theory]
    [InlineData("external")]
    [InlineData("content")]
    [InlineData("token")]
    [InlineData("extra")]
    [InlineData("incomplete")]
    [InlineData("dead")]
    [InlineData("reused")]
    public void BNoLongerExact_RefusesCleanupWithoutWritesOrRamLoss(string change)
    {
        var f = new Fixture();
        Assert.True(f.Control.VerifyRefusal(() => true));
        switch (change)
        {
            case "external": f.State.Owner = 999; break;
            case "content": f.State.Data[13] = [99, 0]; break;
            case "token": f.State.Data[500] = new byte[16]; break;
            case "extra": f.State.Data[900] = [1]; break;
            case "incomplete": f.State.Complete = false; break;
            case "dead": case "reused": f.PinnedB = false; break;
        }
        var expected = f.State.Data.ToDictionary(e => e.Key, e => e.Value.ToArray());
        var writes = f.State.Writes;
        Assert.Equal(RestoreStatus.ClipboardChanged, f.Control.CleanupAndRestore());
        Assert.Equal(2, f.State.Clears);
        Assert.Equal(writes, f.State.Writes);
        Assert.True(f.Guardian.HasBackup);
        Assert.All(expected, e => Assert.Equal(e.Value, f.State.Data[e.Key]));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void APreflightMismatchOrRevocation_DoesNotTransfer(bool revoke)
    {
        var f = new Fixture();
        if (!revoke) f.State.Data[13] = [99];
        Assert.False(f.Control.VerifyRefusal(() => !revoke));
        Assert.Equal((nint)20, f.State.Owner);
        Assert.Equal(1, f.State.Clears);
        Assert.True(f.Guardian.HasBackup);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    public void PartialReturnToA_IsRecoveredOnlyByUnchangedGuardian(int failedWrite)
    {
        var f = new Fixture();
        Assert.True(f.Control.VerifyRefusal(() => true));
        f.State.FailWrite = f.State.Writes + failedWrite;
        Assert.Equal(RestoreStatus.Partial, f.Control.CleanupAndRestore());
        Assert.Equal((nint)20, f.State.Owner);
        Assert.True(f.Guardian.HasBackup);
        f.State.FailWrite = 0;
        Assert.Equal(RestoreStatus.Restored, f.Control.CleanupAndRestore());
        Assert.False(f.Guardian.HasBackup);
    }

    [Fact]
    public void PartialBPublication_IsNotCleanupAuthority()
    {
        var f = new Fixture();
        f.State.FailWrite = f.State.Writes + 2;
        Assert.False(f.Control.VerifyRefusal(() => true));
        Assert.Equal((nint)30, f.State.Owner);
        f.State.FailWrite = 0;
        Assert.Equal(RestoreStatus.ClipboardChanged, f.Control.CleanupAndRestore());
        Assert.True(f.Guardian.HasBackup);
        Assert.Equal(2, f.State.Clears);
    }

    [Fact]
    public async Task AbortCleanup_WaitsForRetainedWorkerSignalThenSettles()
    {
        var f = new Fixture();
        Assert.True(f.Control.VerifyRefusal(() => true));
        var stopped = false;
        await GuardianRecovery.RetainUntilSettledAsync(() => stopped, () => f.Guardian.HasBackup,
            f.Control.CleanupAndRestore, () =>
            {
                Assert.Equal((nint)30, f.State.Owner);
                Assert.Equal(2, f.State.Clears);
                stopped = true;
                return Task.CompletedTask;
            }, _ => throw new InvalidOperationException("Unexpected retry"));
        Assert.False(f.Guardian.HasBackup);
    }

    private sealed class Fixture
    {
        internal readonly State State = new();
        internal readonly GuardianClipboard Guardian;
        internal readonly OwnedOwnerControl Control;
        internal bool PinnedB = true;
        internal Fixture()
        {
            var token = Enumerable.Range(1, 16).Select(i => (byte)i).ToArray();
            var publication = new Dictionary<uint, byte[]>
            { [501] = new byte[4], [502] = new byte[4], [503] = new byte[4], [500] = token, [13] = [88, 0, 0, 0] };
            var a = new Os(State, 20);
            Guardian = new(a, 500, token, new HashSet<uint> { 501, 502, 503 }, publication);
            Assert.Equal(SwapStatus.Swapped, Guardian.Publish(() => true));
            Control = new(Guardian, a, new Os(State, 30), () => true, () => PinnedB, 500, publication);
        }
    }
    private sealed class State
    {
        internal Dictionary<uint, byte[]> Data = new() { [13] = [65, 0, 0, 0] };
        internal nint Owner = 10;
        internal bool Open, Complete = true;
        internal int Clears, Writes, FailWrite;
    }
    private sealed class Os(State state, nint window) : IGuardianClipboardOs
    {
        public nint Window => window;
        public nint Owner => state.Owner;
        public bool TryOpen() { Assert.False(state.Open); state.Open = true; return true; }
        public void Close() { Assert.True(state.Open); state.Open = false; }
        public ClipboardImage Read()
        {
            Assert.True(state.Open);
            return new(state.Data.ToDictionary(e => e.Key, e => e.Value.ToArray()), state.Complete);
        }
        public bool Empty() { Assert.True(state.Open); state.Clears++; state.Data.Clear(); state.Owner = window; return true; }
        public bool CanAllocateExact(int byteCount) => true;
        public bool Write(uint format, byte[] bytes)
        {
            Assert.True(state.Open);
            if (++state.Writes == state.FailWrite) return false;
            state.Data[format] = bytes.ToArray(); return true;
        }
    }
}
