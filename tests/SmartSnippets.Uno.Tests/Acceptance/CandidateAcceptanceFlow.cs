using SmartSnippets.Core.Settings;
using SmartSnippets.Presentation.Navigation;

namespace SmartSnippets.Uno;

/// <summary>Programmatic real-host checks, not proof of rendered event delivery.</summary>
internal static class CandidateAcceptanceFlow
{
    internal static void RequireReady(UnoAppHost host, string root)
    {
        var log = Path.Combine(root, "logs", "app.log");
        if (host.Store is null || host.Snippets is null || host.Popup is null || !File.Exists(log)
            || File.ReadLines(log).Count(line => line.EndsWith(" INFO candidate_started", StringComparison.Ordinal)) != 1
            || File.ReadLines(log).Any(line => line.Contains(" ERROR ", StringComparison.Ordinal)))
            throw new InvalidOperationException("acceptance_not_ready");
    }

    internal static async Task<long> ManagementAsync(UnoAppHost host)
    {
        if (host.Store.Snippets.Count != 0 || host.Store.Folders.Count != 0)
            throw new InvalidOperationException("acceptance_not_fresh");
        await host.ApplyLaunchOptionsAsync(new(PageKind.Folders, false, null));
        host.Folders.NewFolderName = "Acceptance synthetic";
        await host.Folders.CreateAsync();
        var folder = host.Store.Folders.Single();
        var row = host.Folders.Rows.Single(r => r.Id == folder.Id);
        host.Folders.BeginRename(row); row.EditName = "Acceptance renamed";
        if (!await host.Folders.CommitRenameAsync(row)) throw new InvalidOperationException("acceptance_rename");
        await host.ApplyLaunchOptionsAsync(new(PageKind.Snippets, false, null));
        await host.Snippets.NewAsync();
        var editor = host.Snippets.Editor ?? throw new InvalidOperationException("acceptance_editor");
        editor.Text = "acceptance synthetic\nsecond 😀";
        editor.Label = "acceptance-label-canary";
        editor.IsLabelCustom = true;
        editor.TagsInput = "acceptance-tag-canary";
        editor.SelectedFolder = editor.FolderChoices.Single(choice => choice.Id == folder.Id);
        if (!await editor.SaveAsync()) throw new InvalidOperationException("acceptance_save");
        var snippet = host.Store.Snippets.Single().Snippet;
        if (snippet.FolderId != folder.Id || snippet.Label != "acceptance-label-canary")
            throw new InvalidOperationException("acceptance_assignment");
        editor.Text = "acceptance synthetic edited\nsecond 😀";
        if (!await editor.SaveAsync()) throw new InvalidOperationException("acceptance_update");
        // Destructive confirmation dialogs cannot be driven in this no-input stage.
        // Exercise only the real store delete-to-Unfiled invariant, not a GUI Delete claim.
        if (!host.Store.DeleteFolder(folder.Id).Success || host.Store.Snippets.Single().Snippet.FolderId is not null)
            throw new InvalidOperationException("acceptance_unfiled");
        await host.ApplyLaunchOptionsAsync(new(PageKind.Settings, false, null));
        host.SettingsPage.SelectedTheme = ThemeMode.Dark;
        if (AppSettings.Load(host.SettingsStore).Theme != ThemeMode.Dark)
            throw new InvalidOperationException("acceptance_settings");
        await host.ApplyLaunchOptionsAsync(new(PageKind.Snippets, false, null));
        host.Store.Load();
        if (host.Store.Snippets.Single().Snippet.Text != "acceptance synthetic edited\nsecond 😀")
            throw new InvalidOperationException("acceptance_reload");
        return snippet.Id;
    }

    internal static void RequirePreview(UnoAppHost host, long id)
    {
        if (!host.Popup.IsPreview || host.Popup.Query != "acceptance synthetic"
            || !host.Popup.Items.Any(row => row.SnippetId == id)
            || host.Store.Snippets.Single().Snippet.UsageCount != 0 || host.Gate.IsAvailable)
            throw new InvalidOperationException("acceptance_preview");
    }
}
