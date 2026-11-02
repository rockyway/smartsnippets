using System.Collections.Concurrent;

namespace SmartSnippets.Uno.Tests.Composition;

/// <summary>Preserves the real store's UI-thread ownership across suspended native-boundary calls.</summary>
internal sealed class HostThread(BlockingCollection<(SendOrPostCallback Callback, object? State)> queue) : SynchronizationContext
{
    public override void Post(SendOrPostCallback callback, object? state) => queue.Add((callback, state));
    internal static async Task RunAsync(Func<Task> action)
    {
        var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                using var work = new BlockingCollection<(SendOrPostCallback, object?)>();
                SetSynchronizationContext(new HostThread(work));
                var task = action();
                _ = task.ContinueWith(_ => work.CompleteAdding(), TaskScheduler.Default);
                foreach (var (callback, state) in work.GetConsumingEnumerable()) callback(state);
                task.GetAwaiter().GetResult(); finished.SetResult();
            }
            catch (Exception ex) { finished.SetException(ex); }
        }) { IsBackground = true };
        thread.Start();
        await finished.Task.WaitAsync(TimeSpan.FromSeconds(30));
        thread.Join();
    }
}
