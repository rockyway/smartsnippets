using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using SmartSnippets.Core.Logging;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Uno.Platforms.Windows;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Memory;
using Windows.Win32.System.Ole;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace SmartSnippets.Uno.Services;

internal sealed partial class ClipboardPasteDispatcher(HWND clipboardOwner) : IPasteDispatcher
{
    private const int FocusTimeoutMs = 500;
    private const long SnapshotLimit = 64L * 1024 * 1024;
    private const uint EnhancedMetafile = 14;
    private const string TransactionFormat = "SmartSnippets.UnoCandidate019.PasteTransaction";
    private static readonly string[] MarkerNames = ["ExcludeClipboardContentFromMonitorProcessing", "CanIncludeInClipboardHistory", "CanUploadToCloudClipboard"];
    private Snapshot? _pending;

    [LibraryImport("user32.dll")]
    private static partial uint GetClipboardSequenceNumber();
    [LibraryImport("user32.dll", EntryPoint = "EnumClipboardFormats", SetLastError = true)]
    private static partial uint EnumerateClipboardFormats(uint format);
    [LibraryImport("gdi32.dll")]
    private static unsafe partial uint GetEnhMetaFileBits(nint metafile, uint size, byte* buffer);
    [LibraryImport("gdi32.dll")]
    private static unsafe partial nint SetEnhMetaFileBits(uint size, byte* buffer);
    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DeleteEnhMetaFile(nint metafile);

    public async Task<FocusPath?> RestoreFocusAsync(InsertionTarget target, CancellationToken ct)
    {
        var hwnd = new HWND(target.WindowHandle);
        PInvoke.SetForegroundWindow(hwnd);
        if (await WaitForForegroundAsync(hwnd, ct)) return FocusPath.Direct;
        var current = PInvoke.GetCurrentThreadId();
        var foreground = PInvoke.GetForegroundWindow();
        var thread = foreground.IsNull ? 0 : PInvoke.GetWindowThreadProcessId(foreground, out _);
        var attached = thread != 0 && thread != current && PInvoke.AttachThreadInput(current, thread, true);
        try { PInvoke.SetForegroundWindow(hwnd); }
        finally { if (attached) PInvoke.AttachThreadInput(current, thread, false); }
        return await WaitForForegroundAsync(hwnd, ct) ? FocusPath.AttachThreadInput : null;
    }

    private static async Task<bool> WaitForForegroundAsync(HWND hwnd, CancellationToken ct)
    {
        var watch = Stopwatch.StartNew();
        while (watch.ElapsedMilliseconds < FocusTimeoutMs)
        {
            ct.ThrowIfCancellationRequested();
            if (PInvoke.GetForegroundWindow() == hwnd) return true;
            await Task.Delay(10, ct);
        }
        return PInvoke.GetForegroundWindow() == hwnd;
    }

    public bool ReleaseHeldModifiers()
    {
        VIRTUAL_KEY[] keys = [VIRTUAL_KEY.VK_SHIFT, VIRTUAL_KEY.VK_CONTROL, VIRTUAL_KEY.VK_MENU, VIRTUAL_KEY.VK_LWIN, VIRTUAL_KEY.VK_RWIN];
        var held = keys.Where(key => (PInvoke.GetAsyncKeyState((int)key) & 0x8000) != 0).ToArray();
        var inputs = new List<INPUT>();
        if (held.Any(key => key is VIRTUAL_KEY.VK_MENU or VIRTUAL_KEY.VK_LWIN or VIRTUAL_KEY.VK_RWIN))
        {
            inputs.Add(KeyInput((VIRTUAL_KEY)0xE8, false));
            inputs.Add(KeyInput((VIRTUAL_KEY)0xE8, true));
        }
        inputs.AddRange(held.Select(key => KeyInput(key, true)));
        if (inputs.Count == 0) return true;
        var batch = inputs.ToArray();
        try { if (Send(batch) == batch.Length) return true; }
        catch (Exception ex) { AppLog.Info("modifier_send_exception", $"kind={ex.GetType().Name}"); }
        // Only key-ups are safe to retry after a partially injected batch.
        var cleanup = new List<INPUT> { KeyInput((VIRTUAL_KEY)0xE8, true) };
        cleanup.AddRange(held.Select(key => KeyInput(key, true)));
        var cleaned = SendKeyUps(cleanup.ToArray());
        AppLog.Info("modifier_release_failed", $"cleanup={cleaned}");
        return false;
    }

