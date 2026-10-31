using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using SmartSnippets.Presentation.Services;

namespace SmartSnippets.UnoProbe;

/// <summary>Native companion alternative, driven by the real hover policy.
/// No pointer/keyboard synthesis, target, clipboard, store or shortcut access.</summary>
internal sealed class CompanionSurfaceProbe
{
    internal static int ExitCode { get; private set; } = 1;
    private readonly Application _app;
    private readonly Window _main;
    private readonly MainPage _mainPage;
    private readonly DispatcherQueue _queue;
    private readonly DispatcherQueueTimer _timer;
    private readonly List<DispatcherQueueTimer> _scheduled = [];
    private readonly HoverDetailsCoordinator _policy;
    private readonly long _deadline = Stopwatch.GetTimestamp() + 20 * Stopwatch.Frequency;
    private Window? _companion;
    private CompanionProbePage? _details;
    private ProbeWindowSurface? _mainSurface, _surface;
    private ProbeScreenRect _work, _placed;
    private string _title = "";
    private Func<bool>? _absent;
    private int _stage, _opens, _hides, _closed;
    private long _foregroundBefore, _handoffUntil;
    private bool _failed, _expectedClose;

    internal CompanionSurfaceProbe(Application app, Window main, MainPage page)
    {
        ExitCode = 1;
        _app = app; _main = main; _mainPage = page;
        _queue = DispatcherQueue.GetForCurrentThread();
        _policy = new HoverDetailsCoordinator(Schedule, Open, Hide);
        _timer = _queue.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(50);
        _timer.Tick += (_, _) => Guard(Tick);
        _main.Closed += (_, _) => MainClosed();
        _timer.Start();
        Console.WriteLine("ui_companion_started");
    }

    private void Schedule(TimeSpan delay, Action callback)
    {
        var timer = _queue.CreateTimer();
        timer.Interval = delay;
        timer.IsRepeating = false;
        timer.Tick += (_, _) => { timer.Stop(); _scheduled.Remove(timer); Guard(callback); };
        _scheduled.Add(timer);
        timer.Start();
    }

    private void Tick()
    {
        Require(_queue.HasThreadAccess && Stopwatch.GetTimestamp() < _deadline);
        switch (_stage)
        {
            case 0 when _mainPage.IsLoaded && _mainPage.ActualWidth > 0 && _mainPage.ActualHeight > 0:
                _mainSurface = ProbeWindowSurface.Bind(_main, _main.Title);
                _work = _mainSurface.WorkArea;
                Require(_work.Width >= 900 && _work.Height >= 480);
                // Only owned windows move; on Windows this is still the bottom
                // monitor of the leftmost column selected by initial parking.
                _mainSurface.SetFrame(new(_work.X + 16, _work.Bottom - 416, 520, 400));
                var owner = _mainSurface.Frame;
                var placement = HoverDetailsPlacement.PlaceBesideWindow(_work.X, _work.Y, _work.Right, _work.Bottom,
                    owner.X, owner.Right, owner.Y, 360, owner.Height);
                Require(placement.HasValue);
                var p = placement!.Value;
                _placed = new(p.X, p.Y, p.Width, p.Height);
                _stage = 1;
                _policy.OpenPinned(_mainPage);
                break;
            case 1 when _details is { IsReady: true }:
                RevealChecked();
                Console.WriteLine("ui_companion_external_geometry_ok binding=COMPILED foreground=UNCHANGED");
                _details.Scroll();
                _stage = 2;
                break;
            case 2 when _details is { IsReady: true, IsScrolled: true }:
                VerifyGeometry();
                Console.WriteLine("ui_companion_scroll_ok");
                _policy.Cancel();
                Require(!_policy.IsOpen && !_surface!.IsVisible && _hides == 1);
                Console.WriteLine("ui_companion_hidden");
                _stage = 3;
                _policy.Enter(_mainPage); // Real 500ms hover-policy scheduler; no fake pointer receipt.
                Require(!_policy.IsOpen && _opens == 1);
                break;
            case 3 when _opens == 2 && _details is { IsReady: true }:
                RevealChecked();
                Console.WriteLine("ui_companion_hover_policy_opened");
                _policy.OwnerExited();
                _policy.TargetExited();
                _policy.SurfaceEntered();
                Require(_policy.IsOpen && !_policy.IsHandingOff);
                _handoffUntil = Stopwatch.GetTimestamp() + Stopwatch.Frequency / 5;
                _stage = 4;
                break;
            case 4 when Stopwatch.GetTimestamp() >= _handoffUntil:
                Require(_policy.IsOpen && _surface!.IsVisible);
                _policy.SurfaceExited();
                Require(_policy.IsHandingOff);
                _stage = 5;
                break;
            case 5 when !_policy.IsOpen && _surface is { IsVisible: false }:
                Require(_hides == 2);
                Console.WriteLine("ui_companion_handoff_ok");
                _absent = _surface.CreateAbsenceCheck(_title);
                _surface = null; // Never retain a closed NSWindow pointer.
                _expectedClose = true;
                _stage = 6;
                _companion!.Close();
                break;
            case 6 when _closed == 1 && _absent!():
                Console.WriteLine("ui_companion_native_closed");
                _stage = 7;
                _main.Close();
                break;
        }
    }

