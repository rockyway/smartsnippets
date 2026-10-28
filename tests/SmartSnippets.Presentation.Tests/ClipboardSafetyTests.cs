using SmartSnippets.Presentation.Services;

namespace SmartSnippets.Presentation.Tests;

public sealed class ClipboardSafetyTests
{
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
