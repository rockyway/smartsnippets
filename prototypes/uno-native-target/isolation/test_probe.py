"""Pure contract tests: no DLL loading, native calls, child processes or clipboard access."""
import ctypes as C
import io
import json
from pathlib import Path
import sys
import unittest
from unittest.mock import MagicMock, patch

sys.path.insert(0, str(Path(__file__).resolve().parent))
import probe


class GateTests(unittest.TestCase):
    def test_closed_review_gate_prevents_native_calls(self):
        with patch.object(probe, "REVIEWED", False), patch.object(probe, "Native") as native:
            self.assertEqual(2, probe.main(["--authorized-private-station"]))
            native.assert_not_called()

    def test_authorization_required_even_after_review(self):
        with patch.object(probe, "REVIEWED", True), patch.object(probe.sys, "platform", "win32"), \
                patch.object(probe, "Native") as native:
            self.assertEqual(2, probe.main([]))
            native.assert_not_called()

    def test_non_windows_never_loads_dll(self):
        with patch.object(probe, "REVIEWED", True), patch.object(probe.sys, "platform", "linux"), \
                patch.object(probe, "Native") as native:
            self.assertEqual(2, probe.main(["--authorized-private-station"]))
            native.assert_not_called()


class ProtocolTests(unittest.TestCase):
    def test_receipts_allowlist_and_numeric_count_only(self):
        for bad in ([{"receipt": "payload", "count": 1}],
                    [{"receipt": "probe_failed", "count": True}],
                    [{"receipt": "probe_failed", "count": 1, "text": "x"}],
                    [probe.receipt("probe_failed")] * 2):
            with self.subTest(bad=bad), self.assertRaises(probe.Closed):
                probe.valid_receipts(bad)
        self.assertEqual([], probe.valid_receipts([]))

    def test_identity_pins_script_interpreter_parent_and_guid(self):
        good = {"run": "00000000-0000-0000-0000-000000000001", "script": "hash",
                "python": "hash", "parent": 42, "sid": "S-1-5-21-1"}
        with patch.object(probe, "digest", return_value="hash"), patch.object(probe.os, "getppid", return_value=42):
            probe.validate_identity(good, 42, "python")
            for key, value in (("run", "not-guid"), ("script", "wrong"),
                               ("python", "wrong"), ("parent", 43)):
                with self.subTest(key=key), self.assertRaises((probe.Closed, ValueError)):
                    probe.validate_identity(dict(good, **{key: value}), 42, "python")

    def test_channel_rejects_overlong_and_eof(self):
        for text in ("x" * 4097 + "\n", "", "not-json\n"):
            with self.subTest(length=len(text)), self.assertRaises(probe.Closed):
                probe.Channel(io.StringIO(text)).get()

    def test_channel_pumps_while_waiting_for_worker(self):
        channel = object.__new__(probe.Channel)
        channel.messages = probe.queue.Queue()
        pump = MagicMock(side_effect=lambda: channel.messages.put({"state": "ready"}))
        self.assertEqual({"state": "ready"}, channel.get(pump))
        pump.assert_called_once()

    def test_channel_accepts_bounded_json(self):
        self.assertEqual({"state": "ready"}, probe.Channel(io.StringIO('{"state":"ready"}\n')).get())


