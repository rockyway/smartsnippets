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
            if (alive(retired) || resources() != before)
                throw new InvalidOperationException("shell_message_leak");
        }
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
