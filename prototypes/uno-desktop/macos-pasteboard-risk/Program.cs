using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SmartSnippets.UnoProbe;

// Pure source-linked policy checks: no Cocoa/clipboard/input/files/profile calls.
internal static class Program
{
    private static int _checks;
    private const string Body = "Synthetic λ 日本語 😀\nsecond";
    private const string Plain = PasteboardSeedPolicy.Plain;
    private const string External = PasteboardSeedPolicy.Utf16External;
    public static int Main()
    {
        try
        {
            var text = Encoding.UTF8.GetBytes(Body);
            var seed = new Dictionary<string, byte[]> { [Plain] = text, ["public.rtf"] = [1, 2, 3] };
            Dictionary<string, byte[]> Form(byte[] bytes) => new() { [External] = bytes, ["public.rtf"] = [1, 2, 3] };
            bool Matches(Dictionary<string, byte[]> output) => PasteboardSeedPolicy.Matches([output], [seed]);
            var little = Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes(Body)).ToArray();
            var big = Encoding.BigEndianUnicode.GetPreamble().Concat(Encoding.BigEndianUnicode.GetBytes(Body)).ToArray();
            Check(Matches(seed));
            Check(Matches(Form(little)));
            Check(Matches(Form(big)));
            var both = Form(little); both[Plain] = text;
            Check(Matches(both));
            Check(!Matches(Form(Encoding.Unicode.GetBytes(Body)))); // Missing external BOM.
            Check(!Matches(Form([0xFF, 0xFE, 0]))); // Odd byte count.
            Check(!Matches(Form([0xFF, 0xFE, 0, 0xD8]))); // Lone surrogate.
            Check(!Matches(Form(Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes("wrong")).ToArray())));
            var invalid = new Dictionary<string, byte[]> { [Plain] = [0xFF], ["public.rtf"] = [1, 2, 3] };
            Check(!Matches(invalid));
            var altered = Form(little); altered["public.rtf"] = [3, 2, 1];
            Check(!Matches(altered));
            var missing = Form(little); missing.Remove("public.rtf");
            Check(!Matches(missing));
            var extra = Form(little); extra["unknown"] = [];
            Check(!Matches(extra));
            var noText = new Dictionary<string, byte[]> { ["public.rtf"] = [1, 2, 3] };
            Check(!Matches(noText));
            Check(!PasteboardSeedPolicy.Matches([], [seed]));
            var marker = new Dictionary<string, byte[]> { ["marker"] = [] };
            Check(PasteboardSeedPolicy.Matches([marker], [marker]));
            Check(!PasteboardSeedPolicy.Matches([seed, marker], [marker, seed]));
            Console.WriteLine($"pasteboard_seed_policy_checks={_checks} result=PASS clipboard=NONE input=NONE");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"pasteboard_seed_policy_failed check={_checks} kind={error.GetType().Name}");
            return 1;
        }
    }
    private static void Check(bool passed) { _checks++; if (!passed) throw new InvalidOperationException(); }
}
