using System.Diagnostics;
using System.IO.Pipes;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Win32.SafeHandles;
using SmartSnippets.Presentation.Navigation;
using SmartSnippets.Uno.Platforms.Windows.AcceptanceNative;

namespace SmartSnippets.Uno;

public static class Program
{
    internal static readonly bool NativeFixtureSourceReviewed = false;
    private static AppInstance? instance;
    internal static bool StartInBackground => true;
    internal static LaunchOptions InitialOptions => new(null, false, null);
    internal static string PrivateRoot { get; private set; } = "";
    internal static string ProfileRoot => Path.Combine(PrivateRoot, "native-fixture-profile");
    internal static string[] Configuration { get; private set; } = [];
    // One bounded private-file read, cached immutably; never a CLI/environment/settings grant.
    private static readonly Lazy<Guid?> priorBaseline = new(() =>
    {
        var path = Path.Combine(PrivateRoot, "native-launch-permit");
        using var permit = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (permit.Length > 128) throw new InvalidOperationException("native_baseline_permit");
        var bytes = new byte[(int)permit.Length];
        permit.ReadExactly(bytes);
        return PositiveProtocol.ReadPriorToken(bytes, PositiveProtocol.IsPositive(Configuration[0]));
    });
    internal static Guid? KnownPriorToken => priorBaseline.Value;
    internal static AnonymousPipeClientStream Commands { get; private set; } = null!;
    internal static AnonymousPipeClientStream Responses { get; private set; } = null!;
    internal static SafeProcessHandle CoordinatorHandle { get; private set; } = null!;
    internal static void RequireReviewed()
    {
        if (!NativeFixtureSourceReviewed) throw new InvalidOperationException("native_fixture_unreviewed");
        NativeAcceptanceFactory.RequireReviewed();
    }
    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            RequireReviewed(); // No child, host, profile, clipboard or HWND work before both source gates.
            PrivateRoot = Path.GetFullPath(Environment.CurrentDirectory);
            if (!System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileName(PrivateRoot), "^ss-uno019-native-[0-9a-f]{32}$") ||
                !File.Exists(Path.Combine(PrivateRoot, "native-launch-permit"))) throw new InvalidOperationException("native_ownership");
            for (var directory = new DirectoryInfo(PrivateRoot); directory is not null; directory = directory.Parent)
                if ((directory.Attributes & FileAttributes.ReparsePoint) != 0) throw new InvalidOperationException("native_alias");
            using var self = Process.GetCurrentProcess();
            if (!RetainedIdentity.OutsideJob(self.SafeHandle)) throw new InvalidOperationException("native_outer_job");
            var sceneIndex = args.Length == 6 && args[0] == "--coordinate" ? 1 : args.Length == 9 && args[0] == "--internal-worker" ? 4 : -1;
            if (sceneIndex < 0) throw new InvalidOperationException("native_arguments");
            Configuration = args[sceneIndex..];
            _ = KnownPriorToken;
            if (PositiveProtocol.IsPositive(args[sceneIndex]))
            {
                PositiveProtocol.RequireReviewed(args[sceneIndex]);
            }
            if (args.Length == 6 && args[0] == "--coordinate") return Coordinator.RunAsync(args[1..]).GetAwaiter().GetResult();
            if (args.Length != 9 || args[0] != "--internal-worker" || Directory.Exists(ProfileRoot) || File.Exists(ProfileRoot)) throw new InvalidOperationException("native_arguments");
            Commands = new(PipeDirection.In, args[1]); Responses = new(PipeDirection.Out, args[2]);
            CoordinatorHandle = new(BootstrapProtocol.Handle(args[3]), ownsHandle: true);
            if (RetainedIdentity.Id(CoordinatorHandle) != RetainedIdentity.Parent(self.SafeHandle) || !RetainedIdentity.OutsideJob(CoordinatorHandle))
                throw new InvalidOperationException("native_coordinator");
            using var image = PinnedNativeImage.Open(Environment.ProcessPath!, Coordinator.Digest(Environment.ProcessPath!), "SmartSnippets.UnoCandidate.exe");
            if (!RetainedIdentity.Pinned(CoordinatorHandle, image)) throw new InvalidOperationException("native_coordinator_image");
            Configuration = args[4..];
            WinRT.ComWrappersSupport.InitializeComWrappers();
            instance = AppInstance.FindOrRegisterForKey("SmartSnippets.UnoNativeFixture019." + Path.GetFileName(PrivateRoot));
            if (!instance.IsCurrent) throw new InvalidOperationException("native_instance");
            Application.Start(parameters =>
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread()));
                _ = new App();
            });
            return Environment.ExitCode;
        }
        catch { Console.Error.WriteLine("native_fixture_blocked"); return 2; }
    }
    internal static void StopActivations() { instance?.UnregisterKey(); instance = null; }
}
