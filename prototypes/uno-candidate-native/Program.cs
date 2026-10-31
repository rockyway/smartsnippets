using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using SmartSnippets.Presentation.Services;
using SmartSnippets.TargetIdentity018;
using SmartSnippets.Uno.Platforms.Windows.AcceptanceNative;
using SmartSnippets.WindowsInsertion018;

namespace SmartSnippets.UnoCandidate.Guardian;

internal static class Program
{
    [STAThread]
    private static async Task<int> Main(string[] args)
    {
        // This is not an independently enabled CLI. Source gate + inherited private pipes +
        // duplicated direct-parent/fixture handles must all pass BEFORE clipboard APIs.
        try
        {
            NativeAcceptanceFactory.RequireReviewed();
            if (args.Length != 3 || !Guid.TryParseExact(args[2], "D", out var run)) return 2;
            using var self = Process.GetCurrentProcess();
            if (!RetainedIdentity.OutsideJob(self.SafeHandle)) return 2;
            using var commands = new AnonymousPipeClientStream(PipeDirection.In, args[0]);
            using var responses = new AnonymousPipeClientStream(PipeDirection.Out, args[1]);
            return await RunAsync(self, commands, responses, run);
        }
        catch (Exception) { return 2; } // No content/path/exception diagnostics.
    }

