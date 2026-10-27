namespace SmartSnippets.Presentation.Services;

public sealed record StartupResult(bool Success, string Message);

public interface IStartupRegistration
{
    bool IsEnabled();

    StartupResult Enable();

    StartupResult Disable();
}
