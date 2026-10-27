using SmartSnippets.Core.Settings;

namespace SmartSnippets.Core.Tests.Settings;

public sealed class HotkeyGestureTests
{
    private const uint VkK = 0x4B;
    private const uint VkSpace = 0x20;

    [Fact]
    public void Default_IsCtrlShiftSpace()
    {
        var g = HotkeyGesture.Default;

        Assert.Equal(HotkeyGesture.ModControl | HotkeyGesture.ModShift, g.Modifiers);
        Assert.Equal(VkSpace, g.Key);
        Assert.True(g.IsValid);
        Assert.Equal("Ctrl+Shift+Space", g.ToDisplayString());
    }

    [Fact]
    public void ShiftOnly_Invalid()
    {
        Assert.False(new HotkeyGesture(HotkeyGesture.ModShift, VkK).IsValid);
        Assert.False(new HotkeyGesture(0, VkK).IsValid);
    }

    [Theory]
    [InlineData(0x10u)]
    [InlineData(0x11u)]
    [InlineData(0x12u)]
    [InlineData(0xA0u)]
    [InlineData(0xA5u)]
    [InlineData(0x5Bu)]
    [InlineData(0x5Cu)]
    [InlineData(0u)]
    public void ModifierAsKey_Invalid(uint key)
    {
        Assert.False(new HotkeyGesture(HotkeyGesture.ModControl, key).IsValid);
    }

    [Fact]
    public void Display_CtrlAltK()
    {
        var g = new HotkeyGesture(HotkeyGesture.ModAlt | HotkeyGesture.ModControl, VkK);

        Assert.Equal("Ctrl+Alt+K", g.ToDisplayString());
    }

    [Fact]
    public void Display_OrderIsCtrlAltShiftWin()
    {
        var all = HotkeyGesture.ModAlt | HotkeyGesture.ModControl | HotkeyGesture.ModShift | HotkeyGesture.ModWin;

        Assert.Equal("Ctrl+Alt+Shift+Win+F5", new HotkeyGesture(all, 0x74).ToDisplayString());
    }

    [Fact]
    public void UnknownVk_DisplaysHex()
    {
        Assert.Equal("Ctrl+VK_0xE7", new HotkeyGesture(HotkeyGesture.ModControl, 0xE7).ToDisplayString());
    }

    [Theory]
    [InlineData(0x30u, "0")]
    [InlineData(0x41u, "A")]
    [InlineData(0x70u, "F1")]
    [InlineData(0x87u, "F24")]
    [InlineData(0x0Du, "Enter")]
    [InlineData(0x25u, "Left")]
    [InlineData(0x22u, "PgDn")]
    [InlineData(0x60u, "Num 0")]
    [InlineData(0x6Bu, "Num +")]
    [InlineData(0xBCu, ",")]
    public void KeyNames_Table(uint key, string expected)
    {
        Assert.Equal(expected, VirtualKeyNames.Get(key));
    }

    [Fact]
    public void NoRepeat_NeverStored()
    {
        Assert.False(new HotkeyGesture(HotkeyGesture.ModControl | HotkeyGesture.ModNoRepeat, VkK).IsValid);

        var ok = HotkeyGesture.TryCreate(HotkeyGesture.ModControl | HotkeyGesture.ModNoRepeat, VkK, out var g);

        Assert.True(ok);
        Assert.Equal(HotkeyGesture.ModControl, g.Modifiers);
    }

    [Fact]
    public void TryCreate_RejectsInvalid()
    {
        Assert.False(HotkeyGesture.TryCreate(HotkeyGesture.ModShift, VkK, out _));
    }

    [Fact]
    public void AppSettings_HotkeyMapsBothWays()
    {
        var g = new HotkeyGesture(HotkeyGesture.ModAlt | HotkeyGesture.ModControl, VkK);

        var s = new AppSettings().WithHotkey(g);

        Assert.Equal(0x0003u, s.HotkeyModifiers);
        Assert.Equal(VkK, s.HotkeyKey);
        Assert.Equal(g, s.Hotkey);
        Assert.Equal(HotkeyGesture.Default, new AppSettings().Hotkey);
    }
}
