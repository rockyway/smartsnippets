#if !WINDOWS
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Security.Cryptography;
using SmartSnippets.Presentation.Services;

namespace SmartSnippets.UnoProbe;

/// <summary>Native fidelity checks on one uniquely named PRIVATE board only.
/// General is used solely for identity and readonly change-count comparison.</summary>
internal sealed unsafe partial class MacPrivatePasteboardProbe : IDisposable
{
    private const int Limit = 64 * 1024 * 1024;
    private const string Plain = PasteboardSeedPolicy.Plain;
    private const string Utf16External = PasteboardSeedPolicy.Utf16External;
    private readonly nint _general, _board, _name;
    private bool _disposed;
    private static string _stage = "setup";
    private static string _captureState = "none";
    private static int _failureLine;
    private static string _unsupportedTypeDigest = "none";
    private static readonly HashSet<string> Supported = new(StringComparer.Ordinal)
    {
        Plain, Utf16External, "public.rtf", "public.html", "public.png", "public.tiff", "public.url",
        "org.nspasteboard.TransientType", "org.nspasteboard.ConcealedType"
    };
    private sealed record Item(Dictionary<string, byte[]> Data);
    private sealed class Snapshot(nint board, long epoch, List<Item> items) : IClipboardSnapshot
    {
        public nint Board { get; } = board;
        public long Epoch { get; set; } = epoch;
        public List<Item> Items { get; } = items;
        public int SavedCount => Items.Sum(i => i.Data.Count);
        public int SkippedByDesignCount => 0;
        public int UnsavedCount => 0;
        public bool CapExceeded => false;
        public long ByteCount => Items.Sum(i => i.Data.Values.Sum(b => (long)b.Length));
    }

    private MacPrivatePasteboardProbe()
    {
        var type = GetClass("NSPasteboard");
        _general = Send(type, Sel("generalPasteboard"));
        _board = Send(type, Sel("pasteboardWithUniqueName"));
        _name = Send(_board, Sel("name"));
        CheckPrivate();
    }

    private long Epoch
    {
        get
        {
            var value = Send(_board, Sel("changeCount")).ToInt64();
            Require(value >= 0);
            return value;
        }
    }
    private void CheckPrivate()
    {
        var generalName = _general != 0 ? Send(_general, Sel("name")) : 0;
        var currentName = _board != 0 ? Send(_board, Sel("name")) : 0;
        Require(!_disposed && _general != 0 && _board != 0 && _name != 0 && generalName != 0 && currentName != 0 &&
            _board != _general && _name != generalName);
        Require(SendBoolArg(_name, Sel("isEqualToString:"), generalName) == 0);
        Require(SendBoolArg(_name, Sel("isEqualToString:"), currentName) != 0);
    }

