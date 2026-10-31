using System.Diagnostics;
using SmartSnippets.Uno.Services;

namespace SmartSnippets.Uno.Tests.Composition;

public sealed class CandidateOwnershipReviewTests
{
    [Fact]
    public void ProductionAncestor_IsRejectedBeforeCreatingOwnershipFile()
    {
        var root = NewRoot();
        try
        {
            var production = Path.Combine(root, "production");
            Directory.CreateDirectory(production);
            Assert.Throws<InvalidOperationException>(() => CandidateProfile.OpenOwnedRoot(root, production));
            Assert.False(File.Exists(Path.Combine(root, ".candidate-owner")));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void CanonicalProductionJunctionTarget_IsRejectedBeforeProfileEnumeration()
    {
        if (!OperatingSystem.IsWindows()) return;
        var root = NewRoot();
        var production = Path.Combine(root, "production-link");
        try
        {
            var target = Path.Combine(root, "target"); Directory.CreateDirectory(target);
            CreateJunction(production, target);
            Assert.Throws<InvalidOperationException>(() => CandidateProfile.OpenOwnedRoot(target, production));
            Assert.False(File.Exists(Path.Combine(target, ".candidate-owner")));
        }
        finally
        {
            if (Directory.Exists(production)) Directory.Delete(production);
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void ConcurrentOwner_IsRejectedBeforeInspectingNestedReparseEntries()
    {
        if (!OperatingSystem.IsWindows()) return;
        var root = NewRoot();
        var candidate = Path.Combine(root, "candidate");
        var nested = Path.Combine(candidate, "nested-link");
        try
        {
            var production = Path.Combine(root, "production"); Directory.CreateDirectory(production);
            var other = Path.Combine(root, "other"); Directory.CreateDirectory(other);
            using var owner = CandidateProfile.OpenOwnedRoot(candidate, production);
            CreateJunction(nested, other);
            // Walking first would throw InvalidOperationException for the nested
            // junction; ownership refusal must happen before any such inspection.
            Assert.Throws<IOException>(() => CandidateProfile.OpenOwnedRoot(candidate, production));
            Directory.Delete(nested);
        }
        finally
        {
            if (Directory.Exists(nested)) Directory.Delete(nested);
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void FailedValidation_ReleasesOwnershipLeaseForRepairAndRetry()
    {
        if (!OperatingSystem.IsWindows()) return;
        var root = NewRoot();
        var candidate = Path.Combine(root, "candidate"); Directory.CreateDirectory(candidate);
        var nested = Path.Combine(candidate, "nested-link");
        try
        {
            var production = Path.Combine(root, "production"); Directory.CreateDirectory(production);
            var other = Path.Combine(root, "other"); Directory.CreateDirectory(other);
            CreateJunction(nested, other);
            Assert.Throws<InvalidOperationException>(() => CandidateProfile.OpenOwnedRoot(candidate, production));
            Directory.Delete(nested);
            using var repaired = CandidateProfile.OpenOwnedRoot(candidate, production);
        }
        finally
        {
            if (Directory.Exists(nested)) Directory.Delete(nested);
            Directory.Delete(root, true);
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void MessageCleanup_AttemptsBothStepsAndInvalidatesHandleOnFailure(bool throwDestroy, bool throwUnregister)
    {
        var native = new FailingCleanup(throwDestroy, throwUnregister);
        var lifetime = new CandidateMessageWindowLifetime(native);
        Assert.Throws<InvalidOperationException>(() => lifetime.Dispose());
        Assert.Equal((nint)0, lifetime.Handle);
        Assert.Equal(1, native.Destroys); Assert.Equal(1, native.Unregisters);
        lifetime.Dispose();
        Assert.Equal(1, native.Destroys); Assert.Equal(1, native.Unregisters);
    }

    private static string NewRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "ss-candidate-ownership-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root); return root;
    }
    private static void CreateJunction(string link, string target)
    {
        // Only fresh GUID-owned fixture paths, no elevation or global changes.
        var start = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe"))
        {
            UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true,
        };
        foreach (var argument in new[] { "/d", "/c", "mklink", "/J", link, target }) start.ArgumentList.Add(argument);
        using var process = Process.Start(start)!;
        if (!process.WaitForExit(5000)) { process.Kill(true); process.WaitForExit(); throw new TimeoutException("Owned junction fixture timed out."); }
        Assert.Equal(0, process.ExitCode);
        Assert.True(Directory.Exists(link));
    }
    private sealed class FailingCleanup(bool throwDestroy, bool throwUnregister) : ICandidateMessageWindowNative
    {
        internal int Destroys, Unregisters;
        public bool RegisterClass() => true;
        public nint CreateWindow() => 1;
        public bool DestroyWindow(nint handle) { Destroys++; if (throwDestroy) throw new IOException("synthetic"); return false; }
        public bool UnregisterClass() { Unregisters++; if (throwUnregister) throw new IOException("synthetic"); return false; }
    }
}
