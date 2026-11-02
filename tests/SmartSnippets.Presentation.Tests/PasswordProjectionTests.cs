using SmartSnippets.Core.Settings;
using SmartSnippets.Presentation.Tests.Fakes;
using SmartSnippets.Presentation.Text;
using SmartSnippets.Presentation.ViewModels;

namespace SmartSnippets.Presentation.Tests;

public sealed class PasswordProjectionTests
{
    [Theory]
    [InlineData("x", false)]
    [InlineData("secret\r\nsecond line\n", true)]
    public void NoBodyInEitherRowProjection(string body, bool grouped)
    {
        var store = new FakeSnippetStore();
        var folder = store.SeedFolder("Folder");
        store.CreateSnippet(new(body, "Safe label", true, folder.Id, "tag", IsPassword: true));
        var main = SnippetRowViewModel.From(Assert.Single(store.Snippets));
        using var popup = new PopupViewModel(store, new MemorySettingsStore(),
            new() { PopupLayout = grouped ? PopupLayout.Folder : PopupLayout.Flat },
            new FakeInsertionService(), new FakeClipboardService(), new FakeElevation(),
            new FakePopupHost(), new ScriptedDialogService(), new RecordingNotifier());
        var row = Assert.Single(popup.Items, i => i.Kind == PopupItemKind.Snippet);
        Assert.True(main.IsPassword);
        Assert.True(row.IsPassword);
        Assert.Equal("••••••••", main.Preview);
        Assert.Equal(SnippetDisplayPolicy.PasswordMask, main.ContentText);
        Assert.Equal(main.ContentText, row.ContentText);
        Assert.Empty(row.Preview);
        popup.Query = body;
        Assert.True(popup.IsEmpty);
        popup.Query = "Safe Folder tag";
        Assert.False(popup.IsEmpty);
    }

    [Fact]
    public void ModeTransitionReplacesRows()
    {
        var store = new FakeSnippetStore();
        var snippet = store.SeedSnippet(SnippetDisplayPolicy.PasswordMask, "Label");
        var before = SnippetRowViewModel.From(Assert.Single(store.Snippets));
        store.UpdateSnippet(snippet.Id, new(snippet.Text, snippet.Label, true, null, "", IsPassword: true));
        var after = SnippetRowViewModel.From(Assert.Single(store.Snippets));
        Assert.Equal(before.ContentText, after.ContentText);
        Assert.NotEqual(before, after);
        Assert.Equal(after, after with { });
    }
}