    private Snapshot? Capture(int cap, out bool exceeded, Action? afterRead = null)
    {
        CheckPrivate();
        _captureState = "started";
        _unsupportedTypeDigest = "none";
        exceeded = false;
        var epoch = Epoch;
        var nativeItems = Send(_board, Sel("pasteboardItems"));
        if (nativeItems == 0) return RefuseCapture("missing_items");
        var count = Send(nativeItems, Sel("count")).ToInt64();
        if (count < 0 || count > 32) return RefuseCapture("item_limit");
        if (count == 0)
        {
            // An empty item list with advertised types is not a proven empty
            // board: do not overwrite unavailable/deferred representations.
            var advertised = Send(_board, Sel("types"));
            if (advertised != 0 && Send(advertised, Sel("count")).ToInt64() != 0) return RefuseCapture("empty_with_types");
        }
        var items = new List<Item>();
        long total = 0;
        for (nuint index = 0; index < (nuint)count; index++)
        {
            var nativeItem = SendIndex(nativeItems, Sel("objectAtIndex:"), index);
            var types = Send(nativeItem, Sel("types"));
            if (nativeItem == 0 || types == 0) return RefuseCapture("missing_item_types");
            var formatCount = Send(types, Sel("count")).ToInt64();
            if (formatCount <= 0 || formatCount > 16) return RefuseCapture("format_limit");
            var data = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            for (nuint format = 0; format < (nuint)formatCount; format++)
            {
                var name = SendIndex(types, Sel("objectAtIndex:"), format);
                var text = Marshal.PtrToStringUTF8(Send(name, Sel("UTF8String")));
                if (text is null) return RefuseCapture("missing_type_name");
                if (!Supported.Contains(text))
                {
                    // Digest only a synthetic PRIVATE-board format identifier,
                    // never data bytes or anything from General.
                    _unsupportedTypeDigest = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
                    return RefuseCapture("unsupported_type");
                }
                if (data.ContainsKey(text)) return RefuseCapture("duplicate_type");
                var bytes = SendArg(nativeItem, Sel("dataForType:"), name);
                if (bytes == 0) return RefuseCapture("unreadable_data");
                var length = Send(bytes, Sel("length")).ToInt64();
                if (length < 0 || length > cap || total > cap - length) { exceeded = true; return RefuseCapture("byte_cap"); }
                var copy = new byte[(int)length];
                if (length > 0)
                {
                    var source = Send(bytes, Sel("bytes"));
                    if (source == 0) return RefuseCapture("missing_bytes");
                    Marshal.Copy(source, copy, 0, copy.Length);
                }
                data.Add(text, copy);
                total += length;
            }
            items.Add(new(data));
        }
        _captureState = "complete";
        afterRead?.Invoke();
        // Retain the start epoch. The caller classifies ownership change as
        // Busy (not an incomplete-format refusal), and must validate freshness.
        return new Snapshot(_board, epoch, items);
    }

    private static Snapshot? RefuseCapture(string reason) { _captureState = reason; return null; }

    private void Write(List<Item> items, Action? afterClear = null)
    {
        CheckPrivate();
        Require(items.Count <= 32);
        nint* objects = stackalloc nint[items.Count];
        for (var index = 0; index < items.Count; index++)
        {
            var item = Send(Send(GetClass("NSPasteboardItem"), Sel("alloc")), Sel("init"));
            Require(item != 0);
            // Autorelease the +1 item; the NSArray and pasteboard retain it.
            _ = Send(item, Sel("autorelease"));
            objects[index] = item;
            foreach (var (type, bytes) in items[index].Data)
            {
                fixed (byte* pointer = bytes)
                {
                    var data = SendBytes(GetClass("NSData"), Sel("dataWithBytes:length:"), pointer, (nuint)bytes.Length);
                    Require(data != 0 && SetData(item, Sel("setData:forType:"), data, Str(type)) != 0);
                }
            }
        }
        var array = items.Count > 0 ? SendArray(GetClass("NSArray"), Sel("arrayWithObjects:count:"), objects, (nuint)items.Count) : 0;
        Require(items.Count == 0 || array != 0);
        CheckPrivate();
        var previous = Epoch;
        var cleared = Send(_board, Sel("clearContents")).ToInt64();
        Require(cleared > 0);
        afterClear?.Invoke();
        Require(cleared > previous && Epoch == cleared);
        // Empty snapshots restore by clear alone; writeObjects:@[] is not a
        // successful write. This path is exercised by the native empty test.
        if (items.Count > 0) Require(SendBoolArg(_board, Sel("writeObjects:"), array) != 0);
    }

    private ClipboardSwap Swap(string text, int cap = Limit, bool failAfterClear = false, Action? afterCapture = null)
    {
        var snapshot = Capture(cap, out var exceeded, afterCapture);
        if (snapshot is null) return new(SwapStatus.NotPreserved, null, 1, exceeded);
        if (Epoch != snapshot.Epoch) return new(SwapStatus.Busy, null);
        var cleared = false;
        try
        {
            Write([TextItem(text)], () =>
            {
                cleared = true;
                if (failAfterClear) throw new PostClearFixtureException();
            });
            snapshot.Epoch = Epoch;
            return new(SwapStatus.Swapped, snapshot);
        }
        catch (Exception error)
        {
            // Private board only, but preserve the same post-clear ownership
            // rule required at the Presentation clipboard boundary.
            if (cleared)
            {
                // This private board has a single in-process writer; retain the
                // actual post-failure epoch so its saved bytes stay recoverable.
                snapshot.Epoch = Epoch;
                throw new ClipboardSwapException(snapshot, error);
            }
            throw;
        }
    }

