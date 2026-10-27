using SmartSnippets.Presentation.Services;

namespace SmartSnippets.App.Services;

internal sealed class Notifier : INotifier
{
    private readonly TrayIcon _tray;

    public Notifier(TrayIcon tray) => _tray = tray;

    public void Notify(string title, string message) => _tray.ShowBalloon(title, message);
}
