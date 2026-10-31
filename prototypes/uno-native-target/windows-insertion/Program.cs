using System.Runtime.InteropServices;
using SmartSnippets.TargetIdentity018;

namespace SmartSnippets.WindowsInsertion018;

internal static class Program
{
    // Security review gate, intentionally closed for this implementation checkpoint.
    // Only a reviewed source edit may expose the native composition. CLI/env/config
    // cannot bypass it; build/test/publish do not touch GUI/input/clipboard.
    private static readonly bool NativeEntryReviewed = false;

    [STAThread]
    private static int Main(string[] args)
    {
        if (!NativeEntryReviewed)
        {
            FixedPipe.Receipt("windows_insertion=NOT_RUN reason=review_gate");
            return 2;
        }
        try
        {
            if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763) || RuntimeInformation.ProcessArchitecture != Architecture.X64)
                throw new ProbeFailure("platform_refused");
            return StaTaskLoop.Run(() => RunRoleAsync(args));
        }
        catch (Exception error)
        {
            FixedPipe.Receipt("windows_insertion=FAILED reason=" + (error is ProbeFailure failure ? failure.Token : "unexpected"));
            return 1;
        }
    }

    private static Task<int> RunRoleAsync(string[] args)
    {
        if (args.Length == 2 && args[0] == "--internal-guardian" && Guid.TryParseExact(args[1], "D", out var guardianRun))
            return ClipboardGuardian.RunAsync(guardianRun);
        if (args.Length == 5 && args[0] == "--internal-worker" && Guid.TryParseExact(args[1], "D", out var workerRun) &&
            Enum.TryParse<InsertionScene>(args[4], out var scene) && Enum.IsDefined(scene))
            return InsertionWorker.RunAsync(args[2], args[3], workerRun, scene);
        if (args.Length == 8 && args[0] == "--authorized-screen-slot" && args[1] == "--authorized-probe-clipboard" &&
            args[2] == "--target" && args[4] == "--sha256" && args[6] == "--scene" &&
            args[7] is "positive" or "abort-after-swap")
            return InsertionWatchdog.RunAsync(args[3], args[5], args[7] == "positive" ? InsertionScene.Positive : InsertionScene.AbortAfterSwap);
        throw new ProbeFailure("authorization_refused");
    }
}
