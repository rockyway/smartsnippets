namespace SmartSnippets.Presentation.Tests;

/// <summary>Supplementary wiring checks; actual TextBox routing and native focus require desktop QA.</summary>
public sealed class PopupKeyboardPreviewSourceTests
{
    private static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "UiContracts", file));

    [Fact]
    public void KeysAreInterceptedBeforeTextBoxEditingAndReleasedEvenWhenHandled()
    {
        var code = Source("PopupWindow.xaml.cs");
        Assert.Contains("UIElement.PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDown), true", code);
        Assert.Contains("UIElement.KeyUpEvent", code);
        Assert.Contains("_keyboardPreview.Released(key)", code);
        Assert.Contains("_isComposing || _contextFlyout is not null || _sortMenuOpen", code);
        Assert.Contains("Down(VirtualKey.Control) || Down(VirtualKey.Shift) || Down(VirtualKey.Menu)", code);
        Assert.Contains("Focus(PopupFocusTarget.Search)", code);
        Assert.Contains("SearchBox.Select(", code);
        Assert.Contains("SearchBox.TextCompositionStarted", code);
        Assert.Contains("SearchBox.TextCompositionEnded", code);
    }

    [Fact]
    public void KeyboardPanelHasStableRootAndProjectionOwnershipNotRealizedRowOwnership()
    {
        var code = Source("PopupWindow.xaml.cs");
        Assert.Contains("new Controls.HoverDetailsSurface(RootGrid, keyboardOwner: true)", code);
        Assert.Contains("ShowKeyboard(selected.ContentText, TextSize.FontSize)", code);
        Assert.Contains("Controls.HoverDetailsSurface.CancelRowHovers(RootGrid.XamlRoot)", code);
        Assert.Contains("_keyboardSurface?.Dispose()", code);
        Assert.Contains("SelectedSnippet is null) ResetKeyboardNavigation()", code);
        Assert.Contains("_movingKeyboardSelection = true", code);
        Assert.Contains("if (_keyboardPreview.IsPreviewOpen && !_movingKeyboardSelection)", code);
        var surface = Source("HoverDetailsSurface.xaml.cs");
        Assert.Contains("_policy.OpenPinned(_owner)", surface);
        Assert.Contains("if (!ReferenceEquals(surface, this)) surface.Cancel()", surface);
        Assert.Contains("if (!surface._keyboardOwner", surface);
        Assert.Contains("KeyboardClosed?.Invoke()", surface);
        Assert.Contains("_popup.Closed -= OnNativePopupClosed", surface);
        Assert.DoesNotContain("SetForegroundWindow", surface);
        Assert.DoesNotContain(".Focus(", surface);
    }
}
