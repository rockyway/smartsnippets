using Microsoft.UI.Xaml.Media;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Uno.Tests.Composition;
using SmartSnippets.Uno.Tests.Wiring;

namespace SmartSnippets.Uno.Popup
{
    public sealed partial class PopupWindow
    {
        internal void CompleteAuditWarmup() => _shows.CompleteWarmup(_shows.WarmupGeneration);
    }
}

namespace SmartSnippets.Uno.Tests.Wiring
{
    public sealed class PopupActivationOrderingTests
    {
        [Fact]
        public Task AcceptanceBoundaryForwardsOnlyLiveCoordinatorReadiness() => CandidateHostThread.RunAsync(async () =>
        {
            using var f = new HostFixture();
            await f.Start();
            var views = new WindowsCandidateBoundary();
            views.CreateTheme(f.Host.Settings);
            using var surface = views.CreatePopup(f.Host);
            Assert.False(surface.IsCaptureSurfaceReady);
            surface.Prewarm();
            Popup.PopupWindow.LastCreated.CompleteAuditWarmup();
            Assert.True(surface.IsCaptureSurfaceReady);
            surface.Dispose();
            Assert.False(surface.IsCaptureSurfaceReady);
        });

        [Fact]
        public Task SupersedingPreviewDuringRevealNeverFocusesCapturedGeneration() => CandidateHostThread.RunAsync(async () =>
        {
            using var f = new HostFixture();
            await f.Start();
            var window = new Popup.PopupWindow(f.Host.TextSize, () => { }) { ViewModel = f.Host.Popup };
            var calls = 0;
            window.TryFocusCapturedPopup = () => { calls++; return true; };
            try
            {
                window.ShowCaptured(new InsertionTarget(11, 21, 0, ""), () => true);
                window.CompleteAuditWarmup();
                window.ShowPreview(new Windows.Win32.Foundation.HWND(0));
                CompositionTarget.Render();
                CompositionTarget.Render();
                Assert.Equal(0, calls);
            }
            finally { window.Hide(); window.Close(); }
        });

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public Task CapturedFocusWaitsForRevealAndHideCancelsDeferredFocus(bool hide) => CandidateHostThread.RunAsync(async () =>
        {
            using var f = new HostFixture();
            await f.Start();
            var window = new Popup.PopupWindow(f.Host.TextSize, () => { }) { ViewModel = f.Host.Popup };
            var calls = 0;
            window.TryFocusCapturedPopup = () =>
            {
                Assert.False(SmartSnippets.Uno.Services.WindowSurface.Cloaked[window]);
                calls++; return true;
            };
            try
            {
                window.ShowCaptured(new InsertionTarget(11, 21, 0, ""), () => true);
                Assert.False(window.IsCaptureSurfaceReady);
                window.CompleteAuditWarmup();
                Assert.True(window.IsCaptureSurfaceReady);
                Assert.True(window.AppWindow.IsVisible);
                Assert.Equal(0, calls); // Old implementation activates while DWM-cloaked.
                if (hide) window.Hide();
                CompositionTarget.Render();
                Assert.Equal(hide ? 0 : 1, calls);
                CompositionTarget.Render();
                Assert.Equal(hide ? 0 : 1, calls); // No retry or stale generation activation.
            }
            finally { window.Hide(); window.Close(); Assert.False(window.IsCaptureSurfaceReady); }
        });
    }
}
