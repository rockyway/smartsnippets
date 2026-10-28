using SmartSnippets.Core.Porting;

namespace SmartSnippets.Core.Tests.Porting;

public class AtomicFileWriterTests
{
    [Fact] public async Task Write_NewFile_CreatedNoTempLeft() => await WithDirectory(async dir => { var path = Path.Combine(dir, "a.json"); await AtomicFileWriter.WriteAsync(path, new byte[] { 1 }); Assert.Equal(new byte[] { 1 }, await File.ReadAllBytesAsync(path)); Assert.Single(Directory.GetFiles(dir)); });
    [Fact] public async Task Write_ExistingFile_Replaced() => await WithDirectory(async dir => { var path = Path.Combine(dir, "a.json"); await File.WriteAllBytesAsync(path, [9]); await AtomicFileWriter.WriteAsync(path, new byte[] { 1 }); Assert.Equal(new byte[] { 1 }, await File.ReadAllBytesAsync(path)); });
    [Fact] public async Task OpenTempFails_TargetUntouched_NoTemp() => await WithDirectory(async dir => { var path = Path.Combine(dir, "a.json"); await File.WriteAllBytesAsync(path, [9]); await Assert.ThrowsAsync<IOException>(() => AtomicFileWriter.WriteAsync(path, new byte[] { 1 }, _ => throw new IOException(), (_, _) => { })); Assert.Equal(new byte[] { 9 }, await File.ReadAllBytesAsync(path)); Assert.Single(Directory.GetFiles(dir)); });
    [Fact] public async Task MoveFails_TargetUntouched_TempDeleted() => await WithDirectory(async dir => { var path = Path.Combine(dir, "a.json"); await File.WriteAllBytesAsync(path, [9]); await Assert.ThrowsAsync<IOException>(() => AtomicFileWriter.WriteAsync(path, new byte[] { 1 }, p => File.Create(p), (_, _) => throw new IOException())); Assert.Equal(new byte[] { 9 }, await File.ReadAllBytesAsync(path)); Assert.Single(Directory.GetFiles(dir)); });
    [Fact] public async Task TempStreamDisposedBeforeMove() => await WithDirectory(async dir => { var path = Path.Combine(dir, "a.json"); Stream? stream = null; await AtomicFileWriter.WriteAsync(path, new byte[] { 1 }, p => stream = File.Create(p), (a, b) => { Assert.False(stream!.CanWrite); File.Move(a, b); }); });
    [Fact] public async Task WriteFailsMidway_TargetUntouched_TempDeleted() => await WithDirectory(async dir => { var path = Path.Combine(dir, "a.json"); await File.WriteAllBytesAsync(path, [9]); await Assert.ThrowsAsync<IOException>(() => AtomicFileWriter.WriteAsync(path, new byte[] { 1, 2, 3, 4 }, p => new FailingStream(File.Create(p)), (_, _) => { })); Assert.Equal(new byte[] { 9 }, await File.ReadAllBytesAsync(path)); Assert.Single(Directory.GetFiles(dir)); });
    [Fact] public async Task TempDeleteFails_OriginalExceptionStillThrown() => await WithDirectory(async dir => { var path = Path.Combine(dir, "a.json"); var expected = new IOException("original"); var thrown = await Assert.ThrowsAsync<IOException>(() => AtomicFileWriter.WriteAsync(path, new byte[] { 1 }, p => { Directory.CreateDirectory(p); throw expected; }, (_, _) => { })); Assert.Same(expected, thrown); });
    private static async Task WithDirectory(Func<string, Task> action) { var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")); Directory.CreateDirectory(dir); try { await action(dir); } finally { Directory.Delete(dir, true); } }
    private sealed class FailingStream(Stream inner) : Stream
    {
        public override bool CanRead => false; public override bool CanSeek => false; public override bool CanWrite => true;
        public override long Length => inner.Length; public override long Position { get => inner.Position; set => throw new NotSupportedException(); }
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) { inner.WriteByte(buffer.Span[0]); throw new IOException(); }
        protected override void Dispose(bool disposing) { if (disposing) inner.Dispose(); base.Dispose(disposing); }
        public override ValueTask DisposeAsync() => inner.DisposeAsync();
        public override void Flush() => inner.Flush(); public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException(); public override void SetLength(long value) => throw new NotSupportedException(); public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
