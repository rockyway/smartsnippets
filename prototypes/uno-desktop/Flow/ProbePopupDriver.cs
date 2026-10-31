using System;
using SmartSnippets.Presentation.Services;

namespace SmartSnippets.UnoProbe;

/// <summary>Only native window operations and callback registration live behind this boundary.
/// Closed must invalidate native handles before invoking its subscriber.</summary>
public interface IProbePopupSurface
{
    event Action Closed;
    void WatchLayout(Action ready);
    void UnwatchLayout();
    void InvalidateMeasure();
    void Cloak(bool value);
    void ShowWarmup();
    void Show();
    void Hide();
    void Focus(PopupFocusTarget target);
    void Close();
}
public interface IProbeFlowTimer : IDisposable { void Start(Action elapsed); void Stop(); }

/// <summary>Actual host and tests execute this same cloak/timer/close policy.
/// All calls and callbacks belong to the UI dispatcher thread.</summary>
public sealed class ProbePopupDriver : ProbeFlowBoundary, IDisposable
{
    private readonly Func<IProbePopupSurface> _createSurface;
    private readonly Func<IProbeFlowTimer> _createTimer;
    private readonly Action _showMain;
    private IProbePopupSurface? _surface;
    private IProbeFlowTimer? _timer;
    private Action? _closedHandler;
    private ProbeFlowComposition? _flow;
    private int _generation;
    private bool _disposed;

    public ProbePopupDriver(Func<IProbePopupSurface> createSurface, Func<IProbeFlowTimer> createTimer,
        Action showMain, IGuardedProbeNativeAdapter? native = null) : base(native)
    { _createSurface = createSurface; _createTimer = createTimer; _showMain = showMain; }
    public void Attach(ProbeFlowComposition flow) => _flow = flow;
    public override void StartWarmup()
    {
        if (_disposed) return;
        var flow = _flow ?? throw new InvalidOperationException("Flow not attached.");
        if (_surface is null)
        {
            var created = _createSurface();
            _surface = created;
            _closedHandler = () => SurfaceClosed(created);
            created.Closed += _closedHandler;
        }
        var surface = _surface;
        var generation = _generation = flow.WarmupGeneration;
        surface.Cloak(true);
        surface.WatchLayout(() =>
        {
            if (!IsCurrent(surface, generation)) return;
            flow.OnLayoutReady(generation); // StopWarmup owns timer/layout cleanup.
        });
        _timer = _createTimer();
        _timer.Start(() =>
        {
            if (!IsCurrent(surface, generation)) return;
            flow.OnWarmupTimeout(generation);
        });
        surface.InvalidateMeasure();
        surface.ShowWarmup();
    }
    private bool IsCurrent(IProbePopupSurface surface, int generation) => !_disposed &&
        ReferenceEquals(surface, _surface) && generation == _generation &&
        _flow is { IsWarming: true } flow && generation == flow.WarmupGeneration;
    private void StopCallbacks(IProbePopupSurface? surface)
    {
        _timer?.Stop(); _timer?.Dispose(); _timer = null;
        surface?.UnwatchLayout();
    }
    private void SurfaceClosed(IProbePopupSurface closed)
    {
        if (!ReferenceEquals(closed, _surface)) return;
        _surface = null; // Never let policy cleanup touch a closed native window.
        if (_closedHandler is not null) closed.Closed -= _closedHandler;
        _closedHandler = null;
        StopCallbacks(closed);
        if (!_disposed) _flow?.OnPopupWindowClosed();
    }
    public override void StopWarmup()
    {
        StopCallbacks(_surface);
        _surface?.Hide();
        _surface?.Cloak(false);
    }
    public override void ShowPopup()
    {
        if (_disposed || _surface is null) return;
        _surface.Cloak(false);
        if (_flow!.Popup.IsPreview) _surface.ShowWarmup(); else _surface.Show();
    }
    public override void HidePopup() => _surface?.Hide();
    public override void ShowMain() { if (!_disposed) _showMain(); }
    public override void Focus(PopupFocusTarget target)
    {
        if (!_disposed) _surface?.Focus(target);
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        var surface = _surface; _surface = null;
        if (surface is not null && _closedHandler is not null) surface.Closed -= _closedHandler;
        _closedHandler = null;
        StopCallbacks(surface);
        // The local is a still-live owned window; Closed cannot route it back into policy.
        surface?.Hide(); surface?.Close();
    }
}
