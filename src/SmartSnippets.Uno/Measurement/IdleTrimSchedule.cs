namespace SmartSnippets.Uno.Measurement;

/// <summary>One trim per continuous idle epoch; showing or busy work cancels the delay.</summary>
internal sealed class IdleTrimSchedule
{
    private long? _since;
    private bool _trimmed;
    internal bool Poll(long milliseconds, bool idle)
    {
        if (!idle) { _since = null; _trimmed = false; return false; }
        _since ??= milliseconds;
        if (_trimmed || milliseconds - _since.Value < 3000) return false;
        _trimmed = true;
        return true;
    }
}
