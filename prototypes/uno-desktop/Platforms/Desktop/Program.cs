using Uno.UI.Hosting;

namespace SmartSnippets.UnoProbe;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Guard before constructing a native host. This probe has no implicit GUI mode.
        if (!args.Contains("--authorized-probe-ui", StringComparer.Ordinal)) return;

        if (!OperatingSystem.IsMacOS())
            throw new PlatformNotSupportedException("The desktop probe target is macOS only.");

        UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseMacOS()
            .Build()
            .Run();
    }
}
