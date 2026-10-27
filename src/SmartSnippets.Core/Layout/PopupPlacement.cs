namespace SmartSnippets.Core.Layout;

public readonly record struct PixelRect(int Left, int Top, int Right, int Bottom)
{
    public int Width => Right - Left;

    public int Height => Bottom - Top;
}

public readonly record struct PixelPoint(int X, int Y);

public static class PopupPlacement
{
    public static PixelPoint CenterIn(PixelRect workArea, int popupWidth, int popupHeight)
    {
        var x = workArea.Left + (workArea.Width - popupWidth) / 2;
        var y = workArea.Top + (workArea.Height - popupHeight) / 2;

        var maxX = workArea.Right - popupWidth;
        var maxY = workArea.Bottom - popupHeight;
        x = Math.Max(workArea.Left, Math.Min(x, maxX));
        y = Math.Max(workArea.Top, Math.Min(y, maxY));
        return new PixelPoint(x, y);
    }
}
