using System.Diagnostics;
using System.Runtime.InteropServices;
using SmartSnippets.Uno.Platforms.Windows;
using SmartSnippets.Uno.Platforms.Windows.AcceptanceNative;

namespace SmartSnippets.Uno;

internal sealed partial class OwnedPreparationWindows(AcceptanceFixture fixture, NativeAcceptanceSession session,
    CandidateNativeCapability capability) : IOwnedPreparationOs
{
    private uint windowThread, editorThread;
    public bool Permitted() => capability.Allows(CandidateNativeOperation.Focus) &&
        session.Validate(CandidateNativeOperation.Focus, session.SessionId, session.Target.WindowHandle, session.Target.ProcessId);
    public unsafe bool PinnedIdentity()
    {
        var lease = fixture.Lease;
        var a = lease.Readiness.WindowA;
        var editor = lease.Readiness.EditorA;
        if (!lease.IsAliveAndPinned() || a == 0 || editor == 0 || IsWindow(a) == 0 || IsWindow(editor) == 0 ||
            GetDlgItem(a, 101) != editor || IsChild(a, editor) == 0) return false;
        var wt = GetWindowThreadProcessId(a, out var wp);
        var et = GetWindowThreadProcessId(editor, out var ep);
        if (wt == 0 || et == 0 || wp != lease.ExpectedPid || ep != lease.ExpectedPid ||
            (windowThread != 0 && wt != windowThread) || (editorThread != 0 && et != editorThread)) return false;
        var text = stackalloc char[128];
        var count = GetClassNameW(a, text, 128);
        if (!new ReadOnlySpan<char>(text, count).SequenceEqual("SS_Target018_" + lease.Run.ToString("N"))) return false;
        count = GetClassNameW(editor, text, 128);
        if (!OwnedTargetPreparation.IsExpectedEditorClass(new ReadOnlySpan<char>(text, count))) return false;
        windowThread = wt; editorThread = et;
        return lease.IsAliveAndPinned();
    }
    public bool VisibleDesktop() => NativeDesktop.IsVisibleCurrent();
    public bool QuietAllInput() => NativeOwnedPopup.QuietAllInput();
    public nint Foreground => NativeFixtureOs.Foreground();
    public bool TryInputEpoch(out uint epoch)
    {
        var info = new LastInput { Size = 8 };
        var ok = GetLastInputInfo(ref info) != 0;
        epoch = info.Tick;
        return ok;
    }
    public long Milliseconds => (long)Stopwatch.GetElapsedTime(0).TotalMilliseconds;
    public Task DelayAsync(CancellationToken ct) => Task.Delay(25, ct);
    public async Task FocusEditorAsync(CancellationToken ct) => await fixture.Lease.CommandAsync("focus_a", ct);
    public bool TargetForeground() => session.IsTargetForeground(session.Target);
    [StructLayout(LayoutKind.Sequential)] private struct LastInput { public uint Size, Tick; }
    [LibraryImport("user32.dll")] private static partial int GetLastInputInfo(ref LastInput info);
    [LibraryImport("user32.dll")] private static partial int IsWindow(nint window);
    [LibraryImport("user32.dll")] private static partial nint GetDlgItem(nint window, int id);
    [LibraryImport("user32.dll")] private static partial int IsChild(nint parent, nint child);
    [LibraryImport("user32.dll")] private static partial uint GetWindowThreadProcessId(nint window, out uint pid);
    [LibraryImport("user32.dll")] private static unsafe partial int GetClassNameW(nint window, char* text, int capacity);
}

