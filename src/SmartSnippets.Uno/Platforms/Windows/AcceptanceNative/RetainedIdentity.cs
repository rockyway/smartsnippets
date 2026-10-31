using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;
using SmartSnippets.TargetIdentity018;

namespace SmartSnippets.Uno.Platforms.Windows.AcceptanceNative;

internal sealed class PinnedNativeImage : IDisposable
{
    internal string Path { get; }
    internal string Digest { get; }
    private readonly FileStream image;
    private PinnedNativeImage(string path, string digest, FileStream image) => (Path, Digest, this.image) = (path, digest, image);
    internal static PinnedNativeImage Open(string path, string digest, string name)
    {
        path = System.IO.Path.GetFullPath(path);
        if (System.IO.Path.GetFileName(path) != name || digest.Length != 64) throw new InvalidOperationException("Native image refused.");
        TargetArtifact.RefuseLinks(path);
        var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        try
        {
            if (stream.Length is < 264 or > 134217728) throw new InvalidOperationException("Native image refused.");
            var bytes = new byte[(int)stream.Length];
            stream.ReadExactly(bytes);
            TargetArtifact.RequireNativeHeader(bytes, TargetPlatform.Windows);
            if (!Convert.ToHexString(SHA256.HashData(bytes)).Equals(digest, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Native image refused.");
            return new(path, digest, stream);
        }
        catch { stream.Dispose(); throw; }
    }
    internal bool IsPinned()
    {
        try
        {
            TargetArtifact.RefuseLinks(Path);
            using var current = new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.Read);
            return current.Length == image.Length && Convert.ToHexString(SHA256.HashData(current)).Equals(Digest, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ProbeFailure) { return false; }
    }
    public void Dispose() => image.Dispose();
}

internal static partial class RetainedIdentity
{
    internal static bool OutsideJob(SafeProcessHandle handle) => IsProcessInJob(handle, 0, out var inJob) != 0 && inJob == 0;
    internal static bool Alive(SafeProcessHandle handle) => !handle.IsClosed && !handle.IsInvalid && WaitForSingleObject(handle, 0) == 258;
    internal static bool Stopped(SafeProcessHandle handle) => !handle.IsClosed && !handle.IsInvalid && WaitForSingleObject(handle, 0) == 0;
    internal static uint Id(SafeProcessHandle handle) => GetProcessId(handle);
    internal static unsafe string Image(SafeProcessHandle handle)
    {
        var buffer = stackalloc char[32768];
        uint size = 32768;
        if (QueryFullProcessImageNameW(handle, 0, buffer, ref size) == 0) throw new InvalidOperationException("Retained identity refused.");
        return new string(buffer, 0, (int)size);
    }
    internal static bool Pinned(SafeProcessHandle handle, PinnedNativeImage image) => Alive(handle) && image.IsPinned() &&
        string.Equals(Image(handle), image.Path, StringComparison.OrdinalIgnoreCase) && WindowsToken.IsSameUnelevatedToken(handle);
    internal static nint Duplicate(SafeProcessHandle source, SafeProcessHandle recipient)
    {
        if (DuplicateHandle(GetCurrentProcess(), source, recipient, out var duplicate, 0x00101000, 0, 0) == 0)
            throw new InvalidOperationException("Retained handle transfer failed.");
        return duplicate;
    }
    internal static unsafe uint Parent(SafeProcessHandle handle)
    {
        var information = new BasicInformation();
        if (NtQueryInformationProcess(handle, 0, &information, (uint)sizeof(BasicInformation), out _) != 0 || information.Parent > uint.MaxValue)
            throw new InvalidOperationException("Direct child identity refused.");
        return (uint)information.Parent;
    }
    [StructLayout(LayoutKind.Sequential)] private struct BasicInformation
    { public nint Reserved, Peb, Reserved2, Reserved3; public nuint Process, Parent; }
    [LibraryImport("kernel32.dll")] private static partial int IsProcessInJob(SafeProcessHandle process, nint job, out int result);
    [LibraryImport("kernel32.dll")] private static partial uint WaitForSingleObject(SafeProcessHandle process, uint milliseconds);
    [LibraryImport("kernel32.dll")] private static partial uint GetProcessId(SafeProcessHandle process);
    [LibraryImport("kernel32.dll")] private static unsafe partial int QueryFullProcessImageNameW(SafeProcessHandle process, uint flags, char* path, ref uint size);
    [LibraryImport("kernel32.dll")] private static partial nint GetCurrentProcess();
    [LibraryImport("kernel32.dll")] private static partial int DuplicateHandle(nint sourceProcess, SafeProcessHandle source, SafeProcessHandle recipient,
        out nint duplicate, uint access, int inherit, uint options);
    [LibraryImport("ntdll.dll")] private static unsafe partial int NtQueryInformationProcess(SafeProcessHandle process, uint kind, void* information, uint size, out uint returned);
}
