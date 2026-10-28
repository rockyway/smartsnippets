namespace SmartSnippets.Presentation.Services;

public enum PopupFocusTarget { Search, Editor, Footer }

public interface IPopupHost
{
    void Hide();
    void ShowAgain();
    void OpenMainWindow();
    void EditSnippet(long snippetId);
    void Focus(PopupFocusTarget target);
}
