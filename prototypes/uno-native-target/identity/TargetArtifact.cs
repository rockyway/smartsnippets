using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace SmartSnippets.TargetIdentity018;

internal enum TargetPlatform { Windows, MacOS }

/// <summary>Pinned read-only artifact; never resolves links into an arbitrary executable.</summary>
internal sealed class TargetArtifact : IDisposable
{
    internal string Path { get; }
    internal TargetPlatform Platform { get; }
    private readonly string digest;
    private readonly FileStream image;
    private readonly string? plistPath;
    private readonly byte[]? plistBytes;

    private TargetArtifact(string path, string digest, TargetPlatform platform, FileStream image, string? plistPath, byte[]? plistBytes)
        => (Path, this.digest, Platform, this.image, this.plistPath, this.plistBytes) = (path, digest, platform, image, plistPath, plistBytes);

    internal static async Task<TargetArtifact> OpenAsync(string nativeRoot, string path, string digest, TargetPlatform platform,
        Func<string, CancellationToken, Task> verifySignature, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (digest.Length != 64 || digest.Any(c => !Uri.IsHexDigit(c))) throw new ProbeFailure("artifact_hash");
        var root = System.IO.Path.GetFullPath(System.IO.Path.Combine(nativeRoot, platform == TargetPlatform.Windows ? "windows" : "macos", "bin"));
        path = System.IO.Path.GetFullPath(path);
        var comparison = platform == TargetPlatform.Windows ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var name = platform == TargetPlatform.Windows ? "SmartSnippets.Target018.exe" : "SmartSnippets.Target018";
        if (!path.StartsWith(root + System.IO.Path.DirectorySeparatorChar, comparison) ||
            !string.Equals(System.IO.Path.GetFileName(path), name, StringComparison.Ordinal)) throw new ProbeFailure("artifact_path");
        RefuseLinks(path);
        if (OperatingSystem.IsMacOS() && (File.GetUnixFileMode(path) &
            (UnixFileMode.SetUser | UnixFileMode.SetGroup | UnixFileMode.GroupWrite | UnixFileMode.OtherWrite)) != 0)
            throw new ProbeFailure("artifact_mode");
        FileStream? image = null;
        try
        {
            image = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (image.Length is < 32 or > 134217728) throw new ProbeFailure("artifact_size");
            var data = new byte[checked((int)image.Length)];
            image.ReadExactly(data);
            RequireNativeHeader(data, platform);
            if (!Convert.ToHexString(SHA256.HashData(data)).Equals(digest, StringComparison.OrdinalIgnoreCase))
                throw new ProbeFailure("artifact_hash");
            string? plist = null;
            byte[]? plistData = null;
            if (platform == TargetPlatform.MacOS)
            {
                var executableDirectory = Directory.GetParent(path)!;
                var contents = executableDirectory.Parent!;
                var bundle = contents.Parent!;
                if (executableDirectory.Name != "MacOS" || contents.Name != "Contents" || bundle.Name != "SmartSnippets.Target018.app")
                    throw new ProbeFailure("bundle_path");
                plist = System.IO.Path.Combine(contents.FullName, "Info.plist");
                RefuseLinks(plist);
                plistData = ReadPlist(plist);
                await verifySignature(bundle.FullName, ct);
            }
            var result = new TargetArtifact(path, digest, platform, image, plist, plistData);
            if (!result.IsStillPinned()) throw new ProbeFailure("artifact_changed");
            return result;
        }
        catch { image?.Dispose(); throw; }
    }

    internal bool IsStillPinned()
    {
        try
        {
            RefuseLinks(Path);
            if (OperatingSystem.IsMacOS() && (File.GetUnixFileMode(Path) &
                (UnixFileMode.SetUser | UnixFileMode.SetGroup | UnixFileMode.GroupWrite | UnixFileMode.OtherWrite)) != 0) return false;
            // Re-open by path as well as retaining the original file. Detect replacement,
            // including POSIX rename-over, rather than hashing only an unlinked old inode.
            using var current = new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (current.Length != image.Length) return false;
            if (!Convert.ToHexString(SHA256.HashData(current)).Equals(digest, StringComparison.OrdinalIgnoreCase)) return false;
            if (plistPath is not null)
            {
                RefuseLinks(plistPath);
                if (!ReadPlist(plistPath).AsSpan().SequenceEqual(plistBytes)) return false;
            }
            return true;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ProbeFailure or XmlException) { return false; }
    }

