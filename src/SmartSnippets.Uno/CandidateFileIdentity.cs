using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace SmartSnippets.Uno;

/// <summary>Read-only filesystem identity checks; never opens the production database.</summary>
internal static unsafe partial class CandidateFileIdentity
{
    internal static string CanonicalDirectory(string path)
    {
        if (!OperatingSystem.IsWindows()) return Path.GetFullPath(path);
        using var handle = CreateFile(path, 0, 7, 0, 3, 0x02000000, 0); // directory, metadata only
        if (handle.IsInvalid) throw new IOException("Candidate directory identity could not be verified.");
        var buffer = new char[32768];
        uint count;
        fixed (char* chars = buffer) count = GetFinalPathNameByHandle(handle, chars, (uint)buffer.Length, 0);
        if (count == 0 || count >= buffer.Length) throw new IOException("Candidate directory identity could not be verified.");
        return new string(buffer, 0, (int)count);
    }
    internal static void RejectHardLink(string path)
    {
        if (!OperatingSystem.IsWindows()) return;
        using var handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        var info = default(FileInformation);
        if (GetFileInformationByHandle(handle, &info) == 0 || info.Links != 1)
            throw new IOException("Candidate files must have a single verified owner path.");
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct FileInformation
    {
        internal uint Attributes;
        internal uint CreationLow, CreationHigh, AccessLow, AccessHigh, WriteLow, WriteHigh;
        internal uint Volume, SizeHigh, SizeLow, Links, IndexHigh, IndexLow;
    }
    [LibraryImport("kernel32.dll", EntryPoint = "CreateFileW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    private static partial SafeFileHandle CreateFile(string name, uint access, uint share, nint security, uint creation, uint flags, nint template);
    [LibraryImport("kernel32.dll", EntryPoint = "GetFinalPathNameByHandleW", SetLastError = true)]
    private static partial uint GetFinalPathNameByHandle(SafeFileHandle handle, char* path, uint size, uint flags);
    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial int GetFileInformationByHandle(SafeFileHandle handle, FileInformation* info);
}
