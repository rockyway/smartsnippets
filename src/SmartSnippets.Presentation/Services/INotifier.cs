namespace SmartSnippets.Presentation.Services;

/// <summary>Background notices shown while no window may be visible (tray balloon).</summary>
public interface INotifier
{
    void Notify(string title, string message);
}
