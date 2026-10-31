using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using SmartSnippets.Core.Logging;
using SmartSnippets.Presentation.Navigation;

namespace SmartSnippets.Uno;

public static class Program
{
    private static AppInstance? _instance;
    private static EventHandler<AppActivationArguments>? _activated;
    internal static CandidateActivationQueue Activations { get; } = new();
    private const string InstanceKey = CandidateProfile.Identity;

    internal static bool StartInBackground { get; private set; }

    internal static LaunchOptions InitialOptions { get; private set; } = new(null, false, null);


    [STAThread]
    private static void Main(string[] args)
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();

        StartInBackground = args.Contains("--background", StringComparer.OrdinalIgnoreCase);
        InitialOptions = LaunchOptions.Parse(args);


        var mainInstance = AppInstance.FindOrRegisterForKey(InstanceKey);
        if (!mainInstance.IsCurrent)
        {
            AppLog.Info("second_instance_redirect");
            if (!StartInBackground || InitialOptions.HasRequest)
            {
                Redirect(mainInstance);
            }

            return;
        }

        _instance = mainInstance;
        _activated = (_, activation) =>
        {
            var launch = activation.Data as Windows.ApplicationModel.Activation.ILaunchActivatedEventArgs;
            var options = launch is null ? new LaunchOptions(null, false, null)
                : LaunchOptions.Parse(LaunchOptions.SplitCommandLine(launch.Arguments));
            Activations.Enqueue(options);
        };
        mainInstance.Activated += _activated;
        Application.Start(p =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            _ = new App();
        });
    }

    internal static void StopActivations()
    {
        Activations.Close();
        if (_instance is not null && _activated is not null) _instance.Activated -= _activated;
        _instance = null; _activated = null;
    }

    // RedirectActivationToAsync completes through the apartment, so it runs on a worker thread
    // while this STA thread blocks in Join, which keeps pumping COM messages.
    private static void Redirect(AppInstance mainInstance)
    {
        var worker = new Thread(() =>
        {
            try
            {
                var activation = AppInstance.GetCurrent().GetActivatedEventArgs();
                mainInstance.RedirectActivationToAsync(activation).AsTask().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                AppLog.Error("redirect_failed", ex);
            }
        });
        worker.SetApartmentState(ApartmentState.MTA);
        worker.Start();
        worker.Join(TimeSpan.FromSeconds(10));
    }
}
