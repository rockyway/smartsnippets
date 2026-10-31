using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using SmartSnippets.Presentation.Services;
using Windows.Graphics;

namespace SmartSnippets.UnoProbe;

/// <summary>Real native Enter → routed KeyDown → production preview VM → owned hide.</summary>
internal sealed class PreviewProbe
{
    internal static int ExitCode { get; private set; } = 1;
    private readonly bool _rowMode;
    private readonly bool _nativeRow;
    private readonly Application _app;
    private readonly Window _main;
    private readonly MainPage _mainPage;
    private readonly DispatcherQueue _queue;
    private readonly DispatcherQueueTimer _timer;
    private readonly PopupShowCoordinator _shows;
    private PreviewModelSession? _session;
    private PreviewProbePage? _page;
    private Window? _popup;
    private ProbeWindowSurface? _surface;
    private Func<bool>? _absence;
    private readonly EventHandler<object> _layout;
    private bool _prepared, _failed, _expectedClose, _closed, _shown;
    private int _stage, _focuses;
    private long _deadline = Stopwatch.GetTimestamp() + 60 * Stopwatch.Frequency;
    private long _clipboardBefore;

    public PreviewProbe(Application app, Window main, MainPage mainPage, bool rowMode = false, bool nativeRow = false)
    {
        ExitCode = 1;
        _rowMode = rowMode;
        _nativeRow = nativeRow;
        if (nativeRow && (!rowMode || !OperatingSystem.IsWindows())) throw new PlatformNotSupportedException();
        _prepared = rowMode;
        _app = app; _main = main; _mainPage = mainPage;
        _queue = DispatcherQueue.GetForCurrentThread();
        _timer = _queue.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(100);
        _timer.Tick += (_, _) => Guard(Tick);
        _main.Closed += (_, _) => Guard(MainClosed);
        _shows = new PopupShowCoordinator(action => Require(_queue.TryEnqueue(() => Guard(action))),
            target => Guard(() =>
            {
                Require(target == PopupFocusTarget.Search && _page!.FocusSearch());
                _focuses++;
                Console.WriteLine("ui_preview_focus_applied");
            }), StartWarmup, StopWarmup, () => Fail(new TimeoutException()));
        _layout = (_, _) => Guard(() =>
        {
            if (!_shows.IsWarming || _page is not { IsReady: true }) return;
            Require(_surface!.IsWarmupCloaked);
            Console.WriteLine("ui_preview_layout_ready");
            _shows.CompleteWarmup(_shows.WarmupGeneration);
        });
        Console.WriteLine(nativeRow ? "ui_preview_row_native_started" : rowMode ? "ui_preview_row_started" : "ui_preview_started");
        _timer.Start();
        if (!rowMode) _ = Task.Run(AwaitPreparationAsync);
    }

    private async Task AwaitPreparationAsync()
    {
        try
        {
            var line = await Console.In.ReadLineAsync().ConfigureAwait(false);
            if (!_queue.TryEnqueue(() => Guard(() =>
            {
                Require(line == "probe_foreground_prepared");
                _prepared = true;
                _deadline = Stopwatch.GetTimestamp() + 15 * Stopwatch.Frequency;
            }))) Environment.Exit(1);
        }
        catch (Exception error) { if (!_queue.TryEnqueue(() => Fail(error))) Environment.Exit(1); }
    }

    private void Tick()
    {
        Require(_queue.HasThreadAccess);
        if (Stopwatch.GetTimestamp() > _deadline) throw new TimeoutException();
        switch (_stage)
        {
            case 0 when _prepared && _mainPage.IsLoaded:
                if (!_rowMode && !ProbeWindowSurface.Bind(_main, _main.Title).IsForeground) break;
                _stage = 1;
                _shows.RequestShow(() =>
                {
                    Require(_page!.IsReady && !_surface!.IsWarmupCloaked);
                    if (_rowMode) _surface!.ShowWarmup(_popup!);
                    else _surface!.Show(_popup!);
                    _shown = true;
                    Console.WriteLine("ui_preview_ready_show");
                    return new(true, _rowMode ? null : PopupFocusTarget.Search);
                });
                break;
            case 1 when _rowMode && _shown && !_shows.IsWarming && _surface!.IsVisible && !_surface.IsWarmupCloaked && _page!.IsReady:
                Require(_focuses == 0 && !_surface!.IsForeground);
                _clipboardBefore = OwnedPreviewEnter.ClipboardEpoch;
                _stage = 2;
                if (_nativeRow)
                {
#if WINDOWS
                    Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(
                        _page.SelectedRow, "OwnedPreviewRow");
                    Console.WriteLine($"ui_preview_row_native_ready hwnd={WinRT.Interop.WindowNative.GetWindowHandle(_popup!)}");
                    Console.Out.Flush();
#endif
                }
                else _ = _page.ActivateSelectedRowAsync();
                break;
            case 1 when !_rowMode && _focuses == 1 && _surface!.IsForeground && _page!.IsSearchFocused:
                _ = OwnedPreviewEnter.TryPost(_surface, () => _page!.IsSearchFocused, () =>
                {
                    _clipboardBefore = OwnedPreviewEnter.ClipboardEpoch;
                    _stage = 2;
                    Console.WriteLine("ui_preview_native_enter_requested");
                });
                break;
            case 2 when (_rowMode ? _page!.RowCompleted : _page!.EnterCompleted) && _session!.Hides == 1:
                Require((_rowMode ? _page!.RowEvents == 1 && _page.EnterEvents == 0 : _page!.EnterEvents == 1) && !_surface!.IsVisible);
                Require(_page!.ItemClickEvents == (_nativeRow ? 1 : 0));
                Require(OwnedPreviewEnter.ClipboardEpoch == _clipboardBefore);
                _session!.VerifyUnchanged();
                Console.WriteLine("ui_preview_no_dispatch_or_usage clipboard_epoch=UNCHANGED forbidden_calls=0");
                _shows.Close();
                _absence = _surface!.CreateAbsenceCheck(_popup!.Title);
                _surface = null;
                _expectedClose = true;
                _stage = 3;
                _popup.Close();
                break;
            case 3 when _closed && _absence!():
                _session!.Dispose();
                Console.WriteLine("ui_preview_profile_removed");
                _stage = 4;
                _main.Close();
                break;
        }
    }

