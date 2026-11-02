using SmartSnippets.Core.Logging;
using SmartSnippets.Presentation.Navigation;
using SmartSnippets.Presentation.Services;

namespace SmartSnippets.Uno;

/// <summary>Serializes redirects on the UI thread, including those arriving before composition finishes.</summary>
internal sealed class CandidateActivationQueue
{
    private readonly object _sync = new();
    private readonly Queue<LaunchOptions> _pending = new();
    private IUiDispatcher? _dispatcher;
    private Func<LaunchOptions, Task>? _apply;
    private bool _closed, _scheduled;
    internal void Enqueue(LaunchOptions options)
    {
        lock (_sync) { if (_closed) return; _pending.Enqueue(options); Schedule(); }
    }
    internal void Attach(IUiDispatcher dispatcher, Func<LaunchOptions, Task> apply)
    {
        lock (_sync) { if (_closed) return; _dispatcher = dispatcher; _apply = apply; Schedule(); }
    }
    private void Schedule()
    {
        if (_scheduled || _dispatcher is not { } dispatcher || _apply is null || _pending.Count == 0) return;
        _scheduled = true;
        dispatcher.Post(() => _ = DrainAsync());
    }
    private async Task DrainAsync()
    {
        while (true)
        {
            LaunchOptions options;
            Func<LaunchOptions, Task>? apply;
            lock (_sync)
            {
                if (_closed || !_pending.TryDequeue(out options!)) { _scheduled = false; return; }
                apply = _apply;
            }
            try { if (apply is not null) await apply(options); }
            catch (Exception ex) { AppLog.Error("candidate_activation_failed", ex); }
        }
    }
    internal void Close()
    {
        lock (_sync) { _closed = true; _pending.Clear(); _dispatcher = null; _apply = null; }
    }
}