    private void Open()
    {
        _foregroundBefore = ProbeWindowSurface.ForegroundOwnerToken;
        Require(_foregroundBefore != 0);
        if (_companion is null)
        {
            _title = "SS-Companion018-" + Guid.NewGuid().ToString("N");
            _details = new CompanionProbePage { RequestedTheme = _mainPage.ActualTheme };
            _companion = new Window { Title = _title, Content = _details };
            _companion.Closed += (_, _) =>
            {
                _surface = null;
                _closed++;
                if (!_expectedClose && !_failed) Fail(new InvalidOperationException());
            };
#if !WINDOWS
            _ = Uno.UI.Xaml.WindowHelper.GetNativeWindow(_companion);
            _companion.Title = _title;
#endif
            _surface = ProbeWindowSurface.Bind(_companion, _title);
        }
        _surface!.SetFrame(_placed);
        _surface.SetWarmupCloak(true);
        _details!.InvalidateMeasure();
        _surface.ShowWarmup(_companion);
        Require(!_surface.IsForeground && ProbeWindowSurface.ForegroundOwnerToken == _foregroundBefore);
        _opens++;
    }

    private void RevealChecked()
    {
        Require(_policy.IsOpen && _surface is { IsVisible: true, IsWarmupCloaked: true });
        _surface!.SetFrame(_placed);
        VerifyGeometry();
        _surface.SetWarmupCloak(false);
        Require(!_surface.IsWarmupCloaked && !_surface.IsForeground &&
            ProbeWindowSurface.ForegroundOwnerToken == _foregroundBefore);
    }

    private void VerifyGeometry()
    {
        var owner = _mainSurface!.Frame;
        var panel = _surface!.Frame;
        Require(_work.Contains(owner) && _work.Contains(panel) &&
            (panel.X >= owner.Right + 2 || panel.Right <= owner.X - 2));
    }

    private void Hide()
    {
        _surface?.Hide();
        _hides++;
    }

    private void MainClosed()
    {
        _mainSurface = null;
        if (_failed) return;
        Guard(() =>
        {
            Require(_stage == 7 && _closed == 1 && _absent!() && _opens == 2 && _hides == 2);
            StopTimers();
            _policy.Dispose();
            Console.WriteLine("ui_companion_complete managed_closed=2 native_absence=1 input=NONE clipboard=UNTOUCHED");
            Console.Out.Flush();
            ExitCode = 0;
            Require(_queue.TryEnqueue(() => Guard(_app.Exit)));
        });
    }

    private void Guard(Action action)
    {
        if (_failed) return;
        try { action(); }
        catch (Exception error) { Fail(error); }
    }

    private void StopTimers()
    {
        _timer.Stop();
        foreach (var timer in _scheduled) timer.Stop();
        _scheduled.Clear();
    }

    private void Fail(Exception error)
    {
        if (_failed) return;
        _failed = true;
        ExitCode = 1;
        StopTimers();
        Console.Error.WriteLine($"ui_companion_failed stage={_stage} kind={error.GetType().Name}");
        _surface = null;
        try { _companion?.Close(); } catch (Exception) { }
        try { _main.Close(); } catch (Exception) { }
        Console.Out.Flush(); Console.Error.Flush();
        Environment.Exit(1);
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException("Companion check failed.");
    }
}
