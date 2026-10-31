using SmartSnippets.Uno.Tests.Composition;

namespace SmartSnippets.Uno.Tests.Acceptance;

/// <summary>Headless assertion-driver tests only; these do not claim rendered event delivery.</summary>
public sealed class UiAssertDriverTests
{
    [Fact]
    public async Task FreshHostCannotPassNewSaveOrCancelWithoutCallbacks()
    {
        using var fixture = new HostFixture(); await fixture.Start();
        UiAssertDriver.RequireFresh(fixture.Host);
        Assert.Throws<InvalidOperationException>(() => UiAssertDriver.RequireNew(fixture.Host));
        Assert.Throws<InvalidOperationException>(() => UiAssertDriver.RequireSaved(fixture.Host));
        Assert.Throws<InvalidOperationException>(() => UiAssertDriver.RequireCancelled(fixture.Host));
        Assert.Empty(fixture.Host.Store.Snippets);
    }

    [Fact]
    public async Task FolderAssertionsRejectMissingMutationsAndVerifyPersistedUnfiledSnippet()
    {
        using var fixture = new HostFixture(); await fixture.Start();
        var host = fixture.Host;
        Assert.Throws<InvalidOperationException>(() => UiAssertDriver.RequireFolderCreated(host));
        Assert.Throws<InvalidOperationException>(() => UiAssertDriver.RequireFolderDeleted(host, 0));
        Assert.Empty(host.Store.Folders);
        host.Folders.NewFolderName = UiAssertDriver.FolderName;
        await host.Folders.CreateAsync(); // Headless test setup, not rendered evidence.
        var id = UiAssertDriver.RequireFolderCreated(host);
        Assert.Throws<InvalidOperationException>(() => UiAssertDriver.RequireFolderAttached(host, id));
        var snippetId = FolderSceneSeed.AttachSyntheticSnippet(host, id);
        UiAssertDriver.RequireFolderAttached(host, id);
        Assert.Throws<InvalidOperationException>(() => UiAssertDriver.RequireFolderRenamed(host, id));
        var row = host.Folders.Rows.Single(r => r.Id == id);
        host.Folders.BeginRename(row);
        row.EditName = UiAssertDriver.RenamedFolderName;
        Assert.True(await host.Folders.CommitRenameAsync(row));
        UiAssertDriver.RequireFolderRenamed(host, id);
        Assert.Throws<InvalidOperationException>(() => UiAssertDriver.RequireFolderDeleted(host, snippetId));
        Assert.True(host.Store.DeleteFolder(id).Success); // Assertion unit test only.
        Assert.Throws<InvalidOperationException>(() => UiAssertDriver.RequireFolderDeleted(host, snippetId + 1));
        UiAssertDriver.RequireFolderDeleted(host, snippetId);
        Assert.True(host.Store.RecordUsage(host.Store.Snippets.Single().Snippet.Id).Success);
        Assert.Throws<InvalidOperationException>(() => UiAssertDriver.RequireFolderDeleted(host, snippetId));
    }

    [Fact]
    public async Task AssertionDriverChecksReloadAndKeepsDirtyCancellationUnpersisted()
    {
        using var fixture = new HostFixture(); await fixture.Start();
        var host = fixture.Host;
        await host.Snippets.NewAsync();
        UiAssertDriver.RequireNew(host);
        var editor = host.Snippets.Editor!;
        editor.Text = UiAssertDriver.SavedText;
        Assert.Throws<InvalidOperationException>(() => UiAssertDriver.RequireSaved(host));
        Assert.True(await editor.SaveAsync());
        UiAssertDriver.RequireSaved(host);
        Assert.Throws<InvalidOperationException>(() => UiAssertDriver.RequireFresh(host));
        Assert.Throws<InvalidOperationException>(() => UiAssertDriver.RequireCancelled(host));
        editor.Text = UiAssertDriver.DirtyText;
        UiAssertDriver.RequireCancelled(host);
        Assert.True(editor.IsDirty);
        host.Store.Load();
        Assert.Equal(UiAssertDriver.SavedText, host.Store.Snippets.Single().Snippet.Text);
        editor.Text = "wrong synthetic text";
        Assert.Throws<InvalidOperationException>(() => UiAssertDriver.RequireCancelled(host));
    }
}
