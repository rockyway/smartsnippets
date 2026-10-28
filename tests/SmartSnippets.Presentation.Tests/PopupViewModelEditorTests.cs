using SmartSnippets.Core.Settings;
using SmartSnippets.Core.Store;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Presentation.Tests.Fakes;
using SmartSnippets.Presentation.Text;
using SmartSnippets.Presentation.ViewModels;

namespace SmartSnippets.Presentation.Tests;

public sealed class PopupViewModelEditorTests
{
    private readonly FakeSnippetStore _store = new();
    private readonly FakePopupHost _host = new();
    private readonly FakeInsertionService _insertion = new();
    private readonly ScriptedDialogService _dialogs = new();
    private static readonly InsertionTarget Target = new(1, 2, 3, "Notepad");
    private PopupViewModel Create() { var vm = new PopupViewModel(_store, new MemorySettingsStore(), new AppSettings(), _insertion, new FakeClipboardService(), new FakeElevation(), _host, _dialogs, new RecordingNotifier()); vm.Open(Target); return vm; }

    [Fact]
    public void AddSnippet_OpensEditor_TargetLineShowsDisplayName()
    {
        using var vm = Create(); vm.AddSnippetCommand.Execute(null); Assert.True(vm.IsEditorOpen); Assert.Equal(UiText.TargetKept("Notepad"), vm.EditorTargetLine);
        Assert.Equal("Focus:Editor", _host.Calls[^1]); Assert.Equal(Target, vm.Target);
    }
    [Fact]
    public async Task Save_SelectsNewSnippet_ClosesEditor_FocusSearch_ShowsStatus()
    {
        using var vm = Create(); vm.AddSnippetCommand.Execute(null); vm.Editor!.Text = "new"; await vm.SaveEditorAsync();
        Assert.False(vm.IsEditorOpen); Assert.Equal("new", _store.Snippets.Single().Snippet.Text); Assert.Equal(_store.Snippets[0].Snippet.Id, vm.Items[vm.SelectedIndex].SnippetId);
        Assert.Equal("Focus:Search", _host.Calls[^1]); Assert.Equal(UiText.SnippetAddedReady, vm.StatusText);
    }
    [Theory]
    [InlineData("new", "new")]
    [InlineData("absent", "")]
    public async Task Save_ClearsQuery_OnlyWhenItWouldHide(string query, string expected)
    {
        using var vm = Create(); vm.Query = query; vm.AddSnippetCommand.Execute(null); vm.Editor!.Text = "new text"; await vm.SaveEditorAsync(); Assert.Equal(expected, vm.Query);
    }
    [Fact]
    public async Task Save_Invalid_StaysOpen_InlineError()
    {
        using var vm = Create(); vm.AddSnippetCommand.Execute(null); await vm.SaveEditorAsync(); Assert.True(vm.IsEditorOpen); Assert.NotEmpty(vm.Editor!.TextError);
    }
    [Fact]
    public async Task Save_StoreException_StaysOpen_InlineFailure()
    {
        using var vm = Create(); vm.AddSnippetCommand.Execute(null); vm.Editor!.Text = "draft"; _store.Failure = new StoreException(StoreFailure.DatabaseUnavailable, "failure");
        await vm.SaveEditorAsync(); Assert.True(vm.IsEditorOpen); Assert.True(vm.Editor!.HasSaveFailure);
    }
    [Fact]
    public async Task Cancel_RestoresQueryAndSelection()
    {
        _store.SeedSnippet("match one", "A"); var b = _store.SeedSnippet("match two", "B"); using var vm = Create(); vm.Query = "match"; vm.MoveSelection(1);
        vm.AddSnippetCommand.Execute(null); await vm.CancelEditorAsync(false); Assert.Equal("match", vm.Query); Assert.Equal(b.Id, vm.Items[vm.SelectedIndex].SnippetId);
    }
    [Fact]
    public async Task Esc_Clean_ClosesWithoutDialog()
    {
        using var vm = Create(); vm.AddSnippetCommand.Execute(null); await vm.CancelEditorAsync(true); Assert.False(vm.IsEditorOpen); Assert.Empty(_dialogs.Confirms);
    }
    [Fact]
    public void Open_WhileEditorOpen_Resumed_KeepsTargetAndEditor()
    {
        using var vm = Create(); vm.AddSnippetCommand.Execute(null); var editor = vm.Editor; Assert.Equal(PopupOpenMode.Resumed, vm.Open(new(10, 20, 30, "other"))); Assert.Same(editor, vm.Editor); Assert.Equal(Target, vm.Target);
    }
    [Fact]
    public void Dispose_DisposesEditor_Unsubscribes()
    {
        var vm = Create(); vm.AddSnippetCommand.Execute(null); Assert.Equal(2, _store.ChangedSubscriberCount); vm.Dispose(); Assert.Equal(0, _store.ChangedSubscriberCount);
    }
}
