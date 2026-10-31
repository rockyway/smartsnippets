using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using SmartSnippets.TargetIdentity018;

namespace SmartSnippets.Uno.Platforms.Windows.AcceptanceNative;

/// <summary>Internal harness creates and retains its own fixture, never adopts an existing PID/window.</summary>
internal sealed class AcceptanceFixture : IAsyncDisposable
{
    internal OwnedFixtureLease Lease { get; }
    internal Process Child { get; }
    internal PinnedNativeImage Image { get; }
    private AcceptanceFixture(OwnedFixtureLease lease, Process child, PinnedNativeImage image) => (Lease, Child, Image) = (lease, child, image);
    internal static async Task<AcceptanceFixture> StartAsync(string nativeRoot, string path, string digest, CancellationToken ct)
    {
        NativeAcceptanceFactory.RequireReviewed();
        var image = PinnedNativeImage.Open(path, digest, "SmartSnippets.Target018.exe");
        try
        {
            var artifact = await TargetArtifact.OpenAsync(nativeRoot, path, digest, TargetPlatform.Windows, (_, _) => Task.CompletedTask, ct);
            RetainedChild? child = null;
            var lease = await OwnedFixtureLease.StartAsync(artifact, (info, _) => child = RetainedChild.Start(info, image), ct);
            return new(lease, child!.Process, image);
        }
        catch { image.Dispose(); throw; }
    }
    public async ValueTask DisposeAsync()
    {
        try { await Lease.DisposeAsync(); }
        finally { Image.Dispose(); }
    }
    private sealed class RetainedChild : IOwnedChild
    {
        internal Process Process { get; }
        private readonly PinnedNativeImage image;
        public uint Id { get; }
        public ChildProtocol Protocol { get; }
        private RetainedChild(Process process, PinnedNativeImage image)
        {
            (Process, this.image) = (process, image);
            Id = checked((uint)process.Id);
            if (!IsIdentityAlive()) throw new InvalidOperationException("Owned fixture identity refused.");
            Protocol = new(process.StandardOutput, process.StandardError, process.StandardInput);
        }
        internal static RetainedChild Start(ProcessStartInfo info, PinnedNativeImage image)
        {
            WindowsToken.RequireUnelevatedLauncher();
            var process = Process.Start(info) ?? throw new InvalidOperationException("Owned fixture start failed.");
            try { return new(process, image); }
            catch
            {
                if (!process.HasExited) process.Kill(entireProcessTree: false);
                if (!process.WaitForExit(3000)) throw new InvalidOperationException("Owned fixture cleanup failed.");
                process.Dispose();
                throw;
            }
        }
        public bool IsIdentityAlive() => RetainedIdentity.Pinned(Process.SafeHandle, image);
        public async ValueTask DisposeAsync()
        {
            if (!Process.HasExited) Process.Kill(entireProcessTree: false);
            await Process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(3));
            await Protocol.DisposeAsync();
            Process.Dispose();
        }
    }
}
