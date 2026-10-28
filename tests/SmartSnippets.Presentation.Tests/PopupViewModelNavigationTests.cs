using SmartSnippets.Core.Settings;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Presentation.Tests.Fakes;
using SmartSnippets.Presentation.ViewModels;

namespace SmartSnippets.Presentation.Tests;

public sealed class PopupViewModelNavigationTests
{
    private readonly FakeSnippetStore _store = new();
    private readonly FakePopupHost _host = new();
    private readonly FakeInsertionService _insertion = new();
    private static readonly InsertionTarget Target = new(1, 2, 3, "app");
    private PopupViewModel Create(AppSettings? initial = null) => new(_store, new MemorySettingsStore(), initial ?? new(), _insertion, new FakeClipboardService(), new FakeElevation(), _host, new ScriptedDialogService(), new RecordingNotifier());
    [Fact]
    public void Open_Fresh_ClearsQuery_SelectsFirstSnippet()
    {
        _store.SeedSnippet("a"); _store.SeedSnippet("b"); using var vm = Create(); vm.Open(Target); vm.MoveSelection(1); vm.Query = "b"; vm.Open(Target);
        Assert.Equal("", vm.Query); Assert.Equal(0, vm.SelectedIndex);
    }
    [Fact]
    public void Open_FolderLayout_SelectsFirstSnippetNotHeading()
    {
        _store.SeedSnippet("a", "a", "Folder"); using var vm = Create(new AppSettings { PopupLayout = PopupLayout.Folder }); vm.Open(Target); Assert.Equal(1, vm.SelectedIndex);
    }
    [Fact]
    public void MoveDown_SkipsHeading_AndClamps()
    {
        _store.SeedSnippet("a", "a", "A"); _store.SeedSnippet("b", "b", "B"); using var vm = Create(new AppSettings { PopupLayout = PopupLayout.Folder }); vm.Open(Target);
        vm.MoveSelection(-1); Assert.Equal(1, vm.SelectedIndex); vm.MoveSelection(1); Assert.Equal(3, vm.SelectedIndex); vm.MoveSelection(1); Assert.Equal(3, vm.SelectedIndex); vm.MoveSelection(-1); Assert.Equal(1, vm.SelectedIndex);
    }
    [Fact]
    public void Move_Empty_NoOp() { using var vm = Create(); vm.Open(Target); vm.MoveSelection(1); Assert.Equal(-1, vm.SelectedIndex); }
    [Fact]
    public void QueryChange_SelectsFirst()
    {
        _store.SeedSnippet("match a", "a"); _store.SeedSnippet("match b", "b"); using var vm = Create(); vm.Open(Target); vm.MoveSelection(1); vm.Query = "match"; Assert.Equal(0, vm.SelectedIndex);
    }
    [Fact]
    public void Close_HidesHost() { using var vm = Create(); vm.Open(Target); vm.Close(); Assert.Equal("Hide", _host.Calls[^1]); }
    [Fact]
    public void Deactivated_Closes() { using var vm = Create(); vm.Open(Target); vm.OnDeactivated(); Assert.Equal("Hide", _host.Calls[^1]); }
    [Fact]
    public void Deactivated_WhileEditorOpen_Ignored()
    {
        using var vm = Create(); vm.Open(Target); vm.AddSnippetCommand.Execute(null); vm.OnDeactivated(); Assert.DoesNotContain("Hide", _host.Calls);
    }
    [Fact]
    public async Task Deactivated_WhileInserting_Ignored()
    {
        _store.SeedSnippet("a"); _insertion.Pending = new(); using var vm = Create(); vm.Open(Target); var pending = vm.InsertSelectedAsync();
        var hides = _host.Calls.Count(c => c == "Hide"); vm.OnDeactivated(); Assert.Equal(hides, _host.Calls.Count(c => c == "Hide"));
        _insertion.Pending.SetResult(new(InsertionOutcome.Inserted, "")); await pending;
    }
    [Fact]
    public void OpenMainWindow_CallsHost() { using var vm = Create(); vm.Open(Target); vm.OpenMainWindowCommand.Execute(null); Assert.Equal("OpenMainWindow", _host.Calls[^1]); }
}
