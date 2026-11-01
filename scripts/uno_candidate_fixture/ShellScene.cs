using SmartSnippets.Presentation.Navigation;

namespace SmartSnippets.Uno;

/// <summary>Fixed N3 protocol. Posted messages/callbacks are not physical pointer/tray proof.</summary>
internal interface IShellScene
{
    Task Checkpoint(string phase);
    Task PreviewOptions();
    Task Hover(bool open);
    Task Tray(string command);
    Task CloseMain();
    Task MessageCycles(int count);
    void RequirePage(PageKind page);
    void RequireHidden();
    void RequirePlacement();
    void RequireUnchanged();
}

internal interface IShellMessageWindow : IDisposable
{
    nint Handle { get; }
}

internal static class ShellMessageCycles
{
    internal static void Run(Func<IShellMessageWindow> create, Func<(uint Handles, uint User)> resources, Func<nint, bool> alive)
    {
        var before = resources();
        for (var i = 0; i < 20; i++)
        {
            nint retired;
            using (var window = create()) retired = window.Handle;
            if (retired == 0 || alive(retired) || resources() != before)
                throw new InvalidOperationException("shell_message_leak");
        }
    }
}

internal static class ShellOwnedPost
{
    internal static void Run(nint hwnd, Func<nint> expected, uint ownedPid,
        Func<nint, (uint Thread, uint Pid)> pidOf, Func<nint, bool> alive, Func<bool> post)
    {
        if (hwnd == 0 || hwnd != expected() || ownedPid == 0)
            throw new InvalidOperationException("shell_post");
        var identity = pidOf(hwnd);
        if (identity.Thread == 0 || identity.Pid == 0 || identity.Pid != ownedPid || !alive(hwnd))
            throw new InvalidOperationException("shell_post");
        // Small check-to-post TOCTOU remains: Win32 cannot atomically authenticate and post.
        // Re-read the retained object's handle; never adopt another same-PID HWND.
        if (hwnd != expected() || !post()) throw new InvalidOperationException("shell_post");
    }
}

internal static class ShellMessageState
{
    internal static void RequireLive(nint handle, Func<nint, bool> alive)
    {
        if (handle == 0 || !alive(handle)) throw new InvalidOperationException("shell_background");
    }
    internal static void RequireDisposed(nint current, nint retired, Func<nint, bool> alive)
    {
        if (current != 0 || retired == 0 || alive(retired)) throw new InvalidOperationException("fixture_cleanup_failed");
    }
}

internal static class ShellScene
{
    internal static readonly string[] Names = ["preview-options", "hover", "tray", "close-background", "message-window", "dpi", "coexistence"];
    internal static async Task Run(string scene, IShellScene surface)
    {
        // The legacy product has no owned-profile bootstrap. Never start it with a made-up
        // --profile switch (unknown options are ignored and would open production state).
        if (scene == "coexistence") throw new InvalidOperationException("shell_legacy_isolation_unavailable");
        if (!Names.Contains(scene, StringComparer.Ordinal)) throw new InvalidOperationException("shell_scene");
        await surface.Checkpoint("shell-ready");
        switch (scene)
        {
            case "preview-options":
                await surface.PreviewOptions();
                await surface.Checkpoint("shell-preview");
                break;
            case "hover":
                await surface.PreviewOptions();
                await surface.Hover(true);
                await surface.Checkpoint("shell-hover-open");
                await surface.Hover(false);
                await surface.Checkpoint("shell-hover-closed");
                break;
            case "tray":
                // Open must change visibility, not pass on the initial visible Snippets page.
                await surface.CloseMain(); surface.RequireHidden();
                await surface.Tray("open"); surface.RequirePage(PageKind.Snippets);
                await surface.Checkpoint("shell-tray-open");
                await surface.Tray("settings"); surface.RequirePage(PageKind.Settings);
                await surface.Checkpoint("shell-tray-settings");
                break;
            case "close-background":
                await surface.CloseMain(); surface.RequireHidden();
                await surface.Checkpoint("shell-background");
                await surface.Tray("open"); surface.RequirePage(PageKind.Snippets);
                await surface.Checkpoint("shell-reopened");
                break;
            case "message-window":
                // Runs before host construction: MessageWindow is deliberately single-owner.
                await surface.MessageCycles(20);
                await surface.Checkpoint("shell-message-cycles");
                break;
            case "dpi":
                surface.RequirePlacement();
                await surface.Checkpoint("shell-dpi");
                break;
        }
        surface.RequireUnchanged();
        await surface.Checkpoint("shell-verified");
        await surface.Tray("exit");
    }
}
