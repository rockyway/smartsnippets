using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace SmartSnippets.TargetIdentity018;

internal interface IOwnedChild : IAsyncDisposable
{
    uint Id { get; }
    ChildProtocol Protocol { get; }
    bool IsIdentityAlive();
}

/// <summary>Only Process.Start-created children; no GetProcessById/attach API.</summary>
internal sealed partial class NativeChild : IOwnedChild
{
    private readonly Process process;
    private readonly string path;
    private readonly TargetPlatform platform;
    private readonly ulong startSeconds, startMicros;
    private readonly SafeProcessHandle handle;
    private bool disposed;
    public uint Id { get; }
    public ChildProtocol Protocol { get; }

    private NativeChild(Process process, string path, TargetPlatform platform)
    {
        this.process = process;
        this.path = path;
        this.platform = platform;
        Id = checked((uint)process.Id);
        handle = process.SafeHandle; // Keep the OS identity, not just its reusable PID.
        if (platform == TargetPlatform.Windows) WindowsToken.RequireSameUnelevatedToken(handle);
        else
        {
            var info = MacInfo();
            startSeconds = info.StartSeconds;
            startMicros = info.StartMicros;
        }
        if (!IsIdentityAlive()) throw new ProbeFailure("child_identity");
        Protocol = new(process.StandardOutput, process.StandardError, process.StandardInput);
    }

    internal static IOwnedChild Start(ProcessStartInfo info, TargetPlatform platform)
    {
        Process? process = null;
        try
        {
            if (platform == TargetPlatform.Windows) WindowsToken.RequireUnelevatedLauncher();
            process = Process.Start(info) ?? throw new ProbeFailure("child_start");
            return new NativeChild(process, info.FileName, platform);
        }
        catch
        {
            if (process is not null)
            {
                try { if (!process.HasExited) process.Kill(entireProcessTree: false); }
                finally
                {
                    try { if (!process.WaitForExit(3000)) throw new ProbeFailure("cleanup_failed"); }
                    finally
                    {
                        ClosePipe(process.StandardInput);
                        ClosePipe(process.StandardOutput);
                        ClosePipe(process.StandardError);
                        process.Dispose();
                    }
                }
            }
            throw;
        }
    }

    public unsafe bool IsIdentityAlive()
    {
        if (disposed || handle.IsClosed || handle.IsInvalid || process.HasExited) return false;
        if (platform == TargetPlatform.Windows)
        {
            var buffer = stackalloc char[32768];
            uint size = 32768;
            return QueryFullProcessImageNameW(handle, 0, buffer, ref size) != 0 &&
                new ReadOnlySpan<char>(buffer, checked((int)size)).Equals(path, StringComparison.OrdinalIgnoreCase) &&
                WindowsToken.IsSameUnelevatedToken(handle);
        }
        try
        {
            var info = MacInfo();
            var buffer = stackalloc byte[4096];
            var size = proc_pidpath((int)Id, buffer, 4096);
            if (size <= 0 || size >= 4096 || info.StartSeconds != startSeconds || info.StartMicros != startMicros) return false;
            return Marshal.PtrToStringUTF8((nint)buffer) == path;
        }
        catch (ProbeFailure) { return false; }
    }

    private unsafe BsdInfo MacInfo()
    {
        var info = new BsdInfo();
        if (proc_pidinfo((int)Id, 3, 0, &info, sizeof(BsdInfo)) != sizeof(BsdInfo) ||
            info.Pid != Id || info.ParentPid != (uint)Environment.ProcessId || info.Status == 5 ||
            info.Uid != geteuid() || info.RealUid != geteuid() || info.SavedUid != geteuid())
            throw new ProbeFailure("child_identity");
        return info;
    }

    public async ValueTask DisposeAsync()
    {
        if (disposed) return;
        disposed = true;
        var stopped = false;
        try
        {
            // Kill only our retained child; never enumerate or terminate by name/PID.
            if (!process.HasExited) process.Kill(entireProcessTree: false);
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await process.WaitForExitAsync(deadline.Token);
            stopped = true;
        }
        finally
        {
            try { await Protocol.DisposeAsync(); }
            finally { process.Dispose(); }
        }
        if (!stopped) throw new ProbeFailure("cleanup_failed");
    }

