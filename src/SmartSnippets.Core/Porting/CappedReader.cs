using System.Text.Unicode;

namespace SmartSnippets.Core.Porting;

public sealed record CappedReadResult(byte[]? Utf8Json, ImportRefusal? Refusal);

public static class CappedReader
{
    public static async Task<CappedReadResult> ReadAsync(Stream stream, int maxBytes, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxBytes);
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        long limit = (long)maxBytes + 1;
        try
        {
            while (buffer.Length < limit)
            {
                var count = (int)Math.Min(chunk.Length, limit - buffer.Length);
                var read = await stream.ReadAsync(chunk.AsMemory(0, count), ct);
                if (read == 0) break;
                buffer.Write(chunk, 0, read);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new(null, ImportRefusal.Unreadable);
        }

        if (buffer.Length > maxBytes) return new(null, ImportRefusal.TooLarge);
        var bytes = buffer.ToArray();
        ReadOnlySpan<byte> span = bytes;
        if (span.StartsWith(new byte[] { 0xff, 0xfe }) || span.StartsWith(new byte[] { 0xfe, 0xff }) ||
            span.StartsWith(new byte[] { 0x00, 0x00, 0xfe, 0xff })) return new(null, ImportRefusal.NotUtf8);
        if (span.StartsWith(new byte[] { 0xef, 0xbb, 0xbf })) bytes = bytes[3..];
        return Utf8.IsValid(bytes) ? new(bytes, null) : new(null, ImportRefusal.NotUtf8);
    }
}
