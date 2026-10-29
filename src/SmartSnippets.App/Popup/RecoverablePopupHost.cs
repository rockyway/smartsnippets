using SmartSnippets.Presentation.Services;

namespace SmartSnippets.App.Popup;

/// <summary>Stable forwarding endpoint beneath DeferredPopupHost's unchanged recovery gate.</summary>
internal sealed class RecoverablePopupHost(Func<PopupWindow> get, Action hide) : IPopupHost
{
    public void Hide() => hide();
    public void ShowAgain() => get().ShowAgain();
    public void OpenMainWindow() => get().OpenMainWindow();
    public void EditSnippet(long id) => get().EditSnippet(id);
    public void Focus(PopupFocusTarget target) => get().Focus(target);
}