    private void StartWarmup()
    {
        // Defer native popup construction until main is activated and verified
        // foreground. The macOS backend assigns its InitialWindow on creation.
        if (_popup is null)
        {
            _session = new PreviewModelSession(HidePreview, action => Require(_queue.TryEnqueue(() => Guard(action))));
            _page = new PreviewProbePage(_session.Model, Fail, () => _session.Hides == 1 && _surface is { IsVisible: false });
            var title = "SS-Probe018-Preview-" + Guid.NewGuid().ToString("N");
            _popup = new Window { Content = _page, Title = title };
            _popup.Closed += (_, _) => Guard(() => { _surface = null; Require(_expectedClose); _closed = true; });
            _popup.AppWindow.Resize(new SizeInt32 { Width = 520, Height = 400 });
#if WINDOWS
            ProbeWindowPlacement.Park(_popup, 520, 400, secondary: true);
#else
            _ = Uno.UI.Xaml.WindowHelper.GetNativeWindow(_popup);
#endif
            _popup.Title = title;
            _surface = ProbeWindowSurface.Bind(_popup, title);
        }
        _surface!.SetWarmupCloak(true);
        _page!.LayoutUpdated += _layout;
        _page.InvalidateMeasure();
        _surface.ShowWarmup(_popup);
        Require(!_surface.IsForeground);
    }

    private void StopWarmup()
    {
        if (_page is not null) _page.LayoutUpdated -= _layout;
        _surface?.Hide();
        _surface?.SetWarmupCloak(false);
    }

    private void HidePreview()
    {
        Require(_stage == 2 && (_rowMode ? _page!.RowEvents == 1 : _page!.EnterEvents == 1));
        _shows.Hide();
        _surface!.Hide();
    }

    private void MainClosed()
    {
        Require(_stage == 4 && _closed && _absence!());
        _timer.Stop();
        ExitCode = 0;
        Console.WriteLine(_nativeRow ? "ui_preview_row_native_complete item_click=1 insertion_calls=0 clipboard_writes=0 usage=0" : _rowMode ? "ui_preview_row_complete shared_handler=1 native_pointer=NOT_TESTED uia=NOT_TESTED input=NONE insertion_calls=0 clipboard_writes=0 usage=0" : "ui_preview_complete native_enter=1 insertion_calls=0 clipboard_writes=0 usage=0");
        Console.Out.Flush();
        Require(_queue.TryEnqueue(() => Guard(() => _app.Exit())));
    }

    private void Guard(Action action)
    {
        if (_failed) return;
        try { action(); } catch (Exception error) { Fail(error); }
    }

    private void Fail(Exception error)
    {
        if (_failed) return;
        _failed = true;
        ExitCode = 1;
        _timer.Stop();
        var nativeFocus = -1;
        var logicalFocus = -1;
        try
        {
            if (_surface is not null) nativeFocus = _surface.IsForeground ? 1 : 0;
            if (_page is not null) logicalFocus = _page.IsSearchFocused ? 1 : 0;
        }
        catch (Exception) { }
        Console.Error.WriteLine($"ui_preview_failed stage={_stage} kind={error.GetType().Name} native_focus={nativeFocus} logical_focus={logicalFocus} input_guard={OwnedPreviewEnter.GuardState}");
        // The synthetic profile remains private on failure; no operator state
        // requires restoration because the only native key is unmodified Enter.
        try { _popup?.Close(); } catch (Exception) { }
        try { _main.Close(); } catch (Exception) { }
        Console.Error.Flush();
        Environment.Exit(1);
    }

    private static void Require(bool value) { if (!value) throw new InvalidOperationException("Preview host assertion failed."); }
}
