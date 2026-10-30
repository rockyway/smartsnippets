#if !WINDOWS
using System;
using System.Runtime.InteropServices;

namespace SmartSnippets.UnoProbe;

/// <summary>Read-only preflight only. Never prompts, posts events, reads windows or clipboard.</summary>
internal static partial class MacCapabilityProbe
{
    internal static int Run()
    {
        if (!OperatingSystem.IsMacOS()) return 2;
        try
        {
            var accessibility = AXIsProcessTrusted() != 0;
            var posting = CGPreflightPostEventAccess() != 0;
            // A successful query is not a successful insertion-capability gate.
            Console.WriteLine("mac_accessibility=" + (accessibility ? "GRANTED" : "NOT_GRANTED"));
            Console.WriteLine("mac_event_posting=" + (posting ? "GRANTED" : "NOT_GRANTED"));
            Console.WriteLine("mac_capability_query_complete prompt=NONE input=NONE clipboard=UNTOUCHED");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine("mac_capability_query_failed kind=" + error.GetType().Name);
            return 1;
        }
    }

    [LibraryImport("/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices")]
    private static partial byte AXIsProcessTrusted();
    [LibraryImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static partial byte CGPreflightPostEventAccess();
}
#endif
