#if !WINDOWS
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace SmartSnippets.UnoProbe;

/// <summary>Read-only General snapshot feasibility. No clear/write/restore APIs.
/// Operator bytes stay in this bounded child and are zeroed before exit.</summary>
internal static partial class MacGeneralSnapshotProbe
{
    private const long Cap = 64 * 1024 * 1024;
    private const string ObjC = "/usr/lib/libobjc.A.dylib";
    private static readonly HashSet<string> Supported = new(StringComparer.Ordinal)
    {
        "public.utf8-plain-text", "public.utf16-external-plain-text", "public.rtf", "public.html",
        "public.png", "public.tiff", "public.url", "org.nspasteboard.TransientType", "org.nspasteboard.ConcealedType"
    };

    internal static int Run()
    {
        var buffers = new List<byte[]>();
        nint pool = 0;
        var stage = "setup";
        try
        {
            if (!OperatingSystem.IsMacOS() || RuntimeInformation.ProcessArchitecture != Architecture.Arm64)
                throw new PlatformNotSupportedException();
            _ = NativeLibrary.Load("/System/Library/Frameworks/AppKit.framework/AppKit");
            pool = Send(Send(Class("NSAutoreleasePool"), Sel("alloc")), Sel("init"));
            if (pool == 0) throw new InvalidOperationException();
            var board = Send(Class("NSPasteboard"), Sel("generalPasteboard"));
            if (board == 0) throw new InvalidOperationException();
            stage = "permission";
            // No prompt request or permission change. Enum 2 is AlwaysAllow.
            // Older hosts/unknown policy need a separately reviewed access path.
            var permission = BoolArg(board, Sel("respondsToSelector:"), Sel("accessBehavior")) != 0 &&
                             Send(board, Sel("accessBehavior")).ToInt64() == 2;
            var status = "REFUSED";
            var reason = "permission";
            if (permission)
            {
                stage = "capture";
                var epoch = Epoch(board);
                reason = Capture(board, buffers);
                if (Epoch(board) != epoch) { status = "BUSY"; reason = "owner_changed"; }
                else status = reason == "complete" ? "COMPLETE" : "REFUSED";
            }
            // This mode never serializes snapshots, hashes payloads, logs type
            // names or writes clipboard data to disk/stdout/another process.
            stage = "zero_buffers";
            Clear(buffers);
            Console.WriteLine($"general_snapshot status={status} reason={reason} secret_buffers=ZEROED mutation=NONE input=NONE");
            Console.WriteLine("general_snapshot_complete scope=READ_ONLY restore=NOT_RUN");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"general_snapshot_failed stage={stage} kind={error.GetType().Name}");
            return 1;
        }
        finally
        {
            Clear(buffers);
            if (pool != 0) SendVoid(pool, Sel("drain"));
        }
    }

    private static long Epoch(nint board)
    {
        var epoch = Send(board, Sel("changeCount")).ToInt64();
        if (epoch < 0) throw new InvalidOperationException();
        return epoch;
    }

    private static string Capture(nint board, List<byte[]> buffers)
    {
        var nativeItems = Send(board, Sel("pasteboardItems"));
        if (nativeItems == 0) return "missing_items";
        var count = Send(nativeItems, Sel("count")).ToInt64();
        if (count < 0 || count > 32) return "item_limit";
        if (count == 0)
        {
            var advertised = Send(board, Sel("types"));
            if (advertised != 0 && Send(advertised, Sel("count")).ToInt64() != 0) return "empty_with_types";
        }
        long total = 0;
        for (nuint index = 0; index < (nuint)count; index++)
        {
            var item = At(nativeItems, Sel("objectAtIndex:"), index);
            if (item == 0) return "missing_item";
            var types = Send(item, Sel("types"));
            if (types == 0) return "missing_types";
            var formats = Send(types, Sel("count")).ToInt64();
            if (formats <= 0 || formats > 16) return "format_limit";
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (nuint format = 0; format < (nuint)formats; format++)
            {
                var name = At(types, Sel("objectAtIndex:"), format);
                if (name == 0) return "missing_type";
                var nameLength = Send(name, Sel("length")).ToInt64();
                if (nameLength < 0 || nameLength > 256) return "unsupported_type";
                var text = Marshal.PtrToStringUTF8(Send(name, Sel("UTF8String")));
                if (text is null || !Supported.Contains(text)) return "unsupported_type";
                if (!seen.Add(text)) return "duplicate_type";
                var data = ObjectArg(item, Sel("dataForType:"), name);
                if (data == 0) return "unreadable_data";
                var length = Send(data, Sel("length")).ToInt64();
                if (length < 0 || length > Cap || total > Cap - length) return "byte_cap";
                var copy = new byte[(int)length];
                buffers.Add(copy); // Register BEFORE copying, including failure paths.
                if (length > 0)
                {
                    var bytes = Send(data, Sel("bytes"));
                    if (bytes == 0) return "missing_bytes";
                    Marshal.Copy(bytes, copy, 0, copy.Length);
                }
                total += length;
            }
        }
        return "complete";
    }

    private static void Clear(List<byte[]> buffers)
    {
        foreach (var buffer in buffers) CryptographicOperations.ZeroMemory(buffer);
        buffers.Clear();
    }

    [LibraryImport(ObjC, EntryPoint = "objc_getClass", StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint Class(string name);
    [LibraryImport(ObjC, EntryPoint = "sel_registerName", StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint Sel(string name);
    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    private static partial nint Send(nint receiver, nint selector);
    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    private static partial nint At(nint receiver, nint selector, nuint index);
    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    private static partial nint ObjectArg(nint receiver, nint selector, nint value);
    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    private static partial byte BoolArg(nint receiver, nint selector, nint value);
    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    private static partial void SendVoid(nint receiver, nint selector);
}
#endif
