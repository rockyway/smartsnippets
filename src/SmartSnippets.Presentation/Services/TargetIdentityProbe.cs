namespace SmartSnippets.Presentation.Services;

/// <summary>Target identity rules, with native window and process queries supplied by the platform.</summary>
public sealed class TargetIdentityProbe(
    Func<nint, bool> isWindow, Func<nint, uint> processId,
    Func<nint> foreground, Func<uint> ownProcessId, Func<uint, bool> isBlocked) : ITargetProbe
{
    public TargetCheck Check(InsertionTarget target)
    {
        if (!isWindow(target.WindowHandle)) return TargetCheck.Closed;
        var pid = processId(target.WindowHandle);
        if (pid == 0 || pid != target.ProcessId) return TargetCheck.Closed;
        if (pid == ownProcessId()) return TargetCheck.OwnProcess;
        return isBlocked(pid) ? TargetCheck.Elevated : TargetCheck.Ok;
    }

    public bool IsTargetForeground(InsertionTarget target)
    {
        var window = foreground();
        return window == target.WindowHandle && processId(window) == target.ProcessId && target.ProcessId != 0;
    }
}
