using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartSnippets.Core.Logging;
using SmartSnippets.Core.Porting;
using SmartSnippets.Core.Store;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Presentation.Text;

namespace SmartSnippets.Presentation.ViewModels;

public sealed partial class SnippetPortingViewModel : ObservableObject
{
    private readonly ISnippetStore _store;
    private readonly IDialogService _dialogs;
    private readonly ISnippetFileService? _files;
    private readonly TimeProvider _time;
    private readonly PortingWorkers _workers;
    public SnippetPortingViewModel(ISnippetStore store, IDialogService dialogs, ISnippetFileService? files, TimeProvider? time = null, PortingWorkers? workers = null)
    { _store = store; _dialogs = dialogs; _files = files; _time = time ?? TimeProvider.System; _workers = workers ?? PortingWorkers.Default; }
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ImportCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportCommand))]
    private bool _isBusy;
    private bool CanRun() => _files is not null && !IsBusy;
    [RelayCommand(CanExecute = nameof(CanRun))] private Task Import() => ImportAsync();
    [RelayCommand(CanExecute = nameof(CanRun))] private Task Export() => ExportAsync();

    public async Task<ImportRunResult?> ImportAsync()
    {
        if (!CanRun()) return null;
        IsBusy = true;
        string stage = "before_commit";
        int? bytes = null;
        ImportRunResult? outcome = null;
        async Task<ImportRunResult> Refuse(ImportFailure failure)
        {
            AppLog.Info("import_refused", $"reason={failure.Refusal} format={failure.Format?.ToString() ?? "none"} bytes={bytes?.ToString() ?? "-"}");
            outcome = new(null, failure);
            await _dialogs.ShowErrorAsync(UiText.ImportFailedTitle, UiText.ImportRefusalMessage(failure));
            return outcome;
        }
        try
        {
            var path = await _files!.PickImportFileAsync();
            if (path is null) { AppLog.Info("import_cancelled"); return null; }
            if (!SnippetFileRules.HasJsonExtension(path)) return await Refuse(new(ImportRefusal.NotJsonFile));
            Stream stream;
            try { stream = _files.OpenRead(path); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return await Refuse(new(ImportRefusal.Unreadable)); }
            CappedReadResult read;
            using (stream) read = await CappedReader.ReadAsync(stream, SnippetFileRules.MaxImportBytes);
            if (read.Refusal is { } refusal) return await Refuse(new(refusal));
            bytes = read.Utf8Json!.Length;
            var stopwatch = Stopwatch.StartNew();
            var parsed = await _workers.ParseAsync(read.Utf8Json, _time.GetUtcNow());
            long parseMs = stopwatch.ElapsedMilliseconds;
            if (parsed.Failure is { } failure) return await Refuse(failure);
            stage = "in_commit";
            stopwatch.Restart();
            var summary = SnippetImport.Commit(parsed.Parsed!, _store);
            long commitMs = stopwatch.ElapsedMilliseconds;
            stage = "after_commit";
            outcome = new(summary, null);
            var detail = $"format={summary.Format} entries={summary.Entries} imported={summary.Imported} duplicates={summary.SkippedDuplicates} unsupported={summary.SkippedUnsupported} rejected={summary.Rejected} folders_created={summary.FoldersCreated} bytes={bytes} parse_ms={parseMs} commit_ms={commitMs}";
            AppLog.Info("import_done", detail);
            if (!summary.IsBalanced) AppLog.Error("import_unbalanced", null, detail);
            await _dialogs.ShowInfoAsync(summary.Imported > 0 ? UiText.ImportCompleteTitle : UiText.ImportNothingTitle, UiText.ImportSummary(summary));
            return outcome;
        }
        catch (OperationCanceledException) { throw; }
        catch (StoreException ex) when (stage == "in_commit")
        {
            AppLog.Info("import_failed", $"kind={ex.Failure}");
            await ErrorSafely(UiText.ImportFailedTitle, UiText.ImportStoreFailed(ex.Message), "import_error", stage);
            return null;
        }
        catch (Exception ex)
        {
            LogError("import_error", stage, ex);
            if (stage != "after_commit") await ErrorSafely(UiText.ImportFailedTitle,
                stage == "in_commit" ? UiText.ImportOutcomeUnknown : UiText.ImportUnexpectedFailure, "import_error", stage);
            return outcome;
        }
        finally { IsBusy = false; }
    }

    public async Task<bool> ExportAsync()
    {
        if (!CanRun()) return false;
        IsBusy = true;
        string stage = "before_write", fileName = "";
        bool written = false;
        try
        {
            var path = await _files!.PickExportFileAsync(SnippetFileRules.ExportBaseName(_time.GetLocalNow()));
            if (path is null) { AppLog.Info("export_cancelled"); return false; }
            if (!SnippetFileRules.HasJsonExtension(path))
            {
                AppLog.Info("export_refused", $"reason=NotJsonFile snippets={_store.Snippets.Count}");
                await _dialogs.ShowErrorAsync(UiText.ExportFailedTitle, UiText.ExportNotJsonFile); return false;
            }
            fileName = Path.GetFileName(path);
            var folders = _store.Folders; var snippets = _store.Snippets;
            var result = await _workers.ExportAsync(folders, snippets, _time.GetUtcNow());
            if (result.Refusal is { } refusal)
            {
                AppLog.Info("export_refused", $"reason={refusal} snippets={result.SnippetCount}");
                await _dialogs.ShowErrorAsync(UiText.ExportFailedTitle, refusal == ExportRefusal.TooLarge ? UiText.ExportTooLarge : UiText.ExportTooManySnippets(result.SnippetCount)); return false;
            }
            stage = "in_write";
            await _files.WriteAtomicallyAsync(path, result.Utf8Json!);
            written = true;
            stage = "after_write";
            AppLog.Info("export_done", $"snippets={result.SnippetCount} folders={result.FolderCount} bytes={result.Bytes}");
            await _dialogs.ShowInfoAsync(UiText.ExportCompleteTitle, UiText.ExportComplete(result.SnippetCount, result.FolderCount, fileName));
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (stage == "in_write" && ex is IOException or UnauthorizedAccessException)
        {
            AppLog.Info("export_failed", $"error={ex.GetType().Name} hresult=0x{ex.HResult:X8}");
            await ErrorSafely(UiText.ExportFailedTitle, UiText.ExportWriteFailed(fileName), "export_error", stage); return false;
        }
        catch (Exception ex)
        {
            LogError("export_error", stage, ex);
            if (stage != "after_write") await ErrorSafely(UiText.ExportFailedTitle,
                stage == "in_write" ? UiText.ExportOutcomeUnknown(fileName) : UiText.ExportUnexpectedFailure, "export_error", stage);
            return written;
        }
        finally { IsBusy = false; }
    }

    private async Task ErrorSafely(string title, string message, string eventName, string stage)
    {
        try { await _dialogs.ShowErrorAsync(title, message); }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { LogError(eventName, stage, ex); }
    }
    private static void LogError(string name, string stage, Exception ex) =>
        AppLog.Error(name, null, $"stage={stage} error={ex.GetType().Name} hresult=0x{ex.HResult:X8}");
}
