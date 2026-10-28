using SmartSnippets.Presentation.Services;

namespace SmartSnippets.Presentation.Tests.Fakes;

internal sealed class FakeInsertionService : IInsertionService
{
    public List<(InsertionTarget Target, long Id, InsertionOptions Options)> Calls { get; } = [];
    public Queue<InsertionResult> Results { get; } = new();
    public TaskCompletionSource<InsertionResult>? Pending { get; set; }
    public bool ThrowNext { get; set; }
    public Action? OnCall { get; set; }

    public Task<InsertionResult> InsertAsync(InsertionTarget target, long snippetId, InsertionOptions options = default, CancellationToken ct = default)
    {
        Calls.Add((target, snippetId, options));
        OnCall?.Invoke();
        if (ThrowNext)
        {
            ThrowNext = false;
            throw new InvalidOperationException("Scripted failure");
        }

        return Pending?.Task ?? Task.FromResult(Results.Count > 0 ? Results.Dequeue() : new InsertionResult(InsertionOutcome.Inserted, string.Empty));
    }
}
