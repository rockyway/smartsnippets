namespace SmartSnippets.Presentation.Services;

public enum TrayState { Ready, PopupOpen, HotkeyUnavailable }

public static class TrayStatePolicy
{
    public static TrayState Compute(bool hotkeyRegistered, bool popupOpen) =>
        !hotkeyRegistered ? TrayState.HotkeyUnavailable : popupOpen ? TrayState.PopupOpen : TrayState.Ready;
}
