using System.Runtime.Versioning;
using System.Security;
using Microsoft.Win32;
using SmartSnippets.Core.Logging;
using SmartSnippets.Presentation.Services;

namespace SmartSnippets.Uno.Services;

// Legacy per-user Run semantics, with a separate candidate value and no legacy migration.
internal sealed class StartupRegistration : IStartupRegistration
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    internal const string ValueName = "SmartSnippets.UnoCandidate019";
    private readonly Func<string?> _executable;
    private readonly Func<string, object?> _read;
    private readonly Action<string, string> _write;
    private readonly Action<string> _delete;

    [SupportedOSPlatform("windows")]
    internal StartupRegistration() : this(() => Environment.ProcessPath, Read, Write, Delete) { }

    // Substitute only the OS boundary in tests; never write the real Run key there.
    internal StartupRegistration(Func<string?> executable, Func<string, object?> read,
        Action<string, string> write, Action<string> delete)
    { _executable = executable; _read = read; _write = write; _delete = delete; }

    internal static string Command(string exe) => $"\"{exe}\" --background";

    public bool IsEnabled()
    {
        try
        {
            return _read(ValueName) is string stored && _executable() is { } exe
                && string.Equals(stored, Command(exe), StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or SecurityException or IOException)
        { AppLog.Error("startup_check_failed", ex); return false; }
    }

    public StartupResult Enable()
    {
        var exe = _executable();
        if (string.IsNullOrEmpty(exe)) return new(false, "Cannot determine the application path.");
        try
        {
            _write(ValueName, Command(exe));
            AppLog.Info("startup_enabled");
            return new(true, "Start at login enabled.");
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or SecurityException or IOException)
        { AppLog.Error("startup_enable_failed", ex); return new(false, "Could not enable start at login."); }
    }

    public StartupResult Disable()
    {
        try
        {
            _delete(ValueName);
            AppLog.Info("startup_disabled");
            return new(true, "Start at login disabled.");
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or SecurityException or IOException)
        { AppLog.Error("startup_disable_failed", ex); return new(false, "Could not disable start at login."); }
    }

    [SupportedOSPlatform("windows")]
    private static object? Read(string name)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return key?.GetValue(name);
    }
    [SupportedOSPlatform("windows")]
    private static void Write(string name, string command)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
        key.SetValue(name, command, RegistryValueKind.String);
    }
    [SupportedOSPlatform("windows")]
    private static void Delete(string name)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        key?.DeleteValue(name, throwOnMissingValue: false);
    }
}
