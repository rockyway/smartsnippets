using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;

namespace SmartSnippets.TargetIdentity018;

internal sealed partial class PrivateDiagnostics : IDisposable
{
    private readonly StreamWriter log;
    internal string Token { get; } = Guid.NewGuid().ToString("N");
    internal string DirectoryPath { get; }

    internal unsafe PrivateDiagnostics()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ss-identity018-" + Token);
        if (OperatingSystem.IsWindows())
        {
            using var identity = WindowsIdentity.GetCurrent();
            var owner = identity.User ?? throw new ProbeFailure("private_directory");
            var security = new DirectorySecurity();
            security.SetOwner(owner);
            security.SetAccessRuleProtection(true, false);
            security.AddAccessRule(new FileSystemAccessRule(owner, FileSystemRights.FullControl,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
            var descriptor = security.GetSecurityDescriptorBinaryForm();
            fixed (byte* data = descriptor)
            {
                var attributes = new SecurityAttributes { Length = (uint)sizeof(SecurityAttributes), Descriptor = (nint)data };
                if (CreateDirectoryW(directory, ref attributes) == 0) throw new ProbeFailure("private_directory");
            }
        }
        else if (OperatingSystem.IsMacOS())
        {
            var template = Encoding.UTF8.GetBytes(directory + "-XXXXXX\0");
            fixed (byte* data = template)
            {
                if (mkdtemp(data) == 0) throw new ProbeFailure("private_directory");
                directory = Marshal.PtrToStringUTF8((nint)data) ?? throw new ProbeFailure("private_directory");
            }
        }
        else throw new ProbeFailure("host_refused");
        DirectoryPath = directory;
        var options = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write, Share = FileShare.Read };
        if (!OperatingSystem.IsWindows()) options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        log = new StreamWriter(new FileStream(Path.Combine(directory, "receipts.log"), options), new UTF8Encoding(false)) { AutoFlush = true };
    }

    internal void Receipt(string fixedToken) => log.WriteLine(fixedToken);
    internal void TryFailureReceipt(string fixedToken)
    {
        try { log.WriteLine(fixedToken); }
        catch (IOException) { } // Diagnostic failure must not escape as a raw exception.
    }
    public void Dispose()
    {
        try { log.Dispose(); }
        catch (IOException) { }
    }

    [StructLayout(LayoutKind.Sequential)] private struct SecurityAttributes { public uint Length; public nint Descriptor; public int Inherit; }
    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16)] private static partial int CreateDirectoryW(string path, ref SecurityAttributes attributes);
    [LibraryImport("/usr/lib/libSystem.B.dylib")] private static unsafe partial nint mkdtemp(byte* template);
}
