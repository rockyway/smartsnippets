namespace SmartSnippets.Presentation.Services;

/// <summary>Owner-root logical coordinates; prefer right of pointer, flip left at the edge.</summary>
public static class HoverDetailsPlacement
{
    public static (double X, double Y, double Width, double Height) Place(
        double rootWidth, double rootHeight, double pointerX, double pointerY, double desiredWidth, double desiredHeight)
    {
        var width = Math.Min(Math.Min(480, desiredWidth), Math.Max(1, rootWidth - 16));
        var height = Math.Min(Math.Min(320, desiredHeight), Math.Max(1, rootHeight - 16));
        var x = pointerX + 12;
        if (x + width > rootWidth - 8) x = pointerX - 12 - width;
        x = Math.Clamp(x, 0, Math.Max(0, rootWidth - width));
        var y = Math.Clamp(pointerY - 24, 0, Math.Max(0, rootHeight - height));
        return (x, y, width, height);
    }
}
