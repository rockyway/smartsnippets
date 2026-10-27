using Microsoft.UI.Xaml;

namespace SmartSnippets.App;

public partial class App : Application
{
    private readonly AppHost _host = new();

    public App()
    {
        InitializeComponent();
        DispatcherShutdownMode = DispatcherShutdownMode.OnExplicitShutdown;
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args) => _host.Start(Program.StartInBackground);
}
