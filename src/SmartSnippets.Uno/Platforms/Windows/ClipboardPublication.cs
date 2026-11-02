namespace SmartSnippets.Uno.Platforms.Windows;

/// <summary>Seal our publication before paste, never rebase a confirmed sequence after dispatch.</summary>
internal static class ClipboardPublication
{
    internal static bool CanRestore(uint? confirmed, uint current, bool sameOwner) =>
        confirmed is { } expected && expected != 0 && current == expected && sameOwner;

    internal static uint? CloseAndCapture(Func<bool> closeWrite, Func<bool> openRead,
        Func<bool> recognizable, Func<bool> materialize, Func<uint> sequence, Action closeRead)
    {
        // CloseClipboard can finalize synthesized formats. A pre-close sequence is not a commit token.
        if (!closeWrite() || !openRead()) return null;
        try
        {
            if (!recognizable() || !materialize() || !recognizable()) return null;
        }
        finally { closeRead(); }
        // Materializing a delayed/synthesized format can itself commit at close. Reopen once more,
        // validate again, and sample under a read-only lock with no remaining synthesis work.
        if (!openRead()) return null;
        try { return CaptureLocked(recognizable, () => true, sequence); }
        finally { closeRead(); }
    }

    internal static uint? CaptureLocked(Func<bool> recognizable, Func<bool> materialize, Func<uint> sequence)
    {
        // The reopened native clipboard lock excludes writers. Check exact owner, private transaction,
        // payload and format inventory before accepting any post-close sequence. Never trust owner alone.
        if (!recognizable() || !materialize() || !recognizable()) return null;
        var value = sequence();
        return value == 0 ? null : value;
    }
}
