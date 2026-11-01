using SmartSnippets.Presentation.Services;
using SmartSnippets.Uno.Platforms.Windows;
using SmartSnippets.Uno.Platforms.Windows.AcceptanceNative;
using SmartSnippets.Uno.Tests.Composition;
using Xunit;

namespace SmartSnippets.Uno;

public sealed partial class PositiveActualHostTests
{
    [Theory]
    [InlineData("popup-copy")] [InlineData("editor-copy")] [InlineData("footer-copy")]
    public Task CapturedCopy_ActualConsumerCommitsOnceAndNeverCreditsUsage(string scene) => CandidateHostThread.RunAsync(async () =>
    {
        using var f = new Fixture(scene: scene);
        await f.Host.StartAsync(true, new(null, false, null));
        var id = f.Host.Store.CreateSnippet(new("AΩ中🙂e\u0301\nsecond line\n", "Synthetic", true, null, "")).Value!.Id;
        f.Broker.Bind(f.Operations, f.Operations.Id); f.Broker.Admit();
        Assert.True(f.Host.ShowCapturedPopup());
        await PopupCopyConsumer.RunAsync(scene, f.Host, id, f.root);
        Assert.Equal("AΩ中🙂e\u0301\nsecond line\n", f.Operations.Text);
        Assert.Equal(1, f.Broker.CopyCommits); Assert.Equal(1, f.Operations.CopyWaits);
        Assert.Equal(1, f.Broker.CaptureCalls); Assert.Equal(0, f.Broker.PasteCalls);
        f.Host.Store.Load();
        Assert.Equal(0, f.Host.Store.Snippets.Single().Snippet.UsageCount);
        Assert.Equal("Synthetic", f.Host.Store.Snippets.Single().Snippet.Label); // failed editor save did not persist
        await f.Host.ExitAsync();
    });

    [Theory]
    [InlineData("popup-copy")] [InlineData("editor-copy")] [InlineData("footer-copy")]
    public Task CapturedCopy_PreviewAndRevocationRefuseBeforeConsumerCommit(string scene) => CandidateHostThread.RunAsync(async () =>
    {
        using var f = new Fixture(scene: scene);
        await f.Host.StartAsync(true, new(null, false, null));
        var id = f.Host.Store.CreateSnippet(new("synthetic", "Synthetic", true, null, "")).Value!.Id;
        f.Broker.Bind(f.Operations, f.Operations.Id); f.Broker.Admit();
        f.Host.ShowPreview();
        await Assert.ThrowsAsync<InvalidOperationException>(() => PopupCopyConsumer.RunAsync(scene, f.Host, id, f.root));
        f.Boundary.Popup.Hide();
        Assert.True(f.Host.ShowCapturedPopup());
        f.Capability.Revoke();
        await Assert.ThrowsAsync<InvalidOperationException>(() => PopupCopyConsumer.RunAsync(scene, f.Host, id, f.root));
        Assert.Equal(0, f.Broker.CopyCommits); Assert.Equal(0, f.Broker.PasteCalls);
        Assert.Equal(0, f.Host.Store.Snippets.Single().Snippet.UsageCount);
        await f.Host.Popup.CancelEditorAsync(false);
        await f.Host.ExitAsync();
    });

    [Fact]
    public Task ContextInsert_InvocationRechecksPermissionAndUsesTrackedSingleFlight() => CandidateHostThread.RunAsync(async () =>
    {
        using var f = new Fixture();
        await f.Host.StartAsync(true, new(null, false, null));
        var id = f.Host.Store.CreateSnippet(new("synthetic", "Synthetic", true, null, "")).Value!.Id;
        f.Broker.Bind(f.Operations, f.Operations.Id); f.Broker.Admit();
        Assert.True(f.Host.ShowCapturedPopup());
        var row = f.Host.Popup.Items.Single(item => item.SnippetId == id);
        Assert.True(PopupContextInsert.CanInvoke(f.Host.Popup, row, () => f.Host.Gate.CanInsert));
        await PopupContextInsert.InvokeAsync(f.Host.Popup, row, () => false,
            _ => throw new Exception("refusal must not enter host operation"));
        var insertion = PopupContextInsert.InvokeAsync(f.Host.Popup, row, () => f.Host.Gate.CanInsert, f.Host.RunOperationAsync);
        Assert.True(f.Host.Popup.IsInserting);
        await PopupContextInsert.InvokeAsync(f.Host.Popup, row, () => true,
            _ => throw new Exception("busy callback must not run"));
        f.Operations.Focus.SetResult(FocusPath.Direct);
        await insertion;
        f.Host.Store.Load();
        Assert.Equal(1, f.Host.Store.Snippets.Single().Snippet.UsageCount);
        Assert.Equal(1, f.Broker.PasteCalls); Assert.Equal(0, f.Broker.CopyCommits);
        f.Host.ShowPreview();
        await PopupContextInsert.InvokeAsync(f.Host.Popup, row, () => true,
            _ => throw new Exception("preview must not run"));
        await f.Host.ExitAsync();
    });
}

public sealed class CopySceneProtocolTests
{
    [Theory]
    [InlineData("popup-copy", false)] [InlineData("editor-copy", false)] [InlineData("footer-copy", false)] [InlineData("context-insert", true)]
    public async Task NewScenes_GrantPrecedesPostAndAllReviewGatesRemainClosed(string scene, bool inserts)
    {
        Assert.True(PositiveProtocol.IsPositive(scene));
        Assert.True(PositiveProtocol.NeedsCapturedPopup(scene));
        Assert.Equal(inserts, PositiveProtocol.Inserts(scene));
        Assert.Equal(!inserts, PositiveProtocol.IsCopy(scene));
        Assert.Equal(inserts, PositiveProtocol.RequiresInputReview(scene));
        Assert.Equal(inserts, (PositiveProtocol.Permissions(scene) & CandidateNativeOperation.Input) != 0);
        Assert.Equal(inserts, (PositiveProtocol.Permissions(scene) & CandidateNativeOperation.Clipboard) != 0);
        Assert.Equal(!inserts, (PositiveProtocol.Permissions(scene) & CandidateNativeOperation.Copy) != 0);
        Assert.Throws<InvalidOperationException>(() => PositiveProtocol.RequireReviewed(scene));
        var grant = new TaskCompletionSource(); var posts = 0;
        var begin = PositiveShortcutRoute.BeginAsync(scene, () => posts++, () => throw new Exception(), () => grant.Task);
        Assert.Equal(0, posts); grant.SetResult(); await begin; Assert.Equal(1, posts);
        await Assert.ThrowsAsync<IOException>(() => PositiveShortcutRoute.BeginAsync(scene, () => throw new Exception(),
            () => throw new Exception(), () => throw new IOException()));
        Assert.Equal("native_result=" + scene + "_settled", PositiveProtocol.Result(scene));
    }
}
