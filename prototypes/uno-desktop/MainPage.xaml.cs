using Microsoft.UI.Xaml.Controls;

namespace SmartSnippets.UnoProbe;

public sealed partial class MainPage : Page
{
    public string Label { get; set; } = "Synthetic label";
    public string Body { get; set; } = "Synthetic Unicode: λ 日本語\nSecond line";

    public MainPage() => InitializeComponent();
}
