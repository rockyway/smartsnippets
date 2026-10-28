namespace SmartSnippets.Presentation.Services;

/// <summary>File boundary for import/export. Implemented in App with Windows App SDK pickers and FileStream.</summary>
public interface ISnippetFileService
{
    /// <summary>Open picker filtered to .json. Null when cancelled.</summary>
    Task<string?> PickImportFileAsync();

    /// <summary>Save picker; <paramref name="suggestedBaseName"/> has no extension (".json" is the default). Null when cancelled.</summary>
    Task<string?> PickExportFileAsync(string suggestedBaseName);

    /// <summary>Opens for async sequential read. Throws IOException or UnauthorizedAccessException.</summary>
    Stream OpenRead(string path);

    /// <summary>Delegates to Core's AtomicFileWriter.WriteAsync: temporary file in the same folder, then replace
    /// <paramref name="path"/>. On failure the original file is untouched (unless the replace itself succeeded) and
    /// deleting the temporary file is attempted (best effort; a failed delete never hides the original exception).
    /// Throws IOException or UnauthorizedAccessException.</summary>
    Task WriteAtomicallyAsync(string path, ReadOnlyMemory<byte> content);
}
