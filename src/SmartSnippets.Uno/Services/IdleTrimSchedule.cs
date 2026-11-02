namespace SmartSnippets.Uno.Services;

/// <summary>One trim per continuous idle epoch; showing or busy work cancels the delay.</summary>
internal sealed class IdleTrimSchedule
{
    internal const int DelayMilliseconds = 3000;
    private long? _since;
    private bool _trimmed;
    internal bool Poll(long milliseconds, bool idle)
    {
        if (!idle) { _since = null; _trimmed = false; return false; }
        _since ??= milliseconds;
        if (_trimmed || milliseconds - _since.Value < DelayMilliseconds) return false;
        _trimmed = true;
        return true;
    }
}