    private RestoreStatus Restore(IClipboardSnapshot value)
    {
        CheckPrivate();
        if (value is not Snapshot snapshot || snapshot.Board != _board) return RestoreStatus.Failed;
        if (Epoch != snapshot.Epoch) return RestoreStatus.ClipboardChanged;
        Write(snapshot.Items);
        var actual = Capture(Limit, out _);
        return actual is not null && actual.Epoch == Epoch && Equal(actual.Items, snapshot.Items) ? RestoreStatus.Restored : RestoreStatus.Partial;
    }

    private sealed class PostClearFixtureException : Exception { }

    private bool HasContents(List<Item> expected)
    {
        var actual = Capture(Limit, out _);
        return actual is not null && actual.Epoch == Epoch && Equal(actual.Items, expected);
    }

    private static bool Equal(List<Item> left, List<Item> right) => left.Count == right.Count &&
        left.Zip(right).All(pair => pair.First.Data.Keys.SequenceEqual(pair.Second.Data.Keys) &&
            pair.First.Data.All(entry => pair.Second.Data.TryGetValue(entry.Key, out var bytes) && entry.Value.SequenceEqual(bytes)));
    private static Item TextItem(string text) => new(new() { [Plain] = Encoding.UTF8.GetBytes(text) });

    private static bool MatchesSeed(List<Item> actual, List<Item> seed) =>
        PasteboardSeedPolicy.Matches(actual.Select(i => i.Data).ToArray(), seed.Select(i => i.Data).ToArray());

    private List<Item> PublishAndCapture(List<Item> seed)
    {
        Write(seed);
        var actual = Capture(Limit, out _);
        Require(actual is not null && actual.Epoch == Epoch && MatchesSeed(actual.Items, seed));
        return actual!.Items;
    }

