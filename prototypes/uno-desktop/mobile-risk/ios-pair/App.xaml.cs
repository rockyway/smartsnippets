using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SmartSnippets.IosPair;

public sealed partial class App : Application
{
    private Window? _window;

    public App() => InitializeComponent();

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new Window
        {
            Content = new TextBlock { Text = "018 build-only iOS pair fixture" }
        };
        _window.Activate();
    }
}
