using Windows.UI.ViewManagement;

namespace SmartSnippets.Uno.Services;

internal static class TaskbarTheme
{
    public static bool IsLight()
    {
        var color = new UISettings().GetColorValue(UIColorType.Background);
        return color.R + color.G + color.B > 382;
    }
}
