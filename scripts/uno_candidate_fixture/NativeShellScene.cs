using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using SmartSnippets.Presentation.Navigation;
using SmartSnippets.Uno.Controls;
using SmartSnippets.Uno.Services;

namespace SmartSnippets.Uno;

/// <summary>Linked-source fixture only. No SendInput, clipboard access or foreign-window messages.</summary>
internal sealed partial class NativeShellScene(UnoAppHost host, FixtureBoundary boundary) : IShellScene
{
    private const uint Seam = 0x8000 + 19;
    private const string Query = "acceptance synthetic";
    private long _snippet;
    internal static void CheckMessageCycles()
    {
        // Warm native class registration once, outside the measured interval.
        using (var warmup = new MessageWindow()) { }
        ShellMessageCycles.Run(() => new MeasuredWindow(), Resources, handle => IsWindow(handle) != 0);
    }
    private sealed class MeasuredWindow : IShellMessageWindow
    {
        private readonly MessageWindow _window = new();
        public nint Handle => WindowHandle(_window);
        public void Dispose() => _window.Dispose();
    }
    private static (uint Handles, uint User) Resources()
    {
        if (GetProcessHandleCount(-1, out var count) == 0) throw new InvalidOperationException("shell_handle_count");
        return (count, GetGuiResources(-1, 1));
    }
    public Task MessageCycles(int count)
    {
        if (count != 20 || !boundary.MessageCyclesVerified) throw new InvalidOperationException("shell_message_cycles");
        return Task.CompletedTask;
    }
    public async Task Checkpoint(string phase)
    {
        Program.Receipt(phase); await Program.WaitAsync(phase);
    }
    public async Task PreviewOptions()
    {
        if (_snippet == 0) _snippet = await CandidateAcceptanceFlow.ManagementAsync(host);
        // Exercise real LaunchOptions parsing and common host consumer, not direct ShowPreview.
        await host.ApplyLaunchOptionsAsync(LaunchOptions.Parse(["--preview-popup", "--preview-popup-query", Query, "--preview-popup-sort-menu"]));
        await Wait(() => boundary.Native.PopupWindow is { } popup && popup.ShellSortMenuOpen);
        CandidateAcceptanceFlow.RequirePreview(host, _snippet);
    }
    public async Task Hover(bool open)
    {
        var popup = boundary.Native.PopupWindow ?? throw new InvalidOperationException("shell_popup");
        var root = popup.ContentXamlRoot ?? throw new InvalidOperationException("shell_root");
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(popup);
        if (open)
        {
            // Hide the sort menu through the actual flyout, without copying/inserting.
            popup.ShellCloseSortMenu();
            var row = FindRow(root.Content) ?? throw new InvalidOperationException("shell_row");
            var point = row.TransformToVisual(root.Content).TransformPoint(new Windows.Foundation.Point(row.ActualWidth / 2, row.ActualHeight / 2));
            var dpi = GetDpiForWindow(hwnd);
            if (dpi == 0) throw new InvalidOperationException("shell_dpi");
            var x = (int)Math.Round(point.X * dpi / 96); var y = (int)Math.Round(point.Y * dpi / 96);
            if (x is < 0 or > 32767 || y is < 0 or > 32767) throw new InvalidOperationException("shell_pointer_bounds");
            PostOwned(hwnd, () => WinRT.Interop.WindowNative.GetWindowHandle(popup), 0x0200, 0, (y << 16) | x);
        }
        else
        {
            PostOwned(hwnd, () => WinRT.Interop.WindowNative.GetWindowHandle(popup), 0x0200, 0, (1 << 16) | 1); // Leave the label, then leave the window.
            PostOwned(hwnd, () => WinRT.Interop.WindowNative.GetWindowHandle(popup), 0x02A3, 0, 0); // WM_MOUSELEAVE, not input synthesis.
        }
        await Wait(() => HoverDetailsSurface.HasVisibleContent(root) == open);
        RequireUnchanged();
    }
    private static FrameworkElement? FindRow(DependencyObject node)
    {
        if (node is SnippetRowDetails row && row.IsLoaded && row.FindName("LabelText") is FrameworkElement label
            && label.ActualWidth > 0 && label.ActualHeight > 0) return label;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
            if (FindRow(VisualTreeHelper.GetChild(node, i)) is { } found) return found;
        return null;
    }
    public async Task Tray(string command)
    {
        var code = command switch { "open" => 1, "settings" => 2, "exit" => 5, _ => throw new InvalidOperationException("shell_command") };
        var window = boundary.Message ?? throw new InvalidOperationException("shell_message");
        var delivered = false;
        void Callback(uint message, nuint w, nint l)
        {
            if (message != Seam || w != (nuint)code || l != 0) return;
            delivered = true;
            // Same callbacks supplied to the real tray. This is NOT TrackPopupMenu/physical-click proof.
            (command switch { "open" => boundary.Open, "settings" => boundary.Settings, _ => boundary.ExitCallback })!();
        }
        window.MessageReceived += Callback;
        try
        {
            PostOwned(WindowHandle(window), () => WindowHandle(window), Seam, (nuint)code, 0);
            await Wait(() => delivered);
            // Host callbacks enqueue navigation/exit on the actual dispatcher.
            if (command != "exit") await Wait(() => boundary.Native.MainWindow?.CurrentPage == (command == "settings" ? PageKind.Settings : PageKind.Snippets)
                && boundary.Native.MainWindow.IsHidden == false);
        }
        finally { window.MessageReceived -= Callback; }
    }
    public async Task CloseMain()
    {
        var main = boundary.Native.MainWindow ?? throw new InvalidOperationException("shell_main");
        PostOwned(WinRT.Interop.WindowNative.GetWindowHandle(main), () => WinRT.Interop.WindowNative.GetWindowHandle(main), 0x0010, 0, 0);
        await Wait(() => main.IsHidden);
    }
    public void RequireHidden()
    {
        if (boundary.Native.MainWindow is not { IsHidden: true } || boundary.Message is null || boundary.TrayDisposed)
            throw new InvalidOperationException("shell_background");
        ShellMessageState.RequireLive(WindowHandle(boundary.Message), handle => IsWindow(handle) != 0);
    }
    public void RequirePage(PageKind page)
    {
        if (boundary.Native.MainWindow is not { IsHidden: false } main || main.CurrentPage != page)
            throw new InvalidOperationException("shell_navigation");
    }
    public void RequirePlacement()
    {
        var main = boundary.Native.MainWindow ?? throw new InvalidOperationException("shell_main");
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(main);
        var dpi = GetDpiForWindow(hwnd);
        // Observer independently verifies real monitor geometry and rejects primary.
        if (dpi == 0 || main.AppWindow.Size.Width <= 0 || main.AppWindow.Size.Height <= 0)
            throw new InvalidOperationException("shell_dpi");
    }
    public void RequireUnchanged()
    {
        if (host.Gate.IsAvailable || host.Store.Snippets.Any(row => row.Snippet.UsageCount != 0))
            throw new InvalidOperationException("shell_mutation");
    }
    internal static unsafe nint WindowHandle(MessageWindow window) => (nint)window.Handle.Value;
    internal static void RequireMessageDisposed(MessageWindow? window, nint retired)
    {
        if (window is null) throw new InvalidOperationException("fixture_cleanup_failed");
        ShellMessageState.RequireDisposed(WindowHandle(window), retired, handle => IsWindow(handle) != 0);
    }
    private static void PostOwned(nint hwnd, Func<nint> expected, uint message, nuint w, nint l) =>
        ShellOwnedPost.Run(hwnd, expected, (uint)Environment.ProcessId,
            handle => { var thread = GetWindowThreadProcessId(handle, out var pid); return (thread, pid); },
            handle => IsWindow(handle) != 0, () => PostMessage(hwnd, message, w, l) != 0);
    private static async Task Wait(Func<bool> ready)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!ready())
        {
            if (DateTime.UtcNow >= deadline) throw new InvalidOperationException("shell_callback_missing");
            await Task.Delay(25);
        }
    }
    [LibraryImport("user32.dll")] private static partial uint GetDpiForWindow(nint hwnd);
    [LibraryImport("user32.dll")] private static partial uint GetWindowThreadProcessId(nint hwnd, out uint pid);
    [LibraryImport("user32.dll")] private static partial int IsWindow(nint hwnd);
    [LibraryImport("user32.dll", EntryPoint = "PostMessageW")] private static partial int PostMessage(nint hwnd, uint message, nuint w, nint l);
    [LibraryImport("kernel32.dll")] private static partial int GetProcessHandleCount(nint process, out uint count);
    [LibraryImport("user32.dll")] private static partial uint GetGuiResources(nint process, uint flags);
}
