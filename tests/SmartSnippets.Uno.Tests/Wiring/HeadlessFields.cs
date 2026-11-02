// XAML-created named rendering fields only; no event or command consumers are replaced here.
// Action delivery uses the linked production code-behind and the independently reviewed manifest.
using SmartSnippets.Uno.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Controls.Primitives;
namespace SmartSnippets.Uno.Tests.Wiring { internal sealed class HeadlessBindings { public void Update() { } } }
namespace SmartSnippets.Uno { public sealed partial class MainWindow {
private SmartSnippets.Uno.Tests.Wiring.HeadlessBindings Bindings { get; } = new();
internal Grid RootGrid { get; } = new();
internal Grid AppTitleBar { get; } = new();
internal NavigationView Navigation { get; } = new();
internal TextBlock NativeStatusText { get; } = new();
internal NavigationViewItem SnippetsItem { get; } = new();
internal NavigationViewItem FoldersItem { get; } = new();
internal NavigationViewItem SettingsItem { get; } = new();
internal static MainWindow LastCreated { get; private set; } = null!;
internal object? AuditPage => Navigation.Content;
private void InitializeComponent() { LastCreated = this; }
} }
namespace SmartSnippets.Uno.Controls { public sealed partial class HoverDetailsSurface {
private SmartSnippets.Uno.Tests.Wiring.HeadlessBindings Bindings { get; } = new();
internal Border Surface { get; } = new();
internal ScrollViewer Scroller { get; } = new();
internal TextBlock Body { get; } = new();
private void InitializeComponent() { }
} }
namespace SmartSnippets.Uno.Controls { public sealed partial class PopupRow {
private SmartSnippets.Uno.Tests.Wiring.HeadlessBindings Bindings { get; } = new();
private void InitializeComponent() { }
} }
namespace SmartSnippets.Uno.Controls { public sealed partial class ShortcutRecorder {
private SmartSnippets.Uno.Tests.Wiring.HeadlessBindings Bindings { get; } = new();
internal TextBlock DisplayBlock { get; } = new();
private void InitializeComponent() { }
} }
namespace SmartSnippets.Uno.Controls { public sealed partial class SnippetEditor {
private SmartSnippets.Uno.Tests.Wiring.HeadlessBindings Bindings { get; } = new();
internal TextBox SnippetTextBox { get; } = new();
internal PasswordBox SnippetPasswordBox { get; } = new();
internal CheckBox PasswordCheckBox { get; } = new();
internal TextBox MultilinePasswordMask { get; } = new();
internal TextBlock PasswordInputHint { get; } = new();
internal TextBox NewFolderBox { get; } = new();
private void InitializeComponent()
{
    SnippetTextBox.TextChanged += OnTextInput;
    SnippetPasswordBox.PasswordChanged += OnPasswordTextInput;
}
} }
namespace SmartSnippets.Uno.Controls { public sealed partial class SnippetRowDetails {
private SmartSnippets.Uno.Tests.Wiring.HeadlessBindings Bindings { get; } = new();
internal TextBlock LabelText { get; } = new();
internal TextBlock FolderText { get; } = new();
internal TextBlock TagsText { get; } = new();
private void InitializeComponent() { }
} }
namespace SmartSnippets.Uno.Pages { public sealed partial class FoldersPage {
private SmartSnippets.Uno.Tests.Wiring.HeadlessBindings Bindings { get; } = new();
internal ListView FolderList { get; } = new();
internal TextBox RenameBox { get; } = new();
private void InitializeComponent() { }
} }
namespace SmartSnippets.Uno.Pages { public sealed partial class SettingsPage {
private SmartSnippets.Uno.Tests.Wiring.HeadlessBindings Bindings { get; } = new();
internal InfoBar StatusBar { get; } = new();
internal InfoBar HotkeyBanner { get; } = new();
internal ItemsControl KeycapsPanel { get; } = new();
internal ShortcutRecorder Recorder { get; } = new();
internal RadioButtons TextSizeRadios { get; } = new();
internal NumberBox CustomSizeBox { get; } = new();
internal RadioButtons ThemeRadios { get; } = new();
internal RadioButtons ColorPaletteRadios { get; } = new();
internal ToggleSwitch StartupToggle { get; } = new();
internal Button PreviewGlobalDialogButton { get; } = new();
private void InitializeComponent() { }
} }
namespace SmartSnippets.Uno.Pages { public sealed partial class SnippetsPage {
private SmartSnippets.Uno.Tests.Wiring.HeadlessBindings Bindings { get; } = new();
internal Grid Root { get; } = new();
internal ColumnDefinition BrowseColumn { get; } = new();
internal ColumnDefinition ListColumn { get; } = new();
internal ColumnDefinition EditorColumn { get; } = new();
internal Grid BrowsePane { get; } = new();
internal TextBox BrowseNewFolderBox { get; } = new();
internal ListView BrowseList { get; } = new();
internal Grid ListPane { get; } = new();
internal ComboBox BrowseCombo { get; } = new();
internal TextBox SearchBox { get; } = new();
internal Button SortButton { get; } = new();
internal MenuFlyout SortFlyout { get; } = new();
internal RadioMenuFlyoutItem SortLast { get; } = new();
internal RadioMenuFlyoutItem SortUsage { get; } = new();
internal RadioMenuFlyoutItem SortCreated { get; } = new();
internal RadioMenuFlyoutItem SortUpdated { get; } = new();
internal ListView SnippetList { get; } = new();
internal Grid EditorPane { get; } = new();
internal Button BackButton { get; } = new();
internal SnippetEditor EditorControl { get; } = new();
private void InitializeComponent() { }
} }
namespace SmartSnippets.Uno.Popup { public sealed partial class PopupWindow {
private SmartSnippets.Uno.Tests.Wiring.HeadlessBindings Bindings { get; } = new();
internal Grid RootGrid { get; } = new();
internal ContentControl PopupContent { get; } = new();
internal TextBox SearchBox { get; } = new();
internal Button SortButton { get; } = new();
internal MenuFlyout SortFlyout { get; } = new();
internal RadioMenuFlyoutItem SortLast { get; } = new();
internal RadioMenuFlyoutItem SortUsage { get; } = new();
internal RadioMenuFlyoutItem SortCreated { get; } = new();
internal RadioMenuFlyoutItem SortUpdated { get; } = new();
internal ListView PopupList { get; } = new();
internal Button PasteAnywayButton { get; } = new();
internal Grid EditorOverlay { get; } = new();
internal SnippetEditor EditorControl { get; } = new();
internal static PopupWindow LastCreated { get; private set; } = null!;
private void InitializeComponent() { LastCreated = this; }
} }
