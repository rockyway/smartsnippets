using SmartSnippets.Core.Domain;
using SmartSnippets.Core.Store;
using SmartSnippets.Presentation.Tests.Fakes;
using SmartSnippets.Presentation.ViewModels;

namespace SmartSnippets.Presentation.Tests;

public sealed class PasswordEditorTests
{
    private readonly FakeSnippetStore _store = new();
    private readonly ScriptedDialogService _dialogs = new();
    private SnippetEditorViewModel Edit(long? id = null) => new(_store, _dialogs, id);

    [Fact]
    public void ToggleClearsOnlyAutomaticLabel()
    {
        using var vm = Edit();
        vm.Text = "body";
        vm.Label = "remembered label";
        vm.IsLabelCustom = false;
        vm.IsPassword = true;
        Assert.Empty(vm.Label);
        Assert.Empty(vm.Suggestion);
        Assert.Empty(vm.AutoLabelHint);
        Assert.Equal("Label", vm.LabelPlaceholder);
        Assert.True(vm.IsLabelCustom);
        Assert.False(vm.CanChangeLabelMode);
        vm.IsLabelCustom = false;
        Assert.True(vm.IsLabelCustom);
        Assert.Empty(vm.Label);
        vm.IsPassword = false;
        vm.IsLabelCustom = true;
        Assert.Equal("body", vm.Label); // stale remembered label cannot return
        vm.Label = "explicit label";
        vm.IsPassword = true;
        Assert.Equal("explicit label", vm.Label);
        vm.IsPassword = false;
        Assert.True(vm.IsLabelCustom);
        Assert.Equal("body", vm.Text);
    }

    [Fact]
    public async Task ClearingLabelStaysManualAndInvalid()
    {
        using var vm = Edit();
        vm.Text = "body";
        vm.TagsInput = "tag";
        vm.IsPassword = true;
        vm.Label = "safe label";
        vm.Label = "";
        vm.IsLabelCustom = false;
        Assert.True(vm.IsLabelCustom);
        Assert.Empty(vm.Label);
        Assert.False(await vm.SaveAsync());
        Assert.NotEmpty(vm.LabelError);
        Assert.Empty(_store.Snippets);
        Assert.True(vm.IsDirty);
        Assert.Equal("body", vm.Text);
    }

    [Fact]
    public void ModeOnlyEditIsDirtyAndDiscardRestoresIt()
    {
        var saved = _store.SeedSnippet("body", "safe label");
        using var vm = Edit(saved.Id);
        vm.IsPassword = true;
        Assert.True(vm.IsDirty);
        vm.ResetToSaved();
        Assert.False(vm.IsPassword);
        Assert.False(vm.IsDirty);
        Assert.Equal(saved.Text, vm.Text);
        Assert.Equal(saved.Label, vm.Label);
    }

    [Fact]
    public void UntouchedAutomaticLabelCheckUncheckReturnsToClean()
    {
        var saved = _store.SeedSnippet("body", tags: "tag");
        using var vm = Edit(saved.Id);
        vm.IsPassword = true;
        vm.IsPassword = false;
        Assert.False(vm.IsDirty);
        Assert.False(vm.IsLabelCustom);
        Assert.Equal(saved.Label, vm.Label);
        Assert.Equal(saved.Text, vm.Text);
    }

    [Fact]
    public void LoadCannotEmitPasswordSuggestion()
    {
        var saved = _store.SeedSnippet("ordinary");
        using var vm = Edit(saved.Id);
        var observed = 0;
        vm.PropertyChanged += (_, _) =>
        {
            if (!vm.IsPassword) return;
            observed++;
            Assert.True(vm.IsLabelCustom);
            Assert.Empty(vm.Suggestion);
            Assert.DoesNotContain("secret", vm.LabelPlaceholder);
            Assert.Empty(vm.AutoLabelHint);
        };
        Assert.True(_store.UpdateSnippet(saved.Id, new("secret body", "safe label", true, null, "secret tag", true)).Success);
        Assert.True(observed > 0);
        Assert.True(vm.IsPassword);
        Assert.Equal("safe label", vm.Label);
        Assert.False(vm.IsDirty);
        vm.Text = "another secret";
        vm.ResetToSaved();
        Assert.Equal("secret body", vm.Text);
        Assert.Empty(vm.Suggestion);
    }

