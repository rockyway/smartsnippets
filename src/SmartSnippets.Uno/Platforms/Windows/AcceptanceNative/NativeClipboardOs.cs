using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace SmartSnippets.Uno.Platforms.Windows.AcceptanceNative;

/// <summary>Conservative HGLOBAL-only acceptance subset. Any other representation refuses before EmptyClipboard.</summary>
internal sealed partial class NativeClipboardOs(nint window, uint marker, IReadOnlySet<uint> privacy) : IGuardianClipboardOs
{
    private const int ByteLimit = 4 * 1024 * 1024;
    public nint Window => window;
    public nint Owner => GetClipboardOwner();
    public bool TryOpen() => OpenClipboard(window) != 0;
    public void Close() => _ = CloseClipboard();
    public bool Empty() => EmptyClipboard() != 0;
    public unsafe ClipboardImage Read()
    {
        var entries = new Dictionary<uint, byte[]>();
        var complete = true;
        long total = 0;
        uint previous = 0;
        while (true)
        {
            Marshal.SetLastPInvokeError(0);
            var format = EnumClipboardFormats(previous);
            if (format == 0) { complete &= Marshal.GetLastPInvokeError() == 0; break; }
            if (format == previous || entries.ContainsKey(format)) { complete = false; break; }
            previous = format;
            // Handle formats, delayed/unreadable and arbitrary registered formats are not HGLOBAL promises.
            if (format is not (1 or 7 or 13 or 16) && format != marker && !privacy.Contains(format)) { complete = false; break; }
            var handle = GetClipboardData(format);
            if (handle == 0) { complete = false; break; }
            var size = GlobalSize(handle);
            if (size == 0 || size > ByteLimit || total + (long)size > ByteLimit) { complete = false; break; }
            var source = GlobalLock(handle);
            if (source == null) { complete = false; break; }
            try
            {
                var bytes = new ReadOnlySpan<byte>(source, (int)size).ToArray();
                entries.Add(format, bytes);
                total += bytes.Length;
            }
            finally { _ = GlobalUnlock(handle); }
        }
        return new(entries, complete);
    }
    public bool CanAllocateExact(int byteCount)
    {
        if (byteCount <= 0 || byteCount > ByteLimit) return false;
        var handle = GlobalAlloc(0x0042, (nuint)byteCount);
        if (handle == 0) return false;
        try { return GlobalSize(handle) == (nuint)byteCount; }
        finally { _ = GlobalFree(handle); }
    }
    public unsafe bool Write(uint format, byte[] bytes)
    {
        var handle = GlobalAlloc(0x0042, (nuint)bytes.Length); // movable + zero initialized
        if (handle == 0) return false;
        if (GlobalSize(handle) != (nuint)bytes.Length) { _ = GlobalFree(handle); return false; }
        var destination = GlobalLock(handle);
        if (destination == null) { _ = GlobalFree(handle); return false; }
        try { bytes.CopyTo(new Span<byte>(destination, bytes.Length)); }
        finally { _ = GlobalUnlock(handle); }
        if (SetClipboardData(format, handle) != 0) return true;
        _ = GlobalFree(handle);
        return false;
    }
    [LibraryImport("user32.dll")] private static partial int OpenClipboard(nint window);
    [LibraryImport("user32.dll")] private static partial int CloseClipboard();
    [LibraryImport("user32.dll")] private static partial nint GetClipboardOwner();
    [LibraryImport("user32.dll")] private static partial int EmptyClipboard();
    [LibraryImport("user32.dll", SetLastError = true)] private static partial uint EnumClipboardFormats(uint previous);
    [LibraryImport("user32.dll")] private static partial nint GetClipboardData(uint format);
    [LibraryImport("user32.dll")] private static partial nint SetClipboardData(uint format, nint handle);
    [LibraryImport("kernel32.dll")] private static partial nint GlobalAlloc(uint flags, nuint bytes);
    [LibraryImport("kernel32.dll")] private static partial nuint GlobalSize(nint handle);
    [LibraryImport("kernel32.dll")] private static unsafe partial void* GlobalLock(nint handle);
    [LibraryImport("kernel32.dll")] private static partial int GlobalUnlock(nint handle);
    [LibraryImport("kernel32.dll")] private static partial nint GlobalFree(nint handle);
}
