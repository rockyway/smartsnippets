using SmartSnippets.Core.Settings;

namespace SmartSnippets.Presentation.Services;

public static class PopupLogFormat
{
    // Numeric-only API: no snippet content can be supplied by the caller.
    public static string CacheRebuild(int ms, int rows) => $"popup_cache_rebuild_ms={ms} rows={rows}";
    public static string Show(int ms) => $"popup_show_ms={ms}";
    public static string SearchMs(int ms, PopupLayout layout, int results) =>
        $"{ms} layout={(layout == PopupLayout.Folder ? "folder" : "flat")} results={results}";
}
