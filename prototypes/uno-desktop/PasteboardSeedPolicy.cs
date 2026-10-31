using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SmartSnippets.UnoProbe;

/// <summary>Seed-only verification of AppKit's UTF-8/external-UTF-16 alias.
/// Actual snapshot/restore fidelity is independently byte-for-byte, with no decoding.</summary>
internal static class PasteboardSeedPolicy
{
    internal static string LastFailure { get; private set; } = "none";
    internal static string LastFormat { get; private set; } = "none";
    internal static string ObservedCategory { get; private set; } = "none";

    internal const string Plain = "public.utf8-plain-text";
    internal const string Utf16External = "public.utf16-external-plain-text";

    internal static bool Matches(IReadOnlyList<Dictionary<string, byte[]>> actual, IReadOnlyList<Dictionary<string, byte[]>> seed)
    {
        LastFailure = "none";
        LastFormat = "none";
        ObservedCategory = "none";
        try { return MatchesCore(actual, seed); }
        catch (DecoderFallbackException) { return Fail("invalid_encoding"); }
    }

    private static bool MatchesCore(IReadOnlyList<Dictionary<string, byte[]>> actual, IReadOnlyList<Dictionary<string, byte[]>> seed)
    {
        if (actual.Count != seed.Count) return Fail("item_count");
        for (var index = 0; index < seed.Count; index++)
        {
            var input = seed[index];
            var output = actual[index];
            foreach (var (type, bytes) in input)
                if (type != Plain && (!output.TryGetValue(type, out var saved) || !bytes.SequenceEqual(saved))) return Fail("nontext_format", type);
            if (!input.TryGetValue(Plain, out var utf8))
            {
                if (output.Count != input.Count) return Fail("nontext_count");
                continue;
            }
            var expected = new UTF8Encoding(false, true).GetString(utf8);
            var textFormats = 0;
            foreach (var (type, bytes) in output)
            {
                if (type == Plain)
                {
                    if (new UTF8Encoding(false, true).GetString(bytes) != expected) return Fail("utf8_text", type);
                    textFormats++;
                }
                else if (type == Utf16External)
                {
                    if (bytes.Length < 2 || bytes.Length % 2 != 0) return Fail("utf16_length", type);
                    var little = bytes[0] == 0xFF && bytes[1] == 0xFE;
                    var big = bytes[0] == 0xFE && bytes[1] == 0xFF;
                    if (!little && !big) return Fail("utf16_bom", type);
                    var observed = new UnicodeEncoding(big, false, true).GetString(bytes, 2, bytes.Length - 2);
                    if (observed != expected)
                    {
                        ObservedCategory = observed == expected + "\0" ? "terminal_nul" :
                            observed.ReplaceLineEndings("\n") == expected ? "line_endings" :
                            observed == "https://example.invalid/probe018" ? "url_fixture" :
                            observed == "synthetic" ? "rich_fixture" : "other";
                        return Fail("utf16_text", type);
                    }
                    textFormats++;
                }
                else if (!input.ContainsKey(type)) return Fail("unexpected_format", type);
            }
            if (textFormats == 0 || output.Count != input.Count - 1 + textFormats) return Fail("text_count");
        }
        return true;
    }

    private static bool Fail(string reason, string type = "none")
    {
        LastFailure = reason;
        LastFormat = type switch
        {
            Plain => "utf8", Utf16External => "utf16", "public.rtf" => "rtf",
            "public.html" => "html", "public.url" => "url", "public.png" => "png",
            "org.nspasteboard.TransientType" => "transient", "org.nspasteboard.ConcealedType" => "concealed",
            "none" => "none", _ => "other",
        };
        return false;
    }
}