    private static async Task<int> RunAsync(Process self, Stream commands, Stream responses, Guid run)
    {
        // Read a fixed-size bootstrap with a deadline. No arbitrary strings or PID attachment.
        var bytes = new byte[16 + 16 + 16 + 24 + 24 + 8 + 8 + 3 + 128];
        using (var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
            await commands.ReadExactlyAsync(bytes, deadline.Token);
        using var input = new BinaryReader(new MemoryStream(bytes), Encoding.UTF8);
        using var worker = new SafeProcessHandle((nint)input.ReadInt64(), ownsHandle: true);
        using var fixture = new SafeProcessHandle((nint)input.ReadInt64(), ownsHandle: true);
        var window = (nint)input.ReadInt64();
        var editor = (nint)input.ReadInt64();
        var fixtureRun = new Guid(input.ReadBytes(16));
        var popup = (nint)input.ReadInt64();
        var popupMarker = new Guid(input.ReadBytes(16));
        var popupThread = input.ReadUInt32();
        var main = (nint)input.ReadInt64();
        var mainMarker = new Guid(input.ReadBytes(16));
        var mainThread = input.ReadUInt32();
        var expires = DateTimeOffset.FromUnixTimeMilliseconds(input.ReadInt64());
        var allowInsert = input.ReadBoolean(); var allowCopy = input.ReadBoolean(); var allowClipboardControl = input.ReadBoolean();
        var workerDigest = Encoding.ASCII.GetString(input.ReadBytes(64));
        var targetDigest = Encoding.ASCII.GetString(input.ReadBytes(64));
        if (expires <= DateTimeOffset.UtcNow || expires > DateTimeOffset.UtcNow.AddMinutes(2) ||
            (!allowInsert && !allowCopy && !allowClipboardControl) || RetainedIdentity.Id(worker) != RetainedIdentity.Parent(self.SafeHandle) ||
            RetainedIdentity.Parent(fixture) != RetainedIdentity.Id(worker)) return 2;
        using var workerImage = PinnedNativeImage.Open(RetainedIdentity.Image(worker), workerDigest, "SmartSnippets.UnoCandidate.exe");
        using var targetImage = PinnedNativeImage.Open(RetainedIdentity.Image(fixture), targetDigest, "SmartSnippets.Target018.exe");
        var lease = new RetainedFixture(fixture, targetImage);
        using var identity = new WindowsWindowIdentity(lease, fixtureRun, new(window, 0, editor, 0));
        var probe = new ExactProbe(lease, identity, window);
        var target = new InsertionTarget(window, lease.ExpectedPid, 0, "");
        if (!RetainedIdentity.Pinned(worker, workerImage) || probe.Check(target) != TargetCheck.Ok) return 2;
        // Create retry control before allocating any sensitive clipboard backup.
        await using var recovery = new NamedPipeServerStream("ss-uno019-recovery-" + run.ToString("N"), PipeDirection.InOut,
            1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        using var owner = new GuardianWindow(run);
        var publication = new OwnClipboardPublication(run);
        var clipboard = new GuardianClipboard(new NativeClipboardOs(owner.Handle, publication.MarkerFormat, publication.PrivacyFormats),
            publication.MarkerFormat, publication.Token, publication.PrivacyFormats, publication.Expected);
        using var reader = new StreamReader(commands, Encoding.UTF8, false, leaveOpen: true);
        using var writer = new StreamWriter(responses, new UTF8Encoding(false), leaveOpen: true);
        var lifetime = expires - DateTimeOffset.UtcNow;
        var leaseWatch = Stopwatch.StartNew();
        bool Admitted(bool copy) => leaseWatch.Elapsed < lifetime && DateTimeOffset.UtcNow < expires &&
            RetainedIdentity.Pinned(worker, workerImage) && RetainedIdentity.OutsideJob(self.SafeHandle) && owner.IsPinned() &&
            probe.Check(target) == TargetCheck.Ok && (copy
                ? NativeOwnedPopup.QuietAllInput() && (probe.IsTargetForeground(target) ||
                    NativeOwnedPopup.RemoteForegroundReady(popup, RetainedIdentity.Id(worker), popupMarker, popupThread) ||
                    (main != 0 && NativeOwnedPopup.RemoteForegroundReady(main, RetainedIdentity.Id(worker), mainMarker, mainThread)))
                : InputPreflight.QuietOwnedTarget(probe, target));
        bool ControlAdmitted() => leaseWatch.Elapsed < lifetime && DateTimeOffset.UtcNow < expires &&
            RetainedIdentity.Pinned(worker, workerImage) && RetainedIdentity.OutsideJob(self.SafeHandle) && owner.IsPinned() &&
            probe.Check(target) == TargetCheck.Ok && NativeOwnedPopup.QuietAllInput() &&
            NativeDesktop.IsVisibleCurrent();
        var copied = false;
        try
        {
            await FixedPipe.WriteAsync(writer, "ready");
            while (!RetainedIdentity.Stopped(worker))
            {
                var read = FixedPipe.ReadOrEofAsync(reader);
                while (!read.IsCompleted && !RetainedIdentity.Stopped(worker)) await Task.Delay(50);
                if (RetainedIdentity.Stopped(worker)) break;
                var command = await read;
                if (command is null) break;
                string answer;
                if (command == "restore") answer = clipboard.Restore().ToString();
                else if (command == "commit-copy")
                {
                    copied = copied && allowCopy && clipboard.CommitCopy(() => Admitted(copy: true));
                    answer = copied ? "copied" : "refused";
                }
                else if (command is "publish-copy" or "publish-insert" or "publish-control")
                {
                    var copy = command == "publish-copy";
                    var control = command == "publish-control";
                    var status = clipboard.Publish(() => control
                        ? allowClipboardControl && ControlAdmitted()
                        : (copy ? allowCopy : allowInsert) && Admitted(copy));
                    copied = copy && status == SwapStatus.Swapped;
                    answer = FormattableString.Invariant($"{status} saved={clipboard.SavedCount} bytes={clipboard.ByteCount}");
                }
                else break;
                await FixedPipe.WriteAsync(writer, answer);
            }
        }
        catch (Exception) { } // Every postpublication failure stays in this guardian's recovery path.
        // EOF is not proof of worker death, and SendInput may still be in flight.
        await GuardianRecovery.RetainUntilSettledAsync(() => RetainedIdentity.Stopped(worker), () => clipboard.HasBackup,
            clipboard.Restore, () => Task.Delay(100), async status =>
            {
                // Foreign ownership / busy / partial never discard RAM or overwrite foreign data.
                try
                {
                    await recovery.WaitForConnectionAsync();
                    using var retry = new StreamReader(recovery, leaveOpen: true);
                    return await RecoveryPipeProtocol.RespondAsync(recovery, retry, status, clipboard.Restore, () => clipboard.HasBackup);
                }
                finally { try { recovery.Disconnect(); } catch (Exception) { } }
            });
        return 0;
    }

    private sealed class RetainedFixture(SafeProcessHandle process, PinnedNativeImage image) : IOwnedFixtureLease
    {
        public uint ExpectedPid => RetainedIdentity.Id(process);
        public bool IsAliveAndPinned() => RetainedIdentity.Pinned(process, image);
    }
    private sealed class ExactProbe(IOwnedFixtureLease lease, WindowsWindowIdentity identity, nint window) : ITargetProbe
    {
        public TargetCheck Check(InsertionTarget target) => target.WindowHandle == window && target.ProcessId == lease.ExpectedPid &&
            identity.Exists(window) ? TargetCheck.Ok : TargetCheck.Closed;
        public bool IsTargetForeground(InsertionTarget target) => Check(target) == TargetCheck.Ok && identity.IsForeground(window);
    }
}
