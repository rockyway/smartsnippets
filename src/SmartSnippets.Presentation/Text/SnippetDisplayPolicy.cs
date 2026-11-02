using SmartSnippets.Core.Domain;

namespace SmartSnippets.Presentation.Text;

/// <summary>Display-only text; never use for insertion or explicit Copy.</summary>
public static class SnippetDisplayPolicy
{
    public const string PasswordMask = "••••••••";
    public static string Content(Snippet snippet) => snippet.IsPassword ? PasswordMask : snippet.Text;
}
