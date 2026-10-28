namespace SmartSnippets.Presentation.Services;

/// <summary>Testable sequencing helpers for clipboard operations held under the native clipboard lock.</summary>
public static class ClipboardSafety
{
    /// <summary>Enumerates until a successful end; a native enumeration error means the snapshot is incomplete.</summary>
    public static bool EnumerateFormats(Func<uint, (uint Format, int Error)> next, Action<uint> visit)
    {
        uint previous = 0;
        while (true)
        {
            var (format, error) = next(previous);
            if (format == 0) return error == 0;
            visit(format);
            previous = format;
        }
    }

    /// <summary>Captures the sequence before closing, so a subsequent owner's write cannot be mistaken for ours.</summary>
    public static uint CaptureBeforeClose(Func<uint> sequence, Action close)
    {
        try { return sequence(); }
        finally { close(); }
    }
}
