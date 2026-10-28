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
