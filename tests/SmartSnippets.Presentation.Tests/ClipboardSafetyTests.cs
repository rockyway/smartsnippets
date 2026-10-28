using SmartSnippets.Presentation.Services;

namespace SmartSnippets.Presentation.Tests;

public sealed class ClipboardSafetyTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void EnumerationError_IsIncompleteAtStartOrAfterFormats(int count)
    {
        var visited = new List<uint>();
        var complete = ClipboardSafety.EnumerateFormats(previous => previous < count ? (previous + 1, 0) : (0, 5), visited.Add);
        Assert.False(complete);
        Assert.Equal(count, visited.Count);
    }

    [Fact]
    public void EnumerationExhaustion_IsComplete()
    {
        var visited = new List<uint>();
        Assert.True(ClipboardSafety.EnumerateFormats(previous => previous == 0 ? (1u, 0) : (0u, 0), visited.Add));
        Assert.Equal(new uint[] { 1 }, visited);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MarkerRegistrationOrWriteFailure_IsReported(bool registrationFailure)
    {
        var registered = new HashSet<uint>();
        var writes = new List<uint>();
        var success = ClipboardSafety.InstallMarkers(["first", "second"],
            name => name == "first" && registrationFailure ? 0u : name == "first" ? 1u : 2u,
            format => { writes.Add(format); return registrationFailure || format != 1; }, registered);
        Assert.False(success);
        Assert.Contains(2u, writes);
        Assert.Equal(!registrationFailure, writes.Contains(1u));
    }

    [Fact]
    public void MarkerSuccess_RegistersAndWritesAll()
    {
        var registered = new HashSet<uint>();
        Assert.True(ClipboardSafety.InstallMarkers(["one", "two"], name => name == "one" ? 1u : 2u, _ => true, registered));
        Assert.Equal(new uint[] { 1, 2 }, registered.Order());
    }

    [Fact]
    public void CaptureBeforeClose_ConcurrentWriterChangesSequenceAfterCapture()
    {
        uint sequence = 10;
        var open = true;
        var captured = ClipboardSafety.CaptureBeforeClose(
            () => { Assert.True(open); return sequence; },
            () => { open = false; sequence++; });
        Assert.Equal(10u, captured);
        Assert.NotEqual(captured, sequence);
        Assert.False(open);
    }

    [Fact]
    public void CaptureBeforeClose_ClosesEvenIfSequenceThrows()
    {
        var closed = false;
        Assert.Throws<InvalidOperationException>(() => ClipboardSafety.CaptureBeforeClose(() => throw new InvalidOperationException(), () => closed = true));
        Assert.True(closed);
    }
}
