using System.Globalization;

namespace SmartSnippets.Core.Settings;

/// <summary>Pure virtual-key code to display name table (no Windows dependency).</summary>
public static class VirtualKeyNames
{
    private static readonly Dictionary<uint, string> Named = new()
    {
        [0x08] = "Backspace",
        [0x09] = "Tab",
        [0x0D] = "Enter",
        [0x1B] = "Esc",
        [0x20] = "Space",
        [0x21] = "PgUp",
        [0x22] = "PgDn",
        [0x23] = "End",
        [0x24] = "Home",
        [0x25] = "Left",
        [0x26] = "Up",
        [0x27] = "Right",
        [0x28] = "Down",
        [0x2D] = "Insert",
        [0x2E] = "Delete",
        [0x6A] = "Num *",
        [0x6B] = "Num +",
        [0x6D] = "Num -",
        [0x6E] = "Num .",
        [0x6F] = "Num /",
        [0xBA] = ";",
        [0xBB] = "=",
        [0xBC] = ",",
        [0xBD] = "-",
        [0xBE] = ".",
        [0xBF] = "/",
        [0xC0] = "`",
        [0xDB] = "[",
        [0xDC] = "\\",
        [0xDD] = "]",
        [0xDE] = "'",
    };

    public static string Get(uint key)
    {
        if (key is >= 0x41 and <= 0x5A)
        {
            return ((char)key).ToString();
        }

        if (key is >= 0x30 and <= 0x39)
        {
            return ((char)key).ToString();
        }

        if (key is >= 0x70 and <= 0x87)
        {
            return "F" + (key - 0x6F).ToString(CultureInfo.InvariantCulture);
        }

        if (key is >= 0x60 and <= 0x69)
        {
            return "Num " + (key - 0x60).ToString(CultureInfo.InvariantCulture);
        }

        return Named.TryGetValue(key, out var name)
            ? name
            : "VK_0x" + key.ToString("X2", CultureInfo.InvariantCulture);
    }
}
