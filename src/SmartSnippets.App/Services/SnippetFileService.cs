using Microsoft.UI;
using Microsoft.Windows.Storage.Pickers;
using SmartSnippets.Core.Logging;
using SmartSnippets.Core.Porting;
using SmartSnippets.Presentation.Services;

namespace SmartSnippets.App.Services;

internal sealed class SnippetFileService(Func<WindowId?> owner) : ISnippetFileService
{
    public async Task<string?> PickImportFileAsync()
    {
        if (owner() is not { } id) { AppLog.Info("picker_no_owner"); return null; }
        var picker = new FileOpenPicker(id) { CommitButtonText = "Import" };
        picker.FileTypeFilter.Add(".json");
        return (await picker.PickSingleFileAsync())?.Path;
    }
    public async Task<string?> PickExportFileAsync(string suggestedBaseName)
    {
        if (owner() is not { } id) { AppLog.Info("picker_no_owner"); return null; }
        var picker = new FileSavePicker(id) { SuggestedFileName = suggestedBaseName, DefaultFileExtension = ".json" };
        picker.FileTypeChoices.Add("JSON", [".json"]);
        return (await picker.PickSaveFileAsync())?.Path;
    }
    public Stream OpenRead(string path) => new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920,
        FileOptions.Asynchronous | FileOptions.SequentialScan);
    public Task WriteAtomicallyAsync(string path, ReadOnlyMemory<byte> content) => AtomicFileWriter.WriteAsync(path, content);
}