    private void Exercise()
    {
        var seed = new List<Item>
        {
            TextItem("Synthetic λ 日本語 😀\nsecond"),
            new(new()
            {
                // Representations of one item must describe the SAME content;
                // AppKit derives plain text from richer RTF/HTML representations.
                [Plain] = Encoding.UTF8.GetBytes("synthetic"),
                ["public.rtf"] = Encoding.ASCII.GetBytes("{\\rtf1\\ansi synthetic}"),
                ["public.html"] = Encoding.UTF8.GetBytes("<p>synthetic</p>"),
            }),
            new(new()
            {
                ["public.png"] = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAAC0lEQVR4nGNgAAIAAAUAAXpeqz8AAAAASUVORK5CYII="),
                ["org.nspasteboard.TransientType"] = [],
                ["org.nspasteboard.ConcealedType"] = [],
            }),
            new(new()
            {
                [Plain] = Encoding.UTF8.GetBytes("https://example.invalid/probe018"),
                ["public.url"] = Encoding.ASCII.GetBytes("https://example.invalid/probe018"),
            }),
        };
        _stage = "multi_item_seed";
        var original = PublishAndCapture(seed);
        var savedFormats = original.Sum(i => i.Data.Count);
        Require(savedFormats is >= 9 and <= 12);
        _stage = "multi_item_swap";
        var swap = Swap("Synthetic temporary swap");
        Require(swap.Status == SwapStatus.Swapped && swap.Snapshot is { UnsavedCount: 0 } && swap.Snapshot.SavedCount == savedFormats);
        var temporary = Capture(Limit, out _);
        Require(temporary is not null && temporary.Epoch == Epoch && MatchesSeed(temporary.Items, [TextItem("Synthetic temporary swap")]));
        _stage = "multi_item_restore";
        Require(Restore(swap.Snapshot!) == RestoreStatus.Restored && HasContents(original));
        Console.WriteLine("private_pasteboard_multi_item_roundtrip=PASS representations=EXACT items=4");

        _stage = "empty_roundtrip";
        Write([]);
        swap = Swap("Synthetic from empty");
        Require(swap.Status == SwapStatus.Swapped && swap.Snapshot is { SavedCount: 0 });
        Require(Restore(swap.Snapshot!) == RestoreStatus.Restored && HasContents([]));
        Console.WriteLine("private_pasteboard_empty_roundtrip=PASS");

        _stage = "post_clear_recovery";
        Write(original);
        try
        {
            _ = Swap("Synthetic failed swap", failAfterClear: true);
            throw new InvalidOperationException("Expected post-clear failure.");
        }
        catch (ClipboardSwapException error) when (error.InnerException is PostClearFixtureException)
        {
            Require(error.Snapshot.SavedCount == savedFormats && HasContents([]));
            Require(Restore(error.Snapshot) == RestoreStatus.Restored && HasContents(original));
        }
        Console.WriteLine("private_pasteboard_post_clear_recovery=PASS");

        _stage = "capture_owner_changed";
        var intervening = PublishAndCapture([TextItem("Synthetic capture owner")]);
        long interveningEpoch = -1;
        var busy = Swap("must not replace", afterCapture: () => { Write(intervening); interveningEpoch = Epoch; });
        Require(busy.Status == SwapStatus.Busy && busy.Snapshot is null && Epoch == interveningEpoch && HasContents(intervening));
        Console.WriteLine("private_pasteboard_capture_owner_changed=PASS mutation=NONE");

        _stage = "unknown_format";
        Write([new(new() { ["org.smartsnippets.probe018.unsupported"] = [1, 2, 3] })]);
        var before = Epoch;
        var refused = Swap("must not replace");
        Require(refused.Status == SwapStatus.NotPreserved && !refused.CapExceeded && refused.Snapshot is null && Epoch == before);
        Console.WriteLine("private_pasteboard_unknown_refused=PASS mutation=NONE");

        _stage = "byte_cap";
        var large = new List<Item> { new(new() { [Plain] = Encoding.UTF8.GetBytes(new string('x', 257)) }) };
        var largeActual = PublishAndCapture(large);
        before = Epoch;
        refused = Swap("must not replace", cap: 256);
        Require(refused.Status == SwapStatus.NotPreserved && refused.CapExceeded && refused.Snapshot is null && Epoch == before);
        Require(HasContents(largeActual));
        Console.WriteLine("private_pasteboard_cap_refused=PASS mutation=NONE");

        _stage = "unreadable_declared";
        // Advertised but unavailable data, not an async provider/lazy-file test.
        CheckPrivate();
        // A binary type avoids AppKit's automatically advertised text aliases.
        var unavailableType = Str("public.png");
        var types = SendArray(GetClass("NSArray"), Sel("arrayWithObjects:count:"), &unavailableType, 1);
        Require(unavailableType != 0 && types != 0);
        var previous = Epoch;
        before = Declare(_board, Sel("declareTypes:owner:"), types, 0).ToInt64();
        Require(before > previous && Epoch == before);
        var declaredItems = Send(_board, Sel("pasteboardItems"));
        Require(declaredItems != 0 && Send(declaredItems, Sel("count")).ToInt64() == 1);
        var declaredItem = SendIndex(declaredItems, Sel("objectAtIndex:"), 0);
        var declaredTypes = Send(declaredItem, Sel("types"));
        Require(declaredItem != 0 && declaredTypes != 0 && Send(declaredTypes, Sel("count")).ToInt64() == 1);
        Require(SendBoolArg(unavailableType, Sel("isEqualToString:"), SendIndex(declaredTypes, Sel("objectAtIndex:"), 0)) != 0);
        Require(SendArg(declaredItem, Sel("dataForType:"), unavailableType) == 0);
        refused = Swap("must not replace");
        Require(refused.Status == SwapStatus.NotPreserved && !refused.CapExceeded && refused.Snapshot is null && Epoch == before);
        Console.WriteLine("private_pasteboard_unreadable_refused=PASS mutation=NONE");

        _stage = "changed_owner";
        Write(original);
        swap = Swap("Synthetic temporary swap");
        Require(swap.Status == SwapStatus.Swapped && swap.Snapshot is not null);
        var replacement = PublishAndCapture([TextItem("Synthetic new owner")]);
        before = Epoch;
        Require(Restore(swap.Snapshot!) == RestoreStatus.ClipboardChanged && Epoch == before);
        Require(HasContents(replacement));
        Console.WriteLine("private_pasteboard_new_owner_preserved=PASS mutation=NONE");
    }

