using SmartSnippets.Core;

namespace SmartSnippets.Uno;

/// <summary>Owns an isolated root before logging, migration or store access.</summary>
internal sealed class CandidateProfile : IDisposable
{
    internal const string Identity = "SmartSnippets.UnoCandidate019";
    private readonly FileStream _lease;
    private CandidateProfile(string root, FileStream lease) { Paths = new AppPaths(root); _lease = lease; }
    internal AppPaths Paths { get; }
    internal static CandidateProfile OpenDefault() => OpenOwnedRoot(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Identity));

    // External test boundary only; no CLI or environment profile override.
    internal static CandidateProfile OpenOwnedRoot(string root)
    {
        root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        var production = Path.TrimEndingDirectorySeparator(Path.GetFullPath(new AppPaths().Root));
        if (root.Equals(production, StringComparison.OrdinalIgnoreCase)
            || root.StartsWith(production + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Production profiles are not candidate profiles.");
        RejectReparseAncestors(root);
        Directory.CreateDirectory(root);
        RejectReparseAncestors(root);
        var canonical = CandidateFileIdentity.CanonicalDirectory(root);
        var local = CandidateFileIdentity.CanonicalDirectory(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
        var canonicalProduction = Path.Combine(local, "SmartSnippets");
        if (canonical.Equals(canonicalProduction, StringComparison.OrdinalIgnoreCase)
            || canonical.StartsWith(canonicalProduction + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Production aliases are not candidate profiles.");
        // Inspect each directory before descending: never follow a nested reparse path.
        var pending = new Stack<string>(); pending.Push(root);
        while (pending.TryPop(out var directory))
            foreach (var path in Directory.EnumerateFileSystemEntries(directory))
            {
                var attributes = File.GetAttributes(path);
                if ((attributes & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidOperationException("Candidate profile cannot contain reparse points.");
                if ((attributes & FileAttributes.Directory) != 0) pending.Push(path);
                else CandidateFileIdentity.RejectHardLink(path);
            }
        return new CandidateProfile(root, new FileStream(Path.Combine(root, ".candidate-owner"),
            FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None));
    }
    private static void RejectReparseAncestors(string root)
    {
        for (var current = new DirectoryInfo(root); current is not null; current = current.Parent)
            if (current.Exists && (current.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException("Candidate profile cannot use reparse paths.");
    }
    public void Dispose() => _lease.Dispose();
}
