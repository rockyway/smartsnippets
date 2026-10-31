using System.Runtime.InteropServices;
using System.Text;

namespace SmartSnippets.WindowsInsertion018;

internal sealed partial class OwnClipboardPublication
{
    internal const string Multiline = "AΩ中🙂e\u0301\r\nsecond line\r\n";
    internal const uint PublishedLocale = 0x0409; // Explicit en-US, independent of active keyboard/user default.
    internal const string MarkerName = "SmartSnippets.018.PrivateInsertionOwnership";
    internal static readonly string[] PrivacyNames = ["ExcludeClipboardContentFromMonitorProcessing", "CanIncludeInClipboardHistory", "CanUploadToCloudClipboard"];
    internal IReadOnlyDictionary<uint, byte[]> Expected { get; }
    internal uint MarkerFormat { get; }
    internal byte[] Token { get; }
    internal IReadOnlySet<uint> PrivacyFormats { get; }

    internal OwnClipboardPublication(Guid run)
    {
        Token = run.ToByteArray();
        MarkerFormat = RegisterClipboardFormatW(MarkerName);
        var privacy = PrivacyNames.Select(RegisterClipboardFormatW).ToHashSet();
        if (MarkerFormat == 0 || privacy.Count != 3 || privacy.Contains(0)) throw new InvalidOperationException("Clipboard formats unavailable.");
        PrivacyFormats = privacy;
        var expected = privacy.ToDictionary(f => f, _ => new byte[4]);
        expected.Add(MarkerFormat, Token);
        expected.Add(13, Encoding.Unicode.GetBytes(Multiline + "\0"));
        // Public synthesized text aliases. Only these exact fixed synthetic
        // conversions are recognizable; arbitrary data/extra formats refuse.
        expected.Add(1, Ansi(LocaleCodePage(0x1004))); // LOCALE_IDEFAULTANSICODEPAGE
        expected.Add(7, Ansi(LocaleCodePage(0x000B))); // LOCALE_IDEFAULTCODEPAGE (OEM)
        expected.Add(16, BitConverter.GetBytes(PublishedLocale));
        Expected = expected;
    }

    internal static bool IsCompatibleMarker(ReadOnlySpan<byte> data) => data.Length == 4 && data.SequenceEqual(new byte[4]);

    internal bool IsRecognizable(IReadOnlyDictionary<uint, byte[]> current) =>
        ClipboardOwnershipPolicy.IsRecognizablePublication(current, Expected, MarkerFormat, PrivacyFormats);

    private static unsafe byte[] Ansi(uint codePage)
    {
        var text = Multiline + "\0";
        fixed (char* source = text)
        {
            var length = WideCharToMultiByte(codePage, 0, source, text.Length, null, 0, 0, 0);
            if (length <= 0 || length > 4096) throw new InvalidOperationException("Synthetic conversion failed.");
            var bytes = new byte[length];
            fixed (byte* destination = bytes)
                if (WideCharToMultiByte(codePage, 0, source, text.Length, destination, bytes.Length, 0, 0) != length)
                    throw new InvalidOperationException("Synthetic conversion failed.");
            return bytes;
        }
    }

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)] private static partial uint RegisterClipboardFormatW(string name);
    [LibraryImport("kernel32.dll")] private static unsafe partial int WideCharToMultiByte(uint page, uint flags, char* input, int length, byte* output, int size, nint replacement, nint usedReplacement);
    private static unsafe uint LocaleCodePage(uint kind)
    {
        uint page = 0;
        if (GetLocaleInfoW(PublishedLocale, kind | 0x20000000, (char*)&page, 2) != 2 || page == 0)
            throw new InvalidOperationException("Locale code page unavailable.");
        return page;
    }
    [LibraryImport("kernel32.dll")] private static unsafe partial int GetLocaleInfoW(uint locale, uint kind, char* value, int size);
}
