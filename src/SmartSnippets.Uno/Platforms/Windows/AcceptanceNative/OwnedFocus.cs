using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SmartSnippets.Presentation.Services;
using SmartSnippets.WindowsInsertion018;

namespace SmartSnippets.Uno.Platforms.Windows.AcceptanceNative;

internal interface IAcceptanceFocus
{
    bool AdmissionIdentityReady();
    bool ReadyForAdmission();
    Task<bool> ReadyForAdmissionAsync(CancellationToken ct);
    Task<bool> RestoreTargetAsync(CancellationToken ct);
    bool TryFocusPopup();
}
internal interface IOwnedFocusOs
{
    bool PopupPinned { get; }
    nint Foreground { get; }
    bool QuietDesktop();
    bool SetForeground(nint window);
}

internal sealed class OwnedFocus(ITargetProbe probe, InsertionTarget target, nint popup, IOwnedFocusOs os,
    Func<bool> permitted, Func<bool> focusPermitted, nint main = 0, Func<bool>? mainPinned = null,
    Action<string>? focusReceipt = null) : IAcceptanceFocus
{
    public bool AdmissionIdentityReady() => popup != 0 && permitted() && os.PopupPinned &&
        (main == 0 || mainPinned?.Invoke() == true) && probe.Check(target) == TargetCheck.Ok;
    private bool OwnedForeground(nint foreground) => AdmissionIdentityReady() &&
        (foreground == popup || (main != 0 && foreground == main) || foreground == target.WindowHandle);
    private bool OwnedForeground() => OwnedForeground(os.Foreground);
    public bool ReadyForAdmission() => OwnedForeground() && os.QuietDesktop() && OwnedForeground();
    public async Task<bool> ReadyForAdmissionAsync(CancellationToken ct)
    {
        var watch = Stopwatch.StartNew();
        var gap = new ForegroundNullGap();
        try
        {
            while (watch.ElapsedMilliseconds < 3000 && !ct.IsCancellationRequested && AdmissionIdentityReady())
            {
                var foreground = os.Foreground;
                var allowed = foreground == popup || foreground == target.WindowHandle;
                if (!gap.Observe(foreground == 0, allowed, watch.ElapsedMilliseconds)) return false;
                if (foreground != 0)
                {
                    if (!OwnedForeground(foreground)) return false;
                    if (os.QuietDesktop() && AdmissionIdentityReady()) return true;
                }
                await Task.Delay(50, ct);
            }
        }
        catch (OperationCanceledException) { }
        return false;
    }
    public async Task<bool> RestoreTargetAsync(CancellationToken ct)
    {
        if (!focusPermitted() || !await ReadyForAdmissionAsync(ct) || !focusPermitted() || !ReadyForAdmission()) return false;
        if (probe.IsTargetForeground(target)) return true;
        // No arbitrary current foreground window is ever focused/attached/inspected.
        if (!focusPermitted() || !ReadyForAdmission() || !OwnedForeground() || !os.SetForeground(target.WindowHandle)) return false;
        var watch = Stopwatch.StartNew();
        var gap = new ForegroundNullGap();
        while (watch.ElapsedMilliseconds <= 500)
        {
            ct.ThrowIfCancellationRequested();
            if (!permitted() || !focusPermitted() || !os.PopupPinned || probe.Check(target) != TargetCheck.Ok) return false;
            var foreground = os.Foreground;
            if (!gap.Observe(foreground == 0, foreground == popup || foreground == target.WindowHandle, watch.ElapsedMilliseconds)) return false;
            if (foreground != 0 && probe.IsTargetForeground(target)) return true;
            if (foreground != 0 && !OwnedForeground()) return false;
            await Task.Delay(10, ct);
        }
        return false;
    }
    public bool TryFocusPopup()
    {
        bool Result(bool value, string reason) { focusReceipt?.Invoke(reason); return value; }
        if (!focusPermitted()) return Result(false, "permission");
        var foreground = os.Foreground;
        if (!OwnedForeground(foreground) || !os.QuietDesktop() || !AdmissionIdentityReady()) return Result(false, "admission");
        if (foreground == popup) return Result(true, "already_popup");
        if (foreground != target.WindowHandle) return Result(false, "target");
        if (!os.PopupPinned) return Result(false, "popup");
        var focused = os.SetForeground(popup); // Same single attempt and short-circuit order.
        // The API return alone is not positive foreground evidence (including a partial/NULL transition).
        if (!focused) return Result(false, "foreground_refused");
        var confirmed = os.Foreground == popup;
        return Result(confirmed, confirmed ? "foreground_accepted" : "foreground_unconfirmed");
    }
}

