using System.Diagnostics;
using System.Text;
using SmartSnippets.Core.Logging;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Memory;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Windows.Win32.System.Ole;

namespace SmartSnippets.App.Services;

internal sealed class ClipboardPasteInserter : IInserter
{
    public static readonly TimeSpan RestoreDelay = TimeSpan.FromMilliseconds(250);

    private const int FocusTimeoutMs = 500;
    private const int ClipboardRetries = 10;
    private const int ClipboardRetryDelayMs = 20;

    private readonly HWND _clipboardOwner;

    public ClipboardPasteInserter(HWND clipboardOwner)
    {
        _clipboardOwner = clipboardOwner;
    }

    public async Task<InsertResult> InsertAsync(ForegroundTargetInfo target, string text, CancellationToken ct = default)
    {
        var result = await InsertCoreAsync(target, text, ct);
        AppLog.Info("insert_result", result.Outcome.ToString());
        return result;
    }

    private async Task<InsertResult> InsertCoreAsync(ForegroundTargetInfo target, string text, CancellationToken ct)
    {
        if (ElevationProbe.IsBlocked(target))
        {
            return new InsertResult(InsertOutcome.TargetElevated, "The target application runs as administrator, so SmartSnippets cannot insert text into it.");
        }

        if (!PInvoke.IsWindow(target.Hwnd))
        {
            return new InsertResult(InsertOutcome.TargetClosed, "The target window was closed.");
        }

        if (!await RestoreFocusAsync(target.Hwnd))
        {
            return new InsertResult(InsertOutcome.FocusFailed, "Could not return focus to the target window.");
        }

        if (!TryOpenClipboard())
        {
            return new InsertResult(InsertOutcome.ClipboardBusy, "The clipboard is in use by another application.");
        }

        List<ClipboardEntry> snapshot;
        var textSet = false;
        try
        {
            snapshot = TakeSnapshot(out var skipped);
            AppLog.Info("clipboard_snapshot", $"formats={snapshot.Count} skipped={skipped}");
            PInvoke.EmptyClipboard();
            textSet = SetText(text);
        }
        finally
        {
            PInvoke.CloseClipboard();
        }

        try
        {
            if (!textSet)
            {
                return new InsertResult(InsertOutcome.ClipboardBusy, "Could not place the text on the clipboard.");
            }

            ReleaseHeldModifiers();

            if (!SendPaste())
            {
                return new InsertResult(InsertOutcome.SendInputFailed, "The paste keystroke could not be sent.");
            }

            AppLog.Info("restore_delay_ms", ((int)RestoreDelay.TotalMilliseconds).ToString());
            await Task.Delay(RestoreDelay, CancellationToken.None);
            return new InsertResult(InsertOutcome.Success, "Inserted.");
        }
        finally
        {
            RestoreClipboard(snapshot);
        }
    }

    private async Task<bool> RestoreFocusAsync(HWND target)
    {
        PInvoke.SetForegroundWindow(target);
        if (await WaitForForegroundAsync(target))
        {
            AppLog.Info("focus_path", "direct");
            return true;
        }

        var currentThread = PInvoke.GetCurrentThreadId();
        var foreground = PInvoke.GetForegroundWindow();
        var foregroundThread = foreground.IsNull ? 0 : PInvoke.GetWindowThreadProcessId(foreground, out _);
        var attached = foregroundThread != 0 && foregroundThread != currentThread
            && PInvoke.AttachThreadInput(currentThread, foregroundThread, true);
        try
        {
            PInvoke.SetForegroundWindow(target);
        }
        finally
        {
            if (attached)
            {
                PInvoke.AttachThreadInput(currentThread, foregroundThread, false);
            }
        }

        if (await WaitForForegroundAsync(target))
        {
            AppLog.Info("focus_path", "attach_thread_input");
            return true;
        }

        AppLog.Info("focus_path", "failed");
        return false;
    }

    private static async Task<bool> WaitForForegroundAsync(HWND target)
    {
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < FocusTimeoutMs)
        {
            if (PInvoke.GetForegroundWindow() == target)
            {
                return true;
            }

            await Task.Delay(10);
        }

