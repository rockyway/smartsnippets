using SmartSnippets.Core;

namespace SmartSnippets.Uno;

/// <summary>The official profile. AppLifecycle owns single-instance admission before this is opened.</summary>
internal sealed class AppProfile : IDisposable
{
    internal const string Identity = "SmartSnippets.Main";
    private AppProfile(string root) => Paths = new AppPaths(root);
    internal AppPaths Paths { get; }
    internal static AppProfile OpenDefault() => OpenOwnedRoot(new AppPaths().Root);

    // In-process test boundary only; no CLI/environment profile override.
    internal static AppProfile OpenOwnedRoot(string root)
    {
        root = Path.GetFullPath(root);
        Directory.CreateDirectory(root);
        return new(root);
    }
    public void Dispose() { }
}
