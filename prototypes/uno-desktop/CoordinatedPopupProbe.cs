using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using SmartSnippets.Presentation.Services;
using Windows.Graphics;

namespace SmartSnippets.UnoProbe;

/// <summary>Real coordinator plus native cloaked layout/focus callbacks. No insertion or clipboard.</summary>
internal sealed class CoordinatedPopupProbe
{
    internal static int ExitCode { get; private set; } = 1;
    private readonly Application _application;
    private readonly Window _main;
    private readonly MainPage _mainPage;
    private readonly DispatcherQueue _queue;
    private readonly DispatcherQueueTimer _timer;
    private readonly PopupShowCoordinator _shows;
    private Window? _popup;
    private PopupProbePage? _page;
    private ProbeWindowSurface? _surface;
    private ProbeWindowSurface? _mainSurface;
    private bool _prepared;
    private Func<bool>? _closedAbsent;
    private EventHandler<object>? _layout;
    private bool _allowReady, _layoutReady, _failed, _completingLayout, _expectPopupClose;
    private int _stage, _starts, _focuses, _closed;
    private long _deadline = Stopwatch.GetTimestamp() + 60 * Stopwatch.Frequency;

    public CoordinatedPopupProbe(Application application, Window main, MainPage mainPage)
    {
        ExitCode = 1;
        _application = application;
        _main = main;
        _mainPage = mainPage;
        _queue = DispatcherQueue.GetForCurrentThread();
        _shows = new PopupShowCoordinator(
            action => Require(_queue.TryEnqueue(() => Guard(action))),
            target => Guard(() => ApplyFocus(target)), StartWarmup, StopWarmup,
            () => Fail(new TimeoutException()));
        _timer = _queue.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(100);
        _timer.Tick += (_, _) => Guard(Tick);
        _main.Closed += (_, _) => Guard(MainClosed);
        Console.WriteLine("ui_coordinator_started");
        _timer.Start();
        // Console.In may implement ReadLineAsync synchronously; never let it
        // block the UI constructor before the initial window becomes visible.
        _ = Task.Run(AwaitPreparationAsync);
    }

    private async Task AwaitPreparationAsync()
    {
        try
        {
            var command = await Console.In.ReadLineAsync().ConfigureAwait(false);
            if (!_queue.TryEnqueue(() => Guard(() =>
            {
                Require(command == "probe_foreground_prepared");
                _prepared = true;
                _deadline = Stopwatch.GetTimestamp() + 15 * Stopwatch.Frequency; // Scene excludes bounded preparation.
            }))) Environment.Exit(1);
        }
        catch (Exception error)
        {
            if (!_queue.TryEnqueue(() => Fail(error))) Environment.Exit(1);
        }
    }

    private void Tick()
    {
        Require(_queue.HasThreadAccess);
        if (Stopwatch.GetTimestamp() > _deadline) throw new TimeoutException();
        switch (_stage)
        {
            case 0 when _prepared && _mainPage.IsLoaded:
                _mainSurface ??= ProbeWindowSurface.Bind(_main, _main.Title);
                if (!_mainSurface.IsForeground) break;
                // Intentionally hold the first readiness acknowledgement to
                // test hiding unfinished native warmup without revealing it.
                var foregroundBefore = ProbeWindowSurface.ForegroundOwnerToken;
                Require(foregroundBefore != 0);
                _shows.RequestShow(ShowReady);
                Require(_shows.IsWarming && _starts == 1 && _focuses == 0 && !_surface!.IsForeground);
                Require(ProbeWindowSurface.ForegroundOwnerToken == foregroundBefore);
                var firstGeneration = _shows.WarmupGeneration;
                _shows.Hide();
                Require(!_shows.IsWarming && !_surface!.IsVisible && _focuses == 0);
                Require(ProbeWindowSurface.ForegroundOwnerToken == foregroundBefore);
                Console.WriteLine("ui_coordinator_cold_after_cancel");
                _allowReady = true;
                _shows.RequestShow(ShowReady);
                Require(_shows.WarmupGeneration == firstGeneration + 2 && _starts == 2);
                _stage = 1;
                break;
            case 1 when _layoutReady && _focuses == 1 && _surface!.IsForeground && _page!.IsSearchFocused:
                Require(!_surface.IsWarmupCloaked);
                Console.WriteLine("ui_coordinator_native_focus_ok");
                _shows.Hide();
                _surface.Hide();
                Require(!_surface.IsVisible);
                _shows.Focus(PopupFocusTarget.Search); // Must not focus a hidden surface.
                Console.WriteLine("ui_coordinator_hidden");
                _stage = 2;
                break;
            case 2 when !_surface!.IsForeground:
                Require(_focuses == 1 && !_surface.IsVisible);
                _shows.RequestShow(ShowReady);
                Require(_starts == 2); // Ready reopen must not rerun native warmup.
                _stage = 3;
                break;
            case 3 when _focuses == 2 && _surface!.IsForeground && _page!.IsSearchFocused:
                Console.WriteLine("ui_coordinator_reopen_focus_ok");
                _shows.Close();
                _closedAbsent = _surface.CreateAbsenceCheck(_popup!.Title);
                _surface = null;
                _expectPopupClose = true;
                _popup.Close();
                _stage = 4;
                break;
            case 4 when _closed == 1 && _closedAbsent!():
                _stage = 5;
                _main.Close();
                break;
        }
    }

