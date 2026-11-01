using SmartSnippets.Core;
using SmartSnippets.Core.Data;
using SmartSnippets.Presentation.ViewModels;

namespace SmartSnippets.Uno.Platforms.Windows.AcceptanceNative;

/// <summary>Closed-gate callback scenes over the real host VMs and owned SQLite profile.</summary>
internal static class PopupCopyConsumer
{
    internal static async Task RunAsync(string scene, UnoAppHost host, long id, string ownedRoot)
    {
        if (host.Popup.IsPreview || host.Popup.Target is null) throw new InvalidOperationException("native_copy_target");
        var row = host.Popup.Items.Single(item => item.SnippetId == id);
        switch (scene)
        {
            case "popup-copy":
                if (!host.Popup.CanCopyRow(row)) throw new InvalidOperationException("native_copy_unavailable");
                await host.RunCopyOperationAsync(() => host.Popup.CopyRow(row));
                break;
            case "editor-copy":
                host.Popup.EditRow(row);
                var editor = host.Popup.Editor ?? throw new InvalidOperationException("native_editor_missing");
                var text = editor.Text;
                editor.Label = "Synthetic changed";
                // A genuine persistence failure, not validation failure or a fabricated HasSaveFailure flag.
                using (var connection = new SqliteConnectionFactory(new AppPaths(ownedRoot).DatabasePath).Open())
                {
                    using var command = connection.CreateCommand();
                    command.CommandText = "CREATE TRIGGER native_refuse_save BEFORE UPDATE ON snippets BEGIN SELECT RAISE(ABORT,'synthetic'); END;";
                    command.ExecuteNonQuery();
                    try
                    {
                        if (await editor.SaveAsync() || !editor.HasSaveFailure || !editor.IsDirty || editor.IsSaving || editor.Text != text || host.Popup.Editor != editor)
                            throw new InvalidOperationException("native_editor_failure");
                    }
                    finally
                    {
                        command.CommandText = "DROP TRIGGER native_refuse_save;";
                        command.ExecuteNonQuery();
                    }
                }
                if (!editor.CopyTextCommand.CanExecute(null)) throw new InvalidOperationException("native_editor_copy_denied");
                await host.RunCopyOperationAsync(() => editor.CopyTextCommand.Execute(null));
                editor.Text = text + "Synthetic editable draft";
                if (!editor.IsDirty || !editor.HasSaveFailure || editor.IsSaving || editor.Text != text + "Synthetic editable draft" || host.Popup.Editor != editor)
                    throw new InvalidOperationException("native_editor_not_editable");
                // No successful save/usage credit is needed to clean up the owned draft.
                await host.Popup.CancelEditorAsync(false);
                break;
            case "footer-copy":
                // Deliberate truthful pre-dispatch refusal: no keyboard arm, clipboard swap or paste.
                host.Gate.SuspendAdmission();
                try { await host.Popup.InsertRowAsync(row); }
                finally { host.Gate.ResumeAdmission(); host.RefreshClipboardAvailability(); }
                if (host.Popup.FooterState != PopupFooterState.Error || !host.Popup.CanCopyFailedText)
                    throw new InvalidOperationException("native_footer_missing");
                await host.RunCopyOperationAsync(() => host.Popup.CopyFailedTextCommand.Execute(null));
                break;
            default: throw new InvalidOperationException("native_copy_scene");
        }
    }
}
