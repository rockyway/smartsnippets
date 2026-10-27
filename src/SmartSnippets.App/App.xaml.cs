using Microsoft.UI.Xaml;
using SmartSnippets.App.Diagnostics;

namespace SmartSnippets.App;

public partial class App : Application
{
    private Window? _diagnosticsWindow;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _diagnosticsWindow = new DiagnosticsWindow();
        _diagnosticsWindow.Activate();
    }
}
