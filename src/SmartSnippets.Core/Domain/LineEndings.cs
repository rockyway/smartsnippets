using System.Text;

namespace SmartSnippets.Core.Domain;

public static class LineEndings
{
    public static string ToCrLf(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var first = 0;
        while (first < text.Length)
        {
            if (text[first] == '\r')
            {
                if (first + 1 >= text.Length || text[first + 1] != '\n') break;
                first += 2;
            }
            else if (text[first] == '\n') break;
            else first++;
        }

        if (first == text.Length) return text;
        var result = new StringBuilder(text.Length + 16);
        result.Append(text, 0, first);
        for (var i = first; i < text.Length; i++)
        {
            if (text[i] == '\r')
            {
                result.Append("\r\n");
                if (i + 1 < text.Length && text[i + 1] == '\n') i++;
            }
            else if (text[i] == '\n') result.Append("\r\n");
            else result.Append(text[i]);
        }

        return result.ToString();
    }
}
