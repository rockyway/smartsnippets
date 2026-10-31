import importlib.util
import hashlib
import plistlib
import subprocess
import sys
import tempfile
from unittest.mock import Mock, patch
from pathlib import Path
import unittest

PROTOTYPES = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("target_smoke", PROTOTYPES / "uno-native-target" / "macos" / "run_smoke.py")
target = importlib.util.module_from_spec(spec)
spec.loader.exec_module(target)


class NativeTargetProtocolTests(unittest.TestCase):
    def test_exact_started_pid_and_distinct_positive_native_windows(self):
        self.assertTrue(target.ready_pid_windows("target_ready pid=42 a=101 b=102\n", 42))
        self.assertFalse(target.ready_pid_windows("target_ready pid=41 a=101 b=102\n", 42))
        self.assertFalse(target.ready_pid_windows("target_ready pid=42 a=101 b=101\n", 42))
        self.assertFalse(target.ready_pid_windows("target_ready pid=42 a=1 b=01\n", 42))
        self.assertFalse(target.ready_pid_windows("target_ready pid=42 a=0 b=102\n", 42))
        self.assertTrue(target.ready_pid_windows("target_ready pid=42 a=101 b=102 edit_a=103 edit_b=104\n", 42))
        self.assertFalse(target.ready_pid_windows("target_ready pid=42 a=101 b=102 edit_a=101 edit_b=104\n", 42))

    def test_observer_ids_reject_non_numeric_duplicates_and_scalar(self):
        self.assertEqual({101, 102}, target.observed_ids("[101, 102]"))
        for output in ("2", "[true, 102]", "[101, 101]", "[0]", "[-1]", "[\"title\"]", "invalid"):
            with self.subTest(output=output), self.assertRaisesRegex(ValueError, "Window observer failed"):
                target.observed_ids(output)

    def test_control_requires_all_native_callbacks_in_order(self):
        header = "target_ready pid=42 a=101 b=102"
        lines = [header] + target.CONTROL_RECEIPTS
        self.assertTrue(target.control_completed("\n".join(lines), 42))
        for i in range(1, 4):
            self.assertFalse(target.control_completed("\n".join(lines[:i] + lines[i + 1:]), 42))
        self.assertFalse(target.control_completed("\n".join(lines + [lines[-1]]), 42))
        self.assertFalse(target.control_completed("\n".join(lines).replace("ok=1", "ok=0"), 42))
        self.assertFalse(target.control_completed("\n".join(lines), 41))

    def test_extra_output_or_missing_window_cannot_be_ready(self):
        self.assertFalse(target.ready_pid_windows("target_ready pid=42 a=101\n", 42))
        self.assertFalse(target.ready_pid_windows("target_ready pid=42 a=101 b=102\nuntrusted payload", 42))
        self.assertFalse(target.ready_pid_windows("target_ready pid=42 a=101 b=102 c=103", 42))


class NativeTargetSmokeTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.base = Path(self.temp.name)
        self.entry = self.base / "bin/run/SmartSnippets.Target018.app/Contents/MacOS/SmartSnippets.Target018"
        self.entry.parent.mkdir(parents=True)
        self.entry.write_bytes(b"native fixture")
        self.info = self.entry.parents[1] / "Info.plist"
        self.info.write_bytes(plistlib.dumps({"CFBundleIdentifier": "org.smartsnippets.feasibility018.target", "CFBundleExecutable": self.entry.name}))
        self.private = self.base / "private"
        self.private.mkdir()
        self.args = ["run_smoke", "--executable", str(self.entry), "--sha256", hashlib.sha256(self.entry.read_bytes()).hexdigest(), "--authorized-screen-slot"]
        self.process = Mock(pid=42)
        self.process.poll.return_value = None

    def run_main(self, observer="[101,102]", signature=0, launch=True, windows=False, control=False, closed_ids="[102]"):
        def start(*args, **kwargs):
            kwargs["stdout"].write(b"target_ready pid=42 a=101 b=102\n")
            kwargs["stdout"].flush()
            def command(data):
                self.assertEqual(b"empty_a\nempty_b\nclose_a\n", data)
                kwargs["stdout"].write(("\n".join(target.CONTROL_RECEIPTS) + "\n").encode())
                kwargs["stdout"].flush()
            self.process.stdin.write.side_effect = command
            return self.process
        calls = ([] if windows else [subprocess.CompletedProcess([], signature, b"", b"")]) + [subprocess.CompletedProcess([], 0, observer, "")]
        if control:
            self.args.append("--empty-close")
            calls.append(subprocess.CompletedProcess([], 0, closed_ids, ""))
        with patch.object(target, "BASE", self.base / "macos" if windows else self.base), patch.object(target.sys, "platform", "win32" if windows else "darwin"), \
             patch.object(target.subprocess, "CREATE_NO_WINDOW", 0x08000000, create=True), \
             patch.object(sys, "argv", self.args), patch.object(target, "inspect_executable", return_value=[]), \
             patch.object(target, "private_directory", return_value=self.private), \
             patch.object(target.subprocess, "run", side_effect=calls), \
             patch.object(target.subprocess, "Popen", side_effect=start) as popen:
            try:
                return target.main()
            finally:
                self.assertEqual(int(launch), popen.call_count)

    def windows_image(self, subsystem):
        self.entry = self.base / "windows/bin/target-run/SmartSnippets.Target018.exe"
        self.entry.parent.mkdir(parents=True)
        data = bytearray(256)
        data[0x3c:0x40] = (64).to_bytes(4, "little")
        data[64 + 24 + 68:64 + 24 + 70] = subsystem.to_bytes(2, "little")
        self.entry.write_bytes(data)
        self.args[2] = str(self.entry)
        self.args[4] = hashlib.sha256(data).hexdigest()

    def test_windows_gui_head_observation_and_cleanup(self):
        self.windows_image(2)
        self.assertEqual(0, self.run_main(windows=True))
        self.process.terminate.assert_called_once()

    def test_wrong_window_id_set_cannot_pass_two_window_count(self):
        with patch.object(target.time, "monotonic", side_effect=[0, 1, 20]):
            self.assertEqual(1, self.run_main(observer="[103,104]"))
        self.process.terminate.assert_called_once()

    def test_wrong_same_pid_window_closed_cannot_pass_callback_receipts(self):
        with patch.object(target.time, "monotonic", side_effect=[0, 1, 2, 20]):
            self.assertEqual(1, self.run_main(control=True, closed_ids="[101]"))
        self.process.terminate.assert_called_once()

    def test_windows_native_control_sequence_and_owned_cleanup(self):
        self.windows_image(2)
        self.assertEqual(0, self.run_main(windows=True, control=True))
        self.process.stdin.write.assert_called_once_with(b"empty_a\nempty_b\nclose_a\n")
        self.process.terminate.assert_called_once()

    def test_mac_native_control_sequence_and_owned_cleanup(self):
        self.assertEqual(0, self.run_main(control=True))
        self.process.stdin.write.assert_called_once()
        self.process.terminate.assert_called_once()

    def test_windows_console_head_refused_before_launch(self):
        self.windows_image(3)
        with self.assertRaisesRegex(ValueError, "GUI subsystem"):
            self.run_main(windows=True, launch=False)

    def test_two_windows_observed_and_owned_process_terminated(self):
        self.assertEqual(0, self.run_main())
        self.process.terminate.assert_called_once()
        self.process.kill.assert_not_called()

    def test_slow_termination_escalates_to_owned_kill(self):
        self.process.wait.side_effect = [subprocess.TimeoutExpired("owned", 3), 0]
        self.assertEqual(0, self.run_main())
        self.process.kill.assert_called_once()

    def test_bad_observer_fails_closed_and_stops_owned_process(self):
        with self.assertRaisesRegex(ValueError, "Window observer failed"):
            self.run_main(observer="not-numeric")
        self.process.terminate.assert_called_once()

    def test_no_authorization_never_launches(self):
        self.args.remove("--authorized-screen-slot")
        with self.assertRaisesRegex(ValueError, "Authorized desktop slot"):
            self.run_main(launch=False)

    def test_bad_hash_bundle_or_signature_never_launches(self):
        original = self.info.read_bytes()
        for failure in ("hash", "bundle", "signature"):
            with self.subTest(failure=failure):
                self.info.write_bytes(original)
                self.entry.write_bytes(b"tampered" if failure == "hash" else b"native fixture")
                if failure == "bundle":
                    self.info.write_bytes(plistlib.dumps({"CFBundleIdentifier": "wrong"}))
                with self.assertRaises(ValueError):
                    self.run_main(signature=1 if failure == "signature" else 0, launch=False)


if __name__ == "__main__":
    unittest.main()
