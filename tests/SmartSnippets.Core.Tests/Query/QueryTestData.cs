using SmartSnippets.Core.Domain;
using SmartSnippets.Core.Query;

namespace SmartSnippets.Core.Tests.Query;

internal static class QueryTestData
{
    private static readonly DateTimeOffset Epoch = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static SnippetView View(
        long id,
        string label = "L",
        string text = "T",
        string? folder = null,
        long? folderId = null,
        string[]? tags = null,
        int usage = 0,
        int lastUsedDay = -1,
        int createdDay = 0,
        int updatedDay = 0)
    {
        var snippet = new Snippet(
            id,
            text,
            label,
            true,
            folderId ?? (folder is null ? null : 1),
            tags ?? [],
            Epoch.AddDays(createdDay),
            Epoch.AddDays(updatedDay),
            lastUsedDay < 0 ? null : Epoch.AddDays(lastUsedDay),
            usage);
        return new SnippetView(snippet, folder);
    }

    public static long[] Ids(IEnumerable<SnippetView> views) => [.. views.Select(v => v.Snippet.Id)];
}
