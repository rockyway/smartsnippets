using System;
using System.Linq;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace SmartSnippets.UnoProbe;

internal static class WindowsProgram
{
    [STAThread]
    public static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--probe-data") return DataProbe.Run(args);
        if (args.Length == 2 && args[0] == "--probe-shortcut-register" && args[1] == "--authorized-probe-shortcut")
            return NativeShortcutRegistrationProbe.Run();
        if (!args.Contains("--authorized-probe-ui", StringComparer.Ordinal)) return 0;
        if (new[] { "--probe-ui-lifecycle", "--probe-ui-coordinator", "--probe-ui-preview", "--probe-ui-companion", "--preview-row", "--preview-row-native" }
            .Count(flag => args.Contains(flag, StringComparer.Ordinal)) > 1) return 2;
        WinRT.ComWrappersSupport.InitializeComWrappers();
        Application.Start(parameters =>
        {
            System.Threading.SynchronizationContext.SetSynchronizationContext(
                new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread()));
            _ = new App();
        });
        if (args.Contains("--probe-ui-companion", StringComparer.Ordinal)) return CompanionSurfaceProbe.ExitCode;
        if (args.Contains("--probe-ui-preview", StringComparer.Ordinal) || args.Contains("--preview-row", StringComparer.Ordinal) || args.Contains("--preview-row-native", StringComparer.Ordinal)) return PreviewProbe.ExitCode;
        if (args.Contains("--probe-ui-coordinator", StringComparer.Ordinal)) return CoordinatedPopupProbe.ExitCode;
        return args.Contains("--probe-ui-lifecycle", StringComparer.Ordinal) ? LifecycleProbe.ExitCode : 0;
    }
}
