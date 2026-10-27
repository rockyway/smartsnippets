namespace SmartSnippets.Core.Domain;

public static class FolderNameRules
{
    public static string Normalize(string name) => name.Trim();

    /// <summary>Uniqueness key. Invariant upper-casing only, not full case folding (accepted limitation, plan 003 R1).</summary>
    public static string Key(string name) => Normalize(name).ToUpperInvariant();

    public static ValidationError? Validate(string name) =>
        Normalize(name).Length == 0 ? ValidationError.FolderNameRequired : null;
}
