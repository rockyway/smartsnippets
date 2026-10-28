namespace SmartSnippets.Presentation.Services;

/// <summary>Testable sequencing helpers for clipboard operations held under the native clipboard lock.</summary>
public static class ClipboardSafety
{
    /// <summary>Captures the sequence before closing, so a subsequent owner's write cannot be mistaken for ours.</summary>
    public static uint CaptureBeforeClose(Func<uint> sequence, Action close)
    {
        try { return sequence(); }
        finally { close(); }
    }
}
