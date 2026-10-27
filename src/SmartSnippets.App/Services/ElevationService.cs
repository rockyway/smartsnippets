using System.Runtime.InteropServices;
using SmartSnippets.Core.Logging;
using SmartSnippets.Presentation.Services;
using Windows.Win32;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

namespace SmartSnippets.App.Services;

internal sealed unsafe class ElevationService : IElevation
{
    private const int ErrorCancelled = 1223;

    private readonly Func<bool> _isMainWindowHidden;

    public ElevationService(Func<bool> isMainWindowHidden)
    {
        _isMainWindowHidden = isMainWindowHidden;
        IsElevated = ElevationProbe.IsOwnProcessElevated();
    }

    public bool IsElevated { get; }

    public ElevationResult RestartAsAdministrator()
    {
        var exe = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exe))
        {
            return new ElevationResult(ElevationOutcome.Failed, "Cannot determine the application path.");
        }

        var arguments = $"--after-pid {Environment.ProcessId}";
        if (_isMainWindowHidden())
        {
            arguments += " --background";
        }

        fixed (char* verb = "runas")
        fixed (char* file = exe)
        fixed (char* parameters = arguments)
        {
            var info = default(SHELLEXECUTEINFOW);
            info.cbSize = (uint)sizeof(SHELLEXECUTEINFOW);
            info.fMask = 0;
            info.lpVerb = verb;
            info.lpFile = file;
            info.lpParameters = parameters;
            info.nShow = (int)SHOW_WINDOW_CMD.SW_SHOWNORMAL;

            if (PInvoke.ShellExecuteEx(&info))
            {
                AppLog.Info("restart_elevated_started");
                return new ElevationResult(ElevationOutcome.Started, "Restarting as administrator.");
            }
        }

        var error = Marshal.GetLastPInvokeError();
        if (error == ErrorCancelled)
        {
            AppLog.Info("restart_elevated_cancelled");
            return new ElevationResult(
                ElevationOutcome.Cancelled,
                "Restart cancelled; Smart Snippets keeps running without administrator rights.");
        }

        AppLog.Info("restart_elevated_failed", $"win32={error}");
        return new ElevationResult(ElevationOutcome.Failed, $"Could not restart as administrator (error {error}).");
    }
}
