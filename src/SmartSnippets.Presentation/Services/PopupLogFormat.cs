using SmartSnippets.Core.Settings;

namespace SmartSnippets.Presentation.Services;

public static class PopupLogFormat
{
    public static string SearchMs(int ms, PopupLayout layout, int results) =>
        $"{ms} layout={(layout == PopupLayout.Folder ? "folder" : "flat")} results={results}";
}
