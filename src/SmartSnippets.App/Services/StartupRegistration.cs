using System.Security;
using Microsoft.Win32;
using SmartSnippets.Core.Logging;
using SmartSnippets.Presentation.Services;

namespace SmartSnippets.App.Services;

public sealed class StartupRegistration : IStartupRegistration
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "SmartSnippets";

    public bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            if (key?.GetValue(ValueName) is not string stored)
            {
                return false;
            }

            var exe = Environment.ProcessPath;
            return exe is not null && stored.Contains(exe, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or SecurityException or IOException)
        {
            AppLog.Error("startup_check_failed", ex);
            return false;
        }
    }

    public StartupResult Enable()
    {
        var exe = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exe))
        {
            return new StartupResult(false, "Cannot determine the application path.");
        }

        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
            key.SetValue(ValueName, $"\"{exe}\" --background", RegistryValueKind.String);
            AppLog.Info("startup_enabled");
            return new StartupResult(true, "Start at login enabled.");
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or SecurityException or IOException)
        {
            AppLog.Error("startup_enable_failed", ex);
            return new StartupResult(false, $"Could not enable start at login: {ex.Message}");
        }
    }

    public StartupResult Disable()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
            key?.DeleteValue(ValueName, throwOnMissingValue: false);
            AppLog.Info("startup_disabled");
            return new StartupResult(true, "Start at login disabled.");
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or SecurityException or IOException)
        {
            AppLog.Error("startup_disable_failed", ex);
            return new StartupResult(false, $"Could not disable start at login: {ex.Message}");
        }
    }
}
