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
    private static readonly Dictionary<XamlRoot, Func<(Rect Window, Rect WorkArea)?>> ExternalOwners = [];
    internal static void RegisterExternalOwner(XamlRoot root, Func<(Rect Window, Rect WorkArea)?> bounds) => ExternalOwners[root] = bounds;
    internal static void UnregisterExternalOwner(XamlRoot root)
    {
        CancelActive(root);
        ExternalOwners.Remove(root);
    }
    internal static void TrackExternalPointer(XamlRoot root, Point? point)
    {
        if (_active is not { _isExternal: true } surface || !ReferenceEquals(surface._owner.XamlRoot, root)) return;
        if (point is { } p && surface._transit.Contains(p)) surface._policy.OwnerEntered();
        else surface._policy.OwnerExited();
    }
    internal static Action<Border, TextBlock, ElementTheme, bool>? ResolveAppearance { get; set; }
    internal static event Action? BoundsChanged;
    private readonly FrameworkElement _owner;
    private readonly bool _keyboardOwner;
    private double _keyboardFontSize;
    internal event Action? KeyboardClosed;
    internal bool IsKeyboardOpen => _keyboardOwner && _popup.IsOpen;
    internal static bool HasVisibleContent(XamlRoot root) => _active is { _isMetadata: false } surface
        && surface._popup.IsOpen && ReferenceEquals(surface._owner.XamlRoot, root);
    internal static void CancelRowHovers(XamlRoot? root)
    {
        foreach (var surface in Instances.ToArray())
            if (!surface._keyboardOwner && ReferenceEquals(surface._owner.XamlRoot, root)) surface.Cancel();
    }
    internal void CloseKeyboard() { if (_keyboardOwner) Cancel(); }
    internal void ShowKeyboard(string text, double fontSize)
    {
        if (!_keyboardOwner || _disposed) return;
        foreach (var surface in Instances.ToArray())
            if (!ReferenceEquals(surface, this)) surface.Cancel();
        if (string.IsNullOrEmpty(text)) { Cancel(); return; }
        _target = _owner; _text = text; _isMetadata = false; _keyboardFontSize = fontSize;
        _policy.OpenPinned(_owner);
    }
    private readonly HoverDetailsCoordinator _policy;
    private Microsoft.UI.Xaml.Controls.Primitives.Popup _popup;
    private readonly List<DispatcherQueueTimer> _timers = [];
    private FrameworkElement? _target;
    private string _text = string.Empty;
    private Rect _bounds;
    private Point _pointer;
    private bool _isMetadata;
    private bool _isExternal;
    private Rect _transit;
    private bool _disposed;
    internal HoverDetailsSurface(FrameworkElement owner, bool keyboardOwner = false)
    {
        _owner = owner;
        _keyboardOwner = keyboardOwner;
        InitializeComponent();
        _popup = new Microsoft.UI.Xaml.Controls.Primitives.Popup { Child = this, IsLightDismissEnabled = false, ShouldConstrainToRootBounds = true };
        _policy = new(Schedule, Open, CloseSurface);
        _popup.Closed += OnNativePopupClosed;
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
        return _active?._popup.IsOpen == true && !_active._isExternal && ReferenceEquals(_active._owner.XamlRoot, root);
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
        void Changing(object? sender, ScrollViewerViewChangingEventArgs args) => CancelRowHovers(list.XamlRoot);
        void Changed(object? sender, ScrollViewerViewChangedEventArgs args) => CancelRowHovers(list.XamlRoot);
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
    internal void Request(FrameworkElement target, string text, Point pointer, bool isMetadata)
    {
        if (_disposed) return;
        // While crossing metadata on the way outside, keep content visible. A deliberate
        // 500 ms metadata hover still replaces it; leaving that target cancels its own clock.
        var externalTransit = _active is { _isExternal: true } active
            && ReferenceEquals(active._owner.XamlRoot, _owner.XamlRoot) && active._transit.Contains(pointer);
        if (_active?._keyboardOwner == true) _active.Cancel();
        _pointer = pointer;
        _isMetadata = isMetadata;
        // Start the new target's 500 ms clock immediately. The old surface finishes
        // its 120 ms handoff independently, without being torn down by transit.
        if (!ReferenceEquals(_active, this) && !externalTransit && _active?._policy.IsHandingOff != true)
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
            var external = !_isMetadata && ExternalOwners.ContainsKey(root);
            (Rect Window, Rect WorkArea)? geometry = null;
            if (external)
            {
                geometry = ExternalOwners[root]();
                if (geometry is null) { Cancel(); return; }
            }
            _isExternal = external;
            // An unconstrained Popup cannot be switched back to constrained after it has opened.
            if (_popup.ShouldConstrainToRootBounds == external)
            {
                _popup.IsOpen = false;
                _popup.Child = null;
                _popup.Closed -= OnNativePopupClosed;
                _popup = new Microsoft.UI.Xaml.Controls.Primitives.Popup
                { Child = this, IsLightDismissEnabled = false, ShouldConstrainToRootBounds = !external };
                _popup.Closed += OnNativePopupClosed;
            }
            XamlRoot = root;
            _popup.XamlRoot = root;
            RequestedTheme = _owner.ActualTheme;
            ResolveAppearance?.Invoke(Surface, Body, RequestedTheme, external);
            Height = double.NaN;
            Body.Text = _text;
            Body.FontSize = _keyboardOwner ? _keyboardFontSize : (_owner as SnippetRowDetails)?.TextSize?.FontSize ?? 14;
            MaxWidth = Math.Min(_isMetadata ? 240 : 480,
                Math.Max(1, external ? geometry!.Value.WorkArea.Width : root.Size.Width - 16));
            Width = _isMetadata ? double.NaN : MaxWidth;
            MaxHeight = external ? Math.Max(1, geometry!.Value.WorkArea.Height)
                : Math.Min(320, Math.Max(1, root.Size.Height - 16));
            Scroller.MaxHeight = Math.Max(1, MaxHeight - 26);
            Measure(new Size(MaxWidth, MaxHeight));
            var targetBounds = _target.TransformToVisual(root.Content).TransformBounds(
                new Rect(0, 0, _target.ActualWidth, _target.ActualHeight));
            var placement = HoverDetailsPlacement.Place(root.Size.Width, root.Size.Height,
                _pointer.X, targetBounds.Top, targetBounds.Bottom,
                _isMetadata ? DesiredSize.Width : Width, Math.Min(DesiredSize.Height, MaxHeight));
            if (geometry is { } screen)
            {
                var beside = HoverDetailsPlacement.PlaceBesideWindow(
                    screen.WorkArea.Left, screen.WorkArea.Top, screen.WorkArea.Right, screen.WorkArea.Bottom,
                    screen.Window.Left, screen.Window.Right, screen.Window.Top, Width, screen.Window.Height);
                if (beside is null) { Cancel(); return; }
                placement = beside.Value;
                // Companion panel stays aligned with the overlay even for a single line.
                Height = placement.Height;
                var ownerBounds = _owner.TransformToVisual(root.Content).TransformBounds(
                    new Rect(0, 0, _owner.ActualWidth, _owner.ActualHeight));
                // Keep a horizontal route through the original row, but not neighboring rows.
                var transitLeft = Math.Min(ownerBounds.Left, placement.X);
                var transitRight = Math.Max(ownerBounds.Right, placement.X + placement.Width);
                _transit = new Rect(transitLeft, ownerBounds.Top, transitRight - transitLeft, ownerBounds.Height);
            }
            Width = placement.Width;
            MaxHeight = placement.Height;
            Scroller.MaxHeight = Math.Max(1, MaxHeight - 26);
            _popup.HorizontalOffset = placement.X; _popup.VerticalOffset = placement.Y;
            _bounds = new Rect(placement.X, placement.Y, placement.Width, placement.Height);
            _popup.IsOpen = true;
            if (external && _popup.IsConstrainedToRootBounds) { Cancel(); return; }
            Scroller.ChangeView(null, 0, null, disableAnimation: true);
            BoundsChanged?.Invoke();
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            AppLog.Info("hover_surface_failed"); Cancel();
        }
    }
    private void OnNativePopupClosed(object? sender, object args)
    {
        if (!_keyboardOwner || !_policy.IsOpen) return;
        Cancel();
        KeyboardClosed?.Invoke();
    }
    private void CloseSurface()
    {
        var notifyKeyboard = _keyboardOwner && _popup.IsOpen;
        _popup.IsOpen = false;
        Body.Text = string.Empty;
        _text = string.Empty; _target = null; _bounds = default;
        _isExternal = false; _transit = default;
        BoundsChanged?.Invoke();
        if (notifyKeyboard) KeyboardClosed?.Invoke();
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
        _popup.Closed -= OnNativePopupClosed;
        _popup.Child = null;
        KeyboardClosed = null;
        Instances.Remove(this);
    }
}
