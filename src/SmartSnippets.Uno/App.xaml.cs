using Microsoft.UI.Xaml;
namespace SmartSnippets.Uno;
public partial class App : Application
{
    private readonly UnoAppHost _host = new(new WindowsCandidateBoundary(), CandidateProfile.OpenDefault);
    public App() { InitializeComponent(); DispatcherShutdownMode = DispatcherShutdownMode.OnExplicitShutdown; }
    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        await _host.StartAsync(Program.StartInBackground, Program.InitialOptions);
        Program.Activations.Attach(new Services.UiDispatcher(Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread()),
            options => _host.ApplyLaunchOptionsAsync(options));
    }
}
