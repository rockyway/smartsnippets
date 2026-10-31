using SmartSnippets.WindowsInsertion018;
using Xunit;

namespace SmartSnippets.WindowsInsertion018.Tests;

public sealed class ClipboardOwnershipPolicyTests
{
    [Fact]
    public void CaptureToClearChangeNeverPassesFromEpochArithmetic()
    {
        Assert.True(ClipboardOwnershipPolicy.CanBegin(true, 20, 10, 100, 10, 100, true));
        Assert.False(ClipboardOwnershipPolicy.CanBegin(true, 20, 10, 100, 10, 101, true));
        Assert.False(ClipboardOwnershipPolicy.CanBegin(true, 20, 10, 100, 11, 100, true));
        Assert.False(ClipboardOwnershipPolicy.CanBegin(true, 20, 10, uint.MaxValue, 10, 0, true));
    }

    [Theory]
    [InlineData(false, 20, true)]
    [InlineData(true, 0, true)]
    [InlineData(true, 20, false)]
    public void WrongDestroyedZeroGuardianAndIncompleteCaptureRefuse(bool pinned, int guardian, bool complete) =>
        Assert.False(ClipboardOwnershipPolicy.CanBegin(pinned, guardian, 10, 100, 10, 100, complete));

    [Fact]
    public void RestoreRequiresActualOwnerAndAcknowledgedEpoch()
    {
        Assert.True(ClipboardOwnershipPolicy.CanRestoreAcknowledged(true, 20, 200, 20, 200));
        Assert.False(ClipboardOwnershipPolicy.CanRestoreAcknowledged(true, 20, 200, 21, 200));
        Assert.False(ClipboardOwnershipPolicy.CanRestoreAcknowledged(true, 20, 200, 20, 201));
        Assert.False(ClipboardOwnershipPolicy.CanRestoreAcknowledged(false, 20, 200, 20, 200));
    }

    [Fact]
    public void StoppedWorkerRecoveryRequiresRecognizableStateNotJustOwner()
    {
        Assert.True(ClipboardOwnershipPolicy.CanRecoverStoppedWorker(true, 20, true, 20, true));
        Assert.False(ClipboardOwnershipPolicy.CanRecoverStoppedWorker(true, 20, false, 20, true));
        Assert.False(ClipboardOwnershipPolicy.CanRecoverStoppedWorker(true, 20, true, 20, false));
        Assert.False(ClipboardOwnershipPolicy.CanRecoverStoppedWorker(true, 20, true, 21, true));
        Assert.False(ClipboardOwnershipPolicy.CanRecoverStoppedWorker(false, 20, true, 20, true));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(4, true)]
    [InlineData(8, false)]
    public void MarkerRequiresExactFourZeroBytes(int length, bool expected)
    {
        var bytes = new byte[length];
        Assert.Equal(expected, OwnClipboardPublication.IsCompatibleMarker(bytes));
        if (length > 0)
        {
            bytes[0] = 1;
            Assert.False(OwnClipboardPublication.IsCompatibleMarker(bytes));
        }
    }

    private static readonly HashSet<uint> Privacy = [100, 101, 102];
    private static Dictionary<uint, byte[]> Expected() => new()
    {
        [200] = [5, 6], [100] = new byte[4], [101] = new byte[4], [102] = new byte[4],
        [13] = [10, 0, 0, 0], [1] = [10, 0], [7] = [10, 0], [16] = [9, 4, 0, 0]
    };
    private static bool Recognizes(Dictionary<uint, byte[]> current) =>
        ClipboardOwnershipPolicy.IsRecognizablePublication(current, Expected(), 200, Privacy);

    [Fact]
    public void EmptyAndExactPermittedPartialAndFullPublicationAreRecognizable()
    {
        Assert.True(Recognizes([]));
        Assert.True(Recognizes(new() { [200] = [5, 6] }));
        Assert.True(Recognizes(new() { [200] = [5, 6], [100] = new byte[4] }));
        Assert.True(Recognizes(Expected()));
    }

    [Fact]
    public void ExternalSetWithoutEmptyCannotHideBehindUnchangedOwner()
    {
        var modified = Expected(); modified[13] = [99, 0, 0, 0];
        Assert.False(Recognizes(modified));
        var added = Expected(); added[300] = [9];
        Assert.False(Recognizes(added));
        var token = Expected(); token[200] = [5, 7];
        Assert.False(Recognizes(token));
        var marker = Expected(); marker[100] = [1, 0, 0, 0];
        Assert.False(Recognizes(marker));
    }

    [Fact]
    public void ImpossiblePublicationOrderingAndMissingMarkerRefuse()
    {
        var noMarker = Expected(); noMarker.Remove(200);
        Assert.False(Recognizes(noMarker));
        var noPrivacy = Expected(); noPrivacy.Remove(101);
        Assert.False(Recognizes(noPrivacy));
        var aliasWithoutUnicode = Expected(); aliasWithoutUnicode.Remove(13);
        Assert.False(Recognizes(aliasWithoutUnicode));
    }

    [Fact]
    public void OriginalByteMismatchAndIncompatibleAllocationSizeFailExactComparison()
    {
        Assert.True(ClipboardOwnershipPolicy.ExactBytes([1, 2], [1, 2]));
        Assert.False(ClipboardOwnershipPolicy.ExactBytes([1, 3], [1, 2]));
        Assert.False(ClipboardOwnershipPolicy.ExactBytes([1, 2, 0], [1, 2]));
    }

    [Fact]
    public void TargetReadbackRequiresExactSharedCrLfPayloadAndUtf16Count()
    {
        Assert.Equal(22, OwnClipboardPublication.Multiline.Length);
        Assert.True(SmartSnippets.Target018.Program.VerifyFixture("verify_multiline_a", OwnClipboardPublication.Multiline));
        Assert.False(SmartSnippets.Target018.Program.VerifyFixture("verify_multiline_a", OwnClipboardPublication.Multiline + "\r\n"));
        Assert.False(SmartSnippets.Target018.Program.VerifyFixture("verify_multiline_a", OwnClipboardPublication.Multiline.Replace("\r\n", "\n")));
    }
}
