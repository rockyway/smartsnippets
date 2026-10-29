using System.Text;

namespace SmartSnippets.Presentation.Navigation;

/// <summary>Optional, non-inserting navigation requests from a launch or redirected launch.</summary>
public sealed record LaunchOptions(PageKind? Page, bool PreviewPopup, string? PreviewQuery, bool PreviewPopupSortMenu = false)
{
    public bool HasRequest => Page is not null || PreviewPopup;

    public static LaunchOptions Parse(IReadOnlyList<string> args)
    {
        PageKind? page = null;
        bool preview = false;
        bool sortMenu = false;
        string? query = null;
        for (var i = 0; i < args.Count; i++)
        {
            if (args[i].Equals("--page", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Count && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
            {
                var value = args[++i];
                if (value.Equals("snippets", StringComparison.OrdinalIgnoreCase)) page = PageKind.Snippets;
                else if (value.Equals("folders", StringComparison.OrdinalIgnoreCase)) page = PageKind.Folders;
                else if (value.Equals("settings", StringComparison.OrdinalIgnoreCase)) page = PageKind.Settings;
            }
            else if (args[i].Equals("--preview-popup", StringComparison.OrdinalIgnoreCase)) preview = true;
            else if (args[i].Equals("--preview-popup-sort-menu", StringComparison.OrdinalIgnoreCase)) sortMenu = true;
            else if (args[i].Equals("--preview-popup-query", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Count && !args[i + 1].StartsWith("--", StringComparison.Ordinal)) query = args[++i];
        }
        return new(page, preview, preview ? query : null, preview && sortMenu);
    }

    private static bool IsDelimiter(char c) => c == ' ' || c == '	';

    /// <summary>Split the redirected Windows launch string using the CRT quote/backslash rules.</summary>
    public static string[] SplitCommandLine(string commandLine)
    {
        var args = new List<string>();
        int i = 0;
        while (i < commandLine.Length)
        {
            while (i < commandLine.Length && IsDelimiter(commandLine[i])) i++;
            if (i == commandLine.Length) break;
            var value = new StringBuilder();
            bool quoted = false;
            while (i < commandLine.Length && (quoted || !IsDelimiter(commandLine[i])))
            {
                int slashes = 0;
                while (i < commandLine.Length && commandLine[i] == '\\') { slashes++; i++; }
                if (i < commandLine.Length && commandLine[i] == '"')
                {
                    value.Append('\\', slashes / 2);
                    if ((slashes & 1) != 0) value.Append('"');
                    else if (quoted && i + 1 < commandLine.Length && commandLine[i + 1] == '"')
                    {
                        value.Append('"');
                        i++;
                    }
                    else quoted = !quoted;
                    i++;
                }
                else
                {
                    value.Append('\\', slashes);
                    if (i < commandLine.Length && (quoted || !IsDelimiter(commandLine[i]))) value.Append(commandLine[i++]);
                }
            }
            args.Add(value.ToString());
        }
        return [.. args];
    }
}
