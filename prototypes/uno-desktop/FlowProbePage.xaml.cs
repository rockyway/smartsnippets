using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using SmartSnippets.Presentation.ViewModels;
using SmartSnippets.Presentation.Services;
using Windows.System;

namespace SmartSnippets.UnoProbe;

public sealed partial class FlowProbePage : Page
{
    public ProbeFlowComposition Flow { get; }
    private readonly bool _isPopup;
    public Visibility MainOnly => _isPopup ? Visibility.Collapsed : Visibility.Visible;
    public Visibility PopupOnly => _isPopup ? Visibility.Visible : Visibility.Collapsed;
    public FlowProbePage(ProbeFlowComposition flow, bool isPopup = false)
    {
        Flow = flow; _isPopup = isPopup; InitializeComponent();
#if WINDOWS
        PopupRows.ChoosingItemContainer += ChooseContainer;
        MainRows.ChoosingItemContainer += ChooseContainer;
#endif
    }
#if WINDOWS
    private void ChooseContainer(ListViewBase sender, ChoosingItemContainerEventArgs args)
    {
        if (args.ItemContainer is null) args.ItemContainer = new ListViewItem();
        args.IsContainerPrepared = false;
    }
#endif
    internal void ApplyFocus(PopupFocusTarget target)
    {
        switch (target)
        {
            case PopupFocusTarget.Search: Search.Focus(FocusState.Programmatic); break;
            case PopupFocusTarget.Footer: FooterAction.Focus(FocusState.Programmatic); break;
            case PopupFocusTarget.Editor: Status.Text = "Inline editor focus NOT IMPLEMENTED."; break;
        }
    }
    private void FooterDismissClicked(object sender, RoutedEventArgs args) => Flow.OnFooterDismiss();
    private void PopupCopyClicked(object sender, RoutedEventArgs args)
    {
        if (!_isPopup || Flow.Popup.SelectedIndex < 0) return;
        try { Flow.OnPopupCopy(Flow.Popup.Items[Flow.Popup.SelectedIndex]); }
        catch (InvalidOperationException) { Status.Text = "Clipboard adapter unavailable; no copy performed."; }
    }
    private void ShortcutClicked(object sender, RoutedEventArgs args) => Flow.OnShortcut();
    private void MenuClicked(object sender, RoutedEventArgs args) => Flow.OnMenu();
    private void PreviewClicked(object sender, RoutedEventArgs args) => Flow.OnPreview();
    private void OpenMainClicked(object sender, RoutedEventArgs args) => Flow.OnOpenMain();
    private void HideClicked(object sender, RoutedEventArgs args) => Flow.OnHide();
    private void CopyClicked(object sender, RoutedEventArgs args)
    {
        if (_isPopup) return;
        try { Flow.OnMainCopy(Flow.SnippetId); }
        catch (InvalidOperationException) { Status.Text = "Clipboard adapter unavailable; no copy performed."; }
    }
    private async void MainInsertClicked(object sender, RoutedEventArgs args)
    {
        if (sender is Button { Tag: long id }) await Flow.OnMainInsertAsync(id);
    }
    private async void SearchKeyDown(object sender, KeyRoutedEventArgs args)
    {
        if (args.Key != VirtualKey.Enter) return;
        args.Handled = true;
        await Flow.OnEnterAsync();
    }
    private async void RowClicked(object sender, ItemClickEventArgs args)
    {
        if (args.ClickedItem is PopupItemViewModel row) await Flow.OnRowAsync(row);
    }
}
