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
using SmartSnippets.Uno.Services;
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

namespace SmartSnippets.Uno.Popup;

public sealed partial class PopupWindow : Window, IPopupHost
{
    internal Func<Func<Task>, Task> RunOperation { get; set; } = operation => operation();

    private const int LogicalWidth = 640;
    private const int LogicalHeight = 480;
    private readonly HWND _hwnd;
    private readonly Action _openMainWindow;
    private PopupViewModel _viewModel = null!;
    private bool _visible;
    private long _searchStarted;
    private bool _awaitingLayout;
    private bool _awaitingFrame;
    private MenuFlyout? _contextFlyout;
    private readonly CandidatePopupLifecycle _lifecycle;
    private PopupShowCoordinator _shows => _lifecycle.Shows;
    private DispatcherQueueTimer? _prewarmTimer;
    private readonly UISettings _uiSettings = new();
    private bool _closed;
    private bool _previewSortMenuPending;
    private bool _previewSortMenuQueued;
    private (int Corner, int Border)? _lastFrameFailure;
    private XamlRoot? _hoverRoot;
    private readonly PopupKeyboardPreviewPolicy _keyboardPreview = new();
    private Controls.HoverDetailsSurface? _keyboardSurface;
    private bool _movingKeyboardSelection, _isComposing, _sortMenuOpen;

    private void RegisterHoverOwner(object sender, RoutedEventArgs args)
    {
        if (_hoverRoot is not null) UnregisterHoverOwner();
        if (RootGrid.XamlRoot is not { } root) return;
        _hoverRoot = root;
        Controls.HoverDetailsSurface.RegisterExternalOwner(root, () => WindowPlacement.HoverBounds(_hwnd, root.RasterizationScale));
        root.Changed += OnHoverRootChanged;
    }
    private void UnregisterHoverOwner()
    {
        if (_hoverRoot is not { } root) return;
        root.Changed -= OnHoverRootChanged;
        Controls.HoverDetailsSurface.UnregisterExternalOwner(root);
        _hoverRoot = null;
    }
    private void OnHoverRootChanged(XamlRoot sender, XamlRootChangedEventArgs args)
    {
        ResetKeyboardNavigation();
        Controls.HoverDetailsSurface.CancelActive(sender);
    }
    private void OnHoverWindowChanged(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if ((args.DidPositionChange || args.DidSizeChange) && _hoverRoot is { } root)
        {
            ResetKeyboardNavigation();
            Controls.HoverDetailsSurface.CancelActive(root);
        }
    }

