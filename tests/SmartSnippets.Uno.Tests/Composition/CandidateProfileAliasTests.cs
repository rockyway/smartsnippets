using System.Runtime.InteropServices;

namespace SmartSnippets.Uno.Tests.Composition;

public sealed partial class CandidateProfileAliasTests
{
    [Fact]
    public void HardLinkedProfileFiles_AreRefusedBeforeStoreAccess()
    {
        if (!OperatingSystem.IsWindows()) return;
        using var f = new HostFixture();
        Directory.CreateDirectory(f.Root);
        var prepared = Path.Combine(f.Root, "offline-original.db"); File.WriteAllText(prepared, "synthetic");
        Assert.True(CreateHardLink(Path.Combine(f.Root, "smartsnippets.db"), prepared, 0));
        Assert.Throws<IOException>(() => CandidateProfile.OpenOwnedRoot(f.Root));
        Assert.Equal("synthetic", File.ReadAllText(prepared));
        Assert.False(Directory.Exists(Path.Combine(f.Root, "logs")));
    }
    [LibraryImport("kernel32.dll", EntryPoint = "CreateHardLinkW", StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CreateHardLink(string name, string existing, nint security);
}
