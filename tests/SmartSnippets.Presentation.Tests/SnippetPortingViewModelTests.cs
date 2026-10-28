using System.Text;
using SmartSnippets.Core.Logging;
using SmartSnippets.Core.Porting;
using SmartSnippets.Core.Store;
using SmartSnippets.Presentation.Tests.Fakes;
using SmartSnippets.Presentation.Text;
using SmartSnippets.Presentation.ViewModels;

namespace SmartSnippets.Presentation.Tests;

[Collection("AppLog")]
public class SnippetPortingViewModelTests : IDisposable
{
    private readonly FakeSnippetStore _store = new();
    private readonly ScriptedDialogService _dialogs = new();
    private readonly FakeSnippetFileService _files = new();
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "ss-porting-log-" + Guid.NewGuid().ToString("N"));
    private SnippetPortingViewModel Vm(PortingWorkers? workers = null) => new(_store, _dialogs, _files, workers: workers);
    public SnippetPortingViewModelTests() { AppLog.Shutdown(); AppLog.Initialize(_directory); }
    public void Dispose() { AppLog.Shutdown(); Directory.Delete(_directory, true); }
    private string Log => File.ReadAllText(Path.Combine(_directory, "app.log"));
    private async Task Refused(ImportRefusal reason)
    {
        _store.ThrowOnImport = true; var vm = Vm(); var r = await vm.ImportAsync();
        Assert.Equal(reason, r!.Failure!.Refusal); Assert.Equal(0, _store.ImportCalls); Assert.Empty(_dialogs.Infos); Assert.Equal(UiText.ImportRefusalMessage(r.Failure), Assert.Single(_dialogs.Errors).Message); Assert.False(vm.IsBusy); Assert.Contains("import_refused reason=" + reason, Log);
    }
    [Fact] public async Task Import_Cancelled_NoRead_NoStoreCall_NoDialog_LogsCancelled() { _files.ImportPath = null; _store.ThrowOnImport = true; Assert.Null(await Vm().ImportAsync()); Assert.Equal(0, _files.Opens); Assert.Equal(0, _store.ImportCalls); Assert.Empty(_dialogs.Errors); Assert.Empty(_dialogs.Infos); Assert.Contains("import_cancelled", Log); }
    [Fact] public async Task Import_NotJsonPath_Refused_NoOpen() { _files.ImportPath = "input.txt"; await Refused(ImportRefusal.NotJsonFile); Assert.Equal(0, _files.Opens); }
    [Fact] public async Task Import_OpenThrowsIOException_Unreadable() { _files.OpenException = new IOException("private"); await Refused(ImportRefusal.Unreadable); }
    [Fact] public async Task Import_OpenThrowsUnauthorized_Unreadable() { _files.OpenException = new UnauthorizedAccessException(); await Refused(ImportRefusal.Unreadable); }
    [Fact] public async Task Import_ReadThrowsOnFirstRead_Unreadable() { _files.ReadException = new IOException(); await Refused(ImportRefusal.Unreadable); }
    [Fact] public async Task Import_ReadThrowsAfterSomeBytes_Unreadable() { _files.Bytes = new byte[2000]; _files.BytesBeforeReadException = 1000; _files.ReadException = new IOException(); await Refused(ImportRefusal.Unreadable); }
    [Fact] public async Task Import_TooLarge_Refused() { _files.Bytes = new byte[SnippetFileRules.MaxImportBytes + 1]; await Refused(ImportRefusal.TooLarge); }
    [Fact] public async Task Import_NotUtf8_Refused() { _files.Bytes = [0xff, 0xfe]; await Refused(ImportRefusal.NotUtf8); }
    [Fact] public async Task Import_InvalidJson_Refused() { _files.Bytes = Encoding.UTF8.GetBytes("{"); await Refused(ImportRefusal.InvalidJson); }
    [Fact] public async Task Import_UnknownFormat_Refused() { _files.Bytes = Encoding.UTF8.GetBytes("{}"); await Refused(ImportRefusal.UnknownFormat); }
    [Fact] public async Task Import_UnsupportedVersion_Refused_MessageNamesVersion() { _files.Bytes = Encoding.UTF8.GetBytes("{\"version\":99,\"snippets\":[]}"); await Refused(ImportRefusal.UnsupportedVersion); Assert.Contains("version 99", _dialogs.Errors[0].Message); }
    [Fact] public async Task Import_MissingEntries_Refused() { _files.Bytes = Encoding.UTF8.GetBytes("{\"format\":\"smart-snippets\",\"version\":1}"); await Refused(ImportRefusal.MissingEntries); }
    [Fact] public async Task Import_TooManyEntries_Refused_MessageNamesCountAndLimit() { _files.Bytes = Encoding.UTF8.GetBytes("{\"version\":1,\"snippets\":[" + string.Join(',', Enumerable.Repeat("42", SnippetFileRules.MaxImportEntries + 1)) + "]}"); await Refused(ImportRefusal.TooManyEntries); Assert.Contains($"{SnippetFileRules.MaxImportEntries:N0}", _dialogs.Errors[0].Message); }
    [Fact] public async Task Import_Success_ShowsInfoWithSummary_TitleComplete() { var r = await Vm().ImportAsync(); Assert.Equal(1, r!.Summary!.Imported); Assert.True(r.Summary.IsBalanced); Assert.Equal((UiText.ImportCompleteTitle, UiText.ImportSummary(r.Summary)), Assert.Single(_dialogs.Infos)); Assert.Equal(1, _store.ImportCalls); }
    [Fact] public async Task Import_ZeroImported_TitleNothingImported() { _store.SeedSnippet("Imported text"); var r = await Vm().ImportAsync(); Assert.Equal(0, r!.Summary!.Imported); Assert.Equal(UiText.ImportNothingTitle, Assert.Single(_dialogs.Infos).Title); }
    [Fact] public async Task Import_StoreException_ShowsStoreFailed_NoInfo() { _store.Failure = new(StoreFailure.DatabaseBusy, "safe"); var vm = Vm(); await vm.ImportAsync(); Assert.Equal(UiText.ImportStoreFailed("safe"), Assert.Single(_dialogs.Errors).Message); Assert.Empty(_dialogs.Infos); Assert.False(vm.IsBusy); }
    [Fact] public async Task Import_UnexpectedReadException_BeforeCommit_NothingImportedMessage() { _files.ReadException = new InvalidDataException("private"); await Vm().ImportAsync(); Assert.Equal(UiText.ImportUnexpectedFailure, Assert.Single(_dialogs.Errors).Message); Assert.Equal(0, _store.ImportCalls); }
    [Fact] public async Task Import_ParseWorkerThrows_BeforeCommit_NothingImportedMessage() { var workers = PortingWorkers.Default with { ParseAsync = (_, _) => Task.FromException<ImportParseResult>(new InvalidOperationException("private")) }; await Vm(workers).ImportAsync(); Assert.Equal(UiText.ImportUnexpectedFailure, Assert.Single(_dialogs.Errors).Message); Assert.Equal(0, _store.ImportCalls); }
    [Fact] public async Task Import_StoreThrowsNonStoreException_InCommit_OutcomeUnknownMessage_NeverNothingImported() { _store.ImportException = new InvalidOperationException("private"); await Vm().ImportAsync(); Assert.Equal(UiText.ImportOutcomeUnknown, Assert.Single(_dialogs.Errors).Message); Assert.DoesNotContain("Nothing was imported", _dialogs.Errors[0].Message); }
    [Fact] public async Task Import_SummaryDialogThrows_AfterCommit_NoErrorDialog_Logged() { _dialogs.InfoException = new InvalidOperationException("private"); await Vm().ImportAsync(); Assert.Single(_store.Snippets); Assert.Empty(_dialogs.Errors); Assert.Contains("import_error stage=after_commit", Log); }
    [Fact] public async Task Import_SubscriberThrows_SummaryStillShown() { _store.Changed += (_, _) => throw new InvalidOperationException("private"); await Vm().ImportAsync(); Assert.Single(_dialogs.Infos); Assert.Single(_store.Snippets); Assert.Empty(_dialogs.Errors); }
    private async Task Busy(bool export, string kind)
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        if (kind == "summary") _dialogs.InfoGate = gate;
        else { _dialogs.ErrorGate = gate; if (kind == "refusal") _files.ImportPath = "bad.txt"; else if (kind == "store") _store.Failure = new(StoreFailure.DatabaseBusy, "safe"); else if (export) _files.WriteException = new InvalidOperationException(); else _files.ReadException = new InvalidDataException(); }
        var vm = Vm(); var task = export ? (Task)vm.ExportAsync() : vm.ImportAsync();
        // Production workers may suspend before reaching the dialog.
        for (int i = 0; _dialogs.Infos.Count + _dialogs.Errors.Count == 0 && i < 500; i++) await Task.Delay(10);
        Assert.Equal(1, _dialogs.Infos.Count + _dialogs.Errors.Count); Assert.True(vm.IsBusy); Assert.False(vm.ImportCommand.CanExecute(null)); Assert.False(vm.ExportCommand.CanExecute(null));
        int picks = _files.ImportPicks + _files.ExportPicks; Assert.Null(await vm.ImportAsync()); Assert.False(await vm.ExportAsync()); Assert.Equal(picks, _files.ImportPicks + _files.ExportPicks);
        gate.SetResult(); await task; Assert.False(vm.IsBusy); Assert.True(vm.ImportCommand.CanExecute(null)); Assert.True(vm.ExportCommand.CanExecute(null));
    }
    [Fact] public Task Import_Busy_DuringSummaryDialog() => Busy(false, "summary");
    [Fact] public Task Import_Busy_DuringRefusalDialog() => Busy(false, "refusal");
    [Fact] public Task Import_Busy_DuringUnexpectedErrorDialog() => Busy(false, "unexpected");
    [Fact] public Task Import_Busy_DuringStoreFailureDialog() => Busy(false, "store");
    [Fact] public Task Export_Busy_DuringUnexpectedErrorDialog() => Busy(true, "unexpected");
    [Fact] public Task Export_Busy_DuringSuccessDialog() => Busy(true, "summary");
    private async Task DuringParse(Action change, int imported, int events)
    {
        var parse = new TaskCompletionSource<ImportParseResult>(); var requested = new TaskCompletionSource();
        var workers = PortingWorkers.Default with { ParseAsync = (_, _) => { requested.SetResult(); return parse.Task; } };
        var vm = Vm(workers); var run = vm.ImportAsync(); await requested.Task.WaitAsync(TimeSpan.FromSeconds(5)); change(); _store.Raised.Clear();
        parse.SetResult(ImportParser.Parse(_files.Bytes, DateTimeOffset.UtcNow)); var result = await run;
        Assert.Equal(imported, result!.Summary!.Imported); Assert.True(result.Summary.IsBalanced); Assert.Equal(events, _store.Raised.Count);
    }
    [Fact] public Task Import_MatchingSnippetCreatedDuringParse_IsSkippedAsDuplicate() => DuringParse(() => _store.SeedSnippet("Imported text"), 0, 0);
    [Fact] public async Task Import_MatchingSnippetDeletedDuringParse_IsImported() { var id = _store.SeedSnippet("Imported text").Id; await DuringParse(() => _store.DeleteSnippet(id), 1, 1); }
    [Fact] public async Task Import_FolderRenamedDuringParse_ResolvesToCurrentFolder() { var f = _store.SeedFolder("Old"); await DuringParse(() => _store.RenameFolder(f.Id, "New folder"), 1, 1); Assert.Single(_store.Folders); Assert.Equal(f.Id, _store.Snippets[0].Snippet.FolderId); }
    [Fact] public async Task Import_FolderCreatedDuringParse_Reused() { await DuringParse(() => _store.SeedFolder("New folder"), 1, 1); Assert.Single(_store.Folders); }
    [Fact] public void Page_FilesNull_CommandsDisabled() { using var page = new SnippetsPageViewModel(_store, _dialogs); Assert.False(page.Porting.ImportCommand.CanExecute(null)); Assert.False(page.Porting.ExportCommand.CanExecute(null)); }
    [Fact] public async Task Page_PortingSharesStoreAndDialogs() { using var page = new SnippetsPageViewModel(_store, _dialogs, files: _files); await page.Porting.ImportAsync(); Assert.Single(_store.Snippets); Assert.Single(_dialogs.Infos); Assert.Single(page.List.Items); }
    [Fact] public async Task Export_Cancelled_NoWrite() { _files.ExportPath = null; Assert.False(await Vm().ExportAsync()); Assert.Empty(_files.Writes); Assert.Empty(_dialogs.Infos); Assert.Empty(_dialogs.Errors); }
    [Fact] public async Task Export_NotJson_Refused_NoWrite() { _files.ExportPath = "output.txt"; Assert.False(await Vm().ExportAsync()); Assert.Empty(_files.Writes); Assert.Equal(UiText.ExportNotJsonFile, Assert.Single(_dialogs.Errors).Message); }
    [Fact] public async Task Export_Success_WritesNativeBytes_ShowsCountsAndFileName() { _store.SeedSnippet("body", folder: "Folder"); Assert.True(await Vm().ExportAsync()); var write = Assert.Single(_files.Writes); Assert.Equal(ImportFormat.Native, ImportParser.Parse(write.Bytes, DateTimeOffset.UtcNow).Parsed!.Format); Assert.Equal(UiText.ExportComplete(1, 1, "output.json"), Assert.Single(_dialogs.Infos).Message); }
    [Fact] public async Task Export_WriteThrows_ShowsWriteFailed() { _files.WriteException = new IOException("private"); Assert.False(await Vm().ExportAsync()); Assert.Equal(UiText.ExportWriteFailed("output.json"), Assert.Single(_dialogs.Errors).Message); }
    private async Task ExportRefused(ExportRefusal refusal) { var workers = PortingWorkers.Default with { ExportAsync = (_, _, _) => Task.FromResult(new NativeExportResult(null, refusal, 0, SnippetFileRules.MaxImportEntries + 1, 0)) }; Assert.False(await Vm(workers).ExportAsync()); Assert.Empty(_files.Writes); Assert.Equal(refusal == ExportRefusal.TooLarge ? UiText.ExportTooLarge : UiText.ExportTooManySnippets(SnippetFileRules.MaxImportEntries + 1), Assert.Single(_dialogs.Errors).Message); }
    [Fact] public Task Export_TooLarge_Refused_NoWriteCall() => ExportRefused(ExportRefusal.TooLarge);
    [Fact] public Task Export_TooManySnippets_Refused_NoWriteCall() => ExportRefused(ExportRefusal.TooManySnippets);
    [Fact] public async Task Export_UnexpectedSerializeException_BeforeWrite_NoFileWrittenMessage() { var w = PortingWorkers.Default with { ExportAsync = (_, _, _) => Task.FromException<NativeExportResult>(new InvalidOperationException()) }; Assert.False(await Vm(w).ExportAsync()); Assert.Equal(UiText.ExportUnexpectedFailure, Assert.Single(_dialogs.Errors).Message); Assert.Empty(_files.Writes); }
    [Fact] public async Task Export_NonIoWriteException_InWrite_OutcomeUnknownMessage() { _files.WriteException = new InvalidOperationException("private"); Assert.False(await Vm().ExportAsync()); Assert.Equal(UiText.ExportOutcomeUnknown("output.json"), Assert.Single(_dialogs.Errors).Message); }
    [Fact] public async Task Export_SuccessDialogThrows_AfterWrite_NoErrorDialog_Logged() { _dialogs.InfoException = new InvalidOperationException("private"); Assert.True(await Vm().ExportAsync()); Assert.Single(_files.Writes); Assert.Empty(_dialogs.Errors); Assert.Contains("export_error stage=after_write", Log); }
    [Fact] public async Task Export_SuggestedBaseName_HasDateNoExtension() { await Vm().ExportAsync(); Assert.Matches(@"^smart-snippets-\d{4}-\d{2}-\d{2}$", _files.SuggestedBaseName!); }
    [Fact] public async Task Logs_SuccessAndFailure_NoPathNoFileNameNoText()
    {
        _files.ImportPath = "PRIVATE-PATH/PRIVATE-FILE.json"; _files.Bytes = Encoding.UTF8.GetBytes("{\"version\":1,\"snippets\":[{\"text\":\"PRIVATE-TEXT\",\"label\":\"PRIVATE-LABEL\",\"folder\":\"PRIVATE-FOLDER\",\"tags\":[\"PRIVATE-TAG\"]}]}");
        await Vm().ImportAsync(); Assert.Contains("import_done", Log); _store.Failure = new(StoreFailure.DatabaseBusy, "safe", new IOException("PRIVATE-EXCEPTION")); await Vm().ImportAsync(); Assert.Contains("import_failed", Log);
        foreach (var sentinel in new[] { "PRIVATE-PATH", "PRIVATE-FILE", "PRIVATE-TEXT", "PRIVATE-LABEL", "PRIVATE-FOLDER", "PRIVATE-TAG", "PRIVATE-EXCEPTION" }) Assert.DoesNotContain(sentinel, Log);
    }
}
