using System.Diagnostics;
using System.Runtime.InteropServices;
using SmartSnippets.Presentation.Services;

namespace SmartSnippets.TargetIdentity018;

internal readonly record struct HarnessOptions(string Executable, string Digest, bool NativeCallback = false)
{
    internal static HarnessOptions Parse(string[] args)
    {
        var callback = args.Contains("--inprocess-native-callback");
        if (args.Length != (callback ? 8 : 6) || args.Count(a => a == "--inprocess-native-callback") != (callback ? 1 : 0) ||
            args.Count(a => a == "--authorized-native-hotkey") != (callback ? 1 : 0) ||
            args.Count(a => a == "--authorized-screen-slot") != 1 || args.Count(a => a == "--watchdog-stdin") != 1)
            throw new ProbeFailure("authorization_refused");
        string? executable = null, digest = null;
        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--authorized-screen-slot": case "--watchdog-stdin":
                case "--inprocess-native-callback": case "--authorized-native-hotkey": break;
                case "--executable" when executable is null && index + 1 < args.Length: executable = args[++index]; break;
                case "--sha256" when digest is null && index + 1 < args.Length: digest = args[++index]; break;
                default: throw new ProbeFailure("arguments_refused");
            }
        }
        return executable is not null && digest is not null ? new(executable, digest, callback) : throw new ProbeFailure("arguments_refused");
    }
}

internal static class IdentityHarness
{
    internal static int Main(string[] args) => args.Contains("--inprocess-native-callback")
        ? CallbackHarness.Run(args) : RunIdentityAsync(args).GetAwaiter().GetResult();

    private static async Task<int> RunIdentityAsync(string[] args)
    {
        PrivateDiagnostics? diagnostics = null;
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        using var watchdogInput = Console.OpenStandardInput();
        ConsoleCancelEventHandler interrupt = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += interrupt;
        try
        {
            var options = HarnessOptions.Parse(args);
            var platform = HostPlatform();
            diagnostics = new PrivateDiagnostics();
            Console.WriteLine("private_evidence_token=" + diagnostics.Token);
            Console.Out.Flush();
            // Mandatory outer watchdog creates an isolated process group/job before
            // releasing this gate. EOF is cancellation, not permission to continue.
            var permit = await ReadWatchdogPermitAsync(watchdogInput, stop.Token).WaitAsync(TimeSpan.FromSeconds(5), stop.Token);
            if (permit != "owned_watchdog_ready") throw new ProbeFailure("watchdog_refused");
            _ = Task.Run(() => CancelOnWatchdogEof(watchdogInput, stop));
            var root = NativeRoot();
            using var artifact = await TargetArtifact.OpenAsync(root, options.Executable, options.Digest, platform, VerifySignatureAsync, stop.Token);
            diagnostics.Receipt("artifact_validated");
            await using (var lease = await OwnedFixtureLease.StartAsync(artifact, NativeChild.Start, stop.Token))
            {
                diagnostics.Receipt("child_ready");
                await ObserveAsync(lease, diagnostics.Receipt, FocusPreflight.RequireAsync, stop.Token);
            }
            diagnostics.Receipt("owned_child_stopped");
            Console.WriteLine("owned_identity=OBSERVED cleanup=STOPPED input=NONE clipboard=UNTOUCHED");
            return 0;
        }
        catch (Exception error)
        {
            var token = error is ProbeFailure failure ? failure.Token : error is OperationCanceledException ? "cancelled" : "unexpected";
            diagnostics?.TryFailureReceipt("owned_identity_failed reason=" + token);
            try { Console.Error.WriteLine("owned_identity_failed reason=" + token); }
            catch (IOException) { } // The parent may already have closed its diagnostic pipe.
            return 1;
        }
        finally
        {
            diagnostics?.Dispose();
            Console.CancelKeyPress -= interrupt;
        }
    }