        return PInvoke.GetForegroundWindow() == target;
    }

    private bool TryOpenClipboard()
    {
        for (var i = 0; i < ClipboardRetries; i++)
        {
            if (PInvoke.OpenClipboard(_clipboardOwner))
            {
                return true;
            }

            Thread.Sleep(ClipboardRetryDelayMs);
        }

        return false;
    }

    private static bool IsSkippedFormat(uint format) => format switch
    {
        (uint)CLIPBOARD_FORMAT.CF_BITMAP => true,
        (uint)CLIPBOARD_FORMAT.CF_METAFILEPICT => true,
        (uint)CLIPBOARD_FORMAT.CF_PALETTE => true,
        (uint)CLIPBOARD_FORMAT.CF_ENHMETAFILE => true,
        (uint)CLIPBOARD_FORMAT.CF_OWNERDISPLAY => true,
        (uint)CLIPBOARD_FORMAT.CF_DSPBITMAP => true,
        (uint)CLIPBOARD_FORMAT.CF_DSPMETAFILEPICT => true,
        (uint)CLIPBOARD_FORMAT.CF_DSPENHMETAFILE => true,
        _ => format >= (uint)CLIPBOARD_FORMAT.CF_GDIOBJFIRST && format <= (uint)CLIPBOARD_FORMAT.CF_GDIOBJLAST,
    };

    private static unsafe List<ClipboardEntry> TakeSnapshot(out int skipped)
    {
        var entries = new List<ClipboardEntry>();
        skipped = 0;
        uint format = 0;
        while ((format = PInvoke.EnumClipboardFormats(format)) != 0)
        {
            if (IsSkippedFormat(format))
            {
                skipped++;
                continue;
            }

            var handle = PInvoke.GetClipboardData(format);
            if (handle.IsNull)
            {
                skipped++;
                continue;
            }

            var global = new HGLOBAL(handle.Value);
            var size = PInvoke.GlobalSize(global);
            if (size == 0 || size > int.MaxValue)
            {
                skipped++;
                continue;
            }

            var source = PInvoke.GlobalLock(global);
            if (source is null)
            {
                skipped++;
                continue;
            }

            try
            {
                var data = new byte[(int)size];
                new ReadOnlySpan<byte>(source, data.Length).CopyTo(data);
                entries.Add(new ClipboardEntry(format, data));
            }
            finally
            {
                PInvoke.GlobalUnlock(global);
            }
        }

        return entries;
    }

    private static bool SetText(string text)
    {
        if (!SetData((uint)CLIPBOARD_FORMAT.CF_UNICODETEXT, Encoding.Unicode.GetBytes(text + "\0")))
        {
            return false;
        }

        var excludeFormat = PInvoke.RegisterClipboardFormat("ExcludeClipboardContentFromMonitorProcessing");
        if (excludeFormat != 0)
        {
            SetData(excludeFormat, new byte[4]);
        }

        return true;
    }

    private static unsafe bool SetData(uint format, ReadOnlySpan<byte> data)
    {
        var global = PInvoke.GlobalAlloc(GLOBAL_ALLOC_FLAGS.GMEM_MOVEABLE, (nuint)data.Length);
        if (global.IsNull)
        {
            return false;
        }

        var destination = PInvoke.GlobalLock(global);
        if (destination is null)
        {
            PInvoke.GlobalFree(global);
            return false;
        }

        data.CopyTo(new Span<byte>(destination, data.Length));
        PInvoke.GlobalUnlock(global);

        if (PInvoke.SetClipboardData(format, new HANDLE(global.Value)).IsNull)
        {
            PInvoke.GlobalFree(global);
            return false;
        }

        return true;
    }

    private void RestoreClipboard(List<ClipboardEntry> snapshot)
    {
        try
        {
            if (!TryOpenClipboard())
            {
                AppLog.Info("clipboard_restore_failed", "busy");
                return;
            }

            try
            {
                PInvoke.EmptyClipboard();
                var failed = 0;
                foreach (var entry in snapshot)
                {
                    if (!SetData(entry.Format, entry.Data))
                    {
                        failed++;
                    }
                }

                AppLog.Info("clipboard_restored", $"formats={snapshot.Count} failed={failed}");
            }
            finally
            {
                PInvoke.CloseClipboard();
            }
        }
        catch (Exception ex)
        {
            AppLog.Error("clipboard_restore_failed", ex);
        }
    }

    private static void ReleaseHeldModifiers()
    {
        VIRTUAL_KEY[] modifiers = [VIRTUAL_KEY.VK_SHIFT, VIRTUAL_KEY.VK_CONTROL, VIRTUAL_KEY.VK_MENU, VIRTUAL_KEY.VK_LWIN, VIRTUAL_KEY.VK_RWIN];
        var inputs = new List<INPUT>();
        foreach (var key in modifiers)
        {
            if ((PInvoke.GetAsyncKeyState((int)key) & 0x8000) != 0)
            {
                inputs.Add(KeyInput(key, up: true));
            }
        }

        if (inputs.Count > 0)
        {
            Send(inputs.ToArray());
        }
    }

    private static bool SendPaste()
    {
        INPUT[] inputs =
        [
            KeyInput(VIRTUAL_KEY.VK_CONTROL, up: false),
            KeyInput(VIRTUAL_KEY.VK_V, up: false),
            KeyInput(VIRTUAL_KEY.VK_V, up: true),
            KeyInput(VIRTUAL_KEY.VK_CONTROL, up: true),
        ];
        return Send(inputs) == inputs.Length;
    }

    private static unsafe uint Send(INPUT[] inputs)
    {
        fixed (INPUT* p = inputs)
        {
            return PInvoke.SendInput((uint)inputs.Length, p, sizeof(INPUT));
        }
    }

    private static INPUT KeyInput(VIRTUAL_KEY key, bool up)
    {
        var input = default(INPUT);
        input.type = INPUT_TYPE.INPUT_KEYBOARD;
        input.Anonymous.ki.wVk = key;
        input.Anonymous.ki.dwFlags = up ? KEYBD_EVENT_FLAGS.KEYEVENTF_KEYUP : 0;
        return input;
    }

    private sealed record ClipboardEntry(uint Format, byte[] Data);
}
