using SmartSnippets.Presentation.Services;
using Windows.ApplicationModel.DataTransfer;

namespace SmartSnippets.App.Services;

/// <summary>Plain-text clipboard writes for "Copy text"; called from UI-thread commands.</summary>
internal sealed class ClipboardService : IClipboardService
{
    public void SetText(string text)
    {
        var package = new DataPackage();
        package.SetText(text);
        Clipboard.SetContent(package);
        Clipboard.Flush();
    }
}
