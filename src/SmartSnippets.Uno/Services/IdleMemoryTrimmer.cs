using System.Runtime;
using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
using SmartSnippets.Core.Logging;

namespace SmartSnippets.Uno.Services;

/// <summary>
/// Best-effort resident-memory reclamation after continuous tray idle, and once after startup or a main-window
/// monitor change settles. Never releases UI or popup state.
/// </summary>
internal sealed partial class IdleMemoryTrimmer : IDisposable
{
    private readonly DispatcherQueueTimer _timer;
    private readonly IdleTrimSchedule _schedule = new();
    private readonly Func<bool> _idle, _settled, _trim;
    private readonly Func<long> _clock;
    private long? _settleSince;
    private bool _disposed;

    internal IdleMemoryTrimmer(Func<bool> idle, Func<bool>? trim = null, Func<long>? clock = null, Func<bool>? settled = null)
    {
        _idle = idle;
        _settled = settled ?? idle;
        _trim = trim ?? TrimNative;
        _clock = clock ?? (() => Environment.TickCount64);
        _timer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        // Coarse polling: a tray app should not wake its UI thread often while idle.
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.IsRepeating = true;
        _timer.Tick += OnTick;
        _timer.Start();
    }
    private void OnTick(object? sender, object args) => Poll();
    /// <summary>
    /// Trim once more after the delay, even with the main window open: startup and a move to another monitor
    /// (possibly another graphics adapter) leave resident pages the visible window no longer touches.
    /// Each request restarts the delay.
    /// </summary>
    internal void RequestSettleTrim()
    {
        if (!_disposed) _settleSince = _clock();
    }
    /// <summary>Restart the delay of a pending settle trim, for example while the window is still being dragged.</summary>
    internal void PostponeSettleTrim()
    {
        if (!_disposed && _settleSince is not null) _settleSince = _clock();
    }
    internal void Poll()
    {
        if (_disposed) return;
        var now = _clock();
        if (_schedule.Poll(now, _idle())) { _settleSince = null; Trim(); return; }
        if (_settleSince is not { } since) return;
        // Busy work or a visible popup restarts the delay rather than dropping the request.
        if (!_settled()) _settleSince = now;
        else if (now - since >= IdleTrimSchedule.DelayMilliseconds) { _settleSince = null; Trim(); }
    }
    private void Trim()
    {
        try
        {
            if (!_trim()) AppLog.Info("idle_memory_trim", "result=unavailable");
        }
        catch (Exception)
        {
            // An optional memory optimization must never break show/recovery or disclose exceptions.
            AppLog.Info("idle_memory_trim", "result=unavailable");
        }
    }
    internal static bool TrimNative()
    {
        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        return SetWorkingSet(new nint(-1), nuint.MaxValue, nuint.MaxValue, 0) != 0;
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer.Stop();
        _timer.Tick -= OnTick;
    }
    [LibraryImport("kernel32.dll", EntryPoint = "SetProcessWorkingSetSizeEx", SetLastError = true)]
    private static partial int SetWorkingSet(nint process, nuint minimum, nuint maximum, uint flags);
}
