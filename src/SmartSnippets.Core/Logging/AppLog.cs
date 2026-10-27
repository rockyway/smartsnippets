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
        var text = detail;
        if (exception is not null)
        {
            var ex = $"{exception.GetType().Name}: {exception.Message} (HResult=0x{exception.HResult:X8})";
            text = string.IsNullOrEmpty(detail) ? ex : $"{detail} | {ex}";
        }

        Write("ERROR", eventName, text);
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
