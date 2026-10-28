using SmartSnippets.Presentation.Services;

namespace SmartSnippets.Presentation.Tests;

public sealed class TrayStatePolicyTests
{
    [Theory]
    [InlineData(false, true, TrayState.HotkeyUnavailable)]
    [InlineData(false, false, TrayState.HotkeyUnavailable)]
    [InlineData(true, true, TrayState.PopupOpen)]
    [InlineData(true, false, TrayState.Ready)]
    public void Compute_PrioritizesHotkeyFault(bool registered, bool open, TrayState expected) =>
        Assert.Equal(expected, TrayStatePolicy.Compute(registered, open));
}
