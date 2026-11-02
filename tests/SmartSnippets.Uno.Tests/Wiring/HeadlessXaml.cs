// Headless rendering/OS boundary, not replacement action handlers or view-models.
// No desktop is created. Timers are explicitly advanced by the action tests.
using Windows.Foundation;
namespace Microsoft.UI.Dispatching
{
    public enum DispatcherQueuePriority { Low }
    public sealed class DispatcherQueue
    {
        public static DispatcherQueue GetForCurrentThread() => new();
        public static List<DispatcherQueueTimer> Timers { get; } = [];
        public bool TryEnqueue(Action action) { action(); return true; }
        public bool TryEnqueue(DispatcherQueuePriority priority, Action action) => TryEnqueue(action);
        public DispatcherQueueTimer CreateTimer() { var timer = new DispatcherQueueTimer(); Timers.Add(timer); return timer; }
        public static void Tick() { foreach (var timer in Timers.ToArray()) timer.Fire(); }
    }
    public sealed class DispatcherQueueTimer
    {
        public TimeSpan Interval { get; set; }
        public bool IsRepeating { get; set; }
        public bool IsRunning { get; private set; }
        public event EventHandler<object>? Tick;
        public void Start() => IsRunning = true;
        public void Stop() => IsRunning = false;
        public void Fire() { if (!IsRunning) return; if (!IsRepeating) Stop(); Tick?.Invoke(this, new()); }
    }
}
namespace Windows.Foundation
{
    public record struct Point(double X, double Y);
    public record struct Size(double Width, double Height);
    public record struct Rect(double X, double Y, double Width, double Height)
    {
        public double Left => X; public double Top => Y; public double Right => X + Width; public double Bottom => Y + Height;
        public bool Contains(Point p) => p.X >= Left && p.X <= Right && p.Y >= Top && p.Y <= Bottom;
    }
}
namespace Windows.System
{
    public enum VirtualKey : uint { None, Back=8, Tab=9, Enter=13, Shift=16, Control=17, Menu=18, Escape=27, PageUp=33, PageDown=34, Left=37, Up=38, Right=39, Down=40, Delete=46, A=65, LeftWindows=91, RightWindows=92, Application=93, F10=121 }
}
namespace Windows.UI.Core { [Flags] public enum CoreVirtualKeyStates { None=0, Down=1 } }
namespace Microsoft.UI.Input
{
    public static class InputKeyboardSource
    {
        public static HashSet<Windows.System.VirtualKey> Down { get; } = [];
        public static Windows.UI.Core.CoreVirtualKeyStates GetKeyStateForCurrentThread(Windows.System.VirtualKey key) => Down.Contains(key) ? Windows.UI.Core.CoreVirtualKeyStates.Down : 0;
    }
    public enum NonClientRegionKind { Caption, Passthrough }
    public sealed class InputNonClientPointerSource
    {
        public static InputNonClientPointerSource GetForWindowId(int id) => new();
        public void SetRegionRects(NonClientRegionKind kind, Windows.Graphics.RectInt32[] rects) { }
    }
}
namespace Microsoft.UI.Xaml
{
    public enum Visibility { Visible, Collapsed }
    public enum FocusState { Unfocused, Programmatic, Pointer, Keyboard }
    public enum ElementTheme { Default, Light, Dark }
    public enum WindowActivationState { Deactivated, CodeActivated }
    public enum GridUnitType { Star }
    public record struct GridLength(double Value, GridUnitType Unit = default);
    public delegate void RoutedEventHandler(object sender, RoutedEventArgs args);
    public delegate void SizeChangedEventHandler(object sender, SizeChangedEventArgs args);
    public delegate void WindowActivatedEventHandler(object sender, WindowActivatedEventArgs args);
    public class RoutedEventArgs : EventArgs { }
    public sealed class SizeChangedEventArgs : RoutedEventArgs { public Size NewSize { get; set; } }
    public sealed class DependencyPropertyChangedEventArgs : EventArgs { public DependencyProperty Property { get; set; } = null!; }
    public sealed class PropertyMetadata(object? value, Action<DependencyObject, DependencyPropertyChangedEventArgs>? changed = null)
    { internal object? Value = value; internal Action<DependencyObject, DependencyPropertyChangedEventArgs>? Changed = changed; }
    public sealed class DependencyProperty(PropertyMetadata metadata)
    {
        internal PropertyMetadata Metadata = metadata;
        public static DependencyProperty Register(string name, Type type, Type owner, PropertyMetadata metadata) => new(metadata);
    }
    public class DependencyObject
    {
        private readonly Dictionary<DependencyProperty, object?> values = [];
        public long RegisterPropertyChangedCallback(DependencyProperty property, Action<DependencyObject, DependencyProperty> callback) => 0;
        public DependencyObject? Parent { get; set; }
        public List<DependencyObject> Children { get; } = [];
        public object GetValue(DependencyProperty property) => (values.TryGetValue(property, out var value) ? value : property.Metadata.Value)!;
        public void SetValue(DependencyProperty property, object? value) { values[property] = value; property.Metadata.Changed?.Invoke(this, new() { Property = property }); }
    }
    public class UIElement : DependencyObject
    {
        public static object PointerPressedEvent { get; } = new();
        public static object PointerReleasedEvent { get; } = new();
        public static object PointerCaptureLostEvent { get; } = new();
        public static object PointerCanceledEvent { get; } = new();
        public static object PointerWheelChangedEvent { get; } = new();
        public static object PointerMovedEvent { get; } = new();
        public static object PreviewKeyDownEvent { get; } = new();
        public static object KeyUpEvent { get; } = new();
        public XamlRoot XamlRoot { get; set; } = new();
        public void AddHandler(object routedEvent, Delegate handler, bool handled) { }
        public void RemoveHandler(object routedEvent, Delegate handler) { }
    }
    public class FrameworkElement : UIElement
    {
        public string Name { get; set; } = "";
        public object? DataContext { get; set; }
        public Visibility Visibility { get; set; }
        public bool IsLoaded { get; set; } = true;
        public bool IsEnabled { get; set; } = true;
        public bool IsTabStop { get; set; }
        public double ActualWidth { get; set; } = 1000;
        public double ActualHeight { get; set; } = 700;
        public double Width { get; set; }
        public double Height { get; set; }
        public double MaxWidth { get; set; }
        public double MaxHeight { get; set; }
        public Size DesiredSize { get; set; } = new(240, 100);
        public ElementTheme ActualTheme { get; set; }
        public ElementTheme RequestedTheme { get; set; }
        public FocusState FocusState { get; set; }
        public Microsoft.UI.Dispatching.DispatcherQueue DispatcherQueue { get; } = new();
        public event RoutedEventHandler? Loaded;
        public event RoutedEventHandler? Unloaded;
        public event EventHandler<object>? LayoutUpdated;
        public event Action<FrameworkElement, object>? ActualThemeChanged;
        public event SizeChangedEventHandler? SizeChanged;
        public event EventHandler<Input.PointerRoutedEventArgs>? PointerExited;
        public bool Focus(FocusState state) { FocusState = state; Input.FocusManager.Focused = this; return true; }
        public Media.GeneralTransform TransformToVisual(UIElement? element) => new();
        public void Measure(Size available) { }
        public void RaiseBoundaryEvents() { Loaded?.Invoke(this, new()); Unloaded?.Invoke(this, new()); LayoutUpdated?.Invoke(this, new()); ActualThemeChanged?.Invoke(this, new()); SizeChanged?.Invoke(this, new()); PointerExited?.Invoke(this, new()); }
    }
    public sealed class XamlRoot
    {
        public UIElement Content { get; set; } = null!;
        public bool IsHostVisible { get; set; } = true;
        public Size Size { get; set; } = new(1000,700);
        public double RasterizationScale { get; set; } = 1;
        public event Action<XamlRoot, XamlRootChangedEventArgs>? Changed;
        public void RaiseChanged() => Changed?.Invoke(this, new());
    }
    public sealed class XamlRootChangedEventArgs : EventArgs { }
    public sealed class Style { }
    public sealed class ResourceDictionary : Dictionary<string, object> { }
    public sealed class Application
    {
        public static Application Current { get; } = new();
        public void Exit() { }
        public ResourceDictionary Resources { get; } = new() { ["SolidMenuFlyoutPresenterStyle"] = new Style() };
    }
    public sealed class WindowActivatedEventArgs : EventArgs { public WindowActivationState WindowActivationState { get; set; } }
    public class Window
    {
        public Microsoft.UI.Windowing.AppWindow AppWindow { get; } = new();
        public Microsoft.UI.Dispatching.DispatcherQueue DispatcherQueue { get; } = new();
        public FrameworkElement? Content { get; set; } = new();
        public string Title { get; set; } = "";
        public bool ExtendsContentIntoTitleBar { get; set; }
        public event WindowActivatedEventHandler? Activated;
        public event EventHandler<object>? Closed;
        public void SetTitleBar(UIElement element) { }
        public void Close() => Closed?.Invoke(this, new());
        public void ActivateBoundary() => Activated?.Invoke(this, new());
    }
}
namespace Microsoft.UI.Xaml.Input
{
    public delegate void PointerEventHandler(object sender, PointerRoutedEventArgs e);
    public delegate void KeyEventHandler(object sender, KeyRoutedEventArgs e);
    public sealed class KeyRoutedEventArgs : RoutedEventArgs { public Windows.System.VirtualKey Key { get; set; } public bool Handled { get; set; } }
    public class PointerRoutedEventArgs : RoutedEventArgs
    {
        public bool Handled { get; set; }
        public object? OriginalSource { get; set; }
        public Point Position { get; set; }
        public PointerPoint GetCurrentPoint(UIElement? element) => new(Position);
    }
    public sealed record PointerPoint(Point Position);
    public delegate void RightTappedEventHandler(object sender, RightTappedRoutedEventArgs args);
    public sealed class RightTappedRoutedEventArgs : RoutedEventArgs { public bool Handled { get; set; } public Point GetPosition(UIElement element) => default; }
    public sealed class KeyboardAccelerator { }
    public sealed class KeyboardAcceleratorInvokedEventArgs : EventArgs { public bool Handled { get; set; } }
    public static class FocusManager { public static object? Focused { get; set; } public static object? GetFocusedElement(XamlRoot root) => Focused; }
}
namespace Microsoft.UI.Xaml.Controls
{
    public class Control : FrameworkElement
    {
        protected virtual void OnPointerPressed(Input.PointerRoutedEventArgs e) { }
        protected virtual void OnLostFocus(RoutedEventArgs e) { }
        protected virtual void OnKeyDown(Input.KeyRoutedEventArgs e) { }
        protected virtual void OnKeyUp(Input.KeyRoutedEventArgs e) { }
    }
    public class ContentControl : Control { public object? Background { get; set; } }
    public class ItemsControl : Control { public static DependencyProperty ItemsSourceProperty { get; } = new(new(null)); }
    public class InfoBar : Control { }
    public class Page : Control { }
    public class UserControl : Control { }
    public class Grid : FrameworkElement { public object? Background { get; set; } public static void SetColumn(FrameworkElement element, int column) { } }
    public class StackPanel : FrameworkElement { }
    public sealed class ColumnDefinition { public GridLength Width { get; set; } }
    public sealed class TextChangedEventArgs : RoutedEventArgs { }
    public sealed class SelectionChangedEventArgs : RoutedEventArgs { }
    public class TextBox : Control
    {
        public string Text { get; set; } = "";
        public int SelectionStart { get; set; }
        public int SelectionLength { get; set; }
        public event EventHandler<object>? TextCompositionStarted;
        public event EventHandler<object>? TextCompositionEnded;
        public void SelectAll() { SelectionStart = 0; SelectionLength = Text.Length; }
        public void Select(int start, int length) { SelectionStart = start; SelectionLength = length; }
        public void ComposeBoundary() { TextCompositionStarted?.Invoke(this, new()); TextCompositionEnded?.Invoke(this, new()); }
    }
    public class TextBlock : FrameworkElement { public string Text { get; set; } = ""; public double FontSize { get; set; } }
    public class ToggleSwitch : Control { public bool IsOn { get; set; } }
    public class ComboBox : Control { public object? SelectedItem { get; set; } public bool IsDropDownOpen { get; set; } }
    public class RadioButtons : Control { public int SelectedIndex { get; set; } }
    public class NumberBox : Control { public double Value { get; set; } }
    public sealed class NumberBoxValueChangedEventArgs : EventArgs { public double NewValue { get; set; } }
    public class Border : FrameworkElement { }
    public sealed class ScrollViewerViewChangingEventArgs : EventArgs { }
    public sealed class ScrollViewerViewChangedEventArgs : EventArgs { }
    public class ScrollViewer : FrameworkElement
    {
        public event EventHandler<ScrollViewerViewChangingEventArgs>? ViewChanging;
        public event EventHandler<ScrollViewerViewChangedEventArgs>? ViewChanged;
        public void ChangeView(object? x, double y, object? zoom, bool disableAnimation) { ViewChanging?.Invoke(this, new()); ViewChanged?.Invoke(this, new()); }
    }
    public class ListViewBase : Control { }
    public class ListView : ListViewBase
    {
        public void ApplyTemplate() { }
        public object? SelectedItem { get; set; }
        public object? ItemsSource { get; set; }
        public object? ContainerFromItem(object item) => new ListViewItem { Content = item };
        public void ScrollIntoView(object item) { }
    }
    public class ListViewItem : Primitives.SelectorItem
    {
        public object? Content { get; set; }
        public event Input.RightTappedEventHandler? RightTapped;
        public void RaiseRightTap(Input.RightTappedRoutedEventArgs args) => RightTapped?.Invoke(this, args);
    }
    public sealed class ItemClickEventArgs : RoutedEventArgs { public object? ClickedItem { get; set; } }
    public sealed class ContainerContentChangingEventArgs : EventArgs { public object? Item { get; set; } public ListViewItem ItemContainer { get; set; } = new(); }
    public class Button : Primitives.ButtonBase { }
    public sealed class RadioMenuFlyoutItem : MenuFlyoutItem { public bool IsChecked { get; set; } }
    public class MenuFlyoutItem : FrameworkElement
    {
        public string Text { get; set; } = "";
        public event EventHandler<RoutedEventArgs>? Click;
        public void Invoke() { if (IsEnabled) Click?.Invoke(this, new()); }
    }
    public class MenuFlyout
    {
        public static MenuFlyout? LastShown { get; private set; }
        public Style? MenuFlyoutPresenterStyle { get; set; }
        public List<MenuFlyoutItem> Items { get; } = [];
        public bool IsOpen { get; private set; }
        public event EventHandler<object>? Opening;
        public event EventHandler<object>? Closed;
        public void ShowAt(FrameworkElement element, Primitives.FlyoutShowOptions? options = null) { LastShown = this; Opening?.Invoke(this, new()); IsOpen = true; }
        public void Hide() { if (!IsOpen) return; IsOpen = false; Closed?.Invoke(this, new()); }
    }
    public enum InfoBarSeverity { Informational, Success, Warning, Error }
    public class NavigationView : Control { public object? Content { get; set; } public object? SelectedItem { get; set; } }
    public sealed class NavigationViewItem : Control { public object? Tag { get; set; } }
    public sealed class NavigationViewSelectionChangedEventArgs : EventArgs { public object? SelectedItem { get; set; } }
}
namespace Microsoft.UI.Xaml.Controls.Primitives
{
    public class ButtonBase : Controls.Control { }
    public class SelectorItem : Controls.Control { }
    public class RangeBase : Controls.Control { }
    public enum FlyoutPlacementMode { Bottom }
    public enum FlyoutShowMode { Transient }
    public sealed class FlyoutShowOptions { public Point Position { get; set; } public FlyoutPlacementMode Placement { get; set; } public FlyoutShowMode ShowMode { get; set; } }
    public sealed class Popup : FrameworkElement
    {
        public UIElement? Child { get; set; }
        public bool IsLightDismissEnabled { get; set; }
        public bool ShouldConstrainToRootBounds { get; set; }
        public bool IsConstrainedToRootBounds => ShouldConstrainToRootBounds;
        public double HorizontalOffset { get; set; }
        public double VerticalOffset { get; set; }
        public bool IsOpen { get; set; }
        public event EventHandler<object>? Closed;
        public void CloseBoundary() { IsOpen = false; Closed?.Invoke(this, new()); }
    }
}
namespace Microsoft.UI.Xaml.Media
{
    public sealed class SolidColorBrush { }
    public sealed class GeneralTransform { public Rect TransformBounds(Rect rect) => rect; }
    public static class VisualTreeHelper
    {
        public static int GetChildrenCount(DependencyObject parent) => parent.Children.Count;
        public static DependencyObject GetChild(DependencyObject parent, int index) => parent.Children[index];
        public static DependencyObject? GetParent(DependencyObject child) => child.Parent;
    }
    public static class CompositionTarget { public static event EventHandler<object>? Rendered; public static void Render() => Rendered?.Invoke(null, new()); }
}
namespace Microsoft.UI.Windowing
{
    public sealed class AppWindowChangedEventArgs : EventArgs { public bool DidPositionChange { get; set; } public bool DidSizeChange { get; set; } }
    public sealed class AppWindowClosingEventArgs : EventArgs { public bool Cancel { get; set; } }
    public sealed class AppWindow
    {
        public int Id => 1;
        public Windows.Graphics.SizeInt32 Size { get; set; } = new(1000,700);
        public bool IsVisible { get; private set; }
        public bool IsShownInSwitchers { get; set; }
        public event Action<AppWindow, AppWindowChangedEventArgs>? Changed;
        public event Action<AppWindow, AppWindowClosingEventArgs>? Closing;
        public void SetIcon(string path) { }
        public void SetPresenter(OverlappedPresenter presenter) { }
        public void Show(bool activate) => IsVisible = true;
        public void Hide() => IsVisible = false;
        public void MoveAndResize(Windows.Graphics.RectInt32 rect) { Size = new(rect.Width,rect.Height); Changed?.Invoke(this,new()); }
        public void RequestClose() => Closing?.Invoke(this,new());
    }
    public sealed class OverlappedPresenter
    {
        public static OverlappedPresenter Create() => new();
        public int PreferredMinimumWidth { get; set; }
        public int PreferredMinimumHeight { get; set; }
        public bool IsResizable { get; set; }
        public bool IsAlwaysOnTop { get; set; }
        public bool IsMinimizable { get; set; }
        public bool IsMaximizable { get; set; }
        public bool HasBorder { get; private set; }
        public bool HasTitleBar { get; private set; }
        public void SetBorderAndTitleBar(bool border, bool title) { HasBorder = border; HasTitleBar = title; }
    }
}
namespace Windows.Graphics { public record struct SizeInt32(int Width,int Height); public record struct RectInt32(int X,int Y,int Width,int Height); }
namespace Windows.UI.ViewManagement
{
    public sealed class UISettings { public event Action<UISettings,object>? ColorValuesChanged; public void ChangeBoundary() => ColorValuesChanged?.Invoke(this,new()); }
}
namespace Windows.Win32.Foundation { public readonly record struct HWND(nint Value); }
namespace Windows.Win32.Graphics.Gdi { public readonly record struct HMONITOR(nint Value); }
namespace Windows.Win32.UI.WindowsAndMessaging { }
namespace Windows.Win32 { public static class PInvoke { public static int MessageBox(Foundation.HWND owner, string text, string title, UI.WindowsAndMessaging.MESSAGEBOX_STYLE style) => 0; public static uint GetDpiForWindow(Foundation.HWND window) => 96; public static Foundation.HWND GetForegroundWindow() => new(1); } }
namespace WinRT.Interop { public static class WindowNative { public static nint GetWindowHandle(object window) => 1; } }
namespace Microsoft.UI.Xaml.Automation.Peers
{
    public enum AutomationNotificationKind { Other }
    public enum AutomationNotificationProcessing { MostRecent }
    public sealed class FrameworkElementAutomationPeer
    {
        public static FrameworkElementAutomationPeer? FromElement(FrameworkElement element) => null;
        public static FrameworkElementAutomationPeer CreatePeerForElement(FrameworkElement element) => new();
        public void RaiseNotificationEvent(AutomationNotificationKind kind, AutomationNotificationProcessing processing, string text, string activity) { }
    }
}
namespace SmartSnippets.Uno.Services
{
    internal static class FlyoutSurface { internal static void KeepSolid(Microsoft.UI.Xaml.Controls.MenuFlyout menu) { } }
    internal static class WindowPlacement
    {
        internal static bool ParkBottomLeft(Microsoft.UI.Windowing.AppWindow window,int width,int height) => true;
        internal static bool PlaceManagement(Microsoft.UI.Windowing.AppWindow window,int width,int height,bool qa) => true;
        internal static bool CenterAtCursor(Microsoft.UI.Windowing.AppWindow window,nint capturedMonitor,int width,int height) => true;
        internal static bool CenterOn(Microsoft.UI.Windowing.AppWindow window,Windows.Win32.Graphics.Gdi.HMONITOR monitor,int width,int height) => true;
        internal static (Rect Window, Rect WorkArea)? HoverBounds(Windows.Win32.Foundation.HWND window,double scale) => (new(0,0,640,480),new(0,0,1920,1080));
    }
    internal static class WindowSurface
    {
        internal static (byte R,byte G,byte B) WindowTextColor() => (0,0,0);
        internal static bool IsHighContrast() => false;
        internal static (int Corner,int Border) RoundPopup(Microsoft.UI.Xaml.Window window,int color) => (0,0);
        internal static readonly System.Collections.Generic.Dictionary<Microsoft.UI.Xaml.Window, bool> Cloaked = new();
        internal static void Cloak(Microsoft.UI.Xaml.Window window,bool cloak) { Cloaked[window] = cloak; }
    }
}