    public void Dispose()
    {
        if (_disposed) return;
        CheckPrivate();
        SendVoid(_board, Sel("releaseGlobally"));
        _disposed = true;
    }

    internal static int Run()
    {
        if (!OperatingSystem.IsMacOS()) return 2;
        _stage = "setup";
        nint pool = 0;
        try
        {
            _ = NativeLibrary.Load("/System/Library/Frameworks/AppKit.framework/AppKit");
            pool = Send(Send(GetClass("NSAutoreleasePool"), Sel("alloc")), Sel("init"));
            Require(pool != 0);
            var general = Send(GetClass("NSPasteboard"), Sel("generalPasteboard"));
            Require(general != 0);
            var originalEpoch = Send(general, Sel("changeCount"));
            Require(originalEpoch.ToInt64() >= 0);
            using (var probe = new MacPrivatePasteboardProbe())
            {
                probe.Exercise();
                _stage = "cleanup";
            }
            _stage = "general_epoch";
            Require(Send(general, Sel("changeCount")) == originalEpoch);
            Console.WriteLine("private_pasteboard_complete general_epoch=UNCHANGED general_content_reads=0 general_writes=0 input=NONE");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"private_pasteboard_failed stage={_stage} kind={error.GetType().Name} line={_failureLine} capture={_captureState} type_digest={_unsupportedTypeDigest} seed_policy={PasteboardSeedPolicy.LastFailure} seed_format={PasteboardSeedPolicy.LastFormat} seed_observed={PasteboardSeedPolicy.ObservedCategory}");
            return 1;
        }
        finally { if (pool != 0) SendVoid(pool, Sel("drain")); }
    }

    private static nint Str(string value) => SendString(GetClass("NSString"), Sel("stringWithUTF8String:"), value);
    private static void Require(bool value, [CallerLineNumber] int line = 0)
    {
        if (value) return;
        _failureLine = line;
        throw new InvalidOperationException("Private pasteboard assertion failed.");
    }
    private const string ObjC = "/usr/lib/libobjc.A.dylib";
    [LibraryImport(ObjC, EntryPoint = "objc_getClass", StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint GetClass(string name);
    [LibraryImport(ObjC, EntryPoint = "sel_registerName", StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint Sel(string name);
    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    private static partial nint Send(nint receiver, nint selector);
    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    private static partial nint SendIndex(nint receiver, nint selector, nuint index);
    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    private static partial nint SendArg(nint receiver, nint selector, nint argument);
    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    private static partial byte SendBoolArg(nint receiver, nint selector, nint argument);
    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    private static partial byte SetData(nint receiver, nint selector, nint data, nint type);
    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    private static partial nint SendBytes(nint receiver, nint selector, byte* bytes, nuint count);
    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    private static partial nint SendArray(nint receiver, nint selector, nint* objects, nuint count);
    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    private static partial nint Declare(nint receiver, nint selector, nint types, nint owner);
    [LibraryImport(ObjC, EntryPoint = "objc_msgSend", StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint SendString(nint receiver, nint selector, string value);
    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    private static partial void SendVoid(nint receiver, nint selector);
}
#endif
