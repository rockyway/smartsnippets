using System.Runtime.InteropServices;
using SmartSnippets.Core.Logging;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.System.Threading;

namespace SmartSnippets.App.Services;

internal static unsafe class ElevationProbe
{
    private const int ErrorInvalidParameter = 87;

    public static bool IsBlocked(ForegroundTargetInfo target)
    {
        if (IsOwnProcessElevated())
        {
            return false;
        }

        if (!TryGetIntegrityRid(PInvoke.GetCurrentProcess(), out var ownRid))
        {
            ownRid = 0x2000; // medium
        }

        var process = PInvoke.OpenProcess(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION, false, target.ProcessId);
        if (process.IsNull)
        {
            var error = Marshal.GetLastPInvokeError();
            if (error == ErrorInvalidParameter)
            {
                AppLog.Info("elevation_probe", "target_process_gone");
                return false;
            }

            AppLog.Info("elevation_probe", $"open_process_denied error={error} blocked=True");
            return true;
        }

        try
        {
            if (!TryGetIntegrityRid(process, out var targetRid))
            {
                AppLog.Info("elevation_probe", "token_denied blocked=True");
                return true;
            }

            var blocked = targetRid > ownRid;
            AppLog.Info("elevation_probe", $"own=0x{ownRid:x} target=0x{targetRid:x} blocked={blocked}");
            return blocked;
        }
        finally
        {
            PInvoke.CloseHandle(process);
        }
    }

    internal static bool IsOwnProcessElevated()
    {
        HANDLE token;
        if (!PInvoke.OpenProcessToken(PInvoke.GetCurrentProcess(), TOKEN_ACCESS_MASK.TOKEN_QUERY, &token))
        {
            return false;
        }

        try
        {
            var elevation = default(TOKEN_ELEVATION);
            uint returned;
            if (!PInvoke.GetTokenInformation(token, TOKEN_INFORMATION_CLASS.TokenElevation, &elevation, (uint)sizeof(TOKEN_ELEVATION), &returned))
            {
                return false;
            }

            return elevation.TokenIsElevated != 0;
        }
        finally
        {
            PInvoke.CloseHandle(token);
        }
    }

    private static bool TryGetIntegrityRid(HANDLE process, out uint rid)
    {
        rid = 0;
        HANDLE token;
        if (!PInvoke.OpenProcessToken(process, TOKEN_ACCESS_MASK.TOKEN_QUERY, &token))
        {
            return false;
        }

        try
        {
            uint needed;
            PInvoke.GetTokenInformation(token, TOKEN_INFORMATION_CLASS.TokenIntegrityLevel, null, 0, &needed);
            if (needed == 0)
            {
                return false;
            }

            var buffer = new byte[needed];
            fixed (byte* p = buffer)
            {
                if (!PInvoke.GetTokenInformation(token, TOKEN_INFORMATION_CLASS.TokenIntegrityLevel, p, needed, &needed))
                {
                    return false;
                }

                var label = (TOKEN_MANDATORY_LABEL*)p;
                var sid = label->Label.Sid;
                var count = *PInvoke.GetSidSubAuthorityCount(sid);
                if (count == 0)
                {
                    return false;
                }

                rid = *PInvoke.GetSidSubAuthority(sid, (uint)count - 1);
                return true;
            }
        }
        finally
        {
            PInvoke.CloseHandle(token);
        }
    }
}
