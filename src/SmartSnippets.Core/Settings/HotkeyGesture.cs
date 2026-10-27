using System.Text;

namespace SmartSnippets.Core.Settings;

/// <summary>A global shortcut as Win32 <c>RegisterHotKey</c> modifiers (MOD_*) plus a virtual-key code.</summary>
public readonly record struct HotkeyGesture(uint Modifiers, uint Key)
{
    public const uint ModAlt = 0x0001;
    public const uint ModControl = 0x0002;
    public const uint ModShift = 0x0004;
    public const uint ModWin = 0x0008;
    public const uint ModNoRepeat = 0x4000;

    private const uint AllModifiers = ModAlt | ModControl | ModShift | ModWin;
    private const uint RequiredOneOf = ModAlt | ModControl | ModWin;

    public static HotkeyGesture Default { get; } = new(ModControl | ModShift, 0x20);

    /// <summary>At least one of Ctrl/Alt/Win, no unknown modifier bits, and a non-modifier key.</summary>
    public bool IsValid =>
        (Modifiers & RequiredOneOf) != 0
        && (Modifiers & ~AllModifiers) == 0
        && Key != 0
        && !IsModifierKey(Key);

    /// <summary>Builds a gesture, dropping MOD_NOREPEAT; false when the result is not valid.</summary>
    public static bool TryCreate(uint modifiers, uint key, out HotkeyGesture gesture)
    {
        gesture = new HotkeyGesture(modifiers & ~ModNoRepeat, key);
        return gesture.IsValid;
    }

    public static bool IsModifierKey(uint key) =>
        key is >= 0x10 and <= 0x12
        or >= 0xA0 and <= 0xA5
        or 0x5B
        or 0x5C;

    public string ToDisplayString()
    {
        var sb = new StringBuilder();
        Append(sb, Modifiers, ModControl, "Ctrl");
        Append(sb, Modifiers, ModAlt, "Alt");
        Append(sb, Modifiers, ModShift, "Shift");
        Append(sb, Modifiers, ModWin, "Win");
        sb.Append(VirtualKeyNames.Get(Key));
        return sb.ToString();
    }

    private static void Append(StringBuilder sb, uint modifiers, uint flag, string name)
    {
        if ((modifiers & flag) != 0)
        {
            sb.Append(name).Append('+');
        }
    }
}
