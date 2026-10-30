namespace SmartSnippets.Presentation.Services;

/// <summary>UI-thread hover policy. Scheduled callbacks may run after cancellation; generations reject them.</summary>
public sealed class HoverDetailsCoordinator(Action<TimeSpan, Action> schedule, Action show, Action close) : IDisposable
{
    public static readonly TimeSpan OpenDelay = TimeSpan.FromMilliseconds(500);
    public static readonly TimeSpan HandoffDelay = TimeSpan.FromMilliseconds(120);
    private long _generation;
    private long _leaveGeneration;
    private object? _target;
    private bool _ownerInside, _targetInside, _surfaceInside, _pressed, _disposed, _pinned;
    public bool IsOpen { get; private set; }
    public bool IsHandingOff { get; private set; }

    public void Enter(object target, bool hasContent = true)
    {
        if (_disposed) return;
        if (ReferenceEquals(target, _target)) { _targetInside = true; OwnerEntered(); return; }
        Cancel();
        if (!hasContent) return;
        _target = target;
        _ownerInside = _targetInside = true;
        var generation = _generation;
        schedule(OpenDelay, () =>
        {
            if (_disposed || generation != _generation || !_targetInside) return;
            IsOpen = true;
            show();
        });
    }

    /// <summary>Immediate root-owned keyboard preview; invalidates pointer timers without a close/reopen cycle.</summary>
    public void OpenPinned(object target, bool hasContent = true)
    {
        if (_disposed) return;
        if (!hasContent) { Cancel(); return; }
        _generation++; StopLeave();
        _target = target;
        _ownerInside = _targetInside = _surfaceInside = _pressed = false;
        _pinned = true;
        IsOpen = true;
        show();
    }

    public void TargetExited()
    {
        _targetInside = false;
        if (!IsOpen) Cancel();
    }
    public void OwnerEntered() { _ownerInside = true; StopLeave(); }
    public void OwnerExited() { _ownerInside = false; Leave(); }
    public void SurfaceEntered() { _surfaceInside = true; StopLeave(); }
    public void SurfaceExited() { _surfaceInside = false; Leave(); }
    public void UpdateRegions(bool ownerInside, bool surfaceInside)
    {
        _ownerInside = ownerInside; _surfaceInside = surfaceInside;
    }
    public void Pressed() { _pressed = true; StopLeave(); }
    public void Released() { _pressed = false; Leave(); }
    public void CaptureLost() { _pressed = false; Leave(); }
    private void StopLeave() { _leaveGeneration++; IsHandingOff = false; }
    private void Leave()
    {
        if (_pinned || _ownerInside || _surfaceInside || _pressed) return;
        if (!IsOpen) { Cancel(); return; }
        IsHandingOff = true;
        var leave = ++_leaveGeneration;
        var generation = _generation;
        schedule(HandoffDelay, () =>
        {
            if (generation == _generation && leave == _leaveGeneration && !_ownerInside && !_surfaceInside && !_pressed) Cancel();
        });
    }
    public void Cancel()
    {
        _generation++; StopLeave();
        _target = null;
        _ownerInside = _targetInside = _surfaceInside = _pressed = _pinned = false;
        var wasOpen = IsOpen;
        IsOpen = false;
        if (wasOpen) close();
    }
    public void Dispose() { Cancel(); _disposed = true; }
}
