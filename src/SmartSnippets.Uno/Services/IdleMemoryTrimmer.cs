using System.Runtime;
using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
using SmartSnippets.Core.Logging;

namespace SmartSnippets.Uno.Services;

/// <summary>Best-effort resident-memory reclamation after continuous tray idle. Never releases UI or popup state.</summary>
internal sealed partial class IdleMemoryTrimmer : IDisposable
{
    private readonly DispatcherQueueTimer _timer;
    private readonly IdleTrimSchedule _schedule = new();
    private readonly Func<bool> _idle, _trim;
    private readonly Func<long> _clock;
    private bool _disposed;

    internal IdleMemoryTrimmer(Func<bool> idle, Func<bool>? trim = null, Func<long>? clock = null)
    {
        _idle = idle;
        _trim = trim ?? TrimNative;
        _clock = clock ?? (() => Environment.TickCount64);
        _timer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(100);
        _timer.IsRepeating = true;
        _timer.Tick += OnTick;
        _timer.Start();
    }
    private void OnTick(object? sender, object args) => Poll();
    internal void Poll()
    {
        if (_disposed || !_schedule.Poll(_clock(), _idle())) return;
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
