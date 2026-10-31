using System;
using System.Diagnostics;
using System.IO;
using System.Globalization;
using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using SmartSnippets.Presentation.Services;
using SmartSnippets.WindowsInsertion018;

namespace SmartSnippets.Uno.Platforms.Windows.AcceptanceNative;

/// <summary>Never kills the guardian on Dispose, timeout, worker cancellation or protocol loss.</summary>
internal sealed class GuardianClient : IGuardianChannel, IDisposable
{
    private readonly Process process;
    private readonly PinnedNativeImage image;
    private readonly AnonymousPipeServerStream commands, responses;
    private readonly StreamWriter writer;
    private readonly StreamReader reader;
    private readonly bool allowInsert, allowCopy;
    private readonly object sync = new();
    private bool closed, broken, pending, disposed, spent;
    public string RecoveryControl { get; }
    private GuardianClient(Process process, PinnedNativeImage image, AnonymousPipeServerStream commands,
        AnonymousPipeServerStream responses, Guid run, bool allowInsert, bool allowCopy)
    {
        (this.process, this.image, this.commands, this.responses, this.allowInsert, this.allowCopy) =
            (process, image, commands, responses, allowInsert, allowCopy);
        writer = new(commands, new UTF8Encoding(false), leaveOpen: true);
        reader = new(responses, Encoding.UTF8, false, leaveOpen: true);
        RecoveryControl = "ss-uno019-recovery-" + run.ToString("N");
    }
    public bool IsPinned => !disposed && !broken && RetainedIdentity.Pinned(process.SafeHandle, image) && RetainedIdentity.OutsideJob(process.SafeHandle);
    public bool RecoveryPending => pending;
    public bool CanPublish => !closed && !pending && !spent && IsPinned;
    public bool SnapshotAcknowledged { get; private set; }
    public int SavedCount { get; private set; }
    public long ByteCount { get; private set; }

