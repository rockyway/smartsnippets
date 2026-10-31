using System;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using SmartSnippets.Presentation.Services;
using Windows.Graphics;

namespace SmartSnippets.UnoProbe;

/// <summary>Thin native bindings only; portable ProbePopupDriver owns lifecycle policy.
/// Default adapter remains null. No global registration or input is performed.</summary>
internal sealed class UiFlowProbe
{
    private Window? _main;
    private readonly DispatcherQueue _queue;
    private readonly ProbeFlowComposition _flow;
    private readonly ProbePopupDriver _driver;
    public UiFlowProbe(Window main, IGuardedProbeNativeAdapter? native = null)
    {
        _main = main; _queue = DispatcherQueue.GetForCurrentThread();
        _driver = new ProbePopupDriver(CreatePopup,
            () => new NativeTimer(_queue), () => _main?.Activate(), native);
        _flow = ProbeFlowComposition.Create(_driver, action =>
        {
            if (!_queue.TryEnqueue(() => action())) throw new InvalidOperationException("Dispatcher unavailable.");
        });
        _driver.Attach(_flow);
        main.Content = new FlowProbePage(_flow);
        main.Closed += MainClosed;
    }
    private IProbePopupSurface CreatePopup() => new NativePopup(_flow);
    private async void MainClosed(object sender, WindowEventArgs args)
    {
        var main = _main; _main = null; // Native main handle is no longer usable.
        if (main is not null) main.Closed -= MainClosed;
        try { await _flow.ShutdownAsync(); }
        finally { _driver.Dispose(); }
    }
    private sealed class NativeTimer : IProbeFlowTimer
    {
        private readonly DispatcherQueueTimer _timer;
        private Action? _elapsed;
        public NativeTimer(DispatcherQueue queue)
        {
            _timer = queue.CreateTimer(); _timer.Interval = TimeSpan.FromSeconds(3); _timer.IsRepeating = false;
            _timer.Tick += Tick;
        }
        private void Tick(DispatcherQueueTimer sender, object args) => _elapsed?.Invoke();
        public void Start(Action elapsed) { _elapsed = elapsed; _timer.Start(); }
        public void Stop() => _timer.Stop();
        public void Dispose() { _timer.Stop(); _timer.Tick -= Tick; _elapsed = null; }
    }
    private sealed class NativePopup : IProbePopupSurface
    {
        private Window? _popup;
        private FlowProbePage? _page;
        private ProbeWindowSurface? _surface;
        private EventHandler<object>? _layout;
        public event Action? Closed;
        public NativePopup(ProbeFlowComposition flow)
        {
            _page = new FlowProbePage(flow, isPopup: true);
            var title = "SS-Probe018-Flow-" + Guid.NewGuid().ToString("N");
            _popup = new Window { Content = _page, Title = title };
            _popup.Closed += PopupClosed;
            _popup.AppWindow.Resize(new SizeInt32 { Width = 620, Height = 460 });
#if WINDOWS
            ProbeWindowPlacement.Park(_popup, 620, 460, secondary: true);
#else
            _ = Uno.UI.Xaml.WindowHelper.GetNativeWindow(_popup);
#endif
            _popup.Title = title;
            _surface = ProbeWindowSurface.Bind(_popup, title);
        }
        private void PopupClosed(object sender, WindowEventArgs args)
        {
            var popup = _popup;
            _surface = null; _popup = null; // BEFORE any policy callbacks: NSWindow/HWND may be dead.
            if (popup is not null) popup.Closed -= PopupClosed;
            UnwatchLayout(); _page = null;
            Closed?.Invoke();
        }
        public void WatchLayout(Action ready)
        {
            UnwatchLayout();
            _layout = (_, _) =>
            {
                if (_page is { IsLoaded: true, ActualWidth: > 0, ActualHeight: > 0 }) ready();
            };
            _page!.LayoutUpdated += _layout;
        }
        public void UnwatchLayout()
        {
            if (_page is not null && _layout is not null) _page.LayoutUpdated -= _layout;
            _layout = null;
        }
        public void InvalidateMeasure() => _page?.InvalidateMeasure();
        public void Cloak(bool value) => _surface?.SetWarmupCloak(value);
        public void ShowWarmup() { if (_popup is not null) _surface?.ShowWarmup(_popup); }
        public void Show() { if (_popup is not null) _surface?.Show(_popup); }
        public void Hide() => _surface?.Hide();
        public void Focus(PopupFocusTarget target) => _page?.ApplyFocus(target);
        public void Close() => _popup?.Close();
    }
}
