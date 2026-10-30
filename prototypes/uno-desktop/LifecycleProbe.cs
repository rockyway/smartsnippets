using System;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace SmartSnippets.UnoProbe;

/// <summary>Opt-in real-host fixture; no target, clipboard, input or shortcut APIs.
/// Not warmup/focus/preview/external-hover acceptance.</summary>
internal sealed class LifecycleProbe
{
    internal static int ExitCode { get; private set; } = 1;
    private readonly Application _application;
    private readonly Window _main;
    private readonly MainPage _page;
    private readonly DispatcherQueue _queue;
    private readonly DispatcherQueueTimer _timer;
    private Window? _popup;
    private PopupProbePage? _popupPage;
    private ProbeWindowSurface? _surface;
    private Func<bool>? _closedSurfaceAbsent;
    private string _popupTitle = "";
    private int _stage;
    private int _closed;
    private bool _failed;
    private readonly DateTimeOffset _deadline = DateTimeOffset.UtcNow.AddSeconds(20);

    public LifecycleProbe(Application application, Window main, MainPage page)
    {
        ExitCode = 1;
        _application = application;
        _main = main;
        _page = page;
        _queue = DispatcherQueue.GetForCurrentThread();
        _timer = _queue.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(100);
        _timer.Tick += Tick;
        _main.Closed += (_, _) => MainClosed();
        _timer.Start();
        Console.WriteLine("ui_lifecycle_started");
    }

    private void Tick(DispatcherQueueTimer sender, object args)
    {
        try
        {
            Require(_queue.HasThreadAccess);
            if (DateTimeOffset.UtcNow > _deadline) throw new TimeoutException();
            switch (_stage)
            {
                case 0 when _page.IsLoaded && _page.ActualWidth > 0 && _page.ActualHeight > 0:
                    _page.VerifyLiveCompiledBindings();
                    Console.WriteLine("ui_main_compiled_bindings_ok");
                    var id = Guid.NewGuid().ToString("N");
                    Require(DataProbe.Run(["--probe-data", "seed", id]) == 0);
                    Require(DataProbe.Run(["--probe-data", "verify", id]) == 0);
                    Require(_queue.HasThreadAccess);
                    Console.WriteLine("ui_data_serialized_ok");
                    _stage = 1;
                    CreatePopup();
                    break;
                case 1 when PopupReady():
                    _surface = ProbeWindowSurface.Bind(_popup!, _popupTitle);
                    Require(_surface.IsVisible);
                    _popupPage!.VerifyCompiledRows();
                    Console.WriteLine("ui_popup_compiled_rows_ok");
                    _stage = 2;
                    break;
                case 2 when _popupPage!.ActualTheme == _popupPage.TargetTheme:
                    _surface!.Hide();
                    Require(!_surface.IsVisible);
                    Console.WriteLine("ui_popup_hidden");
                    _stage = 3;
                    break;
                case 3:
                    Require(!_surface!.IsVisible);
                    _surface.Show(_popup!);
                    _stage = 4;
                    break;
                case 4 when _surface!.IsVisible && PopupReady():
                    Console.WriteLine("ui_popup_reopened");
                    _stage = 5;
                    ClosePopup();
                    break;
                case 5 when _closed == 1 && _closedSurfaceAbsent!():
                    CreatePopup();
                    _stage = 6;
                    break;
                case 6 when PopupReady():
                    _surface = ProbeWindowSurface.Bind(_popup!, _popupTitle);
                    Require(_surface.IsVisible);
                    _popupPage!.VerifyCompiledRows();
                    _stage = 7;
                    ClosePopup();
                    break;
                case 7 when _closed == 2 && _closedSurfaceAbsent!():
                    Console.WriteLine("ui_popup_recreated");
                    _stage = 8;
                    // Timer stays armed until the actual main Closed callback.
                    _main.Close();
                    break;
            }
        }
        catch (Exception error) { Fail(error); }
    }

    private void MainClosed()
    {
        if (_failed) return;
        try
        {
            Require(_stage == 8 && _closed == 2 && _closedSurfaceAbsent!());
            _timer.Stop();
            Console.WriteLine("ui_lifecycle_complete managed_closed=3 native_popup_absence=2 store_thread=UI input=NONE");
            Console.Out.Flush();
            ExitCode = 0;
            Require(_queue.TryEnqueue(() =>
            {
                try { _application.Exit(); }
                catch (Exception error) { Fail(error); }
            }));
        }
        catch (Exception error) { Fail(error); }
    }

    private void Fail(Exception error)
    {
        _failed = true;
        ExitCode = 1;
        _timer.Stop();
        var diagnostics = "readiness=-1";
        try
        {
            if (_popupPage is { } page)
                diagnostics = $"readiness={page.ReadinessFlags} panels={page.PanelChildCount} rows={page.VisualRowCount} texts={page.VisualTextCount}";
        }
        catch (Exception) { } // Diagnostic reads of a closed peer cannot block cleanup.
        Console.Error.WriteLine($"ui_lifecycle_failed stage={_stage} kind={error.GetType().Name} {diagnostics}");
        // Never let a cleanup exception escape Uno's native dispatcher, which
        // otherwise prints a raw exception. Exit(1) is failure cleanup only:
        // macOS Application.Exit uses NSApplication terminate and exit(0).
        try { _popup?.Close(); } catch (Exception) { }
        try { _main.Close(); } catch (Exception) { }
        Console.Out.Flush();
        Console.Error.Flush();
        Environment.Exit(1);
    }

    private void ClosePopup()
    {
        _closedSurfaceAbsent = _surface!.CreateAbsenceCheck(_popupTitle);
        _surface = null; // Do not retain/dereference a closed NSWindow pointer.
        _popup!.Close();
    }

    private void CreatePopup()
    {
        _popupPage = new PopupProbePage();
        _popupTitle = "SS-Probe018-" + Guid.NewGuid().ToString("N");
        _popup = new Window { Title = _popupTitle, Content = _popupPage };
        _popup.Closed += (_, _) => _closed++;
        _popup.AppWindow.Resize(new SizeInt32 { Width = 520, Height = 400 });
#if WINDOWS
        ProbeWindowPlacement.Park(_popup, 520, 400, secondary: true);
#endif
        _popup.Activate();
        _popup.Title = _popupTitle; // Reapply after lazy native window creation.
    }

    private bool PopupReady() => _popupPage is
        { IsLoaded: true, ActualWidth: > 0, ActualHeight: > 0, HasRealizedRow: true };
    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException("UI lifecycle check failed.");
    }
}