    internal static GuardianClient Start(AcceptanceFixture fixture, InsertionTarget target, NativeOwnedPopup popup, string path,
        string digest, DateTimeOffset expires, bool allowInsert, bool allowCopy)
    {
        NativeAcceptanceFactory.RequireReviewed();
        using var self = Process.GetCurrentProcess();
        if (!RetainedIdentity.OutsideJob(self.SafeHandle) || !fixture.Lease.IsAliveAndPinned() ||
            RetainedIdentity.Parent(fixture.Child.SafeHandle) != (uint)Environment.ProcessId)
            throw new InvalidOperationException("Guardian must bootstrap outside all worker and observer Jobs.");
        var image = PinnedNativeImage.Open(path, digest, "SmartSnippets.UnoCandidate.Guardian.exe");
        var commands = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);
        var responses = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);
        Process? child = null;
        try
        {
            var run = Guid.NewGuid();
            var info = new ProcessStartInfo(image.Path) { UseShellExecute = false, CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(image.Path)! };
            info.Environment.Clear();
            info.Environment["SystemRoot"] = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            info.Environment["WINDIR"] = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            info.ArgumentList.Add(commands.GetClientHandleAsString());
            info.ArgumentList.Add(responses.GetClientHandleAsString());
            info.ArgumentList.Add(run.ToString("D"));
            child = Process.Start(info) ?? throw new InvalidOperationException("Guardian start failed.");
            commands.DisposeLocalCopyOfClientHandle(); responses.DisposeLocalCopyOfClientHandle();
            if (!RetainedIdentity.Pinned(child.SafeHandle, image) || !RetainedIdentity.OutsideJob(child.SafeHandle) ||
                RetainedIdentity.Parent(child.SafeHandle) != (uint)Environment.ProcessId)
                throw new InvalidOperationException("Guardian identity refused.");
            var selfPath = Environment.ProcessPath ?? throw new InvalidOperationException("Worker image refused.");
            using var file = File.OpenRead(selfPath);
            var workerDigest = Convert.ToHexString(SHA256.HashData(file));
            using var workerImage = PinnedNativeImage.Open(selfPath, workerDigest, "SmartSnippets.UnoCandidate.exe");
            using (var bootstrap = new BinaryWriter(commands, Encoding.UTF8, leaveOpen: true))
            {
                bootstrap.Write(RetainedIdentity.Duplicate(self.SafeHandle, child.SafeHandle).ToInt64());
                bootstrap.Write(RetainedIdentity.Duplicate(fixture.Child.SafeHandle, child.SafeHandle).ToInt64());
                bootstrap.Write(target.WindowHandle.ToInt64());
                var ready = fixture.Lease.Readiness;
                bootstrap.Write((target.WindowHandle == ready.WindowA ? ready.EditorA : ready.EditorB).ToInt64());
                bootstrap.Write(fixture.Lease.Run.ToByteArray());
                bootstrap.Write(popup.Window.ToInt64());
                bootstrap.Write(popup.Marker.ToByteArray());
                bootstrap.Write(expires.ToUnixTimeMilliseconds());
                bootstrap.Write(allowInsert); bootstrap.Write(allowCopy);
                bootstrap.Write(Encoding.ASCII.GetBytes(workerImage.Digest));
                bootstrap.Write(Encoding.ASCII.GetBytes(fixture.Image.Digest));
                bootstrap.Flush();
            }
            var client = new GuardianClient(child, image, commands, responses, run, allowInsert, allowCopy);
            if (client.Read() != "ready") throw new InvalidOperationException("Guardian bootstrap refused.");
            return client;
        }
        catch
        {
            // No publish command has ever been issued on this path; no backup/mutation exists.
            if (child is not null)
            {
                if (!child.HasExited) child.Kill(entireProcessTree: false);
                if (!child.WaitForExit(5000)) throw new InvalidOperationException("Prepublication guardian cleanup failed.");
                child.Dispose();
            }
            commands.Dispose(); responses.Dispose(); image.Dispose();
            throw;
        }
    }
    public SwapStatus Publish(AcceptanceOperation operation)
    {
        if (operation is not (AcceptanceOperation.Copy or AcceptanceOperation.Insert)) return SwapStatus.NotPreserved;
        var copy = operation == AcceptanceOperation.Copy;
        lock (sync)
        {
            if (!CanPublish || !(copy ? allowCopy : allowInsert)) return SwapStatus.NotPreserved;
            spent = true;
            pending = true; // A lost acknowledgement must never erase the recovery obligation.
            var answer = Exchange(copy ? "publish-copy" : "publish-insert");
            var fields = answer.Split(' ');
            if (fields.Length != 3 || !Enum.TryParse<SwapStatus>(fields[0], out var status) || !Enum.IsDefined(status) ||
                !fields[1].StartsWith("saved=", StringComparison.Ordinal) || !fields[2].StartsWith("bytes=", StringComparison.Ordinal) ||
                !int.TryParse(fields[1].AsSpan(6), NumberStyles.None, CultureInfo.InvariantCulture, out var saved) ||
                !long.TryParse(fields[2].AsSpan(6), NumberStyles.None, CultureInfo.InvariantCulture, out var bytes) || saved > 8 || bytes > 4 * 1024 * 1024)
                throw Break();
            SavedCount = saved; ByteCount = bytes; SnapshotAcknowledged = true;
            if (status is SwapStatus.Busy or SwapStatus.NotPreserved) pending = false;
            if (status == SwapStatus.Busy) spent = false;
            return status;
        }
    }
    public bool CommitCopy()
    {
        lock (sync)
        {
            if (closed || !pending || !allowCopy || !IsPinned) return false;
            var answer = Exchange("commit-copy");
            if (answer == "copied") { pending = false; return true; }
            if (answer == "refused") return false;
            throw Break();
        }
    }
    public RestoreStatus Restore()
    {
        lock (sync)
        {
            if (!pending) return RestoreStatus.Restored;
            if (broken || !IsPinned) return RestoreStatus.Failed;
            try
            {
                var answer = Exchange("restore");
                if (!Enum.TryParse<RestoreStatus>(answer, out var status) || !Enum.IsDefined(status)) throw Break();
                if (status == RestoreStatus.Restored) pending = false;
                return status;
            }
            catch (Exception) { return RestoreStatus.Failed; }
        }
    }
    public void CloseAdmission() => closed = true;
    private string Read() => FixedPipe.ReadAsync(reader, TimeSpan.FromSeconds(5), CancellationToken.None).GetAwaiter().GetResult();
    private string Exchange(string command)
    {
        try { FixedPipe.WriteAsync(writer, command).GetAwaiter().GetResult(); return Read(); }
        catch (Exception) { throw Break(); }
    }
    private IOException Break() { broken = true; return new IOException("Guardian control unavailable; recovery remains pending."); }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true; closed = true;
        // Closing control transfers recovery to the retained-worker death path. No TerminateJob/kill.
        Close(writer); Close(reader); Close(commands); Close(responses);
        process.Dispose(); image.Dispose();
    }
    private static void Close(IDisposable stream)
    {
        try { stream.Dispose(); }
        catch (Exception error) when (error is IOException or ObjectDisposedException) { }
    }
}
