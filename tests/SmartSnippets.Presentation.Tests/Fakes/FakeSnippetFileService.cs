using System.Text;
using SmartSnippets.Presentation.Services;

namespace SmartSnippets.Presentation.Tests.Fakes;

internal sealed class FakeSnippetFileService : ISnippetFileService
{
    internal string? ImportPath { get; set; } = "input.json";
    internal string? ExportPath { get; set; } = "output.json";
    internal byte[] Bytes { get; set; } = Encoding.UTF8.GetBytes("{\"version\":1,\"snippets\":[{\"text\":\"Imported text\",\"folder\":\"New folder\",\"tags\":[\"newtag\"]}]}");
    internal Exception? OpenException { get; set; }
    internal Exception? ReadException { get; set; }
    internal int BytesBeforeReadException { get; set; }
    internal Exception? WriteException { get; set; }
    internal bool Suspend { get; set; }
    internal int ImportPicks { get; private set; }
    internal int ExportPicks { get; private set; }
    internal int Opens { get; private set; }
    internal string? SuggestedBaseName { get; private set; }
    internal List<(string Path, byte[] Bytes)> Writes { get; } = [];
    internal Action? OnWrite { get; set; }
    public async Task<string?> PickImportFileAsync() { ImportPicks++; if (Suspend) await Task.Delay(10).ConfigureAwait(false); return ImportPath; }
    public async Task<string?> PickExportFileAsync(string suggestedBaseName) { ExportPicks++; SuggestedBaseName = suggestedBaseName; if (Suspend) await Task.Delay(10).ConfigureAwait(false); return ExportPath; }
    public Stream OpenRead(string path) { Opens++; if (OpenException is { } ex) throw ex; return new ReadStream(this); }
    public Task WriteAtomicallyAsync(string path, ReadOnlyMemory<byte> content) { OnWrite?.Invoke(); if (WriteException is { } ex) throw ex; Writes.Add((path, content.ToArray())); return Task.CompletedTask; }
    private sealed class ReadStream(FakeSnippetFileService owner) : MemoryStream(owner.Bytes)
    {
        private int _read;
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
        {
            if (owner.Suspend) await Task.Delay(10, ct).ConfigureAwait(false);
            if (owner.ReadException is { } ex && _read >= owner.BytesBeforeReadException) throw ex;
            if (owner.ReadException is not null) buffer = buffer[..Math.Min(buffer.Length, owner.BytesBeforeReadException - _read)];
            int n = await base.ReadAsync(buffer, ct).ConfigureAwait(false); _read += n; return n;
        }
    }
}
