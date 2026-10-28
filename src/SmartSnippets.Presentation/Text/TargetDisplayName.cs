namespace SmartSnippets.Presentation.Text;

public static class TargetDisplayName
{
    public static string Format(string? friendly, string? exeName, string? title)
    {
        var exe = Path.GetFileNameWithoutExtension(exeName ?? string.Empty);
        var name = string.IsNullOrWhiteSpace(friendly) ? exe : friendly.Trim();
        var window = title?.Trim() ?? string.Empty;
        string result;
        if (exe.Equals("ApplicationFrameHost", StringComparison.OrdinalIgnoreCase))
        {
            result = string.IsNullOrEmpty(window) ? name : window;
        }
        else if (string.IsNullOrEmpty(name))
        {
            result = window;
        }
        else
        {
            foreach (var separator in new[] { " - ", " — " })
            {
                var suffix = separator + name;
                if (window.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    window = window[..^suffix.Length].Trim();
                    break;
                }
            }
            result = string.IsNullOrEmpty(window) || window.Equals(name, StringComparison.OrdinalIgnoreCase)
                ? name : $"{name}, {window}";
        }
        if (string.IsNullOrWhiteSpace(result)) result = "the target app";
        if (result.Length <= 60) return result;
        var cut = char.IsHighSurrogate(result[58]) ? 58 : 59;
        return result[..cut] + "…";
    }
}
