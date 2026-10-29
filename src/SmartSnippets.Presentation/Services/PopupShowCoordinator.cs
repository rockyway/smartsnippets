namespace SmartSnippets.Presentation.Services;

public readonly record struct PopupShowResult(bool Shown, PopupFocusTarget? DefaultFocus);

/// <summary>Orders restartable surface initialization, show and focus without WinUI dependencies.</summary>
public sealed class PopupShowCoordinator(Action<Action> schedule, Action<PopupFocusTarget> applyFocus,
    Action? startWarmup = null, Action? stopWarmup = null, Action? warmupFailed = null)
{
    private Func<PopupShowResult>? _pendingShow;
    private PopupFocusTarget? _pendingFocus;
    private bool _ready;
    private bool _warming;
    private bool _visible;
    private bool _closed;
    private int _focusVersion;
    private int _warmupRestarts;
    public int WarmupGeneration { get; private set; }
    public bool IsWarming => _warming;

    public void BeginWarmup()
    {
        if (_closed || _ready || _warming) return;
        _warming = true; _visible = false; _focusVersion++; WarmupGeneration++;
        try { startWarmup?.Invoke(); }
        catch { CancelWarmup(); throw; }
    }

    public void RequestShow(Func<PopupShowResult> show)
    {
        if (_closed) return;
        _focusVersion++; // Even a non-activating preview supersedes previously queued focus.
        _pendingShow = show;
        _pendingFocus = null; // A newer show supersedes the old show and its focus.
        if (_ready) ShowPending();
        else
        {
            if (!_warming) _warmupRestarts = 0;
            BeginWarmup();
        }
    }

    public void CompleteWarmup(int generation)
    {
        if (_closed || !_warming || generation != WarmupGeneration) return;
        _warming = false; _ready = true;
        stopWarmup?.Invoke();
        ShowPending();
    }

    public void WarmupTimedOut(int generation)
    {
        if (_closed || !_warming || generation != WarmupGeneration) return;
        CancelWarmup();
        // One bounded retry; a second failure stays cold (never reveal unfinished content).
        // Retain the pending request/focus, and let a subsequent show start another attempt.
        if (_warmupRestarts++ == 0) BeginWarmup();
        else warmupFailed?.Invoke();
    }

    private void CancelWarmup()
    {
        if (!_warming) return;
        _warming = false; _ready = false; WarmupGeneration++;
        stopWarmup?.Invoke();
    }

    private void ShowPending()
    {
        if (_pendingShow is not { } show) return;
        var focus = _pendingFocus;
        _pendingShow = null; _pendingFocus = null;
        var result = show();
        _visible = result.Shown;
        if (_visible && (focus ?? result.DefaultFocus) is { } target) QueueFocus(target);
    }

    public void Focus(PopupFocusTarget target)
    {
        if (_closed) return;
        if (_pendingShow is not null) { _pendingFocus = target; return; }
        if (_ready && _visible) QueueFocus(target);
    }

    private void QueueFocus(PopupFocusTarget target)
    {
        var version = ++_focusVersion;
        schedule(() =>
        {
            if (!_closed && _ready && _visible && version == _focusVersion) applyFocus(target);
        });
    }

    public void Hide()
    {
        _visible = false; _pendingShow = null; _pendingFocus = null; _focusVersion++;
        _warmupRestarts = 0;
        CancelWarmup();
    }

    public void Close() { Hide(); _closed = true; }
}