class NativePolicyTests(unittest.TestCase):
    def make_native(self):
        n = object.__new__(probe.Native)
        n.u, n.k, n.a = MagicMock(), MagicMock(), MagicMock()
        return n

    def test_message_pump_is_bounded_and_dispatches_queued_messages(self):
        n = self.make_native()
        n.u.PeekMessageW.side_effect = [True, True, False]
        n.pump()
        self.assertEqual(2, n.u.DispatchMessageW.call_count)
        n.u.reset_mock()
        n.u.PeekMessageW.side_effect = None
        n.u.PeekMessageW.return_value = True
        n.pump()
        self.assertEqual(64, n.u.PeekMessageW.call_count)

    def test_reader_joins_before_stdout_is_closed(self):
        child, channel = MagicMock(), MagicMock()
        child.poll.return_value = 0
        channel.thread.is_alive.return_value = False
        events = []
        child.wait.side_effect = lambda **kwargs: events.append("reap")
        channel.thread.join.side_effect = lambda **kwargs: events.append("join")
        child.stdout.close.side_effect = lambda: events.append("close")
        probe.stop_child(child, channel)
        self.assertEqual(["reap", "join", "close"], events)

    def test_descriptor_is_freed_even_when_restore_fails(self):
        n = self.make_native()
        surface = probe.Surface(n, "S-1-5-21-1")
        surface.sd = 123
        n.u.SetProcessWindowStation.return_value = False
        with self.assertRaises(probe.Closed):
            surface.close()
        n.k.LocalFree.assert_called_once_with(123)
        self.assertIsNone(surface.sd)

    def test_guardian_surface_closes_when_worker_cleanup_fails(self):
        n, surface, child, incoming = self.make_native(), MagicMock(), MagicMock(), MagicMock()
        incoming.get.return_value = {"sid": "S-1-5-21-1", "run": "fixture"}
        n.transaction = MagicMock()
        with patch.object(probe, "authenticate"), patch.object(probe, "Surface") as factory, \
                patch.object(probe, "spawn", return_value=child), \
                patch.object(probe, "Channel", side_effect=RuntimeError()), \
                patch.object(probe, "stop_child", side_effect=OSError()):
            factory.return_value.open.return_value = surface
            with self.assertRaises(OSError):
                probe.guardian(n, incoming)
            surface.close.assert_called_once()

    def test_private_rejects_interactive_station_before_flag_query(self):
        n = self.make_native()
        n.name = MagicMock(return_value="WinSta0")
        with self.assertRaises(probe.Closed):
            n.private(123)
        n.u.GetUserObjectInformationW.assert_not_called()

    def test_private_rejects_visible_station(self):
        n = self.make_native()
        n.name = MagicMock(return_value="private")
        def info(handle, index, flags, size, needed):
            C.cast(flags, C.POINTER(probe.UserFlags)).contents.flags = 1
            return True
        n.u.GetUserObjectInformationW.side_effect = info
        with self.assertRaises(probe.Closed):
            n.private(123)

    def test_null_create_only_failure_is_terminal_no_open_existing(self):
        n = self.make_native()
        n.u.CreateWindowStationW.return_value = None
        surface = probe.Surface(n, "S-1-5-21-1")
        with self.assertRaises(probe.CreationBlocked):
            surface.open()
        args = n.u.CreateWindowStationW.call_args.args
        self.assertIsNone(args[0])
        self.assertEqual(1, args[1])
        n.u.OpenWindowStationW.assert_not_called()
        n.u.CreateDesktopW.assert_not_called()
        n.u.CreateWindowExW.assert_not_called()
        n.a.ConvertStringSecurityDescriptorToSecurityDescriptorW.assert_called_once()
        self.assertEqual("O:S-1-5-21-1D:P(A;;GA;;;S-1-5-21-1)",
                         n.a.ConvertStringSecurityDescriptorToSecurityDescriptorW.call_args.args[0])

    def test_cleanup_restores_station_then_desktop_and_closes_reverse(self):
        n = self.make_native()
        surface = probe.Surface(n, "S-1-5-21-1")
        surface.hwnd, surface.desktop, surface.station = 10, 20, 30
        n.u.reset_mock()
        surface.close()
        self.assertEqual(["DestroyWindow", "SetProcessWindowStation", "SetThreadDesktop",
                          "CloseDesktop", "CloseWindowStation"],
                         [call[0] for call in n.u.mock_calls if not call[0].endswith("().__bool__")])

    def test_failed_restore_does_not_close_bound_handles(self):
        n = self.make_native()
        surface = probe.Surface(n, "S-1-5-21-1")
        surface.desktop, surface.station = 20, 30
        n.u.SetProcessWindowStation.return_value = False
        with self.assertRaises(probe.Closed):
            surface.close()
        n.u.CloseDesktop.assert_not_called()
        n.u.CloseWindowStation.assert_not_called()

    def test_cross_process_clear_observes_guardian_owner(self):
        n = self.make_native()
        n.owner_pid, n.set_sample, n.check_sample = MagicMock(), MagicMock(), MagicMock()
        n.u.GetClipboardOwner.return_value = 10
        n.transaction(10, 77, probe.SAMPLES[1], clear=True)
        n.owner_pid.assert_called_once_with(10, 77)
        n.u.EmptyClipboard.assert_called_once()
        self.assertEqual(2, n.u.GetClipboardOwner.call_count)
        n.u.CloseClipboard.assert_called_once()

    def test_same_owner_write_never_empties(self):
        n = self.make_native()
        n.owner_pid, n.set_sample, n.check_sample = MagicMock(), MagicMock(), MagicMock()
        n.u.GetClipboardOwner.return_value = 10
        n.transaction(10, 77, probe.SAMPLES[2])
        n.u.EmptyClipboard.assert_not_called()
        n.set_sample.assert_called_once_with(probe.SAMPLES[2])

    def test_fresh_nonempty_clipboard_fails_before_clear_or_seed(self):
        n = self.make_native()
        n.owner_pid, n.set_sample = MagicMock(), MagicMock()
        n.u.CountClipboardFormats.return_value = 1
        with patch.object(C, "set_last_error", create=True), self.assertRaises(probe.Closed):
            n.transaction(10, 77, probe.SAMPLES[0], clear=True, fresh=True)
        n.u.EmptyClipboard.assert_not_called()
        n.set_sample.assert_not_called()
        n.u.CloseClipboard.assert_called_once()

    def test_owner_mismatch_fails_before_seed_and_unlocks(self):
        n = self.make_native()
        n.owner_pid, n.set_sample = MagicMock(), MagicMock()
        n.u.GetClipboardOwner.return_value = 99
        with self.assertRaises(probe.Closed):
            n.transaction(10, 77, probe.SAMPLES[2])
        n.set_sample.assert_not_called()
        n.u.CloseClipboard.assert_called_once()

    def test_failed_open_does_not_close_unacquired_clipboard(self):
        n = self.make_native()
        n.owner_pid = MagicMock()
        n.u.OpenClipboard.return_value = False
        with self.assertRaises(probe.Closed):
            n.transaction(10, 77, probe.SAMPLES[2])
        n.u.CloseClipboard.assert_not_called()

    def test_global_allocation_transferred_only_after_set_succeeds(self):
        for succeeds in (True, False):
            with self.subTest(succeeds=succeeds):
                n = self.make_native()
                n.k.GlobalAlloc.return_value = 123
                n.k.GlobalLock.return_value = 456
                n.k.GlobalUnlock.return_value = False
                n.k.GlobalFree.return_value = None
                n.u.SetClipboardData.return_value = 123 if succeeds else None
                with patch.object(C, "memmove"), patch.object(C, "set_last_error", create=True), \
                        patch.object(C, "get_last_error", return_value=0, create=True):
                    if succeeds:
                        n.set_sample(probe.SAMPLES[0])
                    else:
                        with self.assertRaises(probe.Closed):
                            n.set_sample(probe.SAMPLES[0])
                self.assertEqual(0 if succeeds else 1, n.k.GlobalFree.call_count)

    def test_job_assignment_failure_never_releases_guardian(self):
        n = self.make_native()
        n.name = MagicMock(return_value="WinSta0")
        n.k.AssignProcessToJobObject.return_value = False
        child = MagicMock()
        child._handle = 123
        child.poll.return_value = None
        with patch.object(probe, "spawn", return_value=child), \
                patch.object(probe, "send") as send, patch.object(probe, "drain_job") as drain:
            with self.assertRaises(probe.Closed):
                probe.launcher(n)
            send.assert_not_called()
            drain.assert_called_once()
            child.kill.assert_called_once()
            child.wait.assert_called_once()
            self.assertEqual(2, n.u.GetClipboardSequenceNumber.call_count)

    def test_general_sequence_change_rejects_receipts(self):
        n = self.make_native()
        n.name = MagicMock(return_value="WinSta0")
        n.u.GetClipboardSequenceNumber.side_effect = [1, 2]
        child = MagicMock()
        child._handle, child.returncode = 123, 0
        child.communicate.return_value = (json.dumps({"run": "fixture", "receipts": [
            probe.receipt(name) for name in ("cross_process_guardian_owner", "same_owner_external_write",
                                            "not_run_abort_after_clear", "not_run_foreign_owner_refusal")]}), None)
        with patch.object(probe, "spawn", return_value=child), patch.object(probe, "send"), \
                patch.object(probe, "config_for", return_value={"run": "fixture"}), patch.object(probe, "drain_job"), \
                patch("builtins.print") as output:
            with self.assertRaises(probe.Closed):
                probe.launcher(n)
            output.assert_not_called()

    def test_timeout_drains_job_and_reaps_child_before_sequence_check(self):
        n = self.make_native()
        n.name = MagicMock(return_value="WinSta0")
        n.u.GetClipboardSequenceNumber.return_value = 12
        child = MagicMock()
        child._handle = 123
        child.communicate.side_effect = probe.subprocess.TimeoutExpired("owned", 48)
        order = []
        with patch.object(probe, "spawn", return_value=child), patch.object(probe, "send"), \
                patch.object(probe, "config_for", return_value={"run": "fixture"}), \
                patch.object(probe, "drain_job", side_effect=lambda *args: order.append("drain")), \
                patch.object(probe, "stop_child", side_effect=lambda *args: order.append("reap")):
            with self.assertRaises(probe.subprocess.TimeoutExpired):
                probe.launcher(n)
        self.assertEqual(["drain", "reap"], order)
        self.assertEqual(2, n.u.GetClipboardSequenceNumber.call_count)

    def test_spawn_is_pinned_direct_child_with_no_shell(self):
        with patch.object(probe.subprocess, "Popen") as popen:
            probe.spawn("worker")
        args, options = popen.call_args
        self.assertEqual([str(Path(sys.executable).resolve()), "-I", "-B", str(probe.SCRIPT),
                          "--authorized-private-station", "--role", "worker"], args[0])
        self.assertNotIn("shell", options)
        self.assertTrue(options["close_fds"])
        self.assertEqual(0x08000000, options["creationflags"])
        self.assertEqual(probe.subprocess.DEVNULL, options["stderr"])


if __name__ == "__main__":
    unittest.main()