    internal static async Task ObserveAsync(OwnedFixtureLease lease, Action<string> receipt,
        Func<CancellationToken, Task> focusPreflight, CancellationToken ct,
        Func<IOwnedFixtureLease, TargetReadiness, Guid, IExactWindowIdentity>? nativeBoundary = null)
    {
        using var probe = lease.CreateProbe(nativeBoundary); // Same trusted lease used for capture and revalidation.
        var a = probe.Capture("a");
        var b = probe.Capture("b");
        Require(probe.Check(a) == TargetCheck.Ok && probe.Check(b) == TargetCheck.Ok, "initial_identity");
        await lease.CommandAsync("empty_a", ct);
        await lease.CommandAsync("empty_b", ct);
        foreach (var window in new[] { "a", "b", "a" })
        {
            await focusPreflight(ct);
            await lease.CommandAsync("focus_" + window, ct);
            var expected = window == "a" ? a : b;
            var other = window == "a" ? b : a;
            Require(probe.Check(expected) == TargetCheck.Ok && probe.IsTargetForeground(expected) &&
                probe.Check(other) == TargetCheck.Ok && !probe.IsTargetForeground(other), "exact_foreground");
            receipt("exact_focus_" + window);
        }
        await lease.CommandAsync("close_a", ct);
        Require(probe.Check(a) == TargetCheck.Closed && !probe.IsTargetForeground(a) && probe.Check(b) == TargetCheck.Ok, "closed_identity");
        await lease.CommandAsync("empty_b", ct);
        await focusPreflight(ct);
        await lease.CommandAsync("focus_b", ct);
        Require(probe.Check(a) == TargetCheck.Closed && !probe.IsTargetForeground(a) && probe.IsTargetForeground(b), "surviving_identity");
        await lease.RequireQuietProtocolAsync(ct);
        receipt("closed_a_surviving_b");
    }

    internal static async Task<string> ReadWatchdogPermitAsync(Stream input, CancellationToken ct)
    {
        var bytes = new byte[64];
        var count = 0;
        while (count < bytes.Length)
        {
            if (await input.ReadAsync(bytes.AsMemory(count, 1), ct) == 0) throw new ProbeFailure("watchdog_eof");
            if (bytes[count] == '\n') return System.Text.Encoding.ASCII.GetString(bytes, 0, count);
            if (bytes[count] < ' ' || bytes[count] > '~') throw new ProbeFailure("watchdog_refused");
            count++;
        }
        throw new ProbeFailure("watchdog_refused");
    }

    internal static void CancelOnWatchdogEof(Stream input, CancellationTokenSource stop)
    {
        // Console.In's synchronized ReadLineAsync can block synchronously. A
        // dedicated background read avoids blocking the composition thread.
        try { _ = input.ReadByte(); stop.Cancel(); }
        catch (Exception) { try { stop.Cancel(); } catch (ObjectDisposedException) { } }
    }

    internal static TargetPlatform HostPlatform()
    {
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763) && RuntimeInformation.ProcessArchitecture == Architecture.X64)
            return TargetPlatform.Windows;
        if (OperatingSystem.IsMacOSVersionAtLeast(12) && RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            return TargetPlatform.MacOS;
        throw new ProbeFailure("host_refused");
    }

    internal static string NativeRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (directory.Name == "uno-native-target" && Directory.Exists(Path.Combine(directory.FullName, "windows")) && Directory.Exists(Path.Combine(directory.FullName, "macos")))
            {
                var output = Path.Combine(directory.FullName, "identity", "bin") + Path.DirectorySeparatorChar;
                var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
                if (!Path.GetFullPath(AppContext.BaseDirectory).StartsWith(output, comparison)) throw new ProbeFailure("harness_path");
                TargetArtifact.RefuseLinks(AppContext.BaseDirectory);
                return directory.FullName;
            }
        throw new ProbeFailure("harness_path");
    }

    private static void Require(bool value, string fixedToken) { if (!value) throw new ProbeFailure(fixedToken); }

    internal static async Task VerifySignatureAsync(string bundle, CancellationToken ct)
    {
        var info = new ProcessStartInfo("/usr/bin/codesign") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in new[] { "--verify", "--strict", "--deep", bundle }) info.ArgumentList.Add(argument);
        using var process = Process.Start(info) ?? throw new ProbeFailure("signature_refused");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(TimeSpan.FromSeconds(10));
        try
        {
            var output = DiscardBoundedAsync(process.StandardOutput, deadline.Token);
            var error = DiscardBoundedAsync(process.StandardError, deadline.Token);
            await Task.WhenAll(output, error, process.WaitForExitAsync(deadline.Token));
            if (process.ExitCode != 0) throw new ProbeFailure("signature_refused");
        }
        finally
        {
            if (!process.HasExited) process.Kill(entireProcessTree: false);
            using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await process.WaitForExitAsync(cleanup.Token);
        }
    }

    private static async Task DiscardBoundedAsync(StreamReader reader, CancellationToken ct)
    {
        var buffer = new char[512];
        var count = 0;
        int read;
        while ((read = await reader.ReadAsync(buffer.AsMemory(), ct)) != 0)
            if ((count += read) > 4096) throw new ProbeFailure("signature_output");
    }
}
