using System.Buffers.Binary;
using System.Security.Cryptography;
using SmartSnippets.TargetIdentity018;
using Xunit;

public sealed class TargetArtifactTests
{
    [Fact]
    public async Task ContainedNativeImageIsPinnedAndOutsideOrWrongNamesAreRefused()
    {
        using var fixture = new ArtifactFixture();
        using var artifact = await fixture.OpenAsync();
        Assert.True(artifact.IsStillPinned());
        var outside = Path.Combine(fixture.Root, "SmartSnippets.Target018.exe");
        File.Copy(fixture.Entry, outside);
        await Assert.ThrowsAsync<ProbeFailure>(() => fixture.OpenAsync(outside));
        var wrongName = Path.Combine(Path.GetDirectoryName(fixture.Entry)!, "other.exe");
        File.Copy(fixture.Entry, wrongName);
        await Assert.ThrowsAsync<ProbeFailure>(() => fixture.OpenAsync(wrongName));
    }

    [Fact]
    public async Task WrongHashAndChangedPinnedImageAreRefused()
    {
        using var fixture = new ArtifactFixture();
        await Assert.ThrowsAsync<ProbeFailure>(() => fixture.OpenAsync(digest: new string('0', 64)));
        await Assert.ThrowsAsync<ProbeFailure>(() => fixture.OpenAsync(digest: "not-a-hash"));
        using var artifact = await fixture.OpenAsync();
        if (OperatingSystem.IsWindows())
            Assert.Throws<IOException>(() => File.WriteAllBytes(fixture.Entry, new byte[512])); // Retained file denies writes/replacement.
        else
        {
            File.WriteAllBytes(fixture.Entry, new byte[512]);
            Assert.False(artifact.IsStillPinned());
        }
    }

    [Theory]
    [InlineData("arch")]
    [InlineData("managed")]
    [InlineData("console")]
    [InlineData("host")]
    [InlineData("offset")]
    [InlineData("truncated")]
    public async Task NativeHeaderSpoofsAreRefusedEvenWithMatchingHash(string change)
    {
        using var fixture = new ArtifactFixture();
        var bytes = File.ReadAllBytes(fixture.Entry);
        switch (change)
        {
            case "arch": BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(68), 0xaa64); break;
            case "managed": bytes[64 + 24 + 112 + 14 * 8] = 1; break;
            case "console": bytes[64 + 24 + 68] = 3; break;
            case "host": "coreclr_initialize"u8.CopyTo(bytes.AsSpan(400)); break;
            case "offset": BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(60), uint.MaxValue); break;
            case "truncated": bytes = bytes[..100]; break;
        }
        File.WriteAllBytes(fixture.Entry, bytes);
        await Assert.ThrowsAsync<ProbeFailure>(() => fixture.OpenAsync());
    }

    [Fact]
    public async Task MacBundleIdentityAndSignatureAreRequiredBeforeLaunch()
    {
        using var fixture = new ArtifactFixture(mac: true);
        var signatureCalls = 0;
        using (var artifact = await fixture.OpenAsync(signature: (_, _) => { signatureCalls++; return Task.CompletedTask; }))
            Assert.True(artifact.IsStillPinned());
        Assert.Equal(1, signatureCalls);
        await Assert.ThrowsAsync<ProbeFailure>(() => fixture.OpenAsync(signature: (_, _) => throw new ProbeFailure("signature_refused")));
        File.WriteAllText(fixture.Plist!, "<plist><dict><key>CFBundleIdentifier</key><string>foreign</string></dict></plist>");
        await Assert.ThrowsAsync<ProbeFailure>(() => fixture.OpenAsync(signature: (_, _) => { signatureCalls++; return Task.CompletedTask; }));
        Assert.Equal(1, signatureCalls); // Bad identity never reaches codesign boundary.
    }

    [Fact]
    public void MacWrongArchitectureAndJitDependencyAreRefused()
    {
        var bytes = ArtifactFixture.MachO();
        bytes[4] = 7;
        Assert.Throws<ProbeFailure>(() => TargetArtifact.RequireNativeHeader(bytes, TargetPlatform.MacOS));
        bytes = ArtifactFixture.MachO();
        "hostfxr_main_startupinfo"u8.CopyTo(bytes.AsSpan(64));
        Assert.Throws<ProbeFailure>(() => TargetArtifact.RequireNativeHeader(bytes, TargetPlatform.MacOS));
    }

    [Fact]
    public async Task SymlinkedExecutableIsRefusedWhereTheHostAllowsLinkCreation()
    {
        using var fixture = new ArtifactFixture();
        var directory = Path.Combine(fixture.Root, "windows", "bin", "linked");
        Directory.CreateDirectory(directory);
        var link = Path.Combine(directory, "SmartSnippets.Target018.exe");
        try { File.CreateSymbolicLink(link, fixture.Entry); }
        catch (UnauthorizedAccessException) { return; } // No privilege/security-setting changes for tests.
        await Assert.ThrowsAsync<ProbeFailure>(() => fixture.OpenAsync(link));
    }
}

internal sealed class ArtifactFixture : IDisposable
{
    internal string Root { get; } = Path.Combine(OperatingSystem.IsMacOS() ? "/private/tmp" : Path.GetTempPath(), "identity018-tests-" + Guid.NewGuid().ToString("N"));
    internal string Entry { get; }
    internal string? Plist { get; }
    internal TargetPlatform Platform { get; }

    internal ArtifactFixture(bool mac = false)
    {
        Platform = mac ? TargetPlatform.MacOS : TargetPlatform.Windows;
        Entry = Path.Combine(Root, mac ? "macos" : "windows", "bin", "fixture",
            mac ? "SmartSnippets.Target018.app/Contents/MacOS/SmartSnippets.Target018" : "SmartSnippets.Target018.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(Entry)!);
        File.WriteAllBytes(Entry, mac ? MachO() : Pe());
        if (mac)
        {
            Plist = Path.Combine(Directory.GetParent(Entry)!.Parent!.FullName, "Info.plist");
            File.WriteAllText(Plist, "<plist><dict><key>CFBundleIdentifier</key><string>org.smartsnippets.feasibility018.target</string><key>CFBundleExecutable</key><string>SmartSnippets.Target018</string><key>CFBundlePackageType</key><string>APPL</string></dict></plist>");
        }
    }

    internal Task<TargetArtifact> OpenAsync(string? path = null, string? digest = null, Func<string, CancellationToken, Task>? signature = null) =>
        TargetArtifact.OpenAsync(Root, path ?? Entry, digest ?? Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path ?? Entry))), Platform,
            signature ?? ((_, _) => Task.CompletedTask), CancellationToken.None);

    internal static byte[] Pe()
    {
        var data = new byte[512];
        data[0] = (byte)'M'; data[1] = (byte)'Z';
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(60), 64);
        "PE\0\0"u8.CopyTo(data.AsSpan(64));
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(68), 0x8664);
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(84), 240);
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(88), 0x20b);
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(64 + 24 + 68), 2);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(64 + 24 + 108), 16);
        return data;
    }

    internal static byte[] MachO()
    {
        var data = new byte[512];
        BinaryPrimitives.WriteUInt32LittleEndian(data, 0xfeedfacf);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(4), 0x0100000c);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(12), 2);
        return data;
    }

    public void Dispose() => Directory.Delete(Root, recursive: true);
}
