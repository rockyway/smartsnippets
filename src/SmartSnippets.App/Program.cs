using System.Diagnostics;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using SmartSnippets.Core;
using SmartSnippets.Core.Logging;
using SmartSnippets.Presentation.Navigation;

namespace SmartSnippets.App;

public static class Program
{
    private const string InstanceKey = "SmartSnippets.Main";

    internal static bool StartInBackground { get; private set; }

    internal static LaunchOptions InitialOptions { get; private set; } = new(null, false, null);

    [STAThread]
    private static void Main(string[] args)
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();
        AppLog.Initialize(new AppPaths().LogDirectory);

        StartInBackground = args.Contains("--background", StringComparer.OrdinalIgnoreCase);
        InitialOptions = LaunchOptions.Parse(args);

        WaitForPreviousInstance();

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

        Application.Start(p =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            _ = new App();
        });
    }

    // Restart-as-administrator hand-off: the old instance is exiting, so wait for it before claiming the instance key.
    private static void WaitForPreviousInstance()
    {
        var commandLine = Environment.GetCommandLineArgs();
        var index = Array.FindIndex(commandLine, a => string.Equals(a, "--after-pid", StringComparison.OrdinalIgnoreCase));
        if (index < 0 || index + 1 >= commandLine.Length || !int.TryParse(commandLine[index + 1], out var pid))
        {
            return;
        }

        try
        {
            using var previous = Process.GetProcessById(pid);
            var exited = previous.WaitForExit(TimeSpan.FromSeconds(5));
            AppLog.Info("after_pid_wait", $"exited={exited}");
        }
        catch (ArgumentException)
        {
            // Already gone.
        }
        catch (Exception ex)
        {
            AppLog.Error("after_pid_wait_failed", ex);
        }
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
