using System.Diagnostics;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using SmartSnippets.App.Services;
using SmartSnippets.Core.Logging;
using Windows.Graphics;
using Windows.System;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace SmartSnippets.App.Popup;

public sealed partial class PopupWindow : Window
{
    internal const string TestString = "Héllo – naïve 👋 世界\r\n\tline 2  (trailing spaces)  \r\nline 3";

    private const int LogicalWidth = 640;
    private const int LogicalHeight = 480;

    private readonly IInserter _inserter;
    private readonly HWND _hwnd;
    private ForegroundTargetInfo? _target;
    private long _hotkeyTimestamp;
    private bool _readyPending;
    private bool _inserting;

    internal PopupWindow(IInserter inserter)
    {
        _inserter = inserter;
        InitializeComponent();
        _hwnd = new HWND(WinRT.Interop.WindowNative.GetWindowHandle(this));

        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.SetBorderAndTitleBar(false, false);
            presenter.IsResizable = false;
            presenter.IsAlwaysOnTop = true;
            presenter.IsMinimizable = false;
            presenter.IsMaximizable = false;
        }

        AppWindow.IsShownInSwitchers = false;
        Activated += OnActivated;
        RootGrid.LayoutUpdated += OnLayoutUpdated;
        ResultsList.SelectedIndex = 0;
    }

    /// <summary>Realises the window off-screen once so the first real show is fast, then hides it again.</summary>
    internal void Prewarm()
    {
        AppWindow.MoveAndResize(new RectInt32(-32000, -32000, LogicalWidth, LogicalHeight));
        AppWindow.Show(false);
        AppWindow.Hide();
    }

    internal void ShowFor(ForegroundTargetInfo target, long hotkeyTimestamp)
    {
        _target = target;
        _hotkeyTimestamp = hotkeyTimestamp;
        _readyPending = true;

        WindowPlacement.CenterOn(AppWindow, target.Monitor, LogicalWidth, LogicalHeight);
        SearchBox.Text = string.Empty;
        ResultsList.SelectedIndex = 0;

        AppWindow.Show();
        Activate();
        PInvoke.SetForegroundWindow(_hwnd);
        SearchBox.Focus(FocusState.Programmatic);
    }

    internal void HidePopup() => AppWindow.Hide();

    private void OnActivated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState == WindowActivationState.Deactivated)
        {
            if (AppWindow.IsVisible)
            {
                AppWindow.Hide();
            }

            return;
        }

        SearchBox.Focus(FocusState.Programmatic);
        ReportReady();
    }

    private void OnLayoutUpdated(object? sender, object e) => ReportReady();

    private void ReportReady()
    {
        if (!_readyPending)
        {
            return;
        }

        _readyPending = false;
        var elapsed = Stopwatch.GetElapsedTime(_hotkeyTimestamp);
        AppLog.Info("popup_ready_ms", ((int)elapsed.TotalMilliseconds).ToString());
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape)
        {
            e.Handled = true;
            AppWindow.Hide();
        }
        else if (e.Key == VirtualKey.Enter)
        {
            e.Handled = true;
            _ = InvokeAsync();
        }
    }

    private void OnItemClick(object sender, ItemClickEventArgs e) => _ = InvokeAsync();

    private async Task InvokeAsync()
    {
        if (_inserting)
        {
            AppLog.Info("insert_ignored_duplicate");
            return;
        }

        _inserting = true;
        try
        {
            if (_target is not { } target)
            {
                return;
            }

            AppWindow.Hide();
            var result = await _inserter.InsertAsync(target, TestString);
            if (result.Outcome != InsertOutcome.Success)
            {
                ShowWarning(result.Message);
            }
        }
        catch (Exception ex)
        {
            AppLog.Error("insert_failed", ex);
        }
        finally
        {
            _inserting = false;
        }
    }

    private static void ShowWarning(string message)
    {
        PInvoke.MessageBox(
            HWND.Null,
            message,
            "SmartSnippets",
            MESSAGEBOX_STYLE.MB_OK | MESSAGEBOX_STYLE.MB_ICONWARNING | MESSAGEBOX_STYLE.MB_TOPMOST | MESSAGEBOX_STYLE.MB_SETFOREGROUND);
    }
}
