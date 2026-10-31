using System;
using System.IO;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.Data.Sqlite;
using SmartSnippets.Core.Data;

namespace SmartSnippets.UnoProbe;

internal sealed class ProbeFlowProfile : IDisposable
{
    private readonly string _token = Guid.NewGuid().ToString("N");
    private bool _deleted;
    internal string Root { get; }
    internal SqliteConnectionFactory Factory { get; }
    internal ProbeFlowProfile(string root)
    {
        Root = root;
        if (Directory.Exists(root) || File.Exists(root)) throw new IOException("Profile collision refused.");
        if (OperatingSystem.IsWindows()) CreateWindowsRoot(root);
        else Directory.CreateDirectory(root, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        Factory = new SqliteConnectionFactory(Path.Combine(root, "flow.db"));
        try
        {
            using var marker = new StreamWriter(new FileStream(Path.Combine(root, ".probe-owned"), FileMode.CreateNew));
            marker.Write(_token);
        }
        catch { Directory.Delete(root); throw; }
    }
    public void Dispose()
    {
        if (_deleted) return;
        var marker = Path.Combine(Root, ".probe-owned");
        if ((File.GetAttributes(Root) & System.IO.FileAttributes.ReparsePoint) != 0 ||
            (File.GetAttributes(marker) & System.IO.FileAttributes.ReparsePoint) != 0 || File.ReadAllText(marker) != _token)
            throw new IOException("Owned profile verification failed; retained for recovery.");
        var builder = new SqliteConnectionStringBuilder { DataSource = Factory.DatabasePath, Mode = SqliteOpenMode.ReadWriteCreate };
        using (var connection = new SqliteConnection(builder.ToString())) SqliteConnection.ClearPool(connection);
        Directory.Delete(Root, recursive: true);
        _deleted = true;
    }
    [SupportedOSPlatform("windows")]
    private static void CreateWindowsRoot(string root)
    {
        var security = new DirectorySecurity();
        security.SetAccessRuleProtection(true, false);
        var user = WindowsIdentity.GetCurrent().User ?? throw new InvalidOperationException();
        security.SetOwner(user);
        security.AddAccessRule(new FileSystemAccessRule(user, FileSystemRights.FullControl,
            InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
        new DirectoryInfo(root).Create(security);
    }
}
