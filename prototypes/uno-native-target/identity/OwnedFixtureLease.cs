using System.Diagnostics;
using System.Globalization;

namespace SmartSnippets.TargetIdentity018;

internal readonly record struct TargetReadiness(nint WindowA, nint WindowB, nint EditorA, nint EditorB)
{
    internal static TargetReadiness Parse(string line, uint pid, TargetPlatform platform)
    {
        var parts = line.Split(' ');
        var windows = platform == TargetPlatform.Windows;
        if (parts.Length != (windows ? 6 : 4) || parts[0] != "target_ready" || parts[1] != "pid=" + pid.ToString(CultureInfo.InvariantCulture))
            throw new ProbeFailure("readiness_refused");
        var a = Number(parts[2], "a=");
        var b = Number(parts[3], "b=");
        var editorA = windows ? Number(parts[4], "edit_a=") : 0;
        var editorB = windows ? Number(parts[5], "edit_b=") : 0;
        if (a == b || (windows && new HashSet<nint> { a, b, editorA, editorB }.Count != 4)) throw new ProbeFailure("readiness_refused");
        return new(a, b, editorA, editorB);
    }

    private static nint Number(string field, string prefix)
    {
        if (!field.StartsWith(prefix, StringComparison.Ordinal)) throw new ProbeFailure("readiness_refused");
        var digits = field.AsSpan(prefix.Length);
        if (digits.IsEmpty || digits[0] == '0' || !ulong.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var value) ||
            value > (ulong)nint.MaxValue || value == 0) throw new ProbeFailure("readiness_refused");
        return (nint)value;
    }
}

/// <summary>Owns only its started child, its pinned image and its bounded protocol.</summary>
internal sealed class OwnedFixtureLease : IOwnedFixtureLease, IAsyncDisposable
{
    private readonly TargetArtifact artifact;
    private readonly IOwnedChild child;
    private bool disposed;
    internal Guid Run { get; }
    internal TargetReadiness Readiness { get; private set; }
    internal TargetPlatform Platform => artifact.Platform;
    public uint ExpectedPid => child.Id;

    private OwnedFixtureLease(TargetArtifact artifact, IOwnedChild child, Guid run) => (this.artifact, this.child, Run) = (artifact, child, run);

    // Test seam substitutes only the actual process creation/OS identity boundary;
    // artifact validation, fixed args, readiness and failure cleanup remain real.
    internal static async Task<OwnedFixtureLease> StartAsync(TargetArtifact artifact,
        Func<ProcessStartInfo, TargetPlatform, IOwnedChild> start, CancellationToken ct, TimeSpan? readinessTimeout = null)
    {
        IOwnedChild? child = null;
        try
        {
            ct.ThrowIfCancellationRequested();
            if (!artifact.IsStillPinned()) throw new ProbeFailure("artifact_changed");
            var run = Guid.NewGuid();
            var info = new ProcessStartInfo(artifact.Path)
            {
                UseShellExecute = false, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true,
                CreateNoWindow = true, WorkingDirectory = Path.GetDirectoryName(artifact.Path)!
            };
            // Do not inherit loader-injection/startup-hook variables into a pinned image.
            info.Environment.Clear();
            if (artifact.Platform == TargetPlatform.Windows)
            {
                info.Environment["SystemRoot"] = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                info.Environment["WINDIR"] = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            }
            else info.Environment["PATH"] = "/usr/bin:/bin";
            info.ArgumentList.Add("--authorized-probe-target");
            info.ArgumentList.Add(run.ToString("D"));
            ct.ThrowIfCancellationRequested();
            child = start(info, artifact.Platform);
            var lease = new OwnedFixtureLease(artifact, child, run);
            if (!lease.IsAliveAndPinned()) throw new ProbeFailure("child_identity");
            var line = await child.Protocol.ReadAsync(readinessTimeout ?? TimeSpan.FromSeconds(8), ct);
            lease.Readiness = TargetReadiness.Parse(line, child.Id, artifact.Platform);
            if (!lease.IsAliveAndPinned() || child.Protocol.HasUnexpectedOutput) throw new ProbeFailure("readiness_refused");
            return lease;
        }
        catch
        {
            try { if (child is not null) await child.DisposeAsync(); }
            finally { artifact.Dispose(); }
            throw;
        }
    }

    public bool IsAliveAndPinned() => !disposed && child.Id != 0 && child.Id != (uint)Environment.ProcessId &&
        child.IsIdentityAlive() && artifact.IsStillPinned();

    internal OwnedTargetProbe CreateProbe(Func<IOwnedFixtureLease, TargetReadiness, Guid, IExactWindowIdentity>? nativeBoundary = null)
    {
        if (!IsAliveAndPinned()) throw new ProbeFailure("child_identity");
        IExactWindowIdentity identity = nativeBoundary is not null ? nativeBoundary(this, Readiness, Run) :
            Platform == TargetPlatform.Windows ? new WindowsWindowIdentity(this, Run, Readiness) : new MacWindowIdentity(this);
        return new OwnedTargetProbe(this, identity);
    }

    internal async Task CommandAsync(string command, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (!IsAliveAndPinned() || child.Protocol.HasUnexpectedOutput) throw new ProbeFailure("child_identity");
        await child.Protocol.SendAsync(command, TimeSpan.FromSeconds(2), ct);
        var line = await child.Protocol.ReadAsync(TimeSpan.FromSeconds(4), ct);
        if (line != "target_command name=" + command + " ok=1" || !IsAliveAndPinned()) throw new ProbeFailure("command_failed");
    }

    internal async Task RequireQuietProtocolAsync(CancellationToken ct)
    {
        await Task.Delay(100, ct);
        if (!IsAliveAndPinned() || child.Protocol.HasUnexpectedOutput) throw new ProbeFailure("protocol_refused");
    }

    public async ValueTask DisposeAsync()
    {
        if (disposed) return;
        disposed = true;
        try { await child.DisposeAsync(); }
        finally { artifact.Dispose(); }
    }
}
