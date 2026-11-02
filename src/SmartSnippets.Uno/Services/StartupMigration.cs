using System.Security;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace SmartSnippets.Uno.Services;

internal interface IStartupMigrationRegistry
{
    (object? Value, RegistryValueKind? Kind) Read();
    void Write(string command);
    void RemoveObsoleteRegistration() { }
}

internal enum StartupMigrationResult { Unchanged, Migrated, Failed }

// RegistryValueKind values are plain enum constants; the decision logic is platform-independent.
#pragma warning disable CA1416
internal static class StartupMigration
{
    internal static StartupMigrationResult Run(IStartupMigrationRegistry registry, string? executable)
    {
        try
        {
            registry.RemoveObsoleteRegistration();
            var (value, kind) = registry.Read();
            if (value is null && kind is null) return StartupMigrationResult.Unchanged;
            if (string.IsNullOrEmpty(executable)) return StartupMigrationResult.Failed;
            var directory = Path.GetDirectoryName(executable);
            if (string.IsNullOrEmpty(directory)) return StartupMigrationResult.Failed;
            var expected = $"\"{executable}\" --background";
            if (kind != RegistryValueKind.String || value is not string command ||
                !command.StartsWith('"') || !command.EndsWith("\" --background", StringComparison.Ordinal))
                return StartupMigrationResult.Unchanged;
            var registeredExe = command[1..^14];
            var name = Path.GetFileName(registeredExe);
            var legacy = Path.Combine(directory, "SmartSnippets.App.exe");
            if (!string.Equals(name, "SmartSnippets.exe", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(registeredExe, legacy, StringComparison.OrdinalIgnoreCase))
                return StartupMigrationResult.Unchanged;
            if (string.Equals(command, expected, StringComparison.OrdinalIgnoreCase)) return StartupMigrationResult.Unchanged;
            registry.Write(expected);
            return StartupMigrationResult.Migrated;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or SecurityException or IOException or ArgumentException or ObjectDisposedException)
        {
            return StartupMigrationResult.Failed;
        }
    }
}

internal static class StartupMigrationCoordinator
{
    internal static StartupMigrationResult Start(IStartupMigrationRegistry registry, string? executable, Action<string> log)
    {
        var result = StartupMigration.Run(registry, executable);
        log($"result={result}");
        return result;
    }

    internal static void Deliver(StartupMigrationResult result, Func<bool> notify, Action fallback)
    {
        if (result == StartupMigrationResult.Failed && !notify()) fallback();
    }
}
#pragma warning restore CA1416
[SupportedOSPlatform("windows")]
internal sealed class WindowsStartupMigrationRegistry : IStartupMigrationRegistry
{
    private readonly string _keyPath;
    private const string ValueName = "SmartSnippets";

    internal WindowsStartupMigrationRegistry(string keyPath) => _keyPath = keyPath;

    public (object? Value, RegistryValueKind? Kind) Read()
    {
        using var key = Registry.CurrentUser.OpenSubKey(_keyPath, writable: false);
        if (key is null || !key.GetValueNames().Contains(ValueName, StringComparer.OrdinalIgnoreCase))
            return (null, null);
        return (key.GetValue(ValueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames), key.GetValueKind(ValueName));
    }

    public void RemoveObsoleteRegistration()
    {
        using var key = Registry.CurrentUser.OpenSubKey(_keyPath, writable: true);
        key?.DeleteValue("SmartSnippets.UnoCandidate019", throwOnMissingValue: false);
    }

    public void Write(string command)
    {
        using var key = Registry.CurrentUser.CreateSubKey(_keyPath, writable: true);
        key.SetValue(ValueName, command, RegistryValueKind.String);
    }
}