    internal static void RefuseLinks(string path)
    {
        for (var current = System.IO.Path.GetFullPath(path); !string.IsNullOrEmpty(current); current = System.IO.Path.GetDirectoryName(current))
            if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) throw new ProbeFailure("artifact_link");
    }

    private static byte[] ReadPlist(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length is <= 0 or > 65536) throw new ProbeFailure("bundle_identity");
        var data = new byte[(int)stream.Length];
        stream.ReadExactly(data);
        using var input = new MemoryStream(data);
        using var reader = XmlReader.Create(input, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null, MaxCharactersInDocument = 65536 });
        var document = XDocument.Load(reader);
        var root = document.Root;
        var dict = root?.Element("dict");
        if (root?.Name != "plist" || dict is null || root.Elements().Count() != 1) throw new ProbeFailure("bundle_identity");
        var elements = dict.Elements().ToArray();
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        if (elements.Length % 2 != 0) throw new ProbeFailure("bundle_identity");
        for (var index = 0; index < elements.Length; index += 2)
            if (elements[index].Name != "key" || !values.TryAdd(elements[index].Value, elements[index + 1].Value)) throw new ProbeFailure("bundle_identity");
        if (!values.TryGetValue("CFBundleIdentifier", out var id) || id != "org.smartsnippets.feasibility018.target" ||
            !values.TryGetValue("CFBundleExecutable", out var entry) || entry != "SmartSnippets.Target018" ||
            !values.TryGetValue("CFBundlePackageType", out var type) || type != "APPL") throw new ProbeFailure("bundle_identity");
        // Security-relevant identity values must actually be plist strings.
        foreach (var key in new[] { "CFBundleIdentifier", "CFBundleExecutable", "CFBundlePackageType" })
            if (elements[Array.FindIndex(elements, e => e.Name == "key" && e.Value == key) + 1].Name != "string") throw new ProbeFailure("bundle_identity");
        return data;
    }

    internal static void RequireNativeHeader(ReadOnlySpan<byte> data, TargetPlatform platform)
    {
        // Same refusal rules as existing macos/run_smoke.py + uno-desktop/verify_evidence.py.
        foreach (var marker in new[] { "hostfxr_main_startupinfo", "DotNetRuntimeContractDescriptor", "coreclr_initialize" })
            if (data.IndexOf(Encoding.ASCII.GetBytes(marker)) >= 0) throw new ProbeFailure("artifact_jit");
        if (data.IndexOf(Convert.FromHexString("8b1202b96a612038727b930214d7a03213f5b9e6efae3318ee3b2dce24b36aae")) >= 0)
            throw new ProbeFailure("artifact_jit");
        if (platform == TargetPlatform.Windows)
        {
            if (data.Length < 64 || data[0] != 'M' || data[1] != 'Z') throw new ProbeFailure("artifact_header");
            var pe = BinaryPrimitives.ReadUInt32LittleEndian(data[60..]);
            if (pe > (uint)Math.Max(0, data.Length - 264)) throw new ProbeFailure("artifact_header");
            var header = data[(int)pe..];
            if (!header[..4].SequenceEqual("PE\0\0"u8) || BinaryPrimitives.ReadUInt16LittleEndian(header[4..]) != 0x8664 ||
                BinaryPrimitives.ReadUInt16LittleEndian(header[20..]) < 240 ||
                (long)pe + 24 + BinaryPrimitives.ReadUInt16LittleEndian(header[20..]) > data.Length ||
                BinaryPrimitives.ReadUInt16LittleEndian(header[24..]) != 0x20b ||
                BinaryPrimitives.ReadUInt16LittleEndian(header[(24 + 68)..]) != 2 ||
                BinaryPrimitives.ReadUInt32LittleEndian(header[(24 + 108)..]) < 15 ||
                BinaryPrimitives.ReadUInt64LittleEndian(header[(24 + 112 + 14 * 8)..]) != 0) throw new ProbeFailure("artifact_header");
        }
        else
        {
            if (data.Length < 32 || BinaryPrimitives.ReadUInt32LittleEndian(data) != 0xfeedfacf ||
                BinaryPrimitives.ReadUInt32LittleEndian(data[4..]) != 0x0100000c || BinaryPrimitives.ReadUInt32LittleEndian(data[12..]) != 2)
                throw new ProbeFailure("artifact_header");
            var count = BinaryPrimitives.ReadUInt32LittleEndian(data[16..]);
            var size = BinaryPrimitives.ReadUInt32LittleEndian(data[20..]);
            if (size > data.Length - 32 || count > size / 8) throw new ProbeFailure("artifact_header");
            var offset = 32;
            for (uint index = 0; index < count; index++)
            {
                var end = 32 + (int)size;
                if (offset > end - 8) throw new ProbeFailure("artifact_header");
                var command = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
                var length = BinaryPrimitives.ReadUInt32LittleEndian(data[(offset + 4)..]);
                if (length < 8 || length > end - offset) throw new ProbeFailure("artifact_header");
                if (command is 0xc or 0x18 or 0x80000018 or 0x8000001f)
                {
                    if (length < 24) throw new ProbeFailure("artifact_header");
                    var start = BinaryPrimitives.ReadUInt32LittleEndian(data[(offset + 8)..]);
                    if (start < 24 || start >= length) throw new ProbeFailure("artifact_header");
                    var name = Encoding.UTF8.GetString(data.Slice(offset + (int)start, (int)(length - start))).ToLowerInvariant();
                    if (name.Contains("coreclr", StringComparison.Ordinal) || name.Contains("clrjit", StringComparison.Ordinal) || name.Contains("monosgen", StringComparison.Ordinal))
                        throw new ProbeFailure("artifact_jit");
                }
                offset += (int)length;
            }
            if (offset != 32 + size) throw new ProbeFailure("artifact_header");
        }
    }

    public void Dispose() => image.Dispose();
}

internal sealed class ProbeFailure(string token) : Exception("Owned target probe refused.")
{
    internal string Token { get; } = token;
}