    private static void ClosePipe(IDisposable stream)
    {
        try { stream.Dispose(); }
        catch (IOException) { }
    }

    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct BsdInfo
    {
        public uint Flags, Status, ExitStatus, Pid, ParentPid, Uid, Gid, RealUid, RealGid, SavedUid, SavedGid, Reserved;
        public fixed byte Command[16];
        public fixed byte Name[32];
        public uint Files, GroupId, JobCount, Device, DeviceGroup;
        public int Nice;
        public ulong StartSeconds, StartMicros;
    }

    [LibraryImport("kernel32.dll")] private static unsafe partial int QueryFullProcessImageNameW(SafeProcessHandle process, uint flags, char* path, ref uint size);
    [LibraryImport("/usr/lib/libSystem.B.dylib")] private static partial uint geteuid();
    [LibraryImport("/usr/lib/libproc.dylib")] private static unsafe partial int proc_pidinfo(int pid, int flavor, ulong argument, BsdInfo* buffer, int size);
    [LibraryImport("/usr/lib/libproc.dylib")] private static unsafe partial int proc_pidpath(int pid, byte* buffer, uint size);
}

internal static partial class WindowsToken
{
    [StructLayout(LayoutKind.Sequential)] private struct Luid { public uint Low; public int High; }
    [StructLayout(LayoutKind.Sequential)] private struct Statistics
    {
        public Luid TokenId, AuthenticationId;
        public long Expiration;
        public uint Type, ImpersonationLevel, DynamicCharged, DynamicAvailable, Groups, Privileges;
        public Luid ModifiedId;
    }
    [StructLayout(LayoutKind.Sequential)] private struct SidAttributes { public nint Sid; public uint Attributes; }

    internal static void RequireUnelevatedLauncher()
    {
        using var process = Process.GetCurrentProcess();
        using var token = Open(process.SafeHandle);
        if (Elevated(token)) throw new ProbeFailure("launcher_elevated");
    }

    internal static bool IsSameUnelevatedToken(SafeProcessHandle child)
    {
        try { RequireSameUnelevatedToken(child); return true; }
        catch (ProbeFailure) { return false; }
    }

    internal static unsafe void RequireSameUnelevatedToken(SafeProcessHandle child)
    {
        using var current = Process.GetCurrentProcess();
        using var own = Open(current.SafeHandle);
        using var target = Open(child);
        if (Elevated(own) || Elevated(target)) throw new ProbeFailure("child_token");
        var a = new Statistics();
        var b = new Statistics();
        if (GetTokenInformation(own, 10, &a, (uint)sizeof(Statistics), out _) == 0 ||
            GetTokenInformation(target, 10, &b, (uint)sizeof(Statistics), out _) == 0 ||
            a.AuthenticationId.Low != b.AuthenticationId.Low || a.AuthenticationId.High != b.AuthenticationId.High)
            throw new ProbeFailure("child_token");
        // TOKEN_USER contains an inline pointer into the returned buffer.
        var aUser = stackalloc byte[1024];
        var bUser = stackalloc byte[1024];
        if (GetTokenInformation(own, 1, aUser, 1024, out _) == 0 || GetTokenInformation(target, 1, bUser, 1024, out _) == 0 ||
            EqualSid(((SidAttributes*)aUser)->Sid, ((SidAttributes*)bUser)->Sid) == 0) throw new ProbeFailure("child_token");
    }

    private static SafeAccessTokenHandle Open(SafeProcessHandle process)
    {
        if (OpenProcessToken(process, 8, out var token) == 0) { token?.Dispose(); throw new ProbeFailure("child_token"); }
        return token;
    }

    private static unsafe bool Elevated(SafeAccessTokenHandle token)
    {
        uint elevated;
        if (GetTokenInformation(token, 20, &elevated, 4, out _) == 0) throw new ProbeFailure("child_token");
        return elevated != 0;
    }

    [LibraryImport("advapi32.dll")] private static partial int OpenProcessToken(SafeProcessHandle process, uint access, out SafeAccessTokenHandle token);
    [LibraryImport("advapi32.dll")] private static unsafe partial int GetTokenInformation(SafeAccessTokenHandle token, int type, void* buffer, uint size, out uint returned);
    [LibraryImport("advapi32.dll")] private static partial int EqualSid(nint a, nint b);
}
