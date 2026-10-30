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

    def run_main(self, observer="2", signature=0, launch=True):
        def start(*args, **kwargs):
            kwargs["stdout"].write(b"target_ready pid=42 a=101 b=102\n")
            kwargs["stdout"].flush()
            return self.process
        calls = [subprocess.CompletedProcess([], signature, b"", b""), subprocess.CompletedProcess([], 0, observer, "")]
        with patch.object(target, "BASE", self.base), patch.object(target.sys, "platform", "darwin"), \
             patch.object(sys, "argv", self.args), patch.object(target, "inspect_executable", return_value=[]), \
             patch.object(target, "private_directory", return_value=self.private), \
             patch.object(target.subprocess, "run", side_effect=calls), \
             patch.object(target.subprocess, "Popen", side_effect=start) as popen:
            try:
                return target.main()
            finally:
                self.assertEqual(int(launch), popen.call_count)

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
        with self.assertRaisesRegex(ValueError, "Authorized Mac slot"):
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
