using SmartSnippets.Core;
using SmartSnippets.Core.Data;
using SmartSnippets.Presentation.ViewModels;

namespace SmartSnippets.Uno;

internal sealed record PopupCopyObservation(bool EditorAtCopy, PopupFooterState FooterAtCopy,
    bool SaveFailed, bool EditorEditableAfterCopy, bool EditorCanceled);

/// <summary>Closed-gate callback scenes over the real host VMs and owned SQLite profile.</summary>
internal static class PopupCopyConsumer
{
    internal static async Task<PopupCopyObservation> RunAsync(string scene, UnoAppHost host, long id, string ownedRoot)
    {
        if (host.Popup.IsPreview || host.Popup.Target is null) throw new InvalidOperationException("native_copy_target");
        var row = host.Popup.Items.Single(item => item.SnippetId == id);
        switch (scene)
        {
            case "popup-copy":
                if (!host.Popup.CanCopyRow(row)) throw new InvalidOperationException("native_copy_unavailable");
                var rowState = new PopupCopyObservation(host.Popup.IsEditorOpen, host.Popup.FooterState, false, false, false);
                await host.RunCopyOperationAsync(() => host.Popup.CopyRow(row));
                return rowState;
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
                var editorAtCopy = host.Popup.Editor == editor;
                var footerAtCopy = host.Popup.FooterState;
                var saveFailed = editor.HasSaveFailure;
                await host.RunCopyOperationAsync(() => editor.CopyTextCommand.Execute(null));
                editor.Text = text + "Synthetic editable draft";
                var editable = editor.IsDirty && editor.HasSaveFailure && !editor.IsSaving &&
                    editor.Text == text + "Synthetic editable draft" && host.Popup.Editor == editor;
                if (!editable) throw new InvalidOperationException("native_editor_not_editable");
                // No successful save/usage credit is needed to clean up the owned draft.
                await host.Popup.CancelEditorAsync(false);
                return new(editorAtCopy, footerAtCopy, saveFailed, editable, host.Popup.Editor is null);
            case "footer-copy":
                // Deliberate truthful pre-dispatch refusal: no keyboard arm, clipboard swap or paste.
                host.Gate.SuspendAdmission();
                try { await host.Popup.InsertRowAsync(row); }
                finally { host.Gate.ResumeAdmission(); host.RefreshClipboardAvailability(); }
                if (host.Popup.FooterState != PopupFooterState.Error || !host.Popup.CanCopyFailedText)
                    throw new InvalidOperationException("native_footer_missing");
                var footerState = new PopupCopyObservation(host.Popup.IsEditorOpen, host.Popup.FooterState, false, false, false);
                await host.RunCopyOperationAsync(() => host.Popup.CopyFailedTextCommand.Execute(null));
                return footerState;
            default: throw new InvalidOperationException("native_copy_scene");
        }
    }
}
