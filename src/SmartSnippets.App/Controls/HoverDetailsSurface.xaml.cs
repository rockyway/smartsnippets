using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using SmartSnippets.Core.Logging;
using SmartSnippets.Presentation.Services;
using Windows.Foundation;

namespace SmartSnippets.App.Controls;

public sealed partial class HoverDetailsSurface : UserControl, IDisposable
{
    private static HoverDetailsSurface? _active;
    private static readonly HashSet<HoverDetailsSurface> Instances = [];
    internal static Action<Border, TextBlock, ElementTheme>? ResolveAppearance { get; set; }
    internal static event Action? BoundsChanged;
    private readonly FrameworkElement _owner;
    private readonly HoverDetailsCoordinator _policy;
    private readonly Microsoft.UI.Xaml.Controls.Primitives.Popup _popup;
    private readonly List<DispatcherQueueTimer> _timers = [];
    private FrameworkElement? _target;
    private string _text = string.Empty;
    private Rect _bounds;
    private Point _pointer;
    private bool _disposed;
    internal HoverDetailsSurface(FrameworkElement owner)
    {
        _owner = owner;
        InitializeComponent();
        _popup = new Microsoft.UI.Xaml.Controls.Primitives.Popup { Child = this, IsLightDismissEnabled = false, ShouldConstrainToRootBounds = true };
        _policy = new(Schedule, Open, CloseSurface);
        Scroller.Loaded += (_, _) => Scroller.ChangeView(null, 0, null, disableAnimation: true);
        AddHandler(PointerPressedEvent, new PointerEventHandler((_, _) => _policy.Pressed()), true);
        AddHandler(PointerReleasedEvent, new PointerEventHandler((_, e) => { EndPress(e); e.Handled = true; }), true);
        AddHandler(PointerCaptureLostEvent, new PointerEventHandler((_, e) => EndPress(e)), true);
        AddHandler(PointerCanceledEvent, new PointerEventHandler((_, e) => EndPress(e)), true);
        AddHandler(PointerWheelChangedEvent, new PointerEventHandler((_, e) => e.Handled = true), true);
        _owner.ActualThemeChanged += OnThemeChanged;
        _owner.SizeChanged += OnOwnerSizeChanged;
        Instances.Add(this);
    }
    internal static void CancelActive(XamlRoot? root = null)
    {
        foreach (var surface in Instances.ToArray())
            if (root is null || ReferenceEquals(surface._owner.XamlRoot, root)) surface.Cancel();
    }
    internal static bool TryBounds(XamlRoot root, out Rect bounds)
    {
        bounds = _active?._bounds ?? default;
        return _active?._popup.IsOpen == true && ReferenceEquals(_active._owner.XamlRoot, root);
    }
    internal static void WatchList(ListView list)
    {
        ScrollViewer? scroll = null;
        System.Collections.Specialized.INotifyCollectionChanged? items = null;
        void ItemsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs args) => CancelActive(list.XamlRoot);
        void DetachItems()
        {
            if (items is not null) items.CollectionChanged -= ItemsChanged;
            items = null;
        }
        void AttachItems()
        {
            DetachItems();
            if (list.IsLoaded && list.ItemsSource is System.Collections.Specialized.INotifyCollectionChanged source)
            { items = source; items.CollectionChanged += ItemsChanged; }
        }
        static ScrollViewer? Find(DependencyObject node)
        {
            if (node is ScrollViewer viewer) return viewer;
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
                if (Find(VisualTreeHelper.GetChild(node, i)) is { } found) return found;
            return null;
        }
        void Changing(object? sender, ScrollViewerViewChangingEventArgs args) => CancelActive(list.XamlRoot);
        void Changed(object? sender, ScrollViewerViewChangedEventArgs args) => CancelActive(list.XamlRoot);
        void Detach()
        {
            if (scroll is null) return;
            scroll.ViewChanging -= Changing; scroll.ViewChanged -= Changed; scroll = null;
        }
        void TryAttachScroll(object? sender, object args)
        {
            if (scroll is not null) return;
            scroll = Find(list);
            if (scroll is null) return;
            scroll.ViewChanging += Changing; scroll.ViewChanged += Changed;
            list.LayoutUpdated -= TryAttachScroll;
        }
        list.Loaded += (_, _) =>
        {
            Detach(); AttachItems(); list.ApplyTemplate();
            list.LayoutUpdated -= TryAttachScroll;
            list.LayoutUpdated += TryAttachScroll;
            TryAttachScroll(null, EventArgs.Empty);
        };
        list.Unloaded += (_, _) =>
        {
            CancelActive(list.XamlRoot); Detach(); DetachItems(); list.LayoutUpdated -= TryAttachScroll;
        };
        list.RegisterPropertyChangedCallback(ItemsControl.ItemsSourceProperty, (_, _) => { CancelActive(list.XamlRoot); AttachItems(); });
    }
    internal static bool Owns(object? source)
    {
        for (var node = source as DependencyObject; node is not null; node = VisualTreeHelper.GetParent(node))
            if (node is HoverDetailsSurface) return true;
        return false;
    }
    internal void Request(FrameworkElement target, string text, Point pointer)
    {
        if (_disposed) return;
        _pointer = pointer;
        // Start the new target's 500 ms clock immediately. The old surface finishes
        // its 120 ms handoff independently, without being torn down by transit.
        if (!ReferenceEquals(_active, this) && _active?._policy.IsHandingOff != true)
        { _active?.Cancel(); _active = this; }
        if (!ReferenceEquals(_target, target) || !string.Equals(_text, text, StringComparison.Ordinal))
        {
            _policy.Cancel();
            _target = target; _text = text;
        }
        _policy.Enter(target, text.Length != 0);
    }
    internal void UpdatePointer(Point pointer) { if (!_policy.IsOpen) _pointer = pointer; }
    internal void OwnerEntered() => _policy.OwnerEntered();
    internal void OwnerExited()
    {
        if (!_policy.IsOpen) Cancel(); else _policy.OwnerExited();
    }
    internal void TargetExited()
    {
        if (!_policy.IsOpen) Cancel(); else _policy.TargetExited();
    }
    private void Schedule(TimeSpan delay, Action run)
    {
        var timer = DispatcherQueue.CreateTimer();
        timer.Interval = delay; timer.IsRepeating = false;
        timer.Tick += (_, _) => { _timers.Remove(timer); timer.Stop(); run(); };
        _timers.Add(timer); timer.Start();
    }
    private void Open()
    {
        var root = _owner.XamlRoot;
        if (_disposed || !_owner.IsLoaded || _owner.Visibility != Visibility.Visible || _target is null || root is null || !root.IsHostVisible || root.Size.Width <= 0 || root.Size.Height <= 0)
        { Cancel(); return; }
        try
        {
            if (!ReferenceEquals(_active, this)) { _active?.Cancel(); _active = this; }
            XamlRoot = root;
            _popup.XamlRoot = root;
            RequestedTheme = _owner.ActualTheme;
            ResolveAppearance?.Invoke(Surface, Body, RequestedTheme);
            Body.Text = _text;
            Body.FontSize = (_owner as SnippetRowDetails)?.TextSize?.FontSize ?? 14;
            Width = Math.Min(480, Math.Max(1, root.Size.Width - 16));
            MaxHeight = Math.Min(320, Math.Max(1, root.Size.Height - 16));
            Scroller.MaxHeight = Math.Max(1, MaxHeight - 26);
            Measure(new Size(Width, MaxHeight));
            var placement = HoverDetailsPlacement.Place(root.Size.Width, root.Size.Height,
                _pointer.X, _pointer.Y, Width, Math.Min(DesiredSize.Height, MaxHeight));
            _popup.HorizontalOffset = placement.X; _popup.VerticalOffset = placement.Y;
            _bounds = new Rect(placement.X, placement.Y, placement.Width, placement.Height);
            _popup.IsOpen = true;
            Scroller.ChangeView(null, 0, null, disableAnimation: true);
            BoundsChanged?.Invoke();
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            AppLog.Info("hover_surface_failed"); Cancel();
        }
    }
    private void CloseSurface()
    {
        _popup.IsOpen = false;
        Body.Text = string.Empty;
        _text = string.Empty; _target = null; _bounds = default;
        BoundsChanged?.Invoke();
    }
    private void Cancel()
    {
        _policy.Cancel();
        foreach (var timer in _timers) timer.Stop();
        _timers.Clear();
        CloseSurface();
        if (ReferenceEquals(_active, this)) _active = null;
    }
    private void EndPress(PointerRoutedEventArgs e)
    {
        var root = _owner.XamlRoot;
        if (root is null || !_owner.IsLoaded) { Cancel(); return; }
        var point = e.GetCurrentPoint(root.Content).Position;
        var ownerBounds = _owner.TransformToVisual(root.Content).TransformBounds(new Rect(0, 0, _owner.ActualWidth, _owner.ActualHeight));
        _policy.UpdateRegions(ownerBounds.Contains(point), _bounds.Contains(point));
        _policy.Released();
    }
    private void OnSurfaceEntered(object sender, PointerRoutedEventArgs e) => _policy.SurfaceEntered();
    private void OnSurfaceExited(object sender, PointerRoutedEventArgs e) => _policy.SurfaceExited();
    private void OnThemeChanged(FrameworkElement sender, object args) => Cancel();
    private void OnOwnerSizeChanged(object sender, SizeChangedEventArgs e) => Cancel();
    public void Dispose()
    {
        if (_disposed) return;
        Cancel(); _disposed = true; _policy.Dispose();
        _owner.ActualThemeChanged -= OnThemeChanged;
        _owner.SizeChanged -= OnOwnerSizeChanged;
        _popup.Child = null;
        Instances.Remove(this);
    }
}
