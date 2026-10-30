namespace SmartSnippets.Presentation.Services;

public enum PopupPreviewKey { Right, Left, Escape, Backspace }

[Flags]
public enum PopupPreviewEffect { None = 0, Handled = 1, Show = 2, Close = 4, ReturnSearch = 8 }

/// <summary>Logical result navigation is independent of the TextBox that retains keyboard focus.</summary>
public sealed class PopupKeyboardPreviewPolicy
{
    public bool IsNavigating { get; private set; }
    public bool IsPreviewOpen { get; private set; }
    public int CaretStart { get; private set; }
    public int CaretLength { get; private set; }
    private bool _backspaceHeld, _escapeHeld;

    public PopupPreviewEffect Navigated(long? selectedId, int caretStart, int caretLength)
    {
        if (selectedId is null) return Reset();
        if (!IsNavigating) { CaretStart = caretStart; CaretLength = caretLength; }
        IsNavigating = true;
        return IsPreviewOpen ? PopupPreviewEffect.Show : PopupPreviewEffect.None;
    }

    public PopupPreviewEffect Handle(PopupPreviewKey key, long? selectedId, bool contentVisible = false)
    {
        if (key == PopupPreviewKey.Backspace && _backspaceHeld) return PopupPreviewEffect.Handled;
        if (key == PopupPreviewKey.Escape && _escapeHeld) return PopupPreviewEffect.Handled;
        if (key == PopupPreviewKey.Backspace && IsNavigating)
        {
            _backspaceHeld = true;
            IsNavigating = IsPreviewOpen = false;
            return PopupPreviewEffect.Handled | PopupPreviewEffect.Close | PopupPreviewEffect.ReturnSearch;
        }
        if (key == PopupPreviewKey.Escape && (IsPreviewOpen || contentVisible))
        {
            _escapeHeld = true;
            IsPreviewOpen = false;
            return PopupPreviewEffect.Handled | PopupPreviewEffect.Close;
        }
        if (!IsNavigating) return PopupPreviewEffect.None;
        if (key == PopupPreviewKey.Left && IsPreviewOpen)
        {
            IsPreviewOpen = false;
            return PopupPreviewEffect.Handled | PopupPreviewEffect.Close;
        }
        if (key != PopupPreviewKey.Right) return PopupPreviewEffect.None;
        if (selectedId is null) return Reset() | PopupPreviewEffect.Handled;
        if (IsPreviewOpen) return PopupPreviewEffect.Handled;
        IsPreviewOpen = true;
        return PopupPreviewEffect.Handled | PopupPreviewEffect.Show;
    }

    public void PreviewClosed() => IsPreviewOpen = false;
    public void Released(PopupPreviewKey key)
    {
        if (key == PopupPreviewKey.Backspace) _backspaceHeld = false;
        if (key == PopupPreviewKey.Escape) _escapeHeld = false;
    }
    public PopupPreviewEffect Reset(bool releaseKeys = false)
    {
        var effect = IsPreviewOpen ? PopupPreviewEffect.Close : PopupPreviewEffect.None;
        IsNavigating = IsPreviewOpen = false;
        if (releaseKeys) _backspaceHeld = _escapeHeld = false;
        return effect;
    }
}