    private void StartWarmup()
    {
        _starts++;
        if (_popup is null)
        {
            _page = new PopupProbePage();
            var title = "SS-Probe018-Warmup-" + Guid.NewGuid().ToString("N");
            _popup = new Window { Content = _page, Title = title };
            _popup.Closed += (_, _) => Guard(() =>
            {
                _surface = null;
                _closed++;
                Require(_expectPopupClose);
            });
            _popup.AppWindow.Resize(new SizeInt32 { Width = 520, Height = 400 });
#if WINDOWS
            ProbeWindowPlacement.Park(_popup, 520, 400, secondary: true);
#else
            // Force/observe native creation without revealing an unfinished surface.
            _ = Uno.UI.Xaml.WindowHelper.GetNativeWindow(_popup);
#endif
            _popup.Title = title;
            _surface = ProbeWindowSurface.Bind(_popup, title);
        }
        var generation = _shows.WarmupGeneration;
        _surface!.SetWarmupCloak(true);
        _layout = (_, _) => Guard(() =>
        {
            if (!_allowReady || _completingLayout || !_shows.IsWarming || generation != _shows.WarmupGeneration ||
                _page is not { IsLoaded: true, ActualWidth: > 0, ActualHeight: > 0, HasRealizedRow: true }) return;
            Require(_surface.IsWarmupCloaked);
            _completingLayout = true;
            _page.VerifyCompiledRows();
            _layoutReady = true;
            Console.WriteLine("ui_coordinator_native_layout_ready");
            _shows.CompleteWarmup(generation);
        });
        Console.WriteLine("ui_coordinator_warmup_started");
        _page!.LayoutUpdated += _layout;
        _page.InvalidateMeasure();
        _surface.ShowWarmup(_popup!); // Warmup must not activate or take foreground.
    }

    private void StopWarmup()
    {
        if (_layout is not null) _page!.LayoutUpdated -= _layout;
        _layout = null;
        _surface!.Hide(); // Hide before removing native cloak.
        _surface.SetWarmupCloak(false);
    }

    private PopupShowResult ShowReady()
    {
        Require(_layoutReady && !_shows.IsWarming && !_surface!.IsWarmupCloaked);
        _surface!.Show(_popup!);
        Console.WriteLine("ui_coordinator_ready_show");
        return new(true, PopupFocusTarget.Search);
    }

    private void ApplyFocus(PopupFocusTarget target)
    {
        Require(target == PopupFocusTarget.Search && _layoutReady && _page!.FocusSearch());
        _focuses++;
        Console.WriteLine("ui_coordinator_focus_applied");
    }

    private void MainClosed()
    {
        Require(_stage == 5 && _closed == 1 && _closedAbsent!());
        _timer.Stop();
        Console.WriteLine("ui_coordinator_complete native_focus=2 warmup_starts=2 clipboard=UNTOUCHED keyboard=NONE");
        Console.Out.Flush();
        ExitCode = 0;
        Require(_queue.TryEnqueue(() => Guard(() => _application.Exit())));
    }

    private void Guard(Action action)
    {
        if (_failed) return;
        try { action(); }
        catch (Exception error) { Fail(error); }
    }

    private void Fail(Exception error)
    {
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
        Console.Error.WriteLine($"ui_coordinator_failed stage={_stage} kind={error.GetType().Name} native_focus={nativeFocus} logical_focus={logicalFocus}");
        try { _popup?.Close(); } catch (Exception) { }
        try { _main.Close(); } catch (Exception) { }
        Console.Out.Flush();
        Console.Error.Flush();
        Environment.Exit(1);
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException("Coordinator check failed.");
    }
}
