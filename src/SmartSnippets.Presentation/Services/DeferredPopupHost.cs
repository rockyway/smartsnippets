namespace SmartSnippets.Presentation.Services;

/// <summary>Defers recovery while exit is being considered, without reopening or resetting popup state.</summary>
public sealed class DeferredPopupHost(IPopupHost host) : IPopupHost
{
    private bool _suspended;
    private bool _committed;
    private bool _recoveryPending;
    private PopupFocusTarget? _pendingFocus;

    public void Suspend() => _suspended = true;

    public void Resume()
    {
        if (_committed) return;
        _suspended = false;
        if (!_recoveryPending) return;
        var focus = _pendingFocus;
        _recoveryPending = false;
        _pendingFocus = null;
        host.ShowAgain();
        if (focus is { } target) host.Focus(target);
    }

    public void Commit()
    {
        _committed = true;
        _recoveryPending = false;
        _pendingFocus = null;
    }

    public void Hide()
    {
        _recoveryPending = false;
        _pendingFocus = null;
        host.Hide();
    }

    public void ShowAgain()
    {
        if (_committed) return;
        if (_suspended) { _recoveryPending = true; return; }
        host.ShowAgain();
    }

    public void OpenMainWindow()
    {
        if (!_suspended && !_committed) host.OpenMainWindow();
    }

    public void Focus(PopupFocusTarget target)
    {
        if (_committed) return;
        if (_suspended)
        {
            if (_recoveryPending && target == PopupFocusTarget.Search)
            {
                // The view-model focuses Search after explicitly cancelling or dismissing a prompt.
                _recoveryPending = false;
                _pendingFocus = null;
            }
            else if (_recoveryPending) _pendingFocus = target;
            return;
        }
        host.Focus(target);
    }
}
