using System.Text;
using SmartSnippets.TargetIdentity018;

namespace SmartSnippets.WindowsInsertion018;

internal static class FixedPipe
{
    internal static async Task<string> ReadAsync(StreamReader reader, TimeSpan timeout, CancellationToken ct)
    {
        using var bounded = CancellationTokenSource.CreateLinkedTokenSource(ct);
        bounded.CancelAfter(timeout);
        return await ReadOrEofAsync(reader, bounded.Token).ConfigureAwait(false) ?? throw new ProbeFailure("role_eof");
    }

    internal static async Task<string?> ReadOrEofAsync(StreamReader reader, CancellationToken ct = default)
    {
        var line = new StringBuilder();
        var character = new char[1];
        while (await reader.ReadAsync(character.AsMemory(), ct).ConfigureAwait(false) != 0)
        {
            var value = character[0];
            if (value == '\n') return line.ToString().TrimEnd('\r');
            if (line.Length == 256 || value is < ' ' or > '~' && value != '\r') throw new ProbeFailure("role_protocol");
            line.Append(value);
        }
        if (line.Length != 0) throw new ProbeFailure("role_protocol");
        return null;
    }
    internal static async Task WriteAsync(StreamWriter writer, string fixedLine)
    {
        writer.NewLine = "\n";
        using var bounded = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        await writer.WriteLineAsync(fixedLine.AsMemory(), bounded.Token).ConfigureAwait(false);
        await writer.FlushAsync(bounded.Token).ConfigureAwait(false);
    }
    internal static async Task WriteAsync(Stream stream, string fixedLine)
    {
        using var bounded = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var bytes = Encoding.UTF8.GetBytes(fixedLine + "\n");
        await stream.WriteAsync(bytes.AsMemory(), bounded.Token).ConfigureAwait(false);
        await stream.FlushAsync(bounded.Token).ConfigureAwait(false);
    }
    internal static void Receipt(string fixedLine)
    {
        // Losing the parent output pipe must never kill the only RAM backup.
        try { Console.WriteLine(fixedLine); Console.Out.Flush(); }
        catch (Exception error) when (error is IOException or ObjectDisposedException) { }
    }
}
