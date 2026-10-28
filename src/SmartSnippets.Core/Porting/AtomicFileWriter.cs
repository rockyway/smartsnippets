namespace SmartSnippets.Core.Porting;

public static class AtomicFileWriter
{
    public static Task WriteAsync(string path, ReadOnlyMemory<byte> content, CancellationToken ct = default) =>
        WriteAsync(path, content,
            temp => new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous),
            (temp, target) => File.Move(temp, target, overwrite: true), ct);

    public static async Task WriteAsync(string path, ReadOnlyMemory<byte> content,
        Func<string, Stream> openTemp, Action<string, string> moveOverwrite, CancellationToken ct = default)
    {
        var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await using (var stream = openTemp(temp))
            {
                await stream.WriteAsync(content, ct);
                await stream.FlushAsync(ct);
            }
            moveOverwrite(temp, path);
        }
        catch
        {
            try { File.Delete(temp); } catch { /* Best effort; preserve original failure. */ }
            throw;
        }
    }
}
