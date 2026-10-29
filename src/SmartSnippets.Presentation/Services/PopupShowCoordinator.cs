namespace SmartSnippets.Presentation.Services;

public readonly record struct PopupShowResult(bool Shown, PopupFocusTarget? DefaultFocus);

/// <summary>Orders asynchronous surface readiness, show and focus without WinUI dependencies.</summary>
public sealed class PopupShowCoordinator(Action<Action> schedule, Action<PopupFocusTarget> applyFocus)
{
    private Func<PopupShowResult>? _pendingShow;
    private PopupFocusTarget? _pendingFocus;
    private bool _ready;
    private bool _visible;
    private bool _closed;
    private int _focusVersion;

    public void BeginWarmup() { _ready = false; _visible = false; _focusVersion++; }

    public void RequestShow(Func<PopupShowResult> show)
    {
        if (_closed) return;
        _pendingShow = show;
        _pendingFocus = null; // A newer show supersedes the old show and its focus.
        if (_ready) ShowPending();
    }

    public void CompleteWarmup()
    {
        if (_closed) return;
        _ready = true;
        ShowPending();
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
    }

    public void Close() { Hide(); _closed = true; }
}
