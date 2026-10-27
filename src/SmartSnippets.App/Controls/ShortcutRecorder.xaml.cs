using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using SmartSnippets.Core.Settings;
using Windows.System;
using Windows.UI.Core;

namespace SmartSnippets.App.Controls;

/// <summary>Read-only box that records a key combination while focused. Esc cancels, Backspace clears.</summary>
public sealed partial class ShortcutRecorder : UserControl
{
    private const string Placeholder = "Click here and press a shortcut";

    private string _displayText = string.Empty;

    public ShortcutRecorder()
    {
        InitializeComponent();
        ShowDisplay();
    }

    public event EventHandler<HotkeyGesture>? GestureCaptured;

    public event EventHandler? Cleared;

    /// <summary>Raised when Esc abandons a capture.</summary>
    public event EventHandler? Canceled;

    /// <summary>Text shown when not recording; set by the page from the view-model.</summary>
    public string DisplayText
    {
        get => _displayText;
        set
        {
            _displayText = value ?? string.Empty;
            ShowDisplay();
        }
    }

    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus(FocusState.Pointer);
    }

    protected override void OnLostFocus(RoutedEventArgs e)
    {
        base.OnLostFocus(e);
        ShowDisplay();
    }

    protected override void OnKeyDown(KeyRoutedEventArgs e)
    {
        var modifiers = ReadModifiers();
        var key = (uint)e.Key;

        if (e.Key == VirtualKey.Tab && (modifiers & (HotkeyGesture.ModControl | HotkeyGesture.ModAlt | HotkeyGesture.ModWin)) == 0)
        {
            base.OnKeyDown(e);
            return;
        }

        e.Handled = true;

        if (HotkeyGesture.IsModifierKey(key))
        {
            DisplayBlock.Text = PendingModifiersText(modifiers);
            return;
        }

        if (modifiers == 0 && e.Key == VirtualKey.Escape)
        {
            ShowDisplay();
            Canceled?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (modifiers == 0 && e.Key == VirtualKey.Back)
        {
            _displayText = string.Empty;
            ShowDisplay();
            Cleared?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (key == 0)
        {
            return;
        }

        var gesture = new HotkeyGesture(modifiers, key);
        _displayText = gesture.ToDisplayString();
        ShowDisplay();
        GestureCaptured?.Invoke(this, gesture);
    }

    protected override void OnKeyUp(KeyRoutedEventArgs e)
    {
        base.OnKeyUp(e);
        if (HotkeyGesture.IsModifierKey((uint)e.Key))
        {
            ShowDisplay();
            e.Handled = true;
        }
    }

    private static uint ReadModifiers()
    {
        var modifiers = 0u;
        if (IsDown(VirtualKey.Control))
        {
            modifiers |= HotkeyGesture.ModControl;
        }

        if (IsDown(VirtualKey.Menu))
        {
            modifiers |= HotkeyGesture.ModAlt;
        }

        if (IsDown(VirtualKey.Shift))
        {
            modifiers |= HotkeyGesture.ModShift;
        }

        if (IsDown(VirtualKey.LeftWindows) || IsDown(VirtualKey.RightWindows))
        {
            modifiers |= HotkeyGesture.ModWin;
        }

        return modifiers;
    }

    private static bool IsDown(VirtualKey key) =>
        InputKeyboardSource.GetKeyStateForCurrentThread(key).HasFlag(CoreVirtualKeyStates.Down);

    private static string PendingModifiersText(uint modifiers)
    {
        var text = string.Empty;
        if ((modifiers & HotkeyGesture.ModControl) != 0)
        {
            text += "Ctrl+";
        }

        if ((modifiers & HotkeyGesture.ModAlt) != 0)
        {
            text += "Alt+";
        }

        if ((modifiers & HotkeyGesture.ModShift) != 0)
        {
            text += "Shift+";
        }

        if ((modifiers & HotkeyGesture.ModWin) != 0)
        {
            text += "Win+";
        }

        return text.Length == 0 ? Placeholder : text + "...";
    }

    private void ShowDisplay() =>
        DisplayBlock.Text = _displayText.Length == 0 ? Placeholder : _displayText;
}
