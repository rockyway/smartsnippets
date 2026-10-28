using System.ComponentModel;
using System.Diagnostics;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using SmartSnippets.App.Services;
using SmartSnippets.Core.Logging;
using SmartSnippets.Core.Settings;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Presentation.Text;
using SmartSnippets.Presentation.ViewModels;
using Windows.Graphics;
using Windows.System;
using CoreVirtualKeyStates = Windows.UI.Core.CoreVirtualKeyStates;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

namespace SmartSnippets.App.Popup;

public sealed partial class PopupWindow : Window, IPopupHost
{
    private const int LogicalWidth = 640;
    private const int LogicalHeight = 480;
    private readonly HWND _hwnd;
    private readonly Action _openMainWindow;
    private PopupViewModel _viewModel = null!;
    private long _hotkeyTimestamp;
    private bool _readyPending;
    private bool _visible;
    private long _searchStarted;
    private bool _awaitingLayout;
    private bool _awaitingFrame;

    internal PopupWindow(ITextSizeService textSize, Action openMainWindow)
    {
        TextSize = textSize; _openMainWindow = openMainWindow;
        InitializeComponent();
        _hwnd = new HWND(WinRT.Interop.WindowNative.GetWindowHandle(this));
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.SetBorderAndTitleBar(false, false); presenter.IsResizable = false; presenter.IsAlwaysOnTop = true;
            presenter.IsMinimizable = false; presenter.IsMaximizable = false;
        }
        AppWindow.IsShownInSwitchers = false;
        Activated += OnActivated; RootGrid.LayoutUpdated += OnLayoutUpdated; PopupList.LayoutUpdated += OnListLayoutUpdated;
    }

    public ITextSizeService TextSize { get; }
    public PopupViewModel ViewModel
    {
        get => _viewModel;
        set
        {
            if (_viewModel is not null) _viewModel.PropertyChanged -= OnViewModelChanged;
            _viewModel = value; _viewModel.PropertyChanged += OnViewModelChanged; Bindings.Update();
        }
    }
    internal Action<string>? NotifyReopenFailure { get; set; }
    internal XamlRoot? ContentXamlRoot => RootGrid.XamlRoot;
    public event EventHandler<bool>? VisibleChanged;
    private Visibility Show(bool value) => value ? Visibility.Visible : Visibility.Collapsed;
    private InfoBarSeverity FooterSeverity(PopupFooterState state) => state switch
    {
        PopupFooterState.Error => InfoBarSeverity.Error,
        PopupFooterState.ConfirmClipboardLoss or PopupFooterState.MaybePasted => InfoBarSeverity.Warning,
        _ => InfoBarSeverity.Informational,
    };

    internal void Prewarm()
    {
        AppWindow.MoveAndResize(new RectInt32(-32000, -32000, LogicalWidth, LogicalHeight)); AppWindow.Show(false); AppWindow.Hide();
    }
    internal void ShowFor(InsertionTarget target, long hotkeyTimestamp)
    {
        var mode = ViewModel.Open(target);
        if (mode == PopupOpenMode.Ignored) { AppLog.Info("popup_open_ignored", "reason=inserting"); return; }
        _hotkeyTimestamp = hotkeyTimestamp; _readyPending = true;
        Center(); AppWindow.Show(); SetVisible(true); Activate(); PInvoke.SetForegroundWindow(_hwnd);
        Focus(mode == PopupOpenMode.Resumed ? PopupFocusTarget.Editor : PopupFocusTarget.Search);
    }
    private void Center()
    {
        if (ViewModel.Target is not { } target) return;
        var monitor = new HMONITOR(target.MonitorHandle);
        if (!WindowPlacement.CenterOn(AppWindow, monitor, LogicalWidth, LogicalHeight))
            WindowPlacement.CenterOn(AppWindow, PInvoke.MonitorFromWindow(_hwnd, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTOPRIMARY), LogicalWidth, LogicalHeight);
    }
    public void Hide() { if (!AppWindow.IsVisible && !_visible) return; AppWindow.Hide(); SetVisible(false); }
    public void ShowAgain()
    {
        Center(); AppWindow.Show(); SetVisible(true);
        var focused = ForegroundActivation.TryActivate(_hwnd, out var path);
        AppLog.Info("popup_reopened", $"outcome={ViewModel.LastInsertionOutcome} focus={path}");
        if (focused) Focus(PopupFocusTarget.Search); else NotifyReopenFailure?.Invoke(ViewModel.FooterMessage);
    }
    public void OpenMainWindow() { _openMainWindow(); Hide(); AppLog.Info("popup_closed", "reason=main_window"); }
    public void Focus(PopupFocusTarget target)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (target == PopupFocusTarget.Editor) EditorControl.FocusText();
            else if (target == PopupFocusTarget.Footer) PasteAnywayButton.Focus(FocusState.Programmatic);
            else SearchBox.Focus(FocusState.Programmatic);
        });
    }
    private void SetVisible(bool value) { if (_visible == value) return; _visible = value; VisibleChanged?.Invoke(this, value); }
    private void OnActivated(object sender, WindowActivatedEventArgs e)
    {
        if (e.WindowActivationState == WindowActivationState.Deactivated) { if (_visible) ViewModel.OnDeactivated(); return; }
        ReportReady();
    }
    private void OnLayoutUpdated(object? sender, object e) => ReportReady();
    private void ReportReady() { if (!_readyPending) return; _readyPending = false; AppLog.Info("popup_ready_ms", ((int)Stopwatch.GetElapsedTime(_hotkeyTimestamp).TotalMilliseconds).ToString()); }
    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (ViewModel.IsEditorOpen) return;
        if (e.Key == VirtualKey.Escape) { e.Handled = true; ViewModel.Close(); }
        else if (ReferenceEquals(FocusManager.GetFocusedElement(RootGrid.XamlRoot), SearchBox) && (e.Key == VirtualKey.Up || e.Key == VirtualKey.Down)) { e.Handled = true; ViewModel.MoveSelection(e.Key == VirtualKey.Up ? -1 : 1); }
        else if (ReferenceEquals(FocusManager.GetFocusedElement(RootGrid.XamlRoot), SearchBox) && e.Key == VirtualKey.Enter && !ViewModel.IsConfirmingClipboardLoss) { e.Handled = true; _ = ViewModel.InsertSelectedAsync(); }
    }
    private void OnItemClick(object sender, ItemClickEventArgs e) { if (e.ClickedItem is PopupItemViewModel item) _ = ViewModel.ActivateAsync(item); }
    private void OnContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs e)
    {
        if (e.Item is PopupItemViewModel item)
        {
            e.ItemContainer.IsEnabled = item.Kind == PopupItemKind.Snippet;
            e.ItemContainer.IsTabStop = item.Kind == PopupItemKind.Snippet;
            StyleSelection(e.ItemContainer, ReferenceEquals(e.Item, PopupList.SelectedItem));
        }
    }
    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        foreach (var item in e.RemovedItems)
            if (PopupList.ContainerFromItem(item) is ListViewItem container) StyleSelection(container, false);
        foreach (var item in e.AddedItems)
            if (PopupList.ContainerFromItem(item) is ListViewItem container) StyleSelection(container, true);
    }
    private void StyleSelection(Microsoft.UI.Xaml.Controls.Primitives.SelectorItem container, bool selected)
    {
        container.Background = selected ? SelectedPalette.Background : null;
        container.BorderBrush = selected ? SelectedPalette.BorderBrush : null;
        container.BorderThickness = selected ? new Thickness(3, 0, 0, 0) : new Thickness(0);
    }
    private void OnSortClick(object sender, RoutedEventArgs e)
    {
        SortLast.IsChecked = ViewModel.Sort == SortField.LastUsed; SortUsage.IsChecked = ViewModel.Sort == SortField.UsageCount;
        SortCreated.IsChecked = ViewModel.Sort == SortField.Created; SortUpdated.IsChecked = ViewModel.Sort == SortField.Updated;
    }
    private void OnSortLast(object sender, RoutedEventArgs e) => ViewModel.SetSort(SortField.LastUsed);
    private void OnSortUsage(object sender, RoutedEventArgs e) => ViewModel.SetSort(SortField.UsageCount);
    private void OnSortCreated(object sender, RoutedEventArgs e) => ViewModel.SetSort(SortField.Created);
    private void OnSortUpdated(object sender, RoutedEventArgs e) => ViewModel.SetSort(SortField.Updated);
    private void OnEditorSave(object sender, RoutedEventArgs e) => _ = ViewModel.SaveEditorAsync();
    private void OnEditorCancel(object sender, RoutedEventArgs e) => _ = ViewModel.CancelEditorAsync(false);
    private void OnEditorEscape(object? sender, EventArgs e) => _ = ViewModel.CancelEditorAsync(true);
    private void OnEditorPreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        static bool Down(VirtualKey key) => (InputKeyboardSource.GetKeyStateForCurrentThread(key) & CoreVirtualKeyStates.Down) != 0;
        if (e.Key == VirtualKey.Enter && Down(VirtualKey.Control) && !Down(VirtualKey.Shift) && !Down(VirtualKey.Menu))
        { e.Handled = true; _ = ViewModel.SaveEditorAsync(); }
    }
    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PopupViewModel.Items) && _searchStarted != 0) _awaitingLayout = true;
        if (e.PropertyName == nameof(PopupViewModel.SelectedIndex) && ViewModel.SelectedIndex >= 0 && ViewModel.SelectedIndex < ViewModel.Items.Count)
        {
            var item = ViewModel.Items[ViewModel.SelectedIndex];
            PopupList.ScrollIntoView(item);
            var peer = FrameworkElementAutomationPeer.FromElement(SearchBox) ?? FrameworkElementAutomationPeer.CreatePeerForElement(SearchBox);
            peer?.RaiseNotificationEvent(AutomationNotificationKind.Other, AutomationNotificationProcessing.MostRecent, item.ToString(), "PopupSelection");
        }
    }
    private void OnSearchChanged(object sender, TextChangedEventArgs e)
    {
        if (ViewModel is null || !_visible) return;
        CompositionTarget.Rendered -= OnRendered;
        _searchStarted = Stopwatch.GetTimestamp(); _awaitingLayout = false; _awaitingFrame = false;
        ViewModel.Query = SearchBox.Text;
        if (!_awaitingLayout) AwaitFrame();
    }
    private void OnListLayoutUpdated(object? sender, object e) { if (_awaitingLayout) { _awaitingLayout = false; AwaitFrame(); } }
    private void AwaitFrame() { if (_awaitingFrame || _searchStarted == 0) return; _awaitingFrame = true; CompositionTarget.Rendered += OnRendered; }
    private void OnRendered(object? sender, object e)
    {
        CompositionTarget.Rendered -= OnRendered; _awaitingFrame = false;
        if (_searchStarted == 0) return;
        var elapsed = (int)Stopwatch.GetElapsedTime(_searchStarted).TotalMilliseconds; _searchStarted = 0;
        AppLog.Info("popup_search_ms", PopupLogFormat.SearchMs(elapsed, ViewModel.Layout, ViewModel.Items.Count(i => i.Kind == PopupItemKind.Snippet)));
    }
}
