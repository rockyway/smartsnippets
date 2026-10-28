using System.Globalization;

namespace SmartSnippets.Core.Porting;

public static class SnippetFileRules
{
    public const int MaxImportBytes = 32 * 1024 * 1024;
    public const int MaxImportEntries = 50_000;

    public static bool HasJsonExtension(string path) =>
        string.Equals(Path.GetExtension(path), ".json", StringComparison.OrdinalIgnoreCase);

    public static string ExportBaseName(DateTimeOffset localNow) =>
        "smart-snippets-" + localNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
