namespace SmartSnippets.Core.Settings;

/// <summary>A global shortcut as Win32 <c>RegisterHotKey</c> modifiers (MOD_*) plus a virtual-key code.</summary>
public readonly record struct HotkeyGesture(uint Modifiers, uint Key)
{
    public const uint ModAlt = 0x0001;
    public const uint ModControl = 0x0002;
    public const uint ModShift = 0x0004;
    public const uint ModWin = 0x0008;

    public static HotkeyGesture Default { get; } = new(ModControl | ModShift, 0x20);
}
