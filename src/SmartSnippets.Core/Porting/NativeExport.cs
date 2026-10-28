using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using SmartSnippets.Core.Domain;
using SmartSnippets.Core.Query;

namespace SmartSnippets.Core.Porting;

public static class NativeExport
{
    public const string FormatMarker = "smart-snippets";
    public const int Version = 1;

    public static NativeExportResult Write(IReadOnlyList<Folder> folders, IReadOnlyList<SnippetView> snippets, DateTimeOffset exportedAt)
    {
        if (snippets.Count > SnippetFileRules.MaxImportEntries)
            return new(null, ExportRefusal.TooManySnippets, 0, snippets.Count, folders.Count);
        // The serializer buffers a complete escaped token before writing to the bounded sink.
        // Refuse large string payloads BEFORE it can rent those token-sized intermediate buffers.
        if (StringPayloadExceedsLimit(folders, snippets))
            return new(null, ExportRefusal.TooLarge, SnippetFileRules.MaxImportBytes + 1L, snippets.Count, folders.Count);
        var document = new NativeDocument(FormatMarker, Version, exportedAt.ToUnixTimeMilliseconds(),
            folders.Select(f => new NativeFolder(f.Name)).ToArray(),
            snippets.Select(v => new NativeSnippet(v.Snippet.Text, v.Snippet.Label, v.Snippet.IsLabelCustom, v.FolderName,
                v.Snippet.Tags, v.Snippet.UsageCount, v.Snippet.CreatedAt.ToUnixTimeMilliseconds(),
                v.Snippet.UpdatedAt.ToUnixTimeMilliseconds(), v.Snippet.LastUsedAt?.ToUnixTimeMilliseconds())).ToArray());
        using var buffer = new BoundedBufferStream(SnippetFileRules.MaxImportBytes);
        try
        {
            JsonSerializer.Serialize(buffer, document, NativeJsonContext.Default.NativeDocument);
            var bytes = buffer.ToArray();
            return new(bytes, null, bytes.Length, snippets.Count, folders.Count);
        }
        catch (ExportLimitExceededException)
        {
            return new(null, ExportRefusal.TooLarge, SnippetFileRules.MaxImportBytes + 1L, snippets.Count, folders.Count);
        }
    }

    // Counts the exact default-encoded string payloads without creating encoded strings, DTOs or enumerators.
    // Omitting quotes, property names, numbers and punctuation makes the running sum a lower bound for the
    // whole document. The bounded sink remains the final authority for those small framing costs.
    private static bool StringPayloadExceedsLimit(IReadOnlyList<Folder> folders, IReadOnlyList<SnippetView> snippets)
    {
        int remaining = SnippetFileRules.MaxImportBytes;
        if (!FitsString(FormatMarker, ref remaining)) return true;
        for (int i = 0; i < folders.Count; i++)
            if (!FitsString(folders[i].Name, ref remaining)) return true;
        for (int i = 0; i < snippets.Count; i++)
        {
            var view = snippets[i];
            var snippet = view.Snippet;
            if (!FitsString(snippet.Text, ref remaining) || !FitsString(snippet.Label, ref remaining) ||
                !FitsString(view.FolderName, ref remaining)) return true;
            for (int j = 0; j < snippet.Tags.Count; j++)
                if (!FitsString(snippet.Tags[j], ref remaining)) return true;
        }
        return false;
    }

    private static bool FitsString(string? value, ref int remaining)
    {
        if (value is null) return true;
        // Escaping / UTF-8 never needs fewer bytes than the input's UTF-16 code-unit count.
        if (value.Length > remaining) return false;
        ReadOnlySpan<char> source = value;
        Span<char> escaped = stackalloc char[1024];
        while (!source.IsEmpty)
        {
            // Same encoder as the source-generated context. Its span API handles surrogate pairs and
            // replacement of lone surrogates with bounded scratch space, unlike Encode(string).
            JavaScriptEncoder.Default.Encode(source, escaped, out int consumed, out int written);
            int bytes = Encoding.UTF8.GetByteCount(escaped[..written]);
            if (bytes > remaining) return false;
            remaining -= bytes;
            source = source[consumed..];
        }
        return true;
    }
}

internal sealed class ExportLimitExceededException : Exception;

/// <summary>Checks before every write; neither logical length nor allocated backing buffer can exceed the cap.</summary>
internal sealed class BoundedBufferStream(int limit) : Stream
{
    private byte[] _buffer = [];
    private int _length;
    internal byte[] ToArray() => _buffer.AsSpan(0, _length).ToArray();
    public override void Write(ReadOnlySpan<byte> content)
    {
        if (content.Length > limit - _length) throw new ExportLimitExceededException();
        int needed = _length + content.Length;
        if (needed > _buffer.Length) Array.Resize(ref _buffer, (int)Math.Min(limit, Math.Max(needed, Math.Max(256L, _buffer.Length * 2L))));
        content.CopyTo(_buffer.AsSpan(_length));
        _length = needed;
    }
    public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));
    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => _length;
    public override long Position { get => _length; set => throw new NotSupportedException(); }
    public override void Flush() { }
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
}
