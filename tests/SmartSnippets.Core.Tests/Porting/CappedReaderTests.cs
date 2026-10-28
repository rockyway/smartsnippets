using SmartSnippets.Core.Porting;

namespace SmartSnippets.Core.Tests.Porting;

public class CappedReaderTests
{
    private static Task<CappedReadResult> Read(byte[] bytes, int cap = 1024) => CappedReader.ReadAsync(new MemoryStream(bytes), cap);
    [Fact] public async Task ExactlyCap_Accepted() => Assert.Equal(SnippetFileRules.MaxImportBytes, (await Read(new byte[SnippetFileRules.MaxImportBytes], SnippetFileRules.MaxImportBytes)).Utf8Json!.Length);
    [Fact] public async Task CapPlusOne_TooLarge() => Assert.Equal(ImportRefusal.TooLarge, (await Read(new byte[SnippetFileRules.MaxImportBytes + 1], SnippetFileRules.MaxImportBytes)).Refusal);
    [Fact] public async Task Utf8Bom_Stripped() => Assert.Equal(new byte[] { 65 }, (await Read([0xef, 0xbb, 0xbf, 65])).Utf8Json);
    [Fact] public async Task Utf16LeBom_NotUtf8() => Assert.Equal(ImportRefusal.NotUtf8, (await Read([0xff, 0xfe])).Refusal);
    [Fact] public async Task Utf16BeBom_NotUtf8() => Assert.Equal(ImportRefusal.NotUtf8, (await Read([0xfe, 0xff])).Refusal);
    [Fact] public async Task Utf32Bom_NotUtf8() => Assert.Equal(ImportRefusal.NotUtf8, (await Read([0, 0, 0xfe, 0xff])).Refusal);
    [Fact] public async Task InvalidUtf8InsideString_NotUtf8() => Assert.Equal(ImportRefusal.NotUtf8, (await Read([34, 0xc3, 0x28, 34])).Refusal);
    [Fact] public async Task Empty_ReturnsEmptyBytes()
    {
        var bytes = (await Read([])).Utf8Json!;
        Assert.Empty(bytes);
        Assert.Equal(ImportRefusal.InvalidJson, ImportParser.Parse(bytes, DateTimeOffset.UtcNow).Failure!.Refusal);
    }
    [Fact] public async Task TooLarge_StopsReadingAtCapPlusOne()
    {
        var stream = new CountingStream(64 * 1024 * 1024, false);
        Assert.Equal(ImportRefusal.TooLarge, (await CappedReader.ReadAsync(stream, SnippetFileRules.MaxImportBytes)).Refusal);
        Assert.Equal(SnippetFileRules.MaxImportBytes + 1, stream.Total);
    }
    [Fact] public async Task LengthLies_StillCapped()
    {
        var stream = new CountingStream(100, true);
        Assert.Equal(ImportRefusal.TooLarge, (await CappedReader.ReadAsync(stream, 20)).Refusal);
        Assert.Equal(21, stream.Total);
    }
    [Fact] public async Task ReadThrowsIOException_OnFirstRead_Unreadable() => Assert.Equal(ImportRefusal.Unreadable, (await CappedReader.ReadAsync(new ThrowStream(new IOException(), 0), 2000)).Refusal);
    [Fact] public async Task ReadThrowsIOException_AfterSomeBytes_Unreadable() => Assert.Equal(ImportRefusal.Unreadable, (await CappedReader.ReadAsync(new ThrowStream(new IOException(), 1000), 2000)).Refusal);
    [Fact] public async Task ReadThrowsUnauthorized_Unreadable() => Assert.Equal(ImportRefusal.Unreadable, (await CappedReader.ReadAsync(new ThrowStream(new UnauthorizedAccessException(), 0), 2000)).Refusal);
    [Fact] public async Task ReadCancelled_PropagatesOperationCanceled() => await Assert.ThrowsAsync<OperationCanceledException>(() => CappedReader.ReadAsync(new ThrowStream(new OperationCanceledException(), 0), 100));
    [Fact] public async Task ReadThrowsInvalidData_Propagates() => await Assert.ThrowsAsync<InvalidDataException>(() => CappedReader.ReadAsync(new ThrowStream(new InvalidDataException(), 0), 100));

    private sealed class CountingStream(int remaining, bool seek) : Stream
    {
        public int Total { get; private set; }
        public override bool CanRead => true; public override bool CanSeek => seek; public override bool CanWrite => false;
        public override long Length => 10; public override long Position { get => Total; set => throw new NotSupportedException(); }
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        { int n = Math.Min(buffer.Length, remaining); buffer.Span[..n].Clear(); remaining -= n; Total += n; return ValueTask.FromResult(n); }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void Flush() => throw new NotSupportedException(); public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException(); public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
    private sealed class ThrowStream(Exception exception, int before) : Stream
    {
        public override bool CanRead => true; public override bool CanSeek => false; public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException(); public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        { if (before == 0) throw exception; int n = Math.Min(before, buffer.Length); before -= n; return ValueTask.FromResult(n); }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void Flush() => throw new NotSupportedException(); public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException(); public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
