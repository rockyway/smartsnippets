using Microsoft.UI.Dispatching;
using SmartSnippets.App.Services;
using SmartSnippets.Core.Logging;
using SmartSnippets.Presentation.Services;
using Windows.ApplicationModel.DataTransfer;

namespace SmartSnippets.Presentation.Tests.ClipboardCompatibility;

[Collection("AppLog")]
public sealed class LegacyClipboardServiceTests : IDisposable
{
    public LegacyClipboardServiceTests() { Clipboard.Calls.Clear(); Clipboard.FailAt = null; DispatcherQueue.HasCurrentThread = true; }
    public void Dispose() { Clipboard.FailAt = null; DispatcherQueue.HasCurrentThread = true; }

    [Fact]
    public void SetTextOnlyImplementer_DefaultMembersPreserveCompatibility()
    {
        var original = new SetTextOnly(); IClipboardService service = original;
        Assert.True(service.IsAvailable);
        Assert.True(service.TrySetText("synthetic"));
        Assert.Equal("synthetic", original.Text);
    }
    private sealed class SetTextOnly : IClipboardService
    {
        public string? Text { get; private set; }
        public void SetText(string text) => Text = text;
    }

    [Fact]
    public void LegacyClipboard_FlushSuccessTrue()
    {
        Assert.True(new ClipboardService().TrySetText("synthetic"));
        Assert.Equal(new[] { "SetContent", "Flush" }, Clipboard.Calls);
        Assert.Equal("synthetic", Clipboard.Text);
    }
    [Theory]
    [InlineData("SetContent")]
    [InlineData("Flush")]
    public void LegacyClipboard_ComFailureFalse_DelegatingVoidDoesNotThrow(string failAt)
    {
        var directory = Path.Combine(Path.GetTempPath(), "ss-clipboard-log-" + Guid.NewGuid().ToString("N"));
        try
        {
            AppLog.Initialize(directory);
            Clipboard.FailAt = failAt;
            var service = new ClipboardService();
            Assert.False(service.TrySetText("synthetic"));
            Clipboard.Calls.Clear();
            service.SetText("synthetic");
            Assert.Equal(failAt == "Flush" ? 2 : 1, Clipboard.Calls.Count);
            var log = File.ReadAllText(Path.Combine(directory, "app.log"));
            Assert.Contains("clipboard_copy_failed", log);
            Assert.Contains("COMException (HResult=0x", log);
            Assert.DoesNotContain("PRIVATE-PAYLOAD", log);
        }
        finally
        {
            AppLog.Shutdown();
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }
    [Fact]
    public void LegacyClipboard_VoidDelegatesThroughFlush()
    {
        new ClipboardService().SetText("synthetic");
        Assert.Equal(new[] { "SetContent", "Flush" }, Clipboard.Calls);
    }
    [Fact]
    public void LegacyClipboard_UiThreadPreconditionRetained()
    {
        DispatcherQueue.HasCurrentThread = false;
        var service = new ClipboardService();
        Assert.Throws<InvalidOperationException>(() => service.TrySetText("synthetic"));
        Assert.Throws<InvalidOperationException>(() => service.SetText("synthetic"));
        Assert.Empty(Clipboard.Calls);
    }
}
