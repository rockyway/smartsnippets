using System.Collections.Concurrent;

namespace SmartSnippets.Presentation.Tests.Fakes;

internal sealed class QueueSynchronizationContext(BlockingCollection<(SendOrPostCallback Callback, object? State)> queue) : SynchronizationContext
{
    public override void Post(SendOrPostCallback callback, object? state) => queue.Add((callback, state));
    internal static async Task RunOnOwnerThread(Func<Task> action)
    {
        var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                using var queue = new BlockingCollection<(SendOrPostCallback, object?)>();
                SynchronizationContext.SetSynchronizationContext(new QueueSynchronizationContext(queue));
                var task = action();
                _ = task.ContinueWith(_ => queue.CompleteAdding(), TaskScheduler.Default);
                foreach (var (callback, state) in queue.GetConsumingEnumerable()) callback(state);
                task.GetAwaiter().GetResult(); finished.SetResult();
            }
            catch (Exception ex) { finished.SetException(ex); }
        }) { IsBackground = true };
        thread.Start();
        await finished.Task.WaitAsync(TimeSpan.FromSeconds(60)); thread.Join();
    }
}
