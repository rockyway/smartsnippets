using SmartSnippets.Core.Logging;
using SmartSnippets.Uno.Tests.Composition;

namespace SmartSnippets.Uno.Tests.Acceptance;

public sealed class CandidateAcceptanceFlowTests
{
    [Fact]
    public void MissingInitializationCannotProduceReadiness()
    {
        using var fixture = new HostFixture();
        Assert.Throws<InvalidOperationException>(() => CandidateAcceptanceFlow.RequireReady(fixture.Host, fixture.Root));
    }

    [Fact]
    public async Task VisibleSurfaceWithoutFinalHostReceiptIsNotReady()
    {
        using var fixture = new HostFixture();
        await fixture.Start();
        Assert.True(fixture.Boundary.Main.Visible);
        File.WriteAllText(Path.Combine(fixture.Root, "logs", "app.log"), "");
        Assert.Throws<InvalidOperationException>(() => CandidateAcceptanceFlow.RequireReady(fixture.Host, fixture.Root));
    }

    [Fact]
    public async Task ErrorReceiptCannotProduceReadiness()
    {
        using var fixture = new HostFixture(); await fixture.Start();
        AppLog.Error("candidate_cleanup_failed");
        Assert.Throws<InvalidOperationException>(() => CandidateAcceptanceFlow.RequireReady(fixture.Host, fixture.Root));
    }

    [Fact]
    public async Task SameAcceptanceDriverTraversesRealHostManagementPreviewAndGracefulLeaseRelease()
    {
        using var fixture = new HostFixture(); await fixture.Start();
        CandidateAcceptanceFlow.RequireReady(fixture.Host, fixture.Root);
        var id = await CandidateAcceptanceFlow.ManagementAsync(fixture.Host);
        fixture.Host.ShowPreview("acceptance synthetic");
        CandidateAcceptanceFlow.RequirePreview(fixture.Host, id);
        fixture.Boundary.Popups.Single().Dispose();
        fixture.Host.ShowPreview("acceptance synthetic");
        CandidateAcceptanceFlow.RequirePreview(fixture.Host, id);
        Assert.Equal(2, fixture.Boundary.Popups.Count);
        await fixture.Host.ExitAsync();
        Assert.Equal(1, fixture.Boundary.Exits);
        Assert.Equal(1, fixture.Boundary.MessageDisposals);
        Assert.Equal(1, fixture.Boundary.TrayDisposals);
        Assert.All(fixture.Boundary.Popups, popup => Assert.Equal(1, popup.Disposals));
        using var released = File.Open(Path.Combine(fixture.Root, ".candidate-owner"), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
    }
}