    [Fact]
    public void StoredPasswordWithAutomaticFlagKeepsManualLabel()
    {
        var saved = _store.SeedSnippet("secret", "safe label");
        // A legacy/inconsistent stored row must not activate suggestion fallback.
        var other = new FakeSnippetStore();
        other.SeedStoredSnippet(saved with { IsPassword = true, IsLabelCustom = false });
        using var vm = new SnippetEditorViewModel(other, new ScriptedDialogService(), saved.Id);
        Assert.True(vm.IsPassword);
        Assert.True(vm.IsLabelCustom);
        Assert.Equal("safe label", vm.Label);
        vm.Text = "edited";
        vm.PropertyChanged += (_, _) =>
        {
            if (vm.IsPassword) Assert.True(vm.IsLabelCustom);
        };
        vm.ResetToSaved();
        Assert.Equal("safe label", vm.Label);
        Assert.Empty(vm.Suggestion);
    }

    [Theory]
    [InlineData("  first\r\nsecond\rthird\nlast  ")]
    [InlineData("\n\r\n\t Unicode \ud83d\udd12 \t\n")]
    public async Task MultilineMetadataSavePreservesExactBody(string body)
    {
        var saved = _store.CreateSnippet(new(body, "safe label", true, null, "", true)).Value!;
        using var vm = Edit(saved.Id);
        Assert.True(vm.HasMultilineText);
        Assert.True(vm.IsMultilinePassword);
        Assert.False(vm.CanEditPassword);
        vm.IsPassword = false;
        vm.IsPassword = true;
        vm.TagsInput = "metadata";
        Assert.True(await vm.SaveAsync());
        Assert.True(vm.IsPassword);
        Assert.False(vm.IsDirty);
        Assert.Equal(body, vm.Text);
        Assert.Equal(body, Assert.Single(_store.Snippets).Snippet.Text);
        Assert.True(Assert.Single(_store.Snippets).Snippet.IsPassword);
        using var reloaded = Edit(saved.Id);
        Assert.Equal(body, reloaded.Text);
        Assert.True(reloaded.IsPassword);
    }

    [Fact]
    public async Task FailedConversionKeepsPasswordDraftAndCommittedOrdinaryMode()
    {
        var saved = _store.SeedSnippet("exact body", "safe label");
        using var vm = Edit(saved.Id);
        vm.IsPassword = true;
        _store.Failure = new StoreException(StoreFailure.DatabaseBusy, "The snippet database is busy. Please try again.");
        Assert.False(await vm.SaveAsync());
        Assert.True(vm.HasSaveFailure);
        Assert.True(vm.IsPassword);
        Assert.True(vm.IsDirty);
        Assert.Equal(saved.Text, vm.Text);
        Assert.False(Assert.Single(_store.Snippets).Snippet.IsPassword);
        _store.Failure = null;
        Assert.True(await vm.SaveAsync());
        Assert.True(Assert.Single(_store.Snippets).Snippet.IsPassword);
        Assert.False(vm.HasSaveFailure);
        Assert.False(vm.IsDirty);
    }

    [Fact]
    public async Task ModeSaveAndFailureKeepDraftAndBaseline()
    {
        using var vm = Edit();
        vm.Text = "single line";
        vm.IsPassword = true;
        vm.Label = "safe label";
        Assert.True(vm.CanEditPassword);
        Assert.True(await vm.SaveAsync());
        Assert.True(Assert.Single(_store.Snippets).Snippet.IsPassword);
        vm.IsPassword = false;
        vm.ResetToSaved();
        Assert.True(vm.IsPassword);
        Assert.False(vm.IsDirty);
        vm.Label = "";
        Assert.False(await vm.SaveAsync());
        Assert.True(vm.IsPassword);
        Assert.Equal("single line", vm.Text);
        Assert.True(vm.IsDirty);
    }
}
