using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
using SmartSnippets.Core.Logging;
using SmartSnippets.Presentation.Services;
using Windows.ApplicationModel.DataTransfer;

namespace SmartSnippets.App.Services;

/// <summary>Plain-text clipboard writes for "Copy text"; called from UI-thread commands.</summary>
internal sealed class ClipboardService : IClipboardService
{
    public void SetText(string text)
    {
        if (DispatcherQueue.GetForCurrentThread() is null)
        {
            throw new InvalidOperationException("ClipboardService.SetText must be called on the UI thread.");
        }

        try
        {
            var package = new DataPackage();
            package.SetText(text);
            Clipboard.SetContent(package);
            Clipboard.Flush();
        }
        catch (COMException ex)
        {
            // Another process (clipboard manager, RDP) can hold the clipboard open. "Copy text" exists to
            // protect unsaved text, so it must never crash the app; the text stays in the editor.
            AppLog.Error("clipboard_copy_failed", ex);
        }
    }
}
