using SmartSnippets.Presentation.Services;

namespace SmartSnippets.TargetIdentity018;

// Shared internal boundary for the trusted direct-child factory and its probe.
// The concrete lease retains Process/OS identity and pinned image provenance;
// a bare PID or caller-supplied bool is never accepted by the command-line host.
internal interface IOwnedFixtureLease
{
    uint ExpectedPid { get; }
    bool IsAliveAndPinned();
}

internal interface IExactWindowIdentity : IDisposable
{
    nint Capture(string fixtureWindow);
    bool Exists(nint identity);
    uint ProcessId(nint identity);
    bool IsForeground(nint identity);
}

/// <summary>Read-only probe. Tokens are local registry keys, never macOS window numbers.</summary>
internal sealed class OwnedTargetProbe(IOwnedFixtureLease lease, IExactWindowIdentity native) : ITargetProbe, IDisposable
{
    private readonly HashSet<nint> captured = [];
    private bool disposed;

    internal InsertionTarget Capture(string fixtureWindow)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (fixtureWindow is not ("a" or "b") || !lease.IsAliveAndPinned() ||
            lease.ExpectedPid == 0 || lease.ExpectedPid == (uint)Environment.ProcessId)
            throw new InvalidOperationException("Owned fixture unavailable.");
        var token = native.Capture(fixtureWindow);
        if (token == 0 || native.ProcessId(token) != lease.ExpectedPid || !native.Exists(token))
            throw new InvalidOperationException("Owned window unavailable.");
        captured.Add(token);
        return new(token, lease.ExpectedPid, 0, "");
    }

    public TargetCheck Check(InsertionTarget target)
    {
        if (disposed || !captured.Contains(target.WindowHandle) || !native.Exists(target.WindowHandle))
            return TargetCheck.Closed;
        var pid = native.ProcessId(target.WindowHandle);
        if (pid == 0 || pid != target.ProcessId) return TargetCheck.Closed;
        if (pid == (uint)Environment.ProcessId) return TargetCheck.OwnProcess;
        return pid == lease.ExpectedPid && lease.IsAliveAndPinned() ? TargetCheck.Ok : TargetCheck.Closed;
    }

    public bool IsTargetForeground(InsertionTarget target) =>
        Check(target) == TargetCheck.Ok && native.IsForeground(target.WindowHandle) && Check(target) == TargetCheck.Ok;

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        captured.Clear();
        native.Dispose();
    }
}
