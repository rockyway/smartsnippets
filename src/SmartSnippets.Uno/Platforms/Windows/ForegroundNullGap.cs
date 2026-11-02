namespace SmartSnippets.Uno.Platforms.Windows;

/// <summary>NULL is pending, never positive evidence. Only A/the pinned popup can end a gap.</summary>
internal sealed class ForegroundNullGap
{
    private long? started;
    private long lastNonNull;
    internal bool Pending => started.HasValue;
    internal bool Observe(bool isNull, bool allowed, long elapsedMilliseconds)
    {
        if (isNull) started ??= lastNonNull;
        if (started is { } start && (elapsedMilliseconds - start > 500 || (!isNull && !allowed))) return false;
        if (!isNull) { started = null; lastNonNull = elapsedMilliseconds; }
        return true;
    }
}
