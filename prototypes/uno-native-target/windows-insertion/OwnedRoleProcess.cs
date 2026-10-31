using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using SmartSnippets.TargetIdentity018;

namespace SmartSnippets.WindowsInsertion018;

/// <summary>Self-image children only, with retained handles. Never attach by PID.</summary>
internal sealed partial class OwnedRoleProcess : IDisposable
{
    internal Process Process { get; }
    private readonly Task<bool> stderrSeen;
    private bool evidenceTokenRead;
    private OwnedRoleProcess(Process process)
    {
        Process = process;
        stderrSeen = Task.Run(async () => await process.StandardError.ReadAsync(new char[1].AsMemory()) != 0);
    }
    internal static OwnedRoleProcess Start(string role, Guid run, string temporaryRoot, string? target = null, string? digest = null, InsertionScene scene = InsertionScene.Positive)
    {
        var image = Environment.ProcessPath ?? throw new ProbeFailure("self_path");
        TargetArtifact.RefuseLinks(image);
        WindowsToken.RequireUnelevatedLauncher();
        var info = new ProcessStartInfo(image) { UseShellExecute = false, RedirectStandardInput = true, RedirectStandardOutput = true,
            RedirectStandardError = true, CreateNoWindow = true, WorkingDirectory = Path.GetDirectoryName(image)! };
        info.Environment.Clear();
        info.Environment["SystemRoot"] = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        info.Environment["WINDIR"] = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        // Never fall back to the Windows directory after clearing inherited env.
        // The coordinator supplies its already-created owner-only diagnostics root.
        foreach (var pair in RoleTemporaryEnvironment(temporaryRoot)) info.Environment[pair.Key] = pair.Value;
        info.ArgumentList.Add("--internal-" + role);
        info.ArgumentList.Add(run.ToString("D"));
        if (target is not null && digest is not null)
        {
            info.ArgumentList.Add(target); info.ArgumentList.Add(digest); info.ArgumentList.Add(scene.ToString());
        }
        return new OwnedRoleProcess(Process.Start(info) ?? throw new ProbeFailure("role_start"));
    }
    internal static IReadOnlyDictionary<string, string> RoleTemporaryEnvironment(string root)
    {
        if (!Path.IsPathFullyQualified(root)) throw new ProbeFailure("role_temp_root");
        TargetArtifact.RefuseLinks(root);
        if (!Directory.Exists(root)) throw new ProbeFailure("role_temp_root");
        return new Dictionary<string, string> { ["TEMP"] = root, ["TMP"] = root };
    }
    internal async Task<string> ReadAsync(int seconds = 10)
    {
        var line = await FixedPipe.ReadAsync(Process.StandardOutput, TimeSpan.FromSeconds(seconds), CancellationToken.None);
        if (!evidenceTokenRead)
        {
            const string prefix = "role_private_evidence_token=";
            if (!line.StartsWith(prefix, StringComparison.Ordinal) || !Guid.TryParseExact(line.AsSpan(prefix.Length), "N", out _))
                throw new ProbeFailure("role_evidence_token");
            evidenceTokenRead = true;
            FixedPipe.Receipt(line); // Content-free child directory token, never a user path.
            line = await FixedPipe.ReadAsync(Process.StandardOutput, TimeSpan.FromSeconds(seconds), CancellationToken.None);
        }
        return line;
    }
    internal Task WriteAsync(string fixedLine) => FixedPipe.WriteAsync(Process.StandardInput, fixedLine);
    internal bool HasError => stderrSeen.IsCompletedSuccessfully && stderrSeen.Result;
    internal async Task RequireNaturalExitAsync()
    {
        await Process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
        using var eofTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        if (Process.ExitCode != 0 || await stderrSeen.WaitAsync(TimeSpan.FromSeconds(2)) ||
            await FixedPipe.ReadOrEofAsync(Process.StandardOutput, eofTimeout.Token) is not null)
            throw new ProbeFailure("role_exit");
    }
    internal async Task StopAndReapAsync()
    {
        try { if (!Process.HasExited) Process.Kill(entireProcessTree: false); }
        catch (Exception error) when (error is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            // Job termination may race Kill. Still wait for the retained process;
            // a failed Kill is never treated as proof that it stopped.
        }
        await Process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
    }
    internal static nint DuplicateInto(OwnedRoleProcess source, OwnedRoleProcess recipient)
    {
        if (source.Process.HasExited || recipient.Process.HasExited ||
            DuplicateHandle(GetCurrentProcess(), source.Process.SafeHandle, recipient.Process.SafeHandle, out var duplicate,
                0x00101000, 0, 0) == 0) throw new ProbeFailure("role_handle"); // SYNCHRONIZE | PROCESS_QUERY_LIMITED_INFORMATION
        return duplicate;
    }
    public void Dispose()
    {
        // Disposal intentionally DOES NOT terminate a guardian. Its RAM copy can
        // be the only original backup after mutation or after coordinator failure.
        Close(Process.StandardInput);
        Close(Process.StandardOutput);
        Close(Process.StandardError);
        Process.Dispose();
    }
    private static void Close(IDisposable pipe)
    {
        try { pipe.Dispose(); }
        catch (Exception error) when (error is IOException or ObjectDisposedException) { }
    }
    [LibraryImport("kernel32.dll")] private static partial nint GetCurrentProcess();
    [LibraryImport("kernel32.dll")] private static partial int DuplicateHandle(nint current, SafeProcessHandle source,
        SafeProcessHandle target, out nint duplicate, uint access, int inherit, uint options);
}

/// <summary>Only the worker tree is kill-on-close; the RAM guardian is outside it.</summary>
internal sealed partial class WorkerJob : IDisposable
{
    private readonly SafeFileHandle handle;
    private WorkerJob(SafeFileHandle handle) => this.handle = handle;
    internal static unsafe WorkerJob Assign(OwnedRoleProcess child)
    {
        var job = CreateJobObjectW(0, null);
        if (job.IsInvalid) { job.Dispose(); throw new ProbeFailure("worker_job"); }
        var settings = new Extended { Basic = new Basic { Flags = 0x2000 } }; // KILL_ON_JOB_CLOSE, no breakaway.
        if (SetInformationJobObject(job, 9, &settings, (uint)sizeof(Extended)) == 0 || AssignProcessToJobObject(job, child.Process.SafeHandle) == 0)
        { job.Dispose(); throw new ProbeFailure("worker_job"); }
        return new WorkerJob(job);
    }
    public void Dispose() => handle.Dispose();
    [StructLayout(LayoutKind.Sequential)] private struct Basic
    {
        public long ProcessTime, JobTime; public uint Flags; public nuint Minimum, Maximum; public uint Processes; public nuint Affinity; public uint Priority, Scheduling;
    }
    [StructLayout(LayoutKind.Sequential)] private struct Io { public ulong ReadOps, WriteOps, OtherOps, ReadBytes, WriteBytes, OtherBytes; }
    [StructLayout(LayoutKind.Sequential)] private struct Extended { public Basic Basic; public Io Io; public nuint ProcessMemory, JobMemory, PeakProcess, PeakJob; }
    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16)] private static partial SafeFileHandle CreateJobObjectW(nint attributes, string? name);
    [LibraryImport("kernel32.dll")] private static unsafe partial int SetInformationJobObject(SafeFileHandle job, int kind, void* data, uint length);
    [LibraryImport("kernel32.dll")] private static partial int AssignProcessToJobObject(SafeFileHandle job, SafeProcessHandle process);
}
