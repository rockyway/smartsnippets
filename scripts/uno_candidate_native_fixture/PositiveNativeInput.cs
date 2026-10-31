using System.Runtime.InteropServices;
using System.Text;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Uno.Platforms.Windows.AcceptanceNative;

namespace SmartSnippets.Uno;

internal static partial class PositiveNativeInput
{
    internal static void PostShortcut(nint message, uint modifiers, uint key)
    {
        PositiveProtocol.RequireReviewed("posted-enter");
        if (GetWindowThreadProcessId(message, out var pid) == 0 || pid != (uint)Environment.ProcessId ||
            PostMessageW(message, 0x0312, AcceptanceHotkey.RegistrationId, (nint)((key << 16) | modifiers)) == 0)
            throw new InvalidOperationException("native_post_shortcut");
    }
    internal static unsafe DispatchState Enter(NativeOwnedPopup popup, Func<bool> lease)
    {
        PositiveProtocol.RequireReviewed("posted-enter");
        Input* inputs = stackalloc Input[2];
        inputs[0] = new() { Type = 1, Key = 0x0D };
        inputs[1] = new() { Type = 1, Key = 0x0D, Flags = 2 };
        // Final exact foreground/pin check follows all desktop/input/lease queries; no await, focus or logging between it and dispatch.
        if (!PositiveInputPreflight.Ready(lease, NativeDesktop.IsVisibleCurrent, NativeOwnedPopup.QuietAllInput,
            () => popup.PopupPinned && popup.Foreground == popup.Window)) return DispatchState.NotDispatched;
        return PositiveEnterGuard.Classify(SendInput(2, inputs, sizeof(Input)));
    }
    internal static unsafe bool VerifyCopiedSyntheticText()
    {
        // Read only after a successful explicit Copy. Never read/save the operator's baseline here.
        if (OpenClipboard(0) == 0) return false;
        try
        {
            var handle = GetClipboardData(13);
            var expected = Encoding.Unicode.GetBytes(GuardianClipboard.SyntheticText + "\0");
            if (handle == 0 || GlobalSize(handle) != (nuint)expected.Length) return false;
            var data = GlobalLock(handle);
            if (data == null) return false;
            try { return new ReadOnlySpan<byte>(data, expected.Length).SequenceEqual(expected); }
            finally { _ = GlobalUnlock(handle); }
        }
        finally { _ = CloseClipboard(); }
    }
    [StructLayout(LayoutKind.Explicit, Size = 40)] private struct Input
    { [FieldOffset(0)] public uint Type; [FieldOffset(8)] public ushort Key; [FieldOffset(12)] public uint Flags; }
    [LibraryImport("user32.dll")] private static unsafe partial uint SendInput(uint count, Input* inputs, int size);
    [LibraryImport("user32.dll")] private static partial int PostMessageW(nint window, uint message, nuint id, nint data);
    [LibraryImport("user32.dll")] private static partial uint GetWindowThreadProcessId(nint window, out uint pid);
    [LibraryImport("user32.dll")] private static partial int OpenClipboard(nint window);
    [LibraryImport("user32.dll")] private static partial int CloseClipboard();
    [LibraryImport("user32.dll")] private static partial nint GetClipboardData(uint format);
    [LibraryImport("kernel32.dll")] private static partial nuint GlobalSize(nint handle);
    [LibraryImport("kernel32.dll")] private static unsafe partial void* GlobalLock(nint handle);
    [LibraryImport("kernel32.dll")] private static partial int GlobalUnlock(nint handle);
}
