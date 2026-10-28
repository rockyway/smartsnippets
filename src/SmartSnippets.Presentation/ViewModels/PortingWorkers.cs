using SmartSnippets.Core.Domain;
using SmartSnippets.Core.Porting;
using SmartSnippets.Core.Query;

namespace SmartSnippets.Presentation.ViewModels;

public sealed record PortingWorkers(
    Func<ReadOnlyMemory<byte>, DateTimeOffset, Task<ImportParseResult>> ParseAsync,
    Func<IReadOnlyList<Folder>, IReadOnlyList<SnippetView>, DateTimeOffset, Task<NativeExportResult>> ExportAsync)
{
    public static PortingWorkers Create(Action? onWorkerThread = null) => new(
        (bytes, now) => Task.Run(() => { onWorkerThread?.Invoke(); return ImportParser.Parse(bytes, now); }),
        (folders, snippets, now) => Task.Run(() => { onWorkerThread?.Invoke(); return NativeExport.Write(folders, snippets, now); }));
    public static PortingWorkers Default { get; } = Create();
}
