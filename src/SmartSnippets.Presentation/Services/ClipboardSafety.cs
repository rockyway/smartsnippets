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

    /// <summary>Installs every exclusion marker; registration and write failures are both reported.</summary>
    public static bool InstallMarkers(IEnumerable<string> names, Func<string, uint> register, Func<uint, bool> write, ISet<uint> registered)
    {
        var success = true;
        foreach (var name in names)
        {
            var format = register(name);
            if (format == 0) { success = false; continue; }
            registered.Add(format);
            if (!write(format)) success = false;
        }
        return success;
    }

    /// <summary>Captures the sequence before closing, so a subsequent owner's write cannot be mistaken for ours.</summary>
    public static uint CaptureBeforeClose(Func<uint> sequence, Action close)
    {
        try { return sequence(); }
        finally { close(); }
    }
}
