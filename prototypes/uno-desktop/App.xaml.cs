using System;
using System.Linq;
using Microsoft.UI.Xaml;

namespace SmartSnippets.UnoProbe;

public partial class App : Application
{
    private Window? _window;
    private LifecycleProbe? _lifecycle;

    public App() => InitializeComponent();

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Merely building/publishing this probe must not authorize a GUI launch.
        if (!Environment.GetCommandLineArgs().Contains("--authorized-probe-ui", StringComparer.Ordinal))
        {
            Exit();
            return;
        }

        var page = new MainPage();
        _window = new Window { Content = page };
        if (Environment.GetCommandLineArgs().Contains("--probe-ui-lifecycle", StringComparer.Ordinal))
            _lifecycle = new LifecycleProbe(this, _window, page);
        _window.Activate();
    }
}