    public ClipboardSwap TrySwapClipboard(string text, bool allowIncomplete)
    {
        if (!PInvoke.OpenClipboard(clipboardOwner)) return new(SwapStatus.Busy, null);
        Snapshot? snapshot = null;
        var status = SwapStatus.Failed;
        try
        {
            snapshot = TakeSnapshot();
            if (snapshot.UnsavedCount != 0 && !allowIncomplete)
                return new(SwapStatus.NotPreserved, null, snapshot.UnsavedCount, snapshot.CapExceeded);
            if (!PInvoke.EmptyClipboard()) return new(SwapStatus.Failed, null);
            // Once empty, retain the snapshot even if allocation or setting the text fails.
            _pending = snapshot;
            try { status = SetText(text, snapshot) ? SwapStatus.Swapped : SwapStatus.Failed; }
            catch (Exception ex) { AppLog.Info("clipboard_set_exception", $"kind={ex.GetType().Name}"); }
        }
        finally
        {
            if (_pending == snapshot && snapshot is not null)
            {
                try
                {
                    var beforeClose = GetClipboardSequenceNumber();
                    snapshot.Sequence = ClipboardPublication.CloseAndCapture(
                        () => PInvoke.CloseClipboard(), () => PInvoke.OpenClipboard(clipboardOwner),
                        () => RecognizesPublication(snapshot), MaterializeTextFormats,
                        GetClipboardSequenceNumber, () => PInvoke.CloseClipboard());
                    AppLog.Info("clipboard_publication", $"confirmed={snapshot.Sequence.HasValue} sequence_before_close={beforeClose} sequence_confirmed={snapshot.Sequence.GetValueOrDefault()}");
                    if (snapshot.Sequence is null) status = SwapStatus.Failed;
                }
                catch { status = SwapStatus.Failed; AppLog.Info("clipboard_publication_unconfirmed"); }
            }
            else PInvoke.CloseClipboard();
        }
        return new(status, snapshot);
    }

    public SwapStatus TryPlaceText(string text)
    {
        if (!PInvoke.OpenClipboard(clipboardOwner)) return SwapStatus.Busy;
        try { return PInvoke.EmptyClipboard() && SetText(text) ? SwapStatus.Swapped : SwapStatus.Failed; }
        finally { PInvoke.CloseClipboard(); }
    }

    public DispatchState SendPaste()
    {
        INPUT[] inputs = [KeyInput(VIRTUAL_KEY.VK_CONTROL, false), KeyInput(VIRTUAL_KEY.VK_V, false), KeyInput(VIRTUAL_KEY.VK_V, true), KeyInput(VIRTUAL_KEY.VK_CONTROL, true)];
        var count = Send(inputs);
        var state = count == 4 ? DispatchState.Dispatched : count == 0 ? DispatchState.NotDispatched : DispatchState.Uncertain;
        AppLog.Info("paste_sent", $"state={state} injected={count}");
        return state;
    }

    public bool ReleasePasteKeys() => SendKeyUps([KeyInput(VIRTUAL_KEY.VK_V, true), KeyInput(VIRTUAL_KEY.VK_CONTROL, true)]);

