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
