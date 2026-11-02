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
        using var list = new SnippetListViewModel(store);
        var main = Assert.Single(list.Items);
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
        list.Query = body;
        Assert.Empty(list.Items);
        list.Query = "Safe Folder tag";
        Assert.Single(list.Items);
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
        using var list = new SnippetListViewModel(store);
        var before = Assert.Single(list.Items);
        store.UpdateSnippet(snippet.Id, new(snippet.Text, snippet.Label, true, null, "", IsPassword: true));
        var after = Assert.Single(list.Items);
        Assert.NotSame(before, after);
        Assert.NotEqual(before.GetHashCode(), after.GetHashCode());
        Assert.Equal(before.ContentText, after.ContentText);
        Assert.NotEqual(before, after);
        Assert.Equal(after, after with { });
    }
}
