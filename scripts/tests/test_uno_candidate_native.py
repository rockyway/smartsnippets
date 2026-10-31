"""Pure bootstrap/build/protocol tests: never start a native process."""
import importlib.util
import io
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
        with patch.object(native.sys, 'stderr', io.StringIO()), self.assertRaises(SystemExit):
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

    def test_wrong_directory_is_rejected_before_any_artifact_bytes(self):
        for basename in (native.ENTRY, 'SmartSnippets.Target018.exe', 'SmartSnippets.UnoCandidate.Guardian.exe'):
            with patch.object(native.helpers, 'owned_bytes') as read:
                with self.assertRaisesRegex(ValueError, 'provenance'):
                    native.validate_image(Path('untrusted') / basename, 'a' * 64, basename)
                read.assert_not_called()

    def test_exact_ordered_receipts_and_private_log_privacy_are_required(self):
        private = Path(tempfile.gettempdir()) / 'owned-parent-directory' / 'owned-child-directory'
        expected = b'native_result=regular_restored'
        good = b'native_phase=guardian_ready\nnative_phase=published\n' + expected + b'\n'
        log = b'fixed INFO candidate_started\nfixed INFO candidate_shutdown_complete\n'
        with patch.object(native.helpers, 'owned_bytes', return_value=log):
            native.verify_result(private, [good, b''], expected)
            for output in (expected + b'\n', b'foreign\n' + good, good + b'extra\n', good.replace(b'published', b'wrong')):
                with self.assertRaisesRegex(ValueError, 'native_result_receipt'):
                    native.verify_result(private, [output, b''], expected)
            with self.assertRaisesRegex(ValueError, 'native_result_receipt'):
                native.verify_result(private, [good, b'warning'], expected)
        for invalid in (b'not ready', log + b'fixed ERROR candidate_cleanup_failed\n'):
            with patch.object(native.helpers, 'owned_bytes', return_value=invalid):
                with self.assertRaisesRegex(ValueError, 'native_host_readiness_cleanup'):
                    native.verify_result(private, [good, b''], expected)
        with patch.object(native.helpers, 'owned_bytes', return_value=log + b'second line'):
            with self.assertRaisesRegex(ValueError, 'native_privacy'):
                native.verify_result(private, [good, b''], expected)

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
