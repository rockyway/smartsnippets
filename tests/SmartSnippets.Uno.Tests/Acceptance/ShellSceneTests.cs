using SmartSnippets.Presentation.Navigation;
using SmartSnippets.Uno.Tests.Composition;

namespace SmartSnippets.Uno.Tests.Acceptance;

public sealed class ShellSceneTests
{
    [Theory]
    [InlineData("preview-options", "shell-ready,preview,shell-preview,unchanged,shell-verified,exit")]
    [InlineData("hover", "shell-ready,preview,hover-open,shell-hover-open,hover-closed,shell-hover-closed,unchanged,shell-verified,exit")]
    [InlineData("tray", "shell-ready,open,page-Snippets,shell-tray-open,settings,page-Settings,shell-tray-settings,unchanged,shell-verified,exit")]
    [InlineData("close-background", "shell-ready,close,hidden,shell-background,open,page-Snippets,shell-reopened,unchanged,shell-verified,exit")]
    [InlineData("message-window", "shell-ready,cycles-20,shell-message-cycles,unchanged,shell-verified,exit")]
    [InlineData("dpi", "shell-ready,dpi,shell-dpi,unchanged,shell-verified,exit")]
    public async Task FixedOrderingAndRealHostConsumers(string scene, string expected)
    {
        using var fixture = new HostFixture();
        await fixture.Start();
        var surface = new Surface(fixture);
        await ShellScene.Run(scene, surface);
        Assert.Equal(expected, string.Join(',', surface.Events));
        Assert.Equal(1, fixture.Boundary.Exits);
        Assert.Equal(1, fixture.Boundary.TrayDisposals);
        Assert.Equal(1, fixture.Boundary.MessageDisposals);
        Assert.Equal(1, fixture.Boundary.Main.Disposals);
    }

    [Theory]
    [InlineData("coexistence", "shell_legacy_isolation_unavailable")]
    [InlineData("arbitrary PRIVATE payload", "shell_scene")]
    public async Task UnsafeLegacyOrUnknownSceneRefusesBeforeAnySurface(string scene, string reason)
    {
        using var fixture = new HostFixture();
        var surface = new Surface(fixture);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => ShellScene.Run(scene, surface));
        Assert.Equal(reason, error.Message);
        Assert.Empty(surface.Events);
        Assert.Equal(0, fixture.Boundary.Messages);
    }

    [Theory]
    [InlineData("preview-options", "preview")]
    [InlineData("hover", "hover-open")]
    [InlineData("tray", "settings")]
    [InlineData("close-background", "hidden")]
    [InlineData("message-window", "cycles-20")]
    [InlineData("dpi", "dpi")]
    public async Task MissingCallbackOrObservationCannotReachVerified(string scene, string fail)
    {
        var fixture = new HostFixture();
        try
        {
            await fixture.Start();
            var surface = new Surface(fixture) { Fail = fail };
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => ShellScene.Run(scene, surface));
            Assert.Equal("shell_stub_refusal", error.Message);
            Assert.DoesNotContain("shell-verified", surface.Events);
            Assert.DoesNotContain("exit", surface.Events);
        }
        finally { fixture.Dispose(); }
        Assert.Equal(1, fixture.Boundary.TrayDisposals);
        Assert.Equal(1, fixture.Boundary.MessageDisposals);
    }

    [Fact]
    public async Task MutationRefusesBeforeVerifiedAndExit()
    {
        using var fixture = new HostFixture(); await fixture.Start();
        var surface = new Surface(fixture) { Fail = "unchanged" };
        await Assert.ThrowsAsync<InvalidOperationException>(() => ShellScene.Run("dpi", surface));
        Assert.DoesNotContain("shell-verified", surface.Events);
        Assert.Equal(0, fixture.Boundary.Exits);
    }

    private sealed class Surface(HostFixture fixture) : IShellScene
    {
        internal readonly List<string> Events = [];
        internal string? Fail;
        private void Record(string value)
        {
            Events.Add(value);
            if (Fail == value) throw new InvalidOperationException("shell_stub_refusal");
        }
        public Task Checkpoint(string phase) { Record(phase); return Task.CompletedTask; }
        public async Task PreviewOptions()
        {
            Record("preview");
            await fixture.Host.ApplyLaunchOptionsAsync(LaunchOptions.Parse(["--preview-popup", "--preview-popup-query", "acceptance synthetic", "--preview-popup-sort-menu"]));
            var popup = fixture.Boundary.Popups.Last();
            Assert.Equal("acceptance synthetic", popup.PreviewQuery);
            Assert.True(popup.PreviewSortMenu);
            Assert.True(fixture.Host.Popup.IsPreview);
            Assert.False(fixture.Host.Gate.IsAvailable);
        }
        public Task Hover(bool open) { Record(open ? "hover-open" : "hover-closed"); return Task.CompletedTask; }
        public async Task Tray(string command)
        {
            Record(command);
            switch (command)
            {
                case "open": fixture.Boundary.TrayOpen!(); break;
                case "settings": fixture.Boundary.TraySettings!(); break;
                case "exit": fixture.Boundary.TrayExit!(); break;
                default: throw new InvalidOperationException();
            }
            fixture.Boundary.Drain();
            await Task.CompletedTask;
        }
        public async Task CloseMain() { Record("close"); await fixture.Boundary.Main.CloseAsync(); }
        public Task MessageCycles(int count) { Record("cycles-" + count); Assert.Equal(20, count); return Task.CompletedTask; }
        public void RequirePage(PageKind page)
        {
            Record("page-" + page);
            Assert.Equal(page, fixture.Boundary.Main.Current);
            Assert.True(fixture.Boundary.Main.Visible);
        }
        public void RequireHidden()
        {
            Record("hidden"); Assert.False(fixture.Boundary.Main.Visible);
            Assert.Equal(0, fixture.Boundary.Exits);
            Assert.Equal(0, fixture.Boundary.TrayDisposals);
        }
        public void RequirePlacement() => Record("dpi");
        public void RequireUnchanged()
        {
            Record("unchanged"); Assert.False(fixture.Host.Gate.IsAvailable);
            Assert.All(fixture.Host.Store.Snippets, row => Assert.Equal(0, row.Snippet.UsageCount));
        }
    }
}
