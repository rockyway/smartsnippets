using SmartSnippets.Core;

namespace SmartSnippets.Uno;

/// <summary>The official profile. AppLifecycle owns single-instance admission before this is opened.</summary>
internal sealed class AppProfile : IDisposable
{
#if SMARTSNIPPETS_MEASURE
    internal static readonly string Identity = "SmartSnippets.Measure." + Path.GetFileName(Measurement.MeasurementProfile.Root);
#else
    internal const string Identity = "SmartSnippets.Main";
#endif
    private AppProfile(string root) => Paths = new AppPaths(root);
    internal AppPaths Paths { get; }
#if SMARTSNIPPETS_MEASURE
    internal static AppProfile OpenDefault() => OpenOwnedRoot(Measurement.MeasurementProfile.Root);
#else
    internal static AppProfile OpenDefault() => OpenOwnedRoot(new AppPaths().Root);
#endif

    // In-process test boundary only; no CLI/environment profile override.
    internal static AppProfile OpenOwnedRoot(string root)
    {
        root = Path.GetFullPath(root);
        Directory.CreateDirectory(root);
        return new(root);
    }
    public void Dispose() { }
}
