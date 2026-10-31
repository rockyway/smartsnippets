#if !WINDOWS
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using SmartSnippets.Presentation.Services;

namespace SmartSnippets.UnoProbe;

internal sealed unsafe partial class MacPrivatePasteboardProbe
{
    private const string WatchdogTemporary = "Synthetic watchdog temporary λ 日本語 😀\nsecond";
    private const string WatchdogNewOwner = "Synthetic watchdog newer owner";

    private MacPrivatePasteboardProbe(Guid run, bool owner)
    {
        var type = GetClass("NSPasteboard");
        _general = Send(type, Sel("generalPasteboard"));
        _name = Str("org.smartsnippets.feasibility018.guardian." + run.ToString("N"));
        _board = SendArg(type, Sel("pasteboardWithName:"), _name);
        _releaseOwnedBoard = owner;
        CheckPrivate();
        if (owner)
        {
            var advertised = Send(_board, Sel("types"));
            Require(advertised == 0 || Send(advertised, Sel("count")).ToInt64() == 0);
        }
    }

    internal static int RunWatchdog(string runId, bool owner)
    {
        if (!OperatingSystem.IsMacOS() || RuntimeInformation.ProcessArchitecture != Architecture.Arm64 ||
            !Guid.TryParseExact(runId, "N", out var run)) return 2;
        nint pool = 0;
        var stage = "setup";
        try
        {
            _ = NativeLibrary.Load("/System/Library/Frameworks/AppKit.framework/AppKit");
            pool = Send(Send(GetClass("NSAutoreleasePool"), Sel("alloc")), Sel("init"));
            Require(pool != 0);
            var general = Send(GetClass("NSPasteboard"), Sel("generalPasteboard"));
            Require(general != 0);
            var epoch = Send(general, Sel("changeCount"));
            Require(epoch.ToInt64() >= 0);
            using (var probe = new MacPrivatePasteboardProbe(run, owner))
            {
                stage = owner ? "guardian" : "worker";
                if (owner) probe.GuardPrivateWorker();
                else probe.MutatePrivateFixture();
            }
            stage = "general_epoch";
            Require(Send(general, Sel("changeCount")) == epoch);
            Console.WriteLine(owner
                ? "private_watchdog_guardian_complete general_epoch=UNCHANGED general_content_reads=0 general_writes=0"
                : "private_watchdog_worker_complete general_epoch=UNCHANGED general_content_reads=0 general_writes=0");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"private_watchdog_failed stage={stage} kind={error.GetType().Name}");
            return 1;
        }
        finally { if (pool != 0) SendVoid(pool, Sel("drain")); }
    }

    private static List<Item> WatchdogSeed() => [
            TextItem("Synthetic guardian original λ 日本語 😀\nsecond"),
            new(new()
            {
                [Plain] = Encoding.UTF8.GetBytes("synthetic"),
                ["public.rtf"] = Encoding.ASCII.GetBytes("{\\rtf1\\ansi synthetic}"),
                ["public.html"] = Encoding.UTF8.GetBytes("<p>synthetic</p>")
            })
        ];

    private void GuardPrivateWorker()
    {
        // The native guardian alone holds the original byte/order snapshot.
        // It is never serialized to the supervisor, a file or process streams.
        var original = PublishAndCapture(WatchdogSeed());
        var saved = new Snapshot(_board, Epoch, original);
        Snapshot? temporary = null;
        var settled = false;
        Console.WriteLine("private_watchdog_guardian_ready snapshot=MEMORY_ONLY");
        string? command;
        while ((command = Console.ReadLine()) is not null)
        {
            if (command == "capture_swapped" && temporary is null && !settled)
            {
                var captured = Capture(Limit, out _);
                Require(captured is not null && captured.Epoch == Epoch && captured.Epoch > saved.Epoch &&
                    MatchesSeed(captured.Items, [TextItem(WatchdogTemporary)]));
                temporary = captured;
                saved.Epoch = captured!.Epoch;
                Console.WriteLine("private_watchdog_swapped_observed snapshot=MEMORY_ONLY");
            }
            else if (command == "restore" && temporary is not null && !settled)
            {
                Require(HasContents(temporary.Items));
                Require(Restore(saved) == RestoreStatus.Restored && HasContents(original));
                settled = true;
                Console.WriteLine("private_watchdog_restored representations=EXACT");
            }
            else if (command == "refuse_changed" && temporary is not null && !settled)
            {
                var current = Capture(Limit, out _);
                Require(current is not null && current.Epoch == Epoch && current.Epoch != saved.Epoch &&
                    MatchesSeed(current.Items, [TextItem(WatchdogNewOwner)]));
                Require(Restore(saved) == RestoreStatus.ClipboardChanged && Epoch == current!.Epoch && HasContents(current.Items));
                settled = true;
                Console.WriteLine("private_watchdog_changed_owner_preserved mutation=NONE");
            }
            else if (command == "finish" && settled) return;
            else throw new InvalidOperationException("Watchdog command refused.");
        }
        // Supervisor EOF is a recovery trigger, not a blanket success signal.
        // An unacknowledged/intervening write refuses rather than guessing ownership.
        if (!settled && temporary is not null)
        {
            Require(HasContents(temporary.Items));
            Require(Restore(saved) == RestoreStatus.Restored && HasContents(original));
            settled = true;
            Console.WriteLine("private_watchdog_eof_restored representations=EXACT");
        }
        Require(settled);
    }

    private void MutatePrivateFixture()
    {
        // Never releases the shared private board: the guardian owns its lifetime.
        var initial = Capture(Limit, out _);
        Require(initial is not null && initial.Epoch == Epoch && MatchesSeed(initial.Items, WatchdogSeed()));
        Console.WriteLine("private_watchdog_worker_ready");
        var swapped = false;
        string? command;
        while ((command = Console.ReadLine()) is not null)
        {
            if (command == "swap" && !swapped)
            {
                _ = PublishAndCapture([TextItem(WatchdogTemporary)]);
                swapped = true;
                Console.WriteLine("private_watchdog_worker_swapped");
            }
            else if (command == "new_owner" && swapped)
            {
                _ = PublishAndCapture([TextItem(WatchdogNewOwner)]);
                Console.WriteLine("private_watchdog_worker_new_owner");
            }
            else throw new InvalidOperationException("Watchdog command refused.");
        }
    }
}
#endif
