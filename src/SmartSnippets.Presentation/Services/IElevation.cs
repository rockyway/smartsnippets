namespace SmartSnippets.Presentation.Services;

public enum ElevationOutcome
{
    Started,
    Cancelled,
    Failed,
}

public sealed record ElevationResult(ElevationOutcome Outcome, string Message);

public interface IElevation
{
    bool IsElevated { get; }

    /// <summary>Starts an elevated copy; on <see cref="ElevationOutcome.Started"/> the caller exits this instance.</summary>
    ElevationResult RestartAsAdministrator();
}
