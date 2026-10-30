using System;
using System.Linq;
using Microsoft.UI.Xaml;

namespace SmartSnippets.UnoProbe;

public partial class App : Application
{
    private Window? _window;

    public App() => InitializeComponent();

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Merely building/publishing this probe must not authorize a GUI launch.
        if (!Environment.GetCommandLineArgs().Contains("--authorized-probe-ui", StringComparer.Ordinal))
        {
            Exit();
            return;
        }

        _window = new Window { Content = new MainPage() };
        _window.Activate();
    }
}
