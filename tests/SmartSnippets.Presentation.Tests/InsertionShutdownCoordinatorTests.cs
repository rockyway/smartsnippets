using SmartSnippets.Presentation.Services;
using SmartSnippets.Presentation.Tests.Fakes;

namespace SmartSnippets.Presentation.Tests;

public sealed class InsertionShutdownCoordinatorTests
{
    private static readonly InsertionTarget Target = new(1, 2, 3, "target");

    [Fact]
    public async Task Shutdown_WaitsForPasteGracePeriod_AndBlocksAdmissionDuringFlush()
    {
        var calls = new List<string>();
        var store = new FakeSnippetStore();
        var id = store.SeedSnippet("body").Id;
        var probe = new FakeTargetProbe(calls);
        var dispatcher = new FakePasteDispatcher(calls);
        var grace = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var flush = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var service = new InsertionService(store, probe, dispatcher,
            delay: (duration, _) => duration == InsertionTimings.Default.RestoreDelay ? grace.Task : Task.CompletedTask);
        var coordinator = new InsertionShutdownCoordinator(service);
        var insert = coordinator.InsertAsync(Target, id);
        Assert.Contains("send", calls);
        Assert.DoesNotContain("restore", calls);
        Assert.True(coordinator.TryBeginShutdown());
        Assert.False(coordinator.TryBeginShutdown());

        var flushStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task ShutdownAsync()
        {
            await coordinator.WaitForIdleAsync();
            flushStarted.SetResult();
            await flush.Task;
        }
        var shutdown = ShutdownAsync();
        Assert.False(shutdown.IsCompleted);
        Assert.False(flushStarted.Task.IsCompleted);
        Assert.Equal(InsertionOutcome.Busy, (await coordinator.InsertAsync(Target, id)).Outcome);
        Assert.DoesNotContain("restore", calls);

        grace.SetResult();
        Assert.Equal(InsertionOutcome.Inserted, (await insert).Outcome);
        await flushStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Contains("restore", calls);
        Assert.False(shutdown.IsCompleted);
        Assert.Equal(InsertionOutcome.Busy, (await coordinator.InsertAsync(Target, id)).Outcome);
        Assert.Single(calls, c => c == "send");
        flush.SetResult(); await shutdown;

        // A cancelled exit or a busy clipboard flush deliberately reopens admission.
        coordinator.ResumeAdmission();
        Assert.Equal(InsertionOutcome.Inserted, (await coordinator.InsertAsync(Target, id)).Outcome);
        Assert.Equal(2, calls.Count(c => c == "send"));
    }

    [Fact]
    public async Task CancelledShutdown_ReopensAdmissionWhileExistingOperationStillDrains()
    {
        var fake = new FakeInsertionService { Pending = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        var coordinator = new InsertionShutdownCoordinator(fake);
        var first = coordinator.InsertAsync(Target, 1);
        Assert.True(coordinator.TryBeginShutdown());
        coordinator.ResumeAdmission();
        Assert.Equal(InsertionOutcome.Busy, (await coordinator.InsertAsync(Target, 2)).Outcome);
        fake.Pending.SetResult(new(InsertionOutcome.Inserted, "")); await first;
        fake.Pending = null;
        Assert.Equal(InsertionOutcome.Inserted, (await coordinator.InsertAsync(Target, 2)).Outcome);
    }
}
