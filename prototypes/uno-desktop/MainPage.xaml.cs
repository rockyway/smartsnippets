using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Controls;

namespace SmartSnippets.UnoProbe;

public sealed partial class MainPage : Page, INotifyPropertyChanged
{
    private string _label = "Synthetic label";
    private string _body = "Synthetic Unicode: λ 日本語\nSecond line";
    private double _editorSize = 16;
    public string Label { get => _label; set { _label = value; Changed(); } }
    public string Body { get => _body; set { _body = value; Changed(); } }
    public double EditorSize { get => _editorSize; set { _editorSize = value; Changed(); } }
    public event PropertyChangedEventHandler? PropertyChanged;

    public MainPage() => InitializeComponent();

    internal Microsoft.UI.Xaml.FrameworkElement PointerRow => CompanionRow;
    internal void EnableCompanionPointerRow()
    {
        BodyEditor.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
        CompanionRow.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
    }

    private void Changed([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    internal void VerifyLiveCompiledBindings()
    {
        if (LabelEditor.Text != Label || !BodyMatches())
            throw new InvalidOperationException("Compiled initial binding failed.");
        Label = "Rebound synthetic label";
        Body = "Rebound synthetic λ😀\r\nline two";
        EditorSize = 21;
        // Verifies the actual controls, not the model against itself. Disconnect
        // any compiled property-change subscription and these assertions fail.
        if (LabelEditor.Text != Label || !BodyMatches() ||
            LabelEditor.FontSize != 21 || BodyEditor.FontSize != 21)
            throw new InvalidOperationException("Compiled live binding failed.");
    }

    // WinUI RichEdit represents line breaks as CR; Uno may retain LF/CRLF.
    // Preserve exact Unicode and line boundaries, not platform UI encoding.
    private bool BodyMatches() => (BodyEditor.Text.Contains('\r') || BodyEditor.Text.Contains('\n')) &&
        BodyEditor.Text.ReplaceLineEndings("\n") == Body.ReplaceLineEndings("\n");
}
