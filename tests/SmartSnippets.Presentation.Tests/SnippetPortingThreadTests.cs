using SmartSnippets.Core.Porting;
using SmartSnippets.Presentation.Tests.Fakes;
using SmartSnippets.Presentation.ViewModels;

namespace SmartSnippets.Presentation.Tests;

public class SnippetPortingThreadTests
{
    private static Task Hop(bool asynchronous) => QueueSynchronizationContext.RunOnOwnerThread(async () =>
    {
        int owner = Environment.CurrentManagedThreadId, storeThread = 0, eventThread = 0;
        var store = new FakeSnippetStore { OnImport = () => storeThread = Environment.CurrentManagedThreadId };
        store.Changed += (_, _) => eventThread = Environment.CurrentManagedThreadId;
        var files = new FakeSnippetFileService { Suspend = true };
        var tcs = new TaskCompletionSource<ImportParseResult>(asynchronous ? TaskCreationOptions.RunContinuationsAsynchronously : TaskCreationOptions.None);
        var requested = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var workers = PortingWorkers.Default with { ParseAsync = (_, _) => { requested.SetResult(); return tcs.Task; } };
        var vm = new SnippetPortingViewModel(store, new ScriptedDialogService(), files, workers: workers);
        var import = vm.ImportAsync(); await requested.Task.WaitAsync(TimeSpan.FromSeconds(5)); Assert.False(import.IsCompleted);
        await Task.Run(() => tcs.SetResult(ImportParser.Parse(files.Bytes, DateTimeOffset.UtcNow)));
        await import; Assert.Equal(owner, storeThread); Assert.Equal(owner, eventThread);
    });
    [Fact] public Task Import_StoreCallAndChanged_OnOwnerThread_AfterSuspendedReadAndHeldParse() => Hop(true);
    [Fact] public Task Import_ParseCompletesOnWorker_StillCommitsOnOwner() => Hop(false);
    [Fact] public Task Export_SerializeHeld_WriteAndDialogOnOwner() => QueueSynchronizationContext.RunOnOwnerThread(async () =>
    {
        int owner = Environment.CurrentManagedThreadId, writeThread = 0, dialogThread = 0;
        var store = new FakeSnippetStore(); var files = new FakeSnippetFileService { Suspend = true, OnWrite = () => writeThread = Environment.CurrentManagedThreadId };
        var dialogs = new ScriptedDialogService { OnInfo = () => dialogThread = Environment.CurrentManagedThreadId };
        var tcs = new TaskCompletionSource<NativeExportResult>(); var requested = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var workers = PortingWorkers.Default with { ExportAsync = (_, _, _) => { requested.SetResult(); return tcs.Task; } };
        var task = new SnippetPortingViewModel(store, dialogs, files, workers: workers).ExportAsync(); await requested.Task.WaitAsync(TimeSpan.FromSeconds(5)); Assert.False(task.IsCompleted);
        await Task.Run(() => tcs.SetResult(NativeExport.Write(store.Folders, store.Snippets, DateTimeOffset.UtcNow))); Assert.True(await task); Assert.Equal(owner, writeThread); Assert.Equal(owner, dialogThread);
    });
    [Fact] public Task PortingWorkers_Create_RunsOffCallerThread() => QueueSynchronizationContext.RunOnOwnerThread(async () =>
    {
        int owner = Environment.CurrentManagedThreadId; var threads = new List<int>(); var workers = PortingWorkers.Create(() => threads.Add(Environment.CurrentManagedThreadId));
        await workers.ParseAsync("{}"u8.ToArray(), DateTimeOffset.UtcNow); await workers.ExportAsync([], [], DateTimeOffset.UtcNow);
        Assert.Equal(2, threads.Count); Assert.All(threads, id => Assert.NotEqual(owner, id));
    });
    [Fact] public async Task Import_WhileEditorDirty_EditorKeepsEdits_NewFolderInChoices()
    {
        var store = new FakeSnippetStore(); var dialogs = new ScriptedDialogService(); using var page = new SnippetsPageViewModel(store, dialogs, files: new FakeSnippetFileService());
        page.NewCommand.Execute(null); page.Editor!.Text = "Unsaved"; await page.Porting.ImportAsync(); Assert.Equal("Unsaved", page.Editor.Text); Assert.True(page.Editor.IsDirty); Assert.Contains(page.Editor.FolderChoices, f => f.Name == "New folder"); Assert.Empty(dialogs.SaveDiscardPrompts);
    }
}
