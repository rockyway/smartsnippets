namespace SmartSnippets.Uno.Services;

internal static class CandidateTrayText
{
    internal static string Tooltip(bool unavailable) => unavailable
        ? "Smart Snippets — Uno candidate — hotkey unavailable"
        : "Smart Snippets — Uno candidate";
}
