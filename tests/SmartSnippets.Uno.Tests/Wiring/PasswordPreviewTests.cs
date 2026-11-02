using SmartSnippets.Presentation.Text;
using SmartSnippets.Uno.Tests.Composition;

namespace SmartSnippets.Uno.Popup
{
    public sealed partial class PopupWindow
    {
        internal void AuditPasswordPreview(long id, HostFixture fixture)
        {
            ShowKeyboardPreview(); // Root-owned: no realized row is needed.
            Assert.True(_keyboardSurface!.IsKeyboardOpen);
            fixture.Host.Store.UpdateSnippet(id, new("secretOnly", "Safe label", true, null, "", IsPassword: true));
            Assert.False(_keyboardSurface.IsKeyboardOpen); // synchronous dirty notification
            ShowKeyboardPreview(); // must flush even before the dispatcher rebuild
            Assert.False(ViewModel.IsCacheDirty);
            Assert.Equal(SnippetDisplayPolicy.PasswordMask, SelectedSnippet!.ContentText);
            Assert.True(_keyboardSurface.IsKeyboardOpen);
        }
    }
}

namespace SmartSnippets.Uno.Tests.Wiring
{
    public sealed class PasswordPreviewTests
    {
        [Fact]
        public Task DirtyCacheClosesKeyboardPreviewAndNextPreviewUsesCurrentProjection() => HostThread.RunAsync(async () =>
        {
            using var fixture = new HostFixture();
            await fixture.Start();
            var id = fixture.Host.Store.CreateSnippet(new("secretOnly", "Safe label", true, null, "")).Value!.Id;
            var window = new Popup.PopupWindow(fixture.Host.TextSize, () => { }) { ViewModel = fixture.Host.Popup };
            try
            {
                window.ShowPreview(new Windows.Win32.Foundation.HWND(0));
                window.CompleteAuditWarmup();
                window.AuditPasswordPreview(id, fixture);
            }
            finally { window.Hide(); window.Close(); }
        });
    }
}
