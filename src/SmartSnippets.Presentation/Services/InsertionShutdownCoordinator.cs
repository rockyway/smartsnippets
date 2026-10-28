using SmartSnippets.Presentation.Text;

namespace SmartSnippets.Presentation.Services;

/// <summary>Closes admission and drains the complete insertion before clipboard recovery and shutdown.</summary>
public sealed class InsertionShutdownCoordinator(IInsertionService insertion) : IInsertionService
{
    private readonly object _gate = new();
    private bool _stopping;
    private TaskCompletionSource? _active;

    public bool TryBeginShutdown()
    {
        lock (_gate)
        {
            if (_stopping) return false;
            _stopping = true;
            return true;
        }
    }

    public Task WaitForIdleAsync()
    {
        lock (_gate)
        {
            if (!_stopping) throw new InvalidOperationException("Admission must be closed before waiting for idle.");
            return _active?.Task ?? Task.CompletedTask;
        }
    }

    public void ResumeAdmission()
    {
        lock (_gate) _stopping = false;
    }

    public async Task<InsertionResult> InsertAsync(InsertionTarget target, long snippetId, InsertionOptions options = default, CancellationToken ct = default)
    {
        TaskCompletionSource active;
        lock (_gate)
        {
            if (_stopping || _active is not null)
                return new(InsertionOutcome.Busy, UiText.InsertionMessage(InsertionOutcome.Busy, target.DisplayName, false));
            active = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _active = active;
        }

        try { return await insertion.InsertAsync(target, snippetId, options, ct); }
        finally
        {
            lock (_gate)
            {
                _active = null;
                active.TrySetResult();
            }
        }
    }
}
