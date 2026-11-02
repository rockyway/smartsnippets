namespace SmartSnippets.Uno.Measurement;

// This entire directory is excluded from ordinary builds.
internal static class MeasurementProfile
{
    internal static string Root { get; } = ValidateRoot();
    private static string ValidateRoot()
    {
        var value = Environment.GetEnvironmentVariable("SMARTSNIPPETS_MEASURE_ROOT");
        if (string.IsNullOrWhiteSpace(value) || !Path.IsPathFullyQualified(value))
            throw new InvalidOperationException("Measurement root required.");
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(value));
        var parent = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.GetTempPath()));
        if (!string.Equals(Path.GetDirectoryName(root), parent, StringComparison.OrdinalIgnoreCase)
            || !Path.GetFileName(root).StartsWith("ss-mem-01-", StringComparison.Ordinal)
            || !File.Exists(Path.Combine(root, "measurement-owned")))
            throw new InvalidOperationException("Owned temporary measurement root required.");
        return root;
    }
}
