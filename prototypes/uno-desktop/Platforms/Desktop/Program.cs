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
        if (args.Length == 1 && args[0] == "--probe-private-pasteboard") return MacPrivatePasteboardProbe.Run();
        if (args.Length == 2 && args[0] == "--probe-general-snapshot" && args[1] == "--authorized-probe-clipboard")
            return MacGeneralSnapshotProbe.Run();
        if (args.Length == 3 && args[1] == "--authorized-probe-clipboard" &&
            (args[0] == "--probe-private-watchdog-owner" || args[0] == "--probe-private-watchdog-worker"))
            return MacPrivatePasteboardProbe.RunWatchdog(args[2], args[0] == "--probe-private-watchdog-owner");
        // Guard before constructing a native host. This probe has no implicit GUI mode.
        if (args.Length == 2 && args[0] == "--probe-shortcut-register" && args[1] == "--authorized-probe-shortcut")
            return NativeShortcutRegistrationProbe.Run();
        if (!args.Contains("--authorized-probe-ui", StringComparer.Ordinal)) return 0;
        if (new[] { "--probe-ui-lifecycle", "--probe-ui-coordinator", "--probe-ui-preview", "--probe-ui-companion", "--preview-row" }
            .Count(flag => args.Contains(flag, StringComparer.Ordinal)) > 1) return 2;

        if (!OperatingSystem.IsMacOS())
            throw new PlatformNotSupportedException("The desktop probe target is macOS only.");

        UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseMacOS()
            .Build()
            .Run();
        if (args.Contains("--probe-ui-companion", StringComparer.Ordinal)) return CompanionSurfaceProbe.ExitCode;
        if (args.Contains("--probe-ui-preview", StringComparer.Ordinal) || args.Contains("--preview-row", StringComparer.Ordinal)) return PreviewProbe.ExitCode;
        if (args.Contains("--probe-ui-coordinator", StringComparer.Ordinal)) return CoordinatedPopupProbe.ExitCode;
        return args.Contains("--probe-ui-lifecycle", StringComparer.Ordinal) ? LifecycleProbe.ExitCode : 0;
    }
}
