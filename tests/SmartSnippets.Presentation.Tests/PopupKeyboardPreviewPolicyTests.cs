using SmartSnippets.Presentation.Services;

namespace SmartSnippets.Presentation.Tests;

public sealed class PopupKeyboardPreviewPolicyTests
{
    [Fact]
    public void FooterDescribesNavigationPreviewAndSearchShortcuts()
    {
        var hint = SmartSnippets.Presentation.Text.UiText.PopupHint;
        Assert.Contains("↑↓ move", hint);
        Assert.Contains("→ preview", hint);
        Assert.Contains("← hide", hint);
        Assert.Contains("⌫ search", hint);
        Assert.DoesNotContain("\n", hint);
        Assert.Contains("Enter insert", hint);
        Assert.Contains("Esc close", hint);
    }

    [Theory]
    [InlineData(PopupPreviewKey.Right)]
    [InlineData(PopupPreviewKey.Left)]
    [InlineData(PopupPreviewKey.Backspace)]
    public void BeforeNavigationKeysEditSearch(PopupPreviewKey key)
    {
        var p = new PopupKeyboardPreviewPolicy();
        Assert.Equal(PopupPreviewEffect.None, p.Handle(key, 1));
    }

    [Fact]
    public void NavigateThenRightShowsImmediatelyAndRepeatedRightIsIdempotent()
    {
        var p = new PopupKeyboardPreviewPolicy();
        Assert.Equal(PopupPreviewEffect.None, p.Navigated(1, 3, 2));
        Assert.Equal(PopupPreviewEffect.Handled | PopupPreviewEffect.Show, p.Handle(PopupPreviewKey.Right, 1));
        Assert.True(p.IsPreviewOpen);
        Assert.Equal(PopupPreviewEffect.Handled, p.Handle(PopupPreviewKey.Right, 1));
        Assert.Equal(PopupPreviewEffect.Show, p.Navigated(2, 99, 0));
        Assert.Equal(3, p.CaretStart); Assert.Equal(2, p.CaretLength);
    }

    [Fact]
    public void BackspaceReturnsWithoutDeletionThenEditsAfterRelease()
    {
        var p = new PopupKeyboardPreviewPolicy(); p.Navigated(1, 3, 2); p.Handle(PopupPreviewKey.Right, 1);
        var effect = p.Handle(PopupPreviewKey.Backspace, 1);
        Assert.Equal(PopupPreviewEffect.Handled | PopupPreviewEffect.Close | PopupPreviewEffect.ReturnSearch, effect);
        Assert.False(p.IsNavigating); Assert.False(p.IsPreviewOpen);
        Assert.Equal(3, p.CaretStart); Assert.Equal(2, p.CaretLength);
        for (var i = 0; i < 20; i++) Assert.Equal(PopupPreviewEffect.Handled, p.Handle(PopupPreviewKey.Backspace, 1));
        p.Released(PopupPreviewKey.Backspace);
        Assert.Equal(PopupPreviewEffect.None, p.Handle(PopupPreviewKey.Backspace, 1));
    }

    [Fact]
    public void FirstBackspaceIsFocusOnlyEvenWithoutOpenPreview()
    {
        var p = new PopupKeyboardPreviewPolicy(); p.Navigated(1, 0, 0);
        Assert.True(p.Handle(PopupPreviewKey.Backspace, 1).HasFlag(PopupPreviewEffect.ReturnSearch));
    }

    [Fact]
    public void LeftClosesButKeepsNavigationArmed()
    {
        var p = new PopupKeyboardPreviewPolicy(); p.Navigated(1, 3, 0); p.Handle(PopupPreviewKey.Right, 1);
        Assert.Equal(PopupPreviewEffect.Handled | PopupPreviewEffect.Close, p.Handle(PopupPreviewKey.Left, 1));
        Assert.True(p.IsNavigating); Assert.False(p.IsPreviewOpen);
        Assert.True(p.Handle(PopupPreviewKey.Right, 1).HasFlag(PopupPreviewEffect.Show));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EscapeClosesContentBeforeExistingSearchClearAndConsumesRepeats(bool keyboard)
    {
        var p = new PopupKeyboardPreviewPolicy();
        if (keyboard) { p.Navigated(1, 1, 0); p.Handle(PopupPreviewKey.Right, 1); }
        Assert.Equal(PopupPreviewEffect.Handled | PopupPreviewEffect.Close, p.Handle(PopupPreviewKey.Escape, 1, true));
        Assert.Equal(PopupPreviewEffect.Handled, p.Handle(PopupPreviewKey.Escape, 1));
        p.Released(PopupPreviewKey.Escape);
        Assert.Equal(PopupPreviewEffect.None, p.Handle(PopupPreviewKey.Escape, 1));
    }

    [Fact]
    public void InvalidSelectionAndResetCannotReopenPreview()
    {
        var p = new PopupKeyboardPreviewPolicy(); p.Navigated(1, 1, 0); p.Handle(PopupPreviewKey.Right, 1);
        Assert.Equal(PopupPreviewEffect.Close, p.Navigated(null, 1, 0));
        Assert.False(p.IsNavigating); Assert.False(p.IsPreviewOpen);
        Assert.Equal(PopupPreviewEffect.None, p.Handle(PopupPreviewKey.Right, null));
        p.Navigated(2, 1, 0); p.Handle(PopupPreviewKey.Right, 2); p.Reset(true);
        Assert.Equal(PopupPreviewEffect.None, p.Handle(PopupPreviewKey.Right, 2));
    }

    [Fact]
    public void ActualSurfaceFailureOrPointerTakeoverDisarmsPreviewButAllowsRetry()
    {
        var p = new PopupKeyboardPreviewPolicy(); p.Navigated(1, 1, 0); p.Handle(PopupPreviewKey.Right, 1);
        p.PreviewClosed(); Assert.False(p.IsPreviewOpen);
        Assert.Equal(PopupPreviewEffect.Handled | PopupPreviewEffect.Show, p.Handle(PopupPreviewKey.Right, 1));
    }

    [Fact]
    public void ResetWithoutKeyReleasePreservesConsumedBackspaceGesture()
    {
        var p = new PopupKeyboardPreviewPolicy(); p.Navigated(1, 1, 0); p.Handle(PopupPreviewKey.Backspace, 1);
        p.Reset(); Assert.Equal(PopupPreviewEffect.Handled, p.Handle(PopupPreviewKey.Backspace, 1));
        p.Reset(true); Assert.Equal(PopupPreviewEffect.None, p.Handle(PopupPreviewKey.Backspace, 1));
    }
}
