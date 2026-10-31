using System.Globalization;
using System.Text;

namespace SmartSnippets.Core.Logging;

/// <summary>
/// Plain-text append-only log. Callers must never pass snippet text in any argument.
/// </summary>
public static class AppLog
{
    private static readonly object Gate = new();
    private static string? _filePath;

    public static void Initialize(string logDirectory)
    {
        try
        {
            Directory.CreateDirectory(logDirectory);
            lock (Gate)
            {
                _filePath = Path.Combine(logDirectory, "app.log");
            }
        }
        catch (Exception)
        {
            lock (Gate)
            {
                _filePath = null;
            }
        }
    }

    /// <summary>Disables logging again; calls become no-ops until the next Initialize.</summary>
    public static void Shutdown()
    {
        lock (Gate)
        {
            _filePath = null;
        }
    }

    public static void Info(string eventName, string? detail = null) => Write("INFO", eventName, detail);

    public static void Error(string eventName, Exception? exception = null, string? detail = null)
    {
        var text = SafeErrorDetail(detail);
        if (exception is not null)
        {
            var diagnostic = $"{exception.GetType().Name} (HResult=0x{exception.HResult:X8})";
            text = string.IsNullOrEmpty(text) ? diagnostic : $"{text} | {diagnostic}";
        }

        Write("ERROR", eventName, text);
    }

    // Error callers include startup stack frames. Admit only known diagnostic fields, never free text.
    private static string SafeErrorDetail(string? detail)
    {
        return string.Join(" ", (detail ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(field =>
            {
                var parts = field.Split('=');
                if (parts.Length != 2) return false;
                var key = parts[0];
                var value = parts[1];
                if (key is "code" or "stored" or "known" or "version" or "sqlite" or "index" or "of" or "hresult"
                    or "entries" or "imported" or "duplicates" or "unsupported" or "rejected" or "folders_created" or "bytes" or "parse_ms" or "commit_ms")
                    return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _)
                        || (value.StartsWith("0x", StringComparison.Ordinal) && value.Length <= 18
                            && ulong.TryParse(value.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _));
                return (key, value) switch
                {
                    ("reason", "backup" or "newer") => true,
                    ("stage", "before_commit" or "after_commit" or "before_write" or "after_write") => true,
                    ("format", "Native" or "TermFlow" or "Rephlo" or "InkSpoke") => true,
                    ("error", "SqliteException" or "InvalidOperationException" or "IOException" or "UnauthorizedAccessException") => true,
                    ("kind", "SnippetCreated" or "SnippetUpdated" or "SnippetDeleted" or "FolderCreated" or "FolderRenamed" or "FolderDeleted" or "UsageRecorded" or "Reloaded" or "Imported") => true,
                    ("op", "import_snippets" or "create_snippet" or "update_snippet" or "delete_snippet"
                        or "create_folder" or "rename_folder" or "delete_folder" or "record_usage") => true,
                    _ => false,
                };
            }));
    }

    private static void Write(string level, string eventName, string? detail)
    {
        try
        {
            var line = new StringBuilder()
                .Append(DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture))
                .Append(' ').Append(level)
                .Append(' ').Append(eventName);
            if (!string.IsNullOrEmpty(detail))
            {
                line.Append(' ').Append(detail.Replace("\r", " ").Replace("\n", " "));
            }

            line.AppendLine();

            lock (Gate)
            {
                if (_filePath is null)
                {
                    return;
                }

                File.AppendAllText(_filePath, line.ToString(), Encoding.UTF8);
            }
        }
        catch (Exception)
        {
        }
    }
}
