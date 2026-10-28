using SmartSnippets.Presentation.Services;

namespace SmartSnippets.Presentation.Tests.Fakes;

internal sealed class FakePopupHost : IPopupHost
{
    public List<string> Calls { get; } = [];
    public void Hide() => Calls.Add("Hide");
    public void ShowAgain() => Calls.Add("ShowAgain");
    public void OpenMainWindow() => Calls.Add("OpenMainWindow");
    public void Focus(PopupFocusTarget target) => Calls.Add($"Focus:{target}");
}
