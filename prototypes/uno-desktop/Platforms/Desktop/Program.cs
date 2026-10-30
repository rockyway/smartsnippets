using Uno.UI.Hosting;

namespace SmartSnippets.UnoProbe;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        // Data mode returns before constructing AppKit/Uno windows or registering
        // any native callbacks. All database work stays on this single thread.
        if (args.Length > 0 && args[0] == "--probe-data") return DataProbe.Run(args);
        if (args.Length == 1 && args[0] == "--probe-native-capabilities") return MacCapabilityProbe.Run();
        // Guard before constructing a native host. This probe has no implicit GUI mode.
        if (!args.Contains("--authorized-probe-ui", StringComparer.Ordinal)) return 0;

        if (!OperatingSystem.IsMacOS())
            throw new PlatformNotSupportedException("The desktop probe target is macOS only.");

        UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseMacOS()
            .Build()
            .Run();
        return args.Contains("--probe-ui-lifecycle", StringComparer.Ordinal) ? LifecycleProbe.ExitCode : 0;
    }
}
