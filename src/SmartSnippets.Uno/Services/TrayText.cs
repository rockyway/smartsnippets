namespace SmartSnippets.Uno.Services;

internal static class TrayText
{
    internal static string Tooltip(bool unavailable) => unavailable
        ? "Smart Snippets — hotkey unavailable"
        : "Smart Snippets";
}
