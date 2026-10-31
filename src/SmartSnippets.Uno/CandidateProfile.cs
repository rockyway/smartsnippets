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

    // In-process test boundary only; neither root has a CLI/environment override.
    internal static CandidateProfile OpenOwnedRoot(string root, string? productionRoot = null)
    {
        root = Normalize(root);
        var production = Normalize(productionRoot ?? new AppPaths().Root);
        RefuseOverlap(root, production);
        RejectReparseAncestors(root);
        Directory.CreateDirectory(root);
        RejectReparseAncestors(root);
        var canonical = Normalize(CandidateFileIdentity.CanonicalDirectory(root));
        var canonicalProduction = Directory.Exists(production)
            ? Normalize(CandidateFileIdentity.CanonicalDirectory(production))
            : Path.Combine(CandidateFileIdentity.CanonicalDirectory(Path.GetDirectoryName(production)!), Path.GetFileName(production));
        RefuseOverlap(canonical, canonicalProduction);
        var ownerPath = Path.Combine(root, ".candidate-owner");
        if (File.Exists(ownerPath) && (File.GetAttributes(ownerPath) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidOperationException("Candidate ownership file cannot use reparse points.");
        // Lock before inspecting any profile data. Inspect the locked handle itself,
        // rather than reopening it with permissive sharing during the tree walk.
        var lease = new FileStream(ownerPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        try
        {
            RejectReparseAncestors(root);
            if ((File.GetAttributes(ownerPath) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException("Candidate ownership file cannot use reparse points.");
            CandidateFileIdentity.RejectHardLink(lease.SafeFileHandle);
            var pending = new Stack<string>(); pending.Push(root);
            while (pending.TryPop(out var directory))
                foreach (var path in Directory.EnumerateFileSystemEntries(directory))
                {
                    var attributes = File.GetAttributes(path);
                    if ((attributes & FileAttributes.ReparsePoint) != 0)
                        throw new InvalidOperationException("Candidate profile cannot contain reparse points.");
                    if ((attributes & FileAttributes.Directory) != 0) pending.Push(path);
                    else if (!path.Equals(ownerPath, StringComparison.OrdinalIgnoreCase)) CandidateFileIdentity.RejectHardLink(path);
                }
            return new CandidateProfile(root, lease);
        }
        catch { lease.Dispose(); throw; }
    }
    private static string Normalize(string path) => Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
    private static void RefuseOverlap(string root, string production)
    {
        static bool Contains(string parent, string child) => child.Equals(parent, StringComparison.OrdinalIgnoreCase)
            || child.StartsWith(Path.EndsInDirectorySeparator(parent) ? parent : parent + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase);
        if (Contains(root, production) || Contains(production, root))
            throw new InvalidOperationException("Production profiles and their ancestors are not candidate profiles.");
    }
    private static void RejectReparseAncestors(string root)
    {
        for (var current = new DirectoryInfo(root); current is not null; current = current.Parent)
            if (current.Exists && (current.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException("Candidate profile cannot use reparse paths.");
    }
    public void Dispose() => _lease.Dispose();
}
