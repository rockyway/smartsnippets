// Test-only primitives for compiling the actual thin FlowProbePage code-behind.
// They neither implement XAML nor prove native event/binding/focus delivery.
namespace Microsoft.UI.Xaml
{
    public class RoutedEventArgs : EventArgs { }
    public enum FocusState { Programmatic }
    public enum Visibility { Visible, Collapsed }
}
namespace Windows.System { public enum VirtualKey { Enter, Escape } }
namespace Microsoft.UI.Xaml.Input
{
    public sealed class KeyRoutedEventArgs : Microsoft.UI.Xaml.RoutedEventArgs
    {
        public Windows.System.VirtualKey Key { get; set; }
        public bool Handled { get; set; }
    }
}
namespace Microsoft.UI.Xaml.Controls
{
    public class Page { }
    public class Button
    {
        public object? Tag { get; set; }
        public int FocusCalls { get; private set; }
        public bool Focus(Microsoft.UI.Xaml.FocusState state) { FocusCalls++; return true; }
    }
    public sealed class TextBox : Button { public string Text { get; set; } = ""; }
    public sealed class TextBlock { public string Text { get; set; } = ""; }
    public class ListViewBase
    {
        public event Action<ListViewBase, ChoosingItemContainerEventArgs>? ChoosingItemContainer;
        public ChoosingItemContainerEventArgs Choose()
        {
            var args = new ChoosingItemContainerEventArgs(); ChoosingItemContainer?.Invoke(this, args); return args;
        }
    }
    public sealed class ListViewItem { }
    public sealed class ChoosingItemContainerEventArgs : EventArgs
    {
        public ListViewItem? ItemContainer { get; set; }
        public bool IsContainerPrepared { get; set; }
    }
    public sealed class ItemClickEventArgs : EventArgs { public object? ClickedItem { get; set; } }
}
namespace SmartSnippets.UnoProbe
{
    public sealed partial class FlowProbePage
    {
        private readonly Microsoft.UI.Xaml.Controls.TextBox Search = new();
        private readonly Microsoft.UI.Xaml.Controls.Button FooterAction = new();
        private readonly Microsoft.UI.Xaml.Controls.TextBlock Status = new();
        private readonly Microsoft.UI.Xaml.Controls.ListViewBase PopupRows = new(), MainRows = new();
        private void InitializeComponent() { }
        internal string TestStatus => Status.Text;
        internal int SearchFocuses => Search.FocusCalls;
        internal int FooterFocuses => FooterAction.FocusCalls;
        internal void TestShortcut() => ShortcutClicked(this, new());
        internal void TestMenu() => MenuClicked(this, new());
        internal void TestPreview() => PreviewClicked(this, new());
        internal void TestHide() => HideClicked(this, new());
        internal void TestCopy() => CopyClicked(this, new());
        internal void TestPopupCopy() => PopupCopyClicked(this, new());
        internal void TestFooterDismiss() => FooterDismissClicked(this, new());
        internal void TestOpenMain() => OpenMainClicked(this, new());
        internal void TestMainInsert(long id) => MainInsertClicked(new Microsoft.UI.Xaml.Controls.Button { Tag = id }, new());
        internal void TestEnter() => SearchKeyDown(this, new() { Key = Windows.System.VirtualKey.Enter });
        internal void TestRow() => RowClicked(this, new() { ClickedItem = Flow.Popup.Items[Flow.Popup.SelectedIndex] });
        internal void TestContainers()
        {
            Xunit.Assert.NotNull(PopupRows.Choose().ItemContainer);
            Xunit.Assert.NotNull(MainRows.Choose().ItemContainer);
        }
    }
}
