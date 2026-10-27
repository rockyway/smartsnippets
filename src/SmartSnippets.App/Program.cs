using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace SmartSnippets.App;

public static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();

        // TODO(T5): single-instance check (AppInstance.FindOrRegisterForKey + RedirectActivationToAsync) goes here, before Application.Start.

        Application.Start(_ =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            _ = new App();
        });
    }
}
