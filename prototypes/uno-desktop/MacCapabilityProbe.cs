#if !WINDOWS
using System;
using System.Runtime.InteropServices;

namespace SmartSnippets.UnoProbe;

/// <summary>Read-only permissions preflight. Never prompts, posts events, reads windows or clipboard contents.</summary>
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
            Console.WriteLine("mac_pasteboard_access=" + PasteboardAccess());
            Console.WriteLine("mac_capability_query_complete prompt=NONE input=NONE clipboard=UNTOUCHED");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine("mac_capability_query_failed kind=" + error.GetType().Name);
            return 1;
        }
    }

    private static string PasteboardAccess()
    {
        // Installed Xcode NSPasteboard.h: accessBehavior is a readonly enum
        // (macOS 15.4+). Do not call pasteboardItems/types/dataForType here.
        _ = NativeLibrary.Load("/System/Library/Frameworks/AppKit.framework/AppKit");
        var pool = Send(Send(GetClass("NSAutoreleasePool"), Selector("alloc")), Selector("init"));
        try
        {
            var board = Send(GetClass("NSPasteboard"), Selector("generalPasteboard"));
            if (board == 0) return "UNAVAILABLE";
            var property = Selector("accessBehavior");
            if (SendBoolArgument(board, Selector("respondsToSelector:"), property) == 0) return "UNAVAILABLE";
            return Send(board, property).ToInt64() switch
            {
                0 => "DEFAULT_PROMPT_ON_READ",
                1 => "ASK",
                2 => "ALWAYS_ALLOW",
                3 => "ALWAYS_DENY",
                _ => "UNKNOWN",
            };
        }
        finally { if (pool != 0) SendVoid(pool, Selector("drain")); }
    }

    private const string ObjC = "/usr/lib/libobjc.A.dylib";
    [LibraryImport(ObjC, EntryPoint = "objc_getClass", StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint GetClass(string name);
    [LibraryImport(ObjC, EntryPoint = "sel_registerName", StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint Selector(string name);
    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    private static partial nint Send(nint receiver, nint selector);
    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    private static partial byte SendBoolArgument(nint receiver, nint selector, nint argument);
    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    private static partial void SendVoid(nint receiver, nint selector);

    [LibraryImport("/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices")]
    private static partial byte AXIsProcessTrusted();
    [LibraryImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static partial byte CGPreflightPostEventAccess();
}
#endif