/// <summary>An unguessable HWND property dies with the exact popup, preventing same-class handle-reuse adoption.</summary>
internal sealed partial class NativeOwnedPopup : IOwnedFocusOs, IDisposable
{
    private readonly nint window;
    internal Guid Marker { get; } = Guid.NewGuid();
    internal nint Window => window;
    internal uint Thread => thread;
    private string Property => PropertyName(Marker);
    private static string PropertyName(Guid marker) => "SmartSnippets.UnoCandidate019.Acceptance." + marker.ToString("N");
    private readonly Process self;
    private readonly PinnedNativeImage image;
    private readonly uint thread;
    private bool disposed;
    internal NativeOwnedPopup(nint window)
    {
        NativeAcceptanceFactory.RequireReviewed();
        this.window = window;
        self = Process.GetCurrentProcess();
        var path = Environment.ProcessPath ?? throw new InvalidOperationException("Owned candidate image refused.");
        using var file = File.OpenRead(path);
        image = PinnedNativeImage.Open(path, Convert.ToHexString(SHA256.HashData(file)), "SmartSnippets.UnoCandidate.exe");
        thread = GetWindowThreadProcessId(window, out var pid);
        if (thread == 0 || pid != (uint)Environment.ProcessId || !ExpectedClass() || SetPropW(window, Property, 1) == 0)
        { image.Dispose(); self.Dispose(); throw new InvalidOperationException("Owned candidate popup refused."); }
    }
    public bool PopupPinned => !disposed && RetainedIdentity.Pinned(self.SafeHandle, image) &&
        GetWindowThreadProcessId(window, out var pid) == thread && pid == (uint)Environment.ProcessId &&
        ExpectedClass() && GetPropW(window, Property) == 1;
    public nint Foreground => SmartSnippets.TargetIdentity018.ForegroundIdentity.Root(GetForegroundWindow());
    public bool QuietDesktop() => QuietAllInput();
    internal static bool QuietAllInput()
    {
        if (!InputPreflight.QuietCurrentDesktop()) return false;
        for (var key = 1; key < 256; key++) if ((GetAsyncKeyState(key) & 0x8000) != 0) return false;
        return true;
    }
    public bool SetForeground(nint target) => PopupPinned && SetForegroundWindow(target) != 0;
    internal static bool RemoteForegroundReady(nint window, uint pid, Guid marker, uint expectedThread) =>
        expectedThread != 0 && GetWindowThreadProcessId(window, out var actualPid) == expectedThread && actualPid == pid && ExpectedClass(window) &&
        GetPropW(window, PropertyName(marker)) == 1 && SmartSnippets.TargetIdentity018.ForegroundIdentity.Root(GetForegroundWindow()) == window;
    private bool ExpectedClass() => ExpectedClass(window);
    private static unsafe bool ExpectedClass(nint window)
    {
        const string expected = "WinUIDesktopWin32WindowClass";
        var text = stackalloc char[128];
        var count = GetClassNameW(window, text, 128);
        return count == expected.Length && new ReadOnlySpan<char>(text, count).SequenceEqual(expected);
    }
    public void Dispose()
    {
        if (disposed) return;
        if (GetPropW(window, Property) == 1 && RemovePropW(window, Property) != 1)
            throw new InvalidOperationException("Owned popup marker cleanup failed.");
        disposed = true; image.Dispose(); self.Dispose();
    }
    [LibraryImport("user32.dll")] private static partial nint GetForegroundWindow();
    [LibraryImport("user32.dll")] private static partial int SetForegroundWindow(nint window);
    [LibraryImport("user32.dll")] private static partial short GetAsyncKeyState(int key);
    [LibraryImport("user32.dll")] private static partial uint GetWindowThreadProcessId(nint window, out uint pid);
    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)] private static partial int SetPropW(nint window, string name, nint value);
    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)] private static partial nint GetPropW(nint window, string name);
    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)] private static partial nint RemovePropW(nint window, string name);
    [LibraryImport("user32.dll")] private static unsafe partial int GetClassNameW(nint window, char* name, int size);
}
