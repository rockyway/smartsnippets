using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using SmartSnippets.Core;
using SmartSnippets.Core.Logging;

namespace SmartSnippets.App;

public static class Program
{
    private const string InstanceKey = "SmartSnippets.Main";

    internal static bool StartInBackground { get; private set; }

    [STAThread]
    private static void Main(string[] args)
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();
        AppLog.Initialize(new AppPaths().LogDirectory);

        StartInBackground = Environment.GetCommandLineArgs().Contains("--background", StringComparer.OrdinalIgnoreCase);

        var mainInstance = AppInstance.FindOrRegisterForKey(InstanceKey);
        if (!mainInstance.IsCurrent)
        {
            AppLog.Info("second_instance_redirect");
            if (!StartInBackground)
            {
                Redirect(mainInstance);
            }

            return;
        }

        Application.Start(p =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            _ = new App();
        });
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
