namespace SmartSnippets.Presentation.Services;

/// <summary>Puts text on the clipboard ("Copy text" on save and insertion failures). UI thread only.</summary>
public interface IClipboardService
{
    void SetText(string text);
    bool IsAvailable => true;
    bool TrySetText(string text) { SetText(text); return true; }
}
