using System.ComponentModel;
using System.Diagnostics;
using Microsoft.UI.Input;
using DispatcherQueueTimer = Microsoft.UI.Dispatching.DispatcherQueueTimer;
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
using Windows.UI.ViewManagement;
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
    private MenuFlyout? _contextFlyout;
    private readonly PopupShowCoordinator _shows;
    private DispatcherQueueTimer? _prewarmTimer;
    private readonly UISettings _uiSettings = new();
    private bool _closed;
    private (int Corner, int Border)? _lastFrameFailure;

    internal PopupWindow(ITextSizeService textSize, Action openMainWindow)
    {
        TextSize = textSize; _openMainWindow = openMainWindow;
        InitializeComponent();
        RootGrid.AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler(OnSurfacePointerReleased), true);
        _hwnd = new HWND(WinRT.Interop.WindowNative.GetWindowHandle(this));
        _shows = new PopupShowCoordinator(action => DispatcherQueue.TryEnqueue(() => action()), ApplyFocus,
            StartWindowWarmup, StopWindowWarmup, () => AppLog.Info("popup_prewarm_failed", "retry=next_show"));
        // The default presenter is not guaranteed to be an OverlappedPresenter before first show.
        // Keep a native border so DWM owns both the perimeter and the rounded clip.
        var presenter = OverlappedPresenter.Create();
        presenter.SetBorderAndTitleBar(true, false); presenter.IsResizable = false; presenter.IsAlwaysOnTop = true;
        presenter.IsMinimizable = false; presenter.IsMaximizable = false;
        AppWindow.SetPresenter(presenter);
        RefreshFrame();
        _uiSettings.ColorValuesChanged += OnSystemColorsChanged;
        AppWindow.IsShownInSwitchers = false;
        Activated += OnActivated; RootGrid.LayoutUpdated += OnLayoutUpdated; PopupList.LayoutUpdated += OnListLayoutUpdated;
        Closed += (_, _) =>
        {
            _closed = true;
            _uiSettings.ColorValuesChanged -= OnSystemColorsChanged;
            if (_viewModel is not null) _viewModel.PropertyChanged -= OnViewModelChanged;
            CompositionTarget.Rendered -= OnRendered;
            RootGrid.LayoutUpdated -= OnPrewarmLayout;
            _shows.Close();
            EditorControl.ViewModel = null;
        };
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
    internal Func<bool>? CanShow { get; set; }
    internal XamlRoot? ContentXamlRoot => RootGrid.XamlRoot;
    public event EventHandler<bool>? VisibleChanged;
    private string SearchPlaceholder => UiText.PopupSearchPlaceholder;
    private string SearchName => UiText.PopupSearchName;
    private string ToggleLayoutName => UiText.ToggleLayout;
    private string SortName => UiText.SortButton;
    private string LastUsedText => UiText.SortName(SortField.LastUsed);
    private string UsageCountText => UiText.SortName(SortField.UsageCount);
    private string CreatedText => UiText.SortName(SortField.Created);
    private string UpdatedText => UiText.SortName(SortField.Updated);
    private string AddText => UiText.AddSnippet;
    private string MainWindowText => UiText.OpenMainWindow;
    private string CopyText => UiText.CopyText;
    private string RestartText => UiText.RestartAsAdministrator;
    private string PasteAnywayText => UiText.PasteAnyway;
    private string CancelText => UiText.Cancel;
    private string DismissText => UiText.Dismiss;
    private string SaveText => UiText.Save;
    private string EditorHint => UiText.CtrlEnterSaves;
    private Visibility Show(bool value) => value ? Visibility.Visible : Visibility.Collapsed;
    private InfoBarSeverity FooterSeverity(PopupFooterState state) => state switch
    {
        PopupFooterState.Error => InfoBarSeverity.Error,
        PopupFooterState.ConfirmClipboardLoss or PopupFooterState.MaybePasted => InfoBarSeverity.Warning,
        _ => InfoBarSeverity.Informational,
    };

    // ThemeService calls this after its native dark-mode/title-bar updates as well.
    internal void RefreshFrame()
    {
        if (_closed) return;
        try
        {
            var text = WindowSurface.WindowTextColor();
            var color = PopupFramePolicy.BorderColor(RootGrid.ActualTheme == ElementTheme.Dark,
                WindowSurface.IsHighContrast(), text.R, text.G, text.B);
            var result = WindowSurface.RoundPopup(this, color);
            if (result.Corner < 0 || result.Border < 0)
            {
                if (_lastFrameFailure != result)
                    AppLog.Info("popup_frame_failed", $"corner_hr=0x{result.Corner:X8} border_hr=0x{result.Border:X8}");
                _lastFrameFailure = result;
            }
            else _lastFrameFailure = null;
        }
        catch (System.Runtime.InteropServices.COMException ex)
        {
            // Frame styling is cosmetic: never let a platform lookup take the popup (or startup) down.
            AppLog.Info("popup_frame_failed", $"hr=0x{ex.HResult:X8}");
        }
    }
    private void OnSystemColorsChanged(UISettings sender, object args)
        => DispatcherQueue.TryEnqueue(() => RefreshFrame());
    private void ShowSurface(bool activate = true)
    {
        RefreshFrame();
        AppWindow.Show(activate);
        // First Show can initialize/reconfigure the native surface. Reassert before returning.
        RefreshFrame();
    }

    internal void Prewarm() => _shows.BeginWarmup();
    private void StartWindowWarmup()
    {
        // Keep the compositor surface cloaked until themed content has completed first layout.
        // Showing without activation is needed to create XamlRoot; immediate Hide aborted layout.
        WindowSurface.Cloak(this, true);
        RootGrid.LayoutUpdated += OnPrewarmLayout;
        var generation = _shows.WarmupGeneration;
        _prewarmTimer = DispatcherQueue.CreateTimer();
        _prewarmTimer.Interval = TimeSpan.FromSeconds(2);
        _prewarmTimer.IsRepeating = false;
        _prewarmTimer.Tick += (_, _) => _shows.WarmupTimedOut(generation);
        _prewarmTimer.Start();
        AppWindow.MoveAndResize(new RectInt32(-32000, -32000, LogicalWidth, LogicalHeight));
        ShowSurface(false);
    }
    private void StopWindowWarmup()
    {
        _prewarmTimer?.Stop(); _prewarmTimer = null;
        RootGrid.LayoutUpdated -= OnPrewarmLayout;
        AppWindow.Hide();
        WindowSurface.Cloak(this, false);
        RefreshFrame();
    }
    private void OnPrewarmLayout(object? sender, object e)
    {
        if (!_shows.IsWarming || !AppWindow.IsVisible || RootGrid.XamlRoot is null || RootGrid.ActualWidth <= 0) return;
        _shows.CompleteWarmup(_shows.WarmupGeneration);
    }
    internal void ShowFor(InsertionTarget target, long hotkeyTimestamp) => _shows.RequestShow(() => ShowForReady(target, hotkeyTimestamp));
    private PopupShowResult ShowForReady(InsertionTarget target, long hotkeyTimestamp)
    {
        if (CanShow?.Invoke() == false) return default;
        var mode = ViewModel.Open(target);
        if (mode == PopupOpenMode.Ignored) { AppLog.Info("popup_open_ignored", "reason=inserting"); return default; }
        _hotkeyTimestamp = hotkeyTimestamp; _readyPending = true;
        Center(); ShowSurface(); SetVisible(true); ReportReady(); Activate(); PInvoke.SetForegroundWindow(_hwnd);
        return new(true, mode == PopupOpenMode.Resumed ? PopupFocusTarget.Editor : PopupFocusTarget.Search);
    }
    internal void ShowPreview(HWND owner, string? query = null, bool activate = true) => _shows.RequestShow(() => ShowPreviewReady(owner, query, activate));
    private PopupShowResult ShowPreviewReady(HWND owner, string? query, bool activate)
    {
        if (CanShow?.Invoke() == false) return default;
        if (ViewModel.OpenPreview() == PopupOpenMode.Ignored) return default;
        if (query is not null) ViewModel.Query = query;
        var monitor = PInvoke.MonitorFromWindow(owner, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTOPRIMARY);
        if (!WindowPlacement.CenterOn(AppWindow, monitor, LogicalWidth, LogicalHeight))
            WindowPlacement.CenterOn(AppWindow, PInvoke.MonitorFromWindow(_hwnd, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTOPRIMARY), LogicalWidth, LogicalHeight);
        ShowSurface(activate); SetVisible(true);
        if (activate)
        {
            Activate(); PInvoke.SetForegroundWindow(_hwnd);
        }
        return new(true, activate ? PopupFocusTarget.Search : null);
    }
    private void Center()
    {
        if (ViewModel.Target is not { } target) return;
        var monitor = new HMONITOR(target.MonitorHandle);
        if (!WindowPlacement.CenterOn(AppWindow, monitor, LogicalWidth, LogicalHeight))
            WindowPlacement.CenterOn(AppWindow, PInvoke.MonitorFromWindow(_hwnd, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTOPRIMARY), LogicalWidth, LogicalHeight);
    }
    public void Hide()
    {
        _shows.Hide();
        if (!AppWindow.IsVisible && !_visible) return;
        CompositionTarget.Rendered -= OnRendered;
        _searchStarted = 0; _awaitingLayout = false; _awaitingFrame = false;
        AppWindow.Hide(); SetVisible(false);
    }
    public void ShowAgain() => _shows.RequestShow(ShowAgainReady);
    private PopupShowResult ShowAgainReady()
    {
        if (CanShow?.Invoke() == false) return default;
        ViewModel.EnsureCurrent();
        Center(); ShowSurface(); SetVisible(true);
        var focused = ForegroundActivation.TryActivate(_hwnd, out var path);
        AppLog.Info("popup_reopened", $"outcome={ViewModel.LastInsertionOutcome} focus={path}");
        if (!focused) NotifyReopenFailure?.Invoke(ViewModel.FooterMessage);
        return new(true, focused ? PopupFocusTarget.Search : null);
    }
    public void OpenMainWindow() { _openMainWindow(); Hide(); AppLog.Info("popup_closed", "reason=main_window"); }
    // Retain the deferred/recoverable host contract, but edit on this surface.
    public void EditSnippet(long snippetId)
    {
        ViewModel.EnsureCurrent();
        var item = ViewModel.Items.FirstOrDefault(row => row.Kind == PopupItemKind.Snippet && row.SnippetId == snippetId);
        if (item is not null) ViewModel.EditRow(item);
    }
    public void Focus(PopupFocusTarget target) => _shows.Focus(target);
    private void ApplyFocus(PopupFocusTarget target)
    {
        if (CanShow?.Invoke() == false || !_visible) return;
        if (target == PopupFocusTarget.Editor) EditorControl.FocusText();
        else if (target == PopupFocusTarget.Footer) PasteAnywayButton.Focus(FocusState.Programmatic);
        else SearchBox.Focus(FocusState.Programmatic);
    }
    private void SetVisible(bool value) { if (_visible == value) return; _visible = value; VisibleChanged?.Invoke(this, value); }
    private void OnActivated(object sender, WindowActivatedEventArgs e)
    {
        RefreshFrame();
        if (e.WindowActivationState == WindowActivationState.Deactivated) { if (_visible && _contextFlyout is null) ViewModel.OnDeactivated(); return; }
        ReportReady();
    }
    private void OnLayoutUpdated(object? sender, object e) => ReportReady();
    private void ReportReady() { if (!_readyPending || !_visible) return; _readyPending = false; var ms = (int)Stopwatch.GetElapsedTime(_hotkeyTimestamp).TotalMilliseconds; AppLog.Info("popup_ready_ms", ms.ToString()); AppLog.Info(PopupLogFormat.Show(ms)); }
    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (ViewModel.IsEditorOpen) return;
        if (e.Key == VirtualKey.Escape && _contextFlyout is { } flyout) { e.Handled = true; flyout.Hide(); }
        else if (e.Key == VirtualKey.Escape) { e.Handled = true; ViewModel.Close(); }
        else if (e.Key == VirtualKey.Application || (e.Key == VirtualKey.F10 && (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift) & CoreVirtualKeyStates.Down) != 0))
        {
            if (PopupList.SelectedItem is PopupItemViewModel selected && PopupList.ContainerFromItem(selected) is ListViewItem container)
            { e.Handled = true; ShowRowMenu(container, selected); }
        }
        else if (ReferenceEquals(FocusManager.GetFocusedElement(RootGrid.XamlRoot), SearchBox) && (e.Key == VirtualKey.Up || e.Key == VirtualKey.Down)) { e.Handled = true; ViewModel.MoveSelection(e.Key == VirtualKey.Up ? -1 : 1); }
        else if (ReferenceEquals(FocusManager.GetFocusedElement(RootGrid.XamlRoot), SearchBox) && e.Key == VirtualKey.Enter && !ViewModel.IsConfirmingClipboardLoss) { e.Handled = true; _ = ViewModel.InsertSelectedAsync(); }
    }
    private void OnItemClick(object sender, ItemClickEventArgs e) { if (e.ClickedItem is PopupItemViewModel item) _ = ViewModel.ActivateAsync(item); }
    private void OnContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs e)
    {
        if (e.Item is PopupItemViewModel item)
        {
            e.ItemContainer.RightTapped -= OnRowRightTapped;
            e.ItemContainer.RightTapped += OnRowRightTapped;
            e.ItemContainer.IsEnabled = item.Kind == PopupItemKind.Snippet;
            e.ItemContainer.IsTabStop = item.Kind == PopupItemKind.Snippet;
        }
    }
    private void OnRowRightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (sender is not ListViewItem container || container.Content is not PopupItemViewModel item || !ViewModel.CanUseRowActions(item)) return;
        e.Handled = true;
        ShowRowMenu(container, item, e.GetPosition(container));
    }
    private void ShowRowMenu(ListViewItem container, PopupItemViewModel item, Windows.Foundation.Point? position = null)
    {
        if (!ViewModel.CanUseRowActions(item)) return;
        ViewModel.SelectedIndex = ViewModel.Items.IndexOf(item);
        var menu = new MenuFlyout();
        var copy = new MenuFlyoutItem { Text = UiText.RowActionCopy, IsEnabled = ViewModel.CanCopyRow(item) };
        var insert = new MenuFlyoutItem { Text = UiText.RowActionInsert };
        var edit = new MenuFlyoutItem { Text = UiText.RowActionEdit };
        var delete = new MenuFlyoutItem { Text = UiText.RowActionDelete };
        copy.Click += (_, _) => ViewModel.CopyRow(item);
        insert.Click += async (_, _) => await ViewModel.InsertRowAsync(item);
        edit.Click += (_, _) => ViewModel.EditRow(item);
        delete.Click += async (_, _) => await ViewModel.DeleteRowAsync(item);
        menu.Items.Add(copy); menu.Items.Add(insert); menu.Items.Add(edit); menu.Items.Add(delete);
        _contextFlyout = menu;
        menu.Closed += (_, _) =>
        {
            if (ReferenceEquals(_contextFlyout, menu)) _contextFlyout = null;
            DispatcherQueue.TryEnqueue(() =>
            {
                if (!_visible) return;
                if (PInvoke.GetForegroundWindow() != _hwnd) ViewModel.OnDeactivated();
                else Focus(ViewModel.IsEditorOpen ? PopupFocusTarget.Editor : ViewModel.IsConfirmingClipboardLoss ? PopupFocusTarget.Footer : PopupFocusTarget.Search);
            });
        };
        if (position is { } point) menu.ShowAt(container, new Microsoft.UI.Xaml.Controls.Primitives.FlyoutShowOptions { Position = point });
        else menu.ShowAt(container); // Keyboard context-menu invocation has no pointer position.
    }
    private void OnSurfacePointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!_visible || ViewModel.IsEditorOpen || _contextFlyout is not null || SortFlyout.IsOpen) return;
        // Observe handled events too (ScrollViewer consumes blank-list presses), but
        // never steal focus from actionable controls, row activation or scrollbars.
        for (var node = e.OriginalSource as DependencyObject; node is not null && node != RootGrid; node = VisualTreeHelper.GetParent(node))
            if (node is TextBox or Microsoft.UI.Xaml.Controls.Primitives.ButtonBase or Microsoft.UI.Xaml.Controls.Primitives.SelectorItem or Microsoft.UI.Xaml.Controls.Primitives.RangeBase) return;
        Focus(ViewModel.IsConfirmingClipboardLoss ? PopupFocusTarget.Footer : PopupFocusTarget.Search);
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
