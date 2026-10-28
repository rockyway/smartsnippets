using SmartSnippets.Presentation.Services;

namespace SmartSnippets.Presentation.Tests.Fakes;

internal sealed class RecordingNotifier : INotifier
{
    public List<(string Title, string Message)> Calls { get; } = [];
    public void Notify(string title, string message) => Calls.Add((title, message));
}
