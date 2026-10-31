using System.Collections.Concurrent;

namespace SmartSnippets.WindowsInsertion018;

/// <summary>Keeps the actual single-threaded Core store on its creating STA.</summary>
internal sealed class StaTaskLoop : SynchronizationContext
{
    private readonly BlockingCollection<(SendOrPostCallback Callback, object? State)> queue = new();
    public override void Post(SendOrPostCallback callback, object? state)
    {
        try { queue.Add((callback, state)); }
        catch (InvalidOperationException) { } // Completed/disposed loops cannot accept continuations.
    }
    internal static int Run(Func<Task<int>> body)
    {
        var previous = Current;
        var context = new StaTaskLoop();
        SetSynchronizationContext(context);
        try
        {
            var task = body();
            _ = task.ContinueWith(_ => context.queue.CompleteAdding(), CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            foreach (var item in context.queue.GetConsumingEnumerable()) item.Callback(item.State);
            return task.GetAwaiter().GetResult();
        }
        finally { SetSynchronizationContext(previous); context.queue.Dispose(); }
    }
}