    private static bool SendKeyUps(INPUT[] inputs)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try { if (Send(inputs) == inputs.Length) return true; }
            catch (Exception ex) { AppLog.Info("key_up_send_exception", $"kind={ex.GetType().Name}"); }
        }
        return false;
    }

    public RestoreStatus TryRestoreClipboard(IClipboardSnapshot saved)
    {
        var snapshot = (Snapshot)saved;
        if (_pending != snapshot) return RestoreStatus.ClipboardChanged;
        if (!PInvoke.OpenClipboard(clipboardOwner)) return RestoreStatus.Busy;
        try
        {
            // If sealing was contended before paste, recovery may finish it under this lock.
            // A confirmed publication is NEVER rebased: later foreign writes retain legacy refusal.
            snapshot.Sequence ??= ClipboardPublication.CaptureLocked(
                () => RecognizesPublication(snapshot), MaterializeTextFormats, GetClipboardSequenceNumber);
            var currentSequence = GetClipboardSequenceNumber();
            var sameOwner = PInvoke.GetClipboardOwner() == clipboardOwner;
            if (!ClipboardPublication.CanRestore(snapshot.Sequence, currentSequence, sameOwner))
            {
                AppLog.Info("clipboard_restore_guard", $"confirmed={snapshot.Sequence.HasValue} expected={snapshot.Sequence.GetValueOrDefault()} current={currentSequence} same_owner={sameOwner}");
                _pending = null; return RestoreStatus.ClipboardChanged;
            }
            if (!PInvoke.EmptyClipboard()) { _pending = null; return RestoreStatus.Failed; }
            var failed = SetMarkers(out var markers) ? 0 : 1;
            foreach (var entry in snapshot.Entries)
                if (!markers.Contains(entry.Format) && !SetEntry(entry)) failed++;
            _pending = null;
            AppLog.Info("clipboard_restore_formats", $"failed_formats={failed}");
            return failed == 0 ? RestoreStatus.Restored : RestoreStatus.Partial;
        }
        finally { PInvoke.CloseClipboard(); }
    }

    public void DiscardClipboardSnapshot(IClipboardSnapshot snapshot)
    {
        if (ReferenceEquals(_pending, snapshot)) _pending = null;
    }

    public async Task<RestoreStatus> RestorePendingAsync()
    {
        var timings = InsertionTimings.Default;
        var (status, attempts) = await ClipboardSafety.RetryRestoreAsync(
            () => _pending is { } pending ? TryRestoreClipboard(pending) : RestoreStatus.Restored,
            timings.ClipboardAttempts, timings.ClipboardRetryDelay, delay => Task.Delay(delay));
        AppLog.Info("clipboard_restore", $"status={status} attempts={attempts} exit=True");
        return status;
    }

    private static bool Skipped(uint format) => format is 2 or 3 or 9 or 0x80 or 0x82 or 0x83 or 0x8e || format is >= 0x200 and <= 0x3ff;

    private static unsafe Snapshot TakeSnapshot()
    {
        var result = new Snapshot();
        var complete = ClipboardSafety.EnumerateFormats(
            previous =>
            {
                var format = EnumerateClipboardFormats(previous);
                return (format, format == 0 ? Marshal.GetLastPInvokeError() : 0);
            },
            format =>
        {
            if (Skipped(format)) { result.SkippedByDesignCount++; return; }
            if (result.CapExceeded) { result.UnsavedCount++; return; }
            var handle = PInvoke.GetClipboardData(format);
            if (handle.IsNull) { result.UnsavedCount++; return; }
            if (format == EnhancedMetafile)
            {
                var size = GetEnhMetaFileBits((nint)handle.Value, 0, null);
                if (size == 0) { result.UnsavedCount++; return; }
                if (result.ByteCount + size > SnapshotLimit) { result.UnsavedCount++; result.CapExceeded = true; return; }
                var bytes = new byte[size];
                fixed (byte* p = bytes)
                {
                    if (GetEnhMetaFileBits((nint)handle.Value, size, p) != size) { result.UnsavedCount++; return; }
                }
                result.Entries.Add(new(format, bytes)); result.ByteCount += size;
                return;
            }
            var global = new HGLOBAL(handle.Value);
            var length = PInvoke.GlobalSize(global);
            if (length == 0) { result.UnsavedCount++; return; }
            if (length > (nuint)SnapshotLimit || result.ByteCount + (long)length > SnapshotLimit) { result.UnsavedCount++; result.CapExceeded = true; return; }
            var source = PInvoke.GlobalLock(global);
            if (source is null) { result.UnsavedCount++; return; }
            try
            {
                var data = new byte[(int)length];
                new ReadOnlySpan<byte>(source, data.Length).CopyTo(data);
                result.Entries.Add(new(format, data)); result.ByteCount += data.Length;
            }
            finally { PInvoke.GlobalUnlock(global); }
        });
        if (!complete)
        {
            result.UnsavedCount++;
            AppLog.Info("clipboard_enumeration_failed");
        }
        return result;
    }

    private static bool SetText(string text, Snapshot? publication = null)
    {
        bool Write(uint format, byte[] bytes)
        {
            if (format == 0 || !SetData(format, bytes)) return false;
            publication?.PublishedEntries.Add(new(format, bytes));
            return true;
        }
        if (publication is not null && !Write(PInvoke.RegisterClipboardFormat(TransactionFormat), publication.Transaction.ToByteArray())) return false;
        // Retain the exact successful writes too: a partial/failed publication still needs recovery.
        return ClipboardSafety.PublishProtectedText(
            () => ClipboardSafety.InstallMarkers(MarkerNames, PInvoke.RegisterClipboardFormat,
                format => Write(format, new byte[4]), new HashSet<uint>()),
            () => Write((uint)CLIPBOARD_FORMAT.CF_UNICODETEXT, Encoding.Unicode.GetBytes(text + "\0")));
    }

    private static bool SetMarkers(out HashSet<uint> formats)
    {
        formats = [];
        var success = ClipboardSafety.InstallMarkers(
            MarkerNames,
            PInvoke.RegisterClipboardFormat, format => SetData(format, new byte[4]), formats);
        if (!success) AppLog.Info("clipboard_markers_failed");
        return success;
    }

    private static unsafe bool SetEntry(Entry entry)
    {
        if (entry.Format != EnhancedMetafile) return SetData(entry.Format, entry.Data);
        fixed (byte* p = entry.Data)
        {
            var metafile = SetEnhMetaFileBits((uint)entry.Data.Length, p);
            if (metafile == 0) return false;
            if (!PInvoke.SetClipboardData(entry.Format, new HANDLE(metafile)).IsNull) return true;
            DeleteEnhMetaFile(metafile);
            return false;
        }
    }

    private static unsafe bool SetData(uint format, ReadOnlySpan<byte> data)
    {
        var global = PInvoke.GlobalAlloc(GLOBAL_ALLOC_FLAGS.GMEM_MOVEABLE, (nuint)data.Length);
        if (global.IsNull) return false;
        var destination = PInvoke.GlobalLock(global);
        if (destination is null) { PInvoke.GlobalFree(global); return false; }
        data.CopyTo(new Span<byte>(destination, data.Length));
        PInvoke.GlobalUnlock(global);
        if (!PInvoke.SetClipboardData(format, new HANDLE(global.Value)).IsNull) return true;
        PInvoke.GlobalFree(global);
        return false;
    }

    private static unsafe uint Send(INPUT[] inputs)
    {
        fixed (INPUT* p = inputs) return PInvoke.SendInput((uint)inputs.Length, p, sizeof(INPUT));
    }

    private static INPUT KeyInput(VIRTUAL_KEY key, bool up)
    {
        var input = default(INPUT);
        input.type = INPUT_TYPE.INPUT_KEYBOARD;
        input.Anonymous.ki.wVk = key;
        input.Anonymous.ki.dwFlags = up ? KEYBD_EVENT_FLAGS.KEYEVENTF_KEYUP : 0;
        return input;
    }

    private bool RecognizesPublication(Snapshot snapshot)
    {
        if (PInvoke.GetClipboardOwner() != clipboardOwner) return false;
        var allowed = new HashSet<uint>();
        foreach (var entry in snapshot.PublishedEntries)
        {
            if (!MatchesData(entry.Format, entry.Data)) return false;
            allowed.Add(entry.Format);
        }
        if (allowed.Contains((uint)CLIPBOARD_FORMAT.CF_UNICODETEXT))
            allowed.UnionWith(new uint[] { 1, 7, 13, 16 }); // Windows' text/OEM/Unicode/locale synthesis only
        var recognized = true;
        return ClipboardSafety.EnumerateFormats(previous =>
        {
            var format = EnumerateClipboardFormats(previous);
            return (format, format == 0 ? Marshal.GetLastPInvokeError() : 0);
        }, format => { if (!allowed.Contains(format)) recognized = false; }) && recognized;
    }

    private static bool MaterializeTextFormats()
    {
        // Windows synthesizes ANSI/OEM/locale from Unicode during close/read. Resolve those before
        // recording the committed sequence so an ordinary target read cannot invalidate our token.
        foreach (uint format in new uint[] { 1, 7, 13, 16 })
            if (PInvoke.IsClipboardFormatAvailable(format) && PInvoke.GetClipboardData(format).IsNull) return false;
        return true;
    }

    private static unsafe bool MatchesData(uint format, ReadOnlySpan<byte> expected)
    {
        var handle = PInvoke.GetClipboardData(format);
        if (handle.IsNull) return false;
        var global = new HGLOBAL(handle.Value);
        if (PInvoke.GlobalSize(global) < (nuint)expected.Length) return false;
        var data = PInvoke.GlobalLock(global);
        if (data is null) return false;
        try { return new ReadOnlySpan<byte>(data, expected.Length).SequenceEqual(expected); }
        finally { PInvoke.GlobalUnlock(global); }
    }

    private sealed record Entry(uint Format, byte[] Data);
    private sealed class Snapshot : IClipboardSnapshot
    {
        public List<Entry> Entries { get; } = [];
        public int SavedCount => Entries.Count;
        public int SkippedByDesignCount { get; set; }
        public int UnsavedCount { get; set; }
        public bool CapExceeded { get; set; }
        public long ByteCount { get; set; }
        public uint? Sequence { get; set; }
        public Guid Transaction { get; } = Guid.NewGuid();
        public List<Entry> PublishedEntries { get; } = [];
    }
}
