namespace SmartSnippets.Presentation.Services;

/// <summary>Owner-root coordinates; right of the pointer and above the target, with edge-aware fallback.</summary>
public static class HoverDetailsPlacement
{
    /// <summary>All coordinates use the same logical space; null means no usable space beside the window.</summary>
    public static (double X, double Y, double Width, double Height)? PlaceBesideWindow(
        double workLeft, double workTop, double workRight, double workBottom,
        double windowLeft, double windowRight, double windowTop, double desiredWidth, double windowHeight)
    {
        const double gap = 4;
        const double minimumWidth = 48; // Leave room for padding and a scrollbar.
        var right = Math.Max(0, workRight - Math.Max(workLeft, windowRight + gap));
        var left = Math.Max(0, Math.Min(workRight, windowLeft - gap) - workLeft);
        var width = Math.Min(480, desiredWidth);
        var useRight = right >= width || right >= left;
        width = Math.Min(width, useRight ? right : left);
        var height = Math.Min(windowHeight, workBottom - workTop);
        if (width < minimumWidth || height <= 26) return null;
        var x = useRight ? Math.Max(workLeft, windowRight + gap) : Math.Min(workRight, windowLeft - gap) - width;
        var y = Math.Clamp(windowTop, workTop, workBottom - height);
        return (x, y, width, height);
    }

    public static (double X, double Y, double Width, double Height) Place(
        double rootWidth, double rootHeight, double pointerX, double targetTop, double targetBottom,
        double desiredWidth, double desiredHeight)
    {
        var width = Math.Min(Math.Min(480, desiredWidth), Math.Max(1, rootWidth - 16));
        var height = Math.Min(Math.Min(320, desiredHeight), Math.Max(1, rootHeight - 16));
        var x = pointerX + 12;
        if (x + width > rootWidth - 8) x = pointerX - 12 - width;
        x = Math.Clamp(x, 0, Math.Max(0, rootWidth - width));
        const double gap = 6;
        var below = Math.Max(0, rootHeight - targetBottom - gap);
        var above = Math.Max(0, targetTop - gap);
        var useBelow = height > above && (height <= below || below > above);
        height = Math.Min(height, Math.Max(1, useBelow ? below : above));
        var y = useBelow ? targetBottom + gap : targetTop - gap - height;
        y = Math.Clamp(y, 0, Math.Max(0, rootHeight - height));
        return (x, y, width, height);
    }
}
