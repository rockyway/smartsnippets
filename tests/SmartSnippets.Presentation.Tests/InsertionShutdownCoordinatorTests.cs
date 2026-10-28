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
    public async Task ForwardsCompleteRequestIncludingConsentAndCancellation()
    {
        var inner = new RecordingInsertionService();
        var expected = new InsertionResult(InsertionOutcome.CopiedForManualPaste, "result");
        inner.Next = (_, _, _, _) => Task.FromResult(expected);
        var coordinator = new InsertionShutdownCoordinator(inner);
        using var cancellation = new CancellationTokenSource();
        var target = new InsertionTarget((nint)987, 321, (nint)654, "distinct target");
        var options = new InsertionOptions(AllowClipboardLoss: true);

        var result = await coordinator.InsertAsync(target, 12345, options, cancellation.Token);

        var request = Assert.Single(inner.Calls);
        Assert.Equal(target, request.Target); Assert.Equal(12345, request.Id);
        Assert.Equal(options, request.Options); Assert.True(request.Options.AllowClipboardLoss);
        Assert.True(request.Token.CanBeCanceled); Assert.Equal(cancellation.Token, request.Token);
        Assert.Same(expected, result);
    }

    [Theory]
    [InlineData("sync")]
    [InlineData("fault")]
    [InlineData("cancel")]
    public async Task FailedInnerCall_ReleasesActiveTrackingAndAllowsNextCall(string mode)
    {
        using var cancellation = new CancellationTokenSource();
        var inner = new RecordingInsertionService();
        inner.Next = (_, _, _, ct) => mode switch
        {
            "sync" => throw new InvalidOperationException("sync"),
            "fault" => Task.FromException<InsertionResult>(new InvalidOperationException("fault")),
            _ => Task.FromCanceled<InsertionResult>(ct),
        };
        if (mode == "cancel") cancellation.Cancel();
        var coordinator = new InsertionShutdownCoordinator(inner);
        var failed = coordinator.InsertAsync(Target, 7, new(true), cancellation.Token);
        if (mode == "cancel") await Assert.ThrowsAnyAsync<OperationCanceledException>(() => failed);
        else await Assert.ThrowsAsync<InvalidOperationException>(() => failed);

        Assert.True(coordinator.TryBeginShutdown());
        await coordinator.WaitForIdleAsync().WaitAsync(TimeSpan.FromSeconds(5));
        coordinator.ResumeAdmission();
        inner.Next = (_, _, _, _) => Task.FromResult(new InsertionResult(InsertionOutcome.Inserted, ""));
        Assert.Equal(InsertionOutcome.Inserted, (await coordinator.InsertAsync(Target, 8)).Outcome);
        Assert.Equal(2, inner.Calls.Count);
    }

    private sealed class RecordingInsertionService : IInsertionService
    {
        public List<(InsertionTarget Target, long Id, InsertionOptions Options, CancellationToken Token)> Calls { get; } = [];
        public Func<InsertionTarget, long, InsertionOptions, CancellationToken, Task<InsertionResult>> Next { get; set; } =
            (_, _, _, _) => Task.FromResult(new InsertionResult(InsertionOutcome.Inserted, ""));

        public Task<InsertionResult> InsertAsync(InsertionTarget target, long snippetId, InsertionOptions options = default, CancellationToken ct = default)
        {
            Calls.Add((target, snippetId, options, ct));
            return Next(target, snippetId, options, ct);
        }
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
