"""Pure bootstrap/build/protocol tests: never start a native process."""
import importlib.util
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch

SCRIPT = Path(__file__).resolve().parents[1] / 'run_uno_candidate_native.py'
spec = importlib.util.spec_from_file_location('native_bootstrap', SCRIPT)
native = importlib.util.module_from_spec(spec)
spec.loader.exec_module(native)


class NativeBootstrapTests(unittest.TestCase):
    def test_source_gate_precedes_any_launch_or_profile_access(self):
        with patch.object(native.subprocess, 'Popen') as start, patch.object(native, 'validate_build') as validate:
            with self.assertRaisesRegex(ValueError, 'review'):
                native.main(['--scene', 'restore', '--authorized-screen-slot'])
            start.assert_not_called()
            validate.assert_not_called()

    def test_cli_fails_closed_without_native_launch(self):
        result = subprocess.run([sys.executable, '-I', '-B', str(SCRIPT), '--scene', 'abort'],
                                capture_output=True, timeout=10)
        self.assertEqual(2, result.returncode)
        self.assertEqual(b'native_harness_blocked\n', result.stderr.replace(b'\r\n', b'\n'))

    def test_fixed_scene_allowlist_rejects_positive_input(self):
        with self.assertRaises(SystemExit):
            native.main(['--scene', 'paste'])

    def test_source_fingerprint_binds_linked_sources_and_changes(self):
        first = native.source_fingerprint()
        self.assertRegex(first, '^[0-9a-f]{64}$')
        self.assertEqual(first, native.source_fingerprint())

    def test_artifact_basename_and_digest_refuse_before_read(self):
        with self.assertRaisesRegex(ValueError, 'identity'):
            native.validate_image('other.exe', '0' * 64, native.ENTRY)
        with self.assertRaisesRegex(ValueError, 'identity'):
            native.validate_image(native.ENTRY, 'bad', native.ENTRY)

    def test_monitor_never_force_cleans_pending_coordinator(self):
        class Process:
            returncode = 0
            polls = 0
            def poll(self):
                self.polls += 1
                return None if self.polls < 4 else 0
            def kill(self):
                raise AssertionError('must not kill guardian/coordinator')
        class User:
            def GetForegroundWindow(self): return 1
        class Desktop:
            user = User()
            def validate_input_desktop(self): pass
        process = Process()
        native.monitor(process, Desktop(), 1, [], sleep=lambda _: None)
        self.assertEqual(4, process.polls)

    def test_oracle_failure_is_failure_without_process_tree_cleanup(self):
        class Process:
            returncode = 0
            polls = 0
            def poll(self):
                self.polls += 1
                return None if self.polls < 3 else 0
        class User:
            def GetForegroundWindow(self): return 2
        class Desktop:
            user = User()
            def validate_input_desktop(self): pass
        with self.assertRaisesRegex(ValueError, 'failed'):
            native.monitor(Process(), Desktop(), 1, [], sleep=lambda _: None)

    def test_build_capture_does_not_require_or_grant_native_authorization(self):
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory) / 'source.sha256'
            native.main(['--capture-source', str(output)])
            self.assertEqual(native.source_fingerprint() + '\n', output.read_text())
        self.assertFalse(native.NATIVE_SCENES_SOURCE_REVIEWED)


if __name__ == '__main__':
    unittest.main()
