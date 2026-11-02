using SmartSnippets.Core;

namespace SmartSnippets.Uno;

/// <summary>The official profile. AppLifecycle owns single-instance admission before this is opened.</summary>
internal sealed class CandidateProfile : IDisposable
{
    internal const string Identity = "SmartSnippets.Main";
    private CandidateProfile(string root) => Paths = new AppPaths(root);
    internal AppPaths Paths { get; }
    internal static CandidateProfile OpenDefault() => OpenOwnedRoot(new AppPaths().Root);

    // In-process test boundary only; no CLI/environment profile override.
    internal static CandidateProfile OpenOwnedRoot(string root)
    {
        root = Path.GetFullPath(root);
        Directory.CreateDirectory(root);
        return new(root);
    }
    public void Dispose() { }
}