    internal PopupWindow(ITextSizeService textSize, Action openMainWindow)
    {
        TextSize = textSize; _openMainWindow = openMainWindow;
        InitializeComponent();
        FlyoutSurface.KeepSolid(SortFlyout);
        Controls.HoverDetailsSurface.WatchList(PopupList);
        PopupList.AddHandler(UIElement.PointerWheelChangedEvent, new PointerEventHandler((_, _) => _keyboardSurface?.CloseKeyboard()), true);
        TextSize.PropertyChanged += OnKeyboardTextSizeChanged;
        Controls.HoverDetailsSurface.BoundsChanged += UpdateDragRegions;
        SortFlyout.Opening += (_, _) =>
        {
            _sortMenuOpen = true; ResetKeyboardNavigation();
            Controls.HoverDetailsSurface.CancelActive(RootGrid.XamlRoot);
        };
        SortFlyout.Closed += (_, _) => _sortMenuOpen = false;
        RootGrid.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDown), true);
        RootGrid.AddHandler(UIElement.KeyUpEvent, new KeyEventHandler((_, e) =>
        {
            if (PreviewKey(e.Key) is { } key) _keyboardPreview.Released(key);
        }), true);
        RootGrid.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler((_, e) =>
        {
            if (!Controls.HoverDetailsSurface.Owns(e.OriginalSource)) ResetKeyboardNavigation();
        }), true);
        SearchBox.TextCompositionStarted += (_, _) => { _isComposing = true; ResetKeyboardNavigation(); };
        SearchBox.TextCompositionEnded += (_, _) => _isComposing = false;
        RootGrid.AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler(OnSurfacePointerReleased), true);
        _hwnd = new HWND(WinRT.Interop.WindowNative.GetWindowHandle(this));
        RootGrid.Loaded += RegisterHoverOwner;
        RootGrid.Unloaded += (_, _) => UnregisterHoverOwner();
        AppWindow.Changed += OnHoverWindowChanged;
        RootGrid.AddHandler(UIElement.PointerMovedEvent, new PointerEventHandler((_, e) =>
        {
            if (_hoverRoot is { } root) Controls.HoverDetailsSurface.TrackExternalPointer(root, e.GetCurrentPoint(root.Content).Position);
        }), true);
        RootGrid.PointerExited += (_, _) =>
        {
            if (_hoverRoot is { } root) Controls.HoverDetailsSurface.TrackExternalPointer(root, null);
        };
        _lifecycle = new CandidatePopupLifecycle(action => DispatcherQueue.TryEnqueue(() => action()), ApplyFocus,
            StartWindowWarmup, StopWindowWarmup, () => AppLog.Info("popup_prewarm_failed", "retry=next_show"));
        // The default presenter is not guaranteed to be an OverlappedPresenter before first show.
        // Keep a native border so DWM owns both the perimeter and the rounded clip.
        var presenter = OverlappedPresenter.Create();
        presenter.SetBorderAndTitleBar(true, false); presenter.IsResizable = false; presenter.IsAlwaysOnTop = true;
        presenter.IsMinimizable = false; presenter.IsMaximizable = false;
        AppWindow.SetPresenter(presenter);
        AppLog.Info("popup_frame_policy", $"border={presenter.HasBorder} titlebar={presenter.HasTitleBar} topmost={presenter.IsAlwaysOnTop}");
        ExtendsContentIntoTitleBar = true; // lets the OS treat Caption regions as draggable
        RefreshFrame();
        _uiSettings.ColorValuesChanged += OnSystemColorsChanged;
        AppWindow.IsShownInSwitchers = false;
        Activated += OnActivated; RootGrid.LayoutUpdated += OnLayoutUpdated; PopupList.LayoutUpdated += OnListLayoutUpdated;
        Closed += (_, _) =>
        {
            _closed = true; _visible = false;
            CancelReveal();
            _searchTimer?.Stop(); _searchTimer = null;
            _searchStarted = 0; _awaitingLayout = false; _awaitingFrame = false;
            RootGrid.LayoutUpdated -= OnLayoutUpdated;
            PopupList.LayoutUpdated -= OnListLayoutUpdated;
            Activated -= OnActivated;
            Controls.HoverDetailsSurface.CancelActive(RootGrid.XamlRoot);
            Controls.HoverDetailsSurface.BoundsChanged -= UpdateDragRegions;
            AppWindow.Changed -= OnHoverWindowChanged;
            RootGrid.Loaded -= RegisterHoverOwner;
            UnregisterHoverOwner();
            ResetKeyboardNavigation(releaseKeys: true);
            _keyboardSurface?.Dispose(); _keyboardSurface = null;
            TextSize.PropertyChanged -= OnKeyboardTextSizeChanged;
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
    internal Func<bool>? CanInsertNative { get; set; }
    internal XamlRoot? ContentXamlRoot => RootGrid.XamlRoot;
    public event EventHandler<bool>? VisibleChanged;
    private string HintLine(string hint, string status, bool isPreview) => CandidateUiPolicy.HintLine(hint, status, isPreview);
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

    internal void SetPaletteBackground(SolidColorBrush brush)
    {
        RootGrid.Background = brush;
        PopupContent.Background = brush;
        EditorOverlay.Background = brush;
    }

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
    private void ShowSurface(bool activate = false, bool revealAfterFrame = true)
    {
        ResetKeyboardNavigation(releaseKeys: true);
        Controls.HoverDetailsSurface.CancelActive();
        RefreshFrame();
        // A hidden window's swap chain is not presented, so the first frames after Show can be the
        // unpainted (white, then black) surface. Stay cloaked until XAML has produced a frame.
        if (revealAfterFrame) WindowSurface.Cloak(this, true);
        AppWindow.Show(activate);
        // First Show can initialize/reconfigure the native surface. Reassert before returning.
        RefreshFrame();
        if (revealAfterFrame) RevealAfterFrame();
    }
    private int _revealToken;
    private EventHandler<object>? _revealFrameHandler;
    private DispatcherQueueTimer? _revealTimer;
    private void CancelReveal()
    {
        _revealToken++;
        if (_revealFrameHandler is { } handler) CompositionTarget.Rendered -= handler;
        _revealFrameHandler = null;
        _revealTimer?.Stop(); _revealTimer = null;
    }
    private void RevealAfterFrame()
    {
        CancelReveal();
        var token = _revealToken;
        void Reveal()
        {
            if (token != _revealToken || _closed) return;
            CancelReveal();
            if (!_shows.IsWarming) WindowSurface.Cloak(this, false);
        }
        _revealFrameHandler = (_, _) => DispatcherQueue.TryEnqueue(Reveal);
        CompositionTarget.Rendered += _revealFrameHandler;
        _revealTimer = DispatcherQueue.CreateTimer();
        _revealTimer.Interval = TimeSpan.FromMilliseconds(150); _revealTimer.IsRepeating = false;
        _revealTimer.Tick += (_, _) => Reveal();
        _revealTimer.Start();
    }

    internal void Prewarm() => _shows.BeginWarmup();
    private void StartWindowWarmup()
    {
        if (_closed) return;
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
        ShowSurface(false, revealAfterFrame: false);
    }
    private void StopWindowWarmup()
    {
        _prewarmTimer?.Stop(); _prewarmTimer = null;
        RootGrid.LayoutUpdated -= OnPrewarmLayout;
        if (_closed) return;
        AppWindow.Hide();
        WindowSurface.Cloak(this, false);
        RefreshFrame();
    }
    private void OnPrewarmLayout(object? sender, object e)
    {
        if (_closed || !_shows.IsWarming || !AppWindow.IsVisible || RootGrid.XamlRoot is null || RootGrid.ActualWidth <= 0) return;
        _shows.CompleteWarmup(_shows.WarmupGeneration);
    }
    internal void ShowPreview(HWND owner, string? query = null, bool activate = false, bool openSortMenu = false)
    {
        if (CanShow?.Invoke() == false) return;
        _lifecycle.Preview(ViewModel, query, () => ShowPreviewReady(openSortMenu));
    }
    internal void ShowCaptured(InsertionTarget target, Func<bool> canShow)
    {
        if (CanShow?.Invoke() == false || !canShow()) return;
        _lifecycle.Captured(ViewModel, target,
            () => CanShow?.Invoke() != false && canShow(),
            () =>
            {
                if (CanShow?.Invoke() == false || !canShow()) return default;
                Center();
                // Captured routing does not grant native foreground/focus permission.
                // A reviewed harness must separately authorize any activating input path.
                ShowSurface(false); SetVisible(true);
                return new PopupShowResult(true, null);
            });
    }
    private PopupShowResult ShowPreviewReady(bool openSortMenu)
    {
        if (CanShow?.Invoke() == false) return default;
        Center();
        // The QA menu seam is always non-activating, even if a caller uses the default activate value.
        const bool activate = false; // All candidate previews are nonactivating.
        _previewSortMenuPending = openSortMenu;
        ShowSurface(activate); SetVisible(true);
        QueuePreviewSortMenu();

        return new(true, activate ? PopupFocusTarget.Search : null);
    }
    private void Center()
    {
        if (ViewModel.Target is { } target
            && WindowPlacement.CenterOn(AppWindow, new HMONITOR(target.MonitorHandle), LogicalWidth, LogicalHeight)) return;
        if (!WindowPlacement.ParkBottomLeft(AppWindow, LogicalWidth, LogicalHeight))
            throw new InvalidOperationException("Candidate preview bottom-left placement could not be verified.");
    }
    public void Hide()
    {
        CancelReveal();
        if (_closed) return;
        ResetKeyboardNavigation(releaseKeys: true);
        _isComposing = false;
        _previewSortMenuPending = false;
        Controls.PopupRow.CancelActiveTooltip();
        _searchTimer?.Stop();
        SortFlyout.Hide();
        _shows.Hide();
        if (!AppWindow.IsVisible && !_visible) return;
        CompositionTarget.Rendered -= OnRendered;
        _searchStarted = 0; _awaitingLayout = false; _awaitingFrame = false;
        AppWindow.Hide(); SetVisible(false);
    }
    public void ShowAgain() => _lifecycle.Reopen(ViewModel, ShowAgainReady);
    private PopupShowResult ShowAgainReady()
    {
        if (CanShow?.Invoke() == false) return default;
        ViewModel.EnsureCurrent();
        Center(); ShowSurface(false); SetVisible(true);
        const bool focused = false;
        const string path = "candidate_no_activation";
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
        if (_closed || CanShow?.Invoke() == false || !_visible) return;
        if (target == PopupFocusTarget.Editor) EditorControl.FocusText();
        else if (target == PopupFocusTarget.Footer) PasteAnywayButton.Focus(FocusState.Programmatic);
        else SearchBox.Focus(FocusState.Programmatic);
    }
    private void SetVisible(bool value) { if (_visible == value) return; _visible = value; VisibleChanged?.Invoke(this, value); }
    private void OnActivated(object sender, WindowActivatedEventArgs e)
    {
        RefreshFrame();
        if (e.WindowActivationState == WindowActivationState.Deactivated) { ResetKeyboardNavigation(releaseKeys: true); Controls.HoverDetailsSurface.CancelActive(RootGrid.XamlRoot); if (_visible && _contextFlyout is null) ViewModel.OnDeactivated(); return; }

    }
    private void OnLayoutUpdated(object? sender, object e)
    {
        UpdateDragRegions();

        QueuePreviewSortMenu();
    }
    private void QueuePreviewSortMenu()
    {
        if (!_previewSortMenuPending || _previewSortMenuQueued || _closed || !_visible ||
            SortButton.XamlRoot is null || SortButton.ActualWidth <= 0 || SortButton.ActualHeight <= 0) return;
        // Leave the layout callback before creating another popup surface. Transient does not
        // take keyboard focus (Standard, including Button's normal flyout invocation, does).
        _previewSortMenuQueued = DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
        {
            _previewSortMenuQueued = false;
            if (!_previewSortMenuPending || _closed || !_visible || CanShow?.Invoke() == false) return;
            _previewSortMenuPending = false;
            UpdateSortChecks();
            SortFlyout.ShowAt(SortButton, new Microsoft.UI.Xaml.Controls.Primitives.FlyoutShowOptions
            {
                Placement = Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.Bottom,
                ShowMode = Microsoft.UI.Xaml.Controls.Primitives.FlyoutShowMode.Transient,
            });
        });
    }
    private static PopupPreviewKey? PreviewKey(VirtualKey key) => key switch
    {
        VirtualKey.Right => PopupPreviewKey.Right, VirtualKey.Left => PopupPreviewKey.Left,
        VirtualKey.Escape => PopupPreviewKey.Escape, VirtualKey.Back => PopupPreviewKey.Backspace, _ => null,
    };
    private PopupItemViewModel? SelectedSnippet => ViewModel.SelectedIndex >= 0 && ViewModel.SelectedIndex < ViewModel.Items.Count
        && ViewModel.Items[ViewModel.SelectedIndex].Kind == PopupItemKind.Snippet ? ViewModel.Items[ViewModel.SelectedIndex] : null;
    private void ResetKeyboardNavigation(bool releaseKeys = false)
    {
        _keyboardPreview.Reset(releaseKeys);
        _keyboardSurface?.CloseKeyboard();
    }
    private void OnKeyboardTextSizeChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (_keyboardPreview.IsPreviewOpen) ShowKeyboardPreview();
    }
    private void ShowKeyboardPreview()
    {
        if (!_visible || _closed || ViewModel.IsEditorOpen || SelectedSnippet is not { } selected)
        { ResetKeyboardNavigation(); return; }
        if (_keyboardSurface is null)
        {
            _keyboardSurface = new Controls.HoverDetailsSurface(RootGrid, keyboardOwner: true);
            _keyboardSurface.KeyboardClosed += _keyboardPreview.PreviewClosed;
        }
        _keyboardSurface.ShowKeyboard(selected.ContentText, TextSize.FontSize);
        if (!_keyboardSurface.IsKeyboardOpen) _keyboardPreview.PreviewClosed();
    }
    private void ApplyKeyboardEffect(PopupPreviewEffect effect)
    {
        if ((effect & PopupPreviewEffect.Close) != 0) Controls.HoverDetailsSurface.CancelActive(RootGrid.XamlRoot);
        if ((effect & PopupPreviewEffect.Show) != 0) ShowKeyboardPreview();
        if ((effect & PopupPreviewEffect.ReturnSearch) != 0)
        {
            Focus(PopupFocusTarget.Search);
            SearchBox.Select(Math.Clamp(_keyboardPreview.CaretStart, 0, SearchBox.Text.Length),
                Math.Clamp(_keyboardPreview.CaretLength, 0, SearchBox.Text.Length - Math.Clamp(_keyboardPreview.CaretStart, 0, SearchBox.Text.Length)));
        }
    }
    private void OnPreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Handled || !_visible || ViewModel.IsEditorOpen || _isComposing || _contextFlyout is not null || _sortMenuOpen) return;
        var focused = FocusManager.GetFocusedElement(RootGrid.XamlRoot) as DependencyObject;
        var inList = false;
        for (var node = focused; node is not null; node = VisualTreeHelper.GetParent(node))
            if (ReferenceEquals(node, PopupList)) { inList = true; break; }
        if (!ReferenceEquals(focused, SearchBox) && !inList) return;
        static bool Down(VirtualKey key) => (InputKeyboardSource.GetKeyStateForCurrentThread(key) & CoreVirtualKeyStates.Down) != 0;
        if (Down(VirtualKey.Control) || Down(VirtualKey.Shift) || Down(VirtualKey.Menu))
        { ResetKeyboardNavigation(); return; }
        if (e.Key is VirtualKey.Enter or VirtualKey.Down or VirtualKey.Up or VirtualKey.PageDown or VirtualKey.PageUp or VirtualKey.Tab
            || e.Key == VirtualKey.Right && _keyboardPreview.IsNavigating) ApplyQuery();
        if (e.Key is VirtualKey.Up or VirtualKey.Down)
        {
            e.Handled = true;
            _movingKeyboardSelection = true;
            try { ViewModel.MoveSelection(e.Key == VirtualKey.Up ? -1 : 1); }
            finally { _movingKeyboardSelection = false; }
            ApplyKeyboardEffect(_keyboardPreview.Navigated(SelectedSnippet?.SnippetId, SearchBox.SelectionStart, SearchBox.SelectionLength));
            return;
        }
        if (PreviewKey(e.Key) is { } previewKey)
        {
            var effect = _keyboardPreview.Handle(previewKey, SelectedSnippet?.SnippetId,
                Controls.HoverDetailsSurface.HasVisibleContent(RootGrid.XamlRoot));
            ApplyKeyboardEffect(effect);
            if ((effect & PopupPreviewEffect.Handled) != 0) { e.Handled = true; return; }
        }
        // Ordinary caret edits/Tab/typing return to normal search behavior.
        if (e.Key != VirtualKey.Enter) ResetKeyboardNavigation();
    }
    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Handled || ViewModel.IsEditorOpen || _isComposing) return;
        if (e.Key == VirtualKey.Escape && _contextFlyout is { } flyout) { e.Handled = true; flyout.Hide(); }
        else if (e.Key == VirtualKey.Escape)
        {
            e.Handled = true;
            // First Escape clears typed text (including a not-yet-applied debounced edit); the next closes.
            if (SearchBox.Text.Length > 0 && !ViewModel.IsEditorOpen) { SearchBox.Text = string.Empty; ApplyQuery(); }
            else ViewModel.Close();
        }
        else if (e.Key == VirtualKey.Application || (e.Key == VirtualKey.F10 && (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift) & CoreVirtualKeyStates.Down) != 0))
        {
            if (PopupList.SelectedItem is PopupItemViewModel selected && PopupList.ContainerFromItem(selected) is ListViewItem container)
            { e.Handled = true; ShowRowMenu(container, selected); }
        }
        else if (ReferenceEquals(FocusManager.GetFocusedElement(RootGrid.XamlRoot), SearchBox) && e.Key == VirtualKey.Enter && !ViewModel.IsConfirmingClipboardLoss) { e.Handled = true; _ = RunOperation(() => ViewModel.InsertSelectedAsync()); }
    }
    private void OnItemClick(object sender, ItemClickEventArgs e) { if (e.ClickedItem is PopupItemViewModel item) _ = RunOperation(() => ViewModel.ActivateAsync(item)); }
    private void OnContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs e)
    {
        Controls.HoverDetailsSurface.CancelRowHovers(RootGrid.XamlRoot);
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
        ResetKeyboardNavigation();
        if (!ViewModel.CanUseRowActions(item)) return;
        Controls.HoverDetailsSurface.CancelActive(RootGrid.XamlRoot);
        ViewModel.SelectedIndex = ViewModel.Items.IndexOf(item);
        var menu = new MenuFlyout { MenuFlyoutPresenterStyle = (Style)Application.Current.Resources["SolidMenuFlyoutPresenterStyle"] };
        FlyoutSurface.KeepSolid(menu);
        var copy = new MenuFlyoutItem { Text = UiText.RowActionCopy, IsEnabled = ViewModel.CanCopyRow(item) };
        bool CanInsert() => ViewModel.CanInsertFromMain && ViewModel.CanUseRowActions(item) && CanInsertNative?.Invoke() == true;
        var insert = new MenuFlyoutItem { Text = UiText.RowActionInsert, IsEnabled = CanInsert() };
        var edit = new MenuFlyoutItem { Text = UiText.RowActionEdit };
        var delete = new MenuFlyoutItem { Text = UiText.RowActionDelete };
        copy.Click += (_, _) => ViewModel.CopyRow(item);
        insert.Click += async (_, _) =>
        {
            if (CanInsert()) await RunOperation(() => ViewModel.InsertRowAsync(item));
        };
        void RefreshActions(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
        {
            copy.IsEnabled = ViewModel.CanCopyRow(item);
            insert.IsEnabled = CanInsert();
        }
        ViewModel.PropertyChanged += RefreshActions;
        edit.Click += (_, _) => ViewModel.EditRow(item);
        delete.Click += async (_, _) => await RunOperation(() => ViewModel.DeleteRowAsync(item));
        menu.Items.Add(copy); menu.Items.Add(insert); menu.Items.Add(edit); menu.Items.Add(delete);
        _contextFlyout = menu;
        menu.Closed += (_, _) =>
        {
            ViewModel.PropertyChanged -= RefreshActions;
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
    private static bool IsEmptySurface(object? source, UIElement root)
    {
        if (Controls.HoverDetailsSurface.Owns(source)) return false;
        for (var node = source as DependencyObject; node is not null && node != root; node = VisualTreeHelper.GetParent(node))
            if (node is TextBox or Microsoft.UI.Xaml.Controls.Primitives.ButtonBase or Microsoft.UI.Xaml.Controls.Primitives.SelectorItem or Microsoft.UI.Xaml.Controls.Primitives.RangeBase) return false;
        return true;
    }
    // Dragging any empty area moves the popup. The OS owns the move: the whole client area is a
    // Caption region (system drag, no per-move XAML work) and every interactive control is
    // carved out as Passthrough so clicks still reach it.
    private RectInt32[] _passthrough = [];
    private void UpdateDragRegions()
    {
        if (_closed || RootGrid.XamlRoot is null || RootGrid.ActualWidth <= 0) return;
        var scale = RootGrid.XamlRoot.RasterizationScale;
        var holes = new List<RectInt32>();
        void Collect(DependencyObject node)
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
            {
                var child = VisualTreeHelper.GetChild(node, i);
                if (child is FrameworkElement { Visibility: Visibility.Visible } fe)
                {
                    if (fe is TextBox or Microsoft.UI.Xaml.Controls.Primitives.ButtonBase or ListView)
                    {
                        if (fe.ActualWidth <= 0 || fe.ActualHeight <= 0) continue;
                        var b = fe.TransformToVisual(RootGrid.XamlRoot.Content).TransformBounds(new Windows.Foundation.Rect(0, 0, fe.ActualWidth, fe.ActualHeight));
                        holes.Add(new RectInt32((int)(b.X * scale), (int)(b.Y * scale), (int)Math.Ceiling(b.Width * scale), (int)Math.Ceiling(b.Height * scale)));
                        continue;
                    }
                    Collect(fe);
                }
                else if (child is not null) Collect(child);
            }
        }
        // An open editor owns the whole surface; leave it fully interactive.
        if (ViewModel.IsEditorOpen) holes.Add(new RectInt32(0, 0, (int)Math.Ceiling(RootGrid.XamlRoot.Size.Width * scale), (int)Math.Ceiling(RootGrid.XamlRoot.Size.Height * scale)));
        else Collect(RootGrid);
        if (Controls.HoverDetailsSurface.TryBounds(RootGrid.XamlRoot, out var hover))
            holes.Add(new RectInt32((int)(hover.X * scale), (int)(hover.Y * scale), (int)Math.Ceiling(hover.Width * scale), (int)Math.Ceiling(hover.Height * scale)));
        var rects = holes.ToArray();
        if (rects.AsSpan().SequenceEqual(_passthrough)) return;
        _passthrough = rects;
        var source = InputNonClientPointerSource.GetForWindowId(AppWindow.Id);
        source.SetRegionRects(NonClientRegionKind.Caption, [new RectInt32(0, 0, (int)Math.Ceiling(RootGrid.XamlRoot.Size.Width * scale), (int)Math.Ceiling(RootGrid.XamlRoot.Size.Height * scale))]);
        source.SetRegionRects(NonClientRegionKind.Passthrough, rects);
    }
    private void OnSurfacePointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!_visible || ViewModel.IsEditorOpen || _contextFlyout is not null || SortFlyout.IsOpen) return;
        // Observe handled events too (ScrollViewer consumes blank-list presses), but
        // never steal focus from actionable controls, row activation or scrollbars.
        if (!IsEmptySurface(e.OriginalSource, RootGrid)) return;
        Focus(ViewModel.IsConfirmingClipboardLoss ? PopupFocusTarget.Footer : PopupFocusTarget.Search);
    }
    private void OnSortClick(object sender, RoutedEventArgs e) => UpdateSortChecks();
    private void UpdateSortChecks()
    {
        SortLast.IsChecked = ViewModel.Sort == SortField.LastUsed; SortUsage.IsChecked = ViewModel.Sort == SortField.UsageCount;
        SortCreated.IsChecked = ViewModel.Sort == SortField.Created; SortUpdated.IsChecked = ViewModel.Sort == SortField.Updated;
    }
    private void OnSortLast(object sender, RoutedEventArgs e) => ViewModel.SetSort(SortField.LastUsed);
    private void OnSortUsage(object sender, RoutedEventArgs e) => ViewModel.SetSort(SortField.UsageCount);
    private void OnSortCreated(object sender, RoutedEventArgs e) => ViewModel.SetSort(SortField.Created);
    private void OnSortUpdated(object sender, RoutedEventArgs e) => ViewModel.SetSort(SortField.Updated);
    private void OnEditorSave(object sender, RoutedEventArgs e) => _ = RunOperation(() => ViewModel.SaveEditorAsync());
    private void OnEditorCancel(object sender, RoutedEventArgs e) => _ = RunOperation(() => ViewModel.CancelEditorAsync(false));
    private void OnEditorEscape(object? sender, EventArgs e) => _ = RunOperation(() => ViewModel.CancelEditorAsync(true));
    private void OnEditorPreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        static bool Down(VirtualKey key) => (InputKeyboardSource.GetKeyStateForCurrentThread(key) & CoreVirtualKeyStates.Down) != 0;
        if (e.Key == VirtualKey.Enter && Down(VirtualKey.Control) && !Down(VirtualKey.Shift) && !Down(VirtualKey.Menu))
        { e.Handled = true; _ = RunOperation(() => ViewModel.SaveEditorAsync()); }
    }
    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(PopupViewModel.Items) or nameof(PopupViewModel.Editor))
        {
            ResetKeyboardNavigation();
            Controls.HoverDetailsSurface.CancelActive(RootGrid.XamlRoot);
        }
        if (e.PropertyName == nameof(PopupViewModel.Items) && _searchStarted != 0) _awaitingLayout = true;
        if (e.PropertyName == nameof(PopupViewModel.SelectedIndex) && SelectedSnippet is null) ResetKeyboardNavigation();
        if (e.PropertyName == nameof(PopupViewModel.SelectedIndex) && ViewModel.SelectedIndex >= 0 && ViewModel.SelectedIndex < ViewModel.Items.Count)
        {
            var item = ViewModel.Items[ViewModel.SelectedIndex];
            PopupList.ScrollIntoView(item);
            if (_keyboardPreview.IsPreviewOpen && !_movingKeyboardSelection) ShowKeyboardPreview();
            var peer = FrameworkElementAutomationPeer.FromElement(SearchBox) ?? FrameworkElementAutomationPeer.CreatePeerForElement(SearchBox);
            peer?.RaiseNotificationEvent(AutomationNotificationKind.Other, AutomationNotificationProcessing.MostRecent, item.ToString(), "PopupSelection");
        }
    }
    private void OnSearchChanged(object sender, TextChangedEventArgs e)
    {
        if (_closed || ViewModel is null || !_visible) return;
        ResetKeyboardNavigation();
        // Debounce: restart the timer on every keystroke so fast typing searches once, not per character.
        if (SearchBox.Text == ViewModel.Query) { _searchTimer?.Stop(); return; }
        _searchTimer ??= CreateSearchTimer();
        _searchTimer.Stop(); _searchTimer.Start();
    }
    private const int SearchDebounceMs = 120;
    private DispatcherQueueTimer? _searchTimer;
    private DispatcherQueueTimer CreateSearchTimer()
    {
        var timer = DispatcherQueue.CreateTimer();
        timer.Interval = TimeSpan.FromMilliseconds(SearchDebounceMs); timer.IsRepeating = false;
        timer.Tick += (_, _) => ApplyQuery();
        return timer;
    }
    // Also called before Enter/arrow keys act, so they never see a stale result list.
    private void ApplyQuery()
    {
        _searchTimer?.Stop();
        if (_closed || ViewModel is null || !_visible || SearchBox.Text == ViewModel.Query) return;
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
