import hashlib
import importlib.util
import io
from pathlib import Path
import signal
import struct
import subprocess
import sys
import tempfile
import threading
from unittest.mock import Mock, patch
import unittest

BASE = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('identity_harness_runner', BASE / 'run_identity_harness.py')
runner = importlib.util.module_from_spec(spec)
spec.loader.exec_module(runner)
VALID = b'private_evidence_token=' + b'a' * 32 + b'\nowned_identity=OBSERVED cleanup=STOPPED input=NONE clipboard=UNTOUCHED\n'


class FakeProcess:
    def __init__(self, output=VALID, error=b''):
        self.pid = 424242
        self.stdout = io.BytesIO(output)
        self.stderr = io.BytesIO(error)
        self.stdin = Mock()
        self.returncode = None
        self.events = []

    def poll(self):
        raise AssertionError('Do not reap the pinned Mac group leader before owned cleanup')

    def wait(self, timeout):
        self.events.append('reap')
        self.returncode = 0
        return 0


class ArtifactTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.base = Path(self.temp.name).resolve()
        self.entry = self.base / 'identity/bin/publish-win-x64/SmartSnippets.TargetIdentity018.exe'
        self.entry.parent.mkdir(parents=True)
        data = bytearray(512)
        data[:2] = b'MZ'
        struct.pack_into('<I', data, 60, 64)
        data[64:68] = b'PE\0\0'
        struct.pack_into('<H', data, 68, 0x8664)
        struct.pack_into('<H', data, 84, 240)
        struct.pack_into('<H', data, 88, 0x20b)
        struct.pack_into('<I', data, 64 + 24 + 108, 16)
        self.entry.write_bytes(data)
        self.digest = hashlib.sha256(data).hexdigest()

    def test_valid_contained_native_harness_and_spoof_refusals(self):
        with patch.object(runner, 'BASE', self.base):
            self.assertEqual(self.entry, runner.validate_harness(self.entry, self.digest, True))
            for digest in ('invalid', '0' * 64):
                with self.assertRaises(ValueError):
                    runner.validate_harness(self.entry, digest, True)
            outside = self.base / self.entry.name
            outside.write_bytes(self.entry.read_bytes())
            with self.assertRaises(ValueError):
                runner.validate_harness(outside, self.digest, True)
            data = bytearray(self.entry.read_bytes())
            data[68:70] = b'\x64\xaa'
            self.entry.write_bytes(data)
            with self.assertRaises(ValueError):
                runner.validate_harness(self.entry, hashlib.sha256(data).hexdigest(), True)

    def test_absent_authorization_and_unknown_pid_option_never_validate_or_launch(self):
        arguments = ['runner', '--harness', str(self.entry), '--harness-sha256', self.digest, '--executable', 'fixture', '--sha256', self.digest]
        with patch.object(sys, 'argv', arguments), patch.object(runner, 'validate_harness') as validate, patch.object(runner, 'run') as run:
            with self.assertRaises(ValueError):
                runner.main()
            validate.assert_not_called()
            run.assert_not_called()
        with patch.object(sys, 'argv', arguments + ['--authorized-screen-slot', '--pid', '424242']), patch.object(runner, 'run') as run:
            with self.assertRaises(ValueError):
                runner.main()
            run.assert_not_called()


    def test_callback_requires_explicit_authorization_and_excludes_identity_mode(self):
        basic = ['runner', '--harness', 'harness', '--harness-sha256', 'a' * 64,
                 '--executable', 'target', '--sha256', 'a' * 64, '--authorized-screen-slot']
        for extra in (['--inprocess-native-callback'], ['--authorized-native-hotkey'],
                      ['--inprocess-native-callback', '--authorized-native-hotkey', '--identity-only']):
            with patch.object(sys, 'argv', basic + extra), patch.object(runner, 'validate_harness') as validate, patch.object(runner, 'run') as run:
                with self.assertRaises(ValueError):
                    runner.main()
                validate.assert_not_called()
                run.assert_not_called()


class WatchdogTests(unittest.TestCase):
    def invoke(self, process, windows=False, native_callback=False):
        job = Mock()
        process.stdin.write.side_effect = lambda _: process.events.append('permit')
        def assign(child):
            self.assertIs(child, process)
            process.events.append('job')
            return job
        def kill_group(pid, sig):
            self.assertEqual((process.pid, signal.SIGKILL), (pid, sig))
            process.events.append('kill_group')
        with patch.object(runner.subprocess, 'Popen', return_value=process) as start, \
             patch.object(runner.subprocess, 'CREATE_NO_WINDOW', 0x08000000, create=True), \
             patch.object(runner, 'WindowsJob', side_effect=assign), \
             patch.object(runner, 'await_natural_exit', side_effect=lambda p, w: p.events.append('exit_observed')), \
             patch.object(runner.os, 'killpg', side_effect=kill_group, create=True), \
             patch.object(runner.signal, 'SIGKILL', 9, create=True):
            try:
                return runner.run(Path('/owned/harness'), Path('/owned/target'), 'a' * 64, windows, native_callback)
            finally:
                options = start.call_args.kwargs
                self.assertEqual(not windows, options['start_new_session'])
                self.assertNotIn('shell', options)
                expected = ['--authorized-screen-slot', '--watchdog-stdin', '--executable', str(Path('/owned/target')), '--sha256', 'a' * 64]
                if native_callback:
                    expected += ['--inprocess-native-callback', '--authorized-native-hotkey']
                self.assertEqual(expected, start.call_args.args[0][1:])
                if windows:
                    job.close.assert_called_once()
                else:
                    self.assertLess(process.events.index('kill_group'), process.events.index('reap'))

    def test_callback_receipt_is_strict_and_never_accepted_in_default_mode(self):
        callback = b'private_evidence_token=' + b'a' * 32 + b'\n' + runner.CALLBACK_RESULT.encode('ascii') + b'\n'
        for windows in (False, True):
            self.assertEqual(0, self.invoke(FakeProcess(callback), windows, native_callback=True))
            for output in (VALID, callback.replace(b'NOT_RUN', b'PASS'), callback + b'extra\n'):
                with self.assertRaises(ValueError):
                    self.invoke(FakeProcess(output), windows, native_callback=True)
        with self.assertRaises(ValueError):
            self.invoke(FakeProcess(callback))

    def test_valid_owned_mac_group_lifecycle_never_polls_or_attaches(self):
        process = FakeProcess()
        self.assertEqual(0, self.invoke(process))
        self.assertEqual(['permit', 'exit_observed', 'kill_group', 'reap'], process.events)

    def test_windows_job_is_assigned_before_launch_permit(self):
        process = FakeProcess()
        self.assertEqual(0, self.invoke(process, windows=True))
        self.assertEqual(['job', 'permit', 'exit_observed', 'reap'], process.events)

    def test_spoofed_receipts_extra_output_and_stderr_fail_and_cleanup(self):
        for output, error in ((VALID.replace(b'OBSERVED', b'PASS'), b''), (VALID + b'extra\n', b''),
                              (b'x' * 257, b''), (VALID, b'private error'), (b'', b'')):
            process = FakeProcess(output, error)
            with self.subTest(output_length=len(output), stderr=bool(error)), self.assertRaises(ValueError):
                self.invoke(process)
            self.assertIn('reap', process.events)

    def test_broken_pipe_still_kills_and_reaps_only_owned_group(self):
        process = FakeProcess()
        process.stdin.flush.side_effect = BrokenPipeError()
        with self.assertRaises(BrokenPipeError):
            self.invoke(process)
        self.assertIn('reap', process.events)

    def test_interrupt_still_cleans_up_created_group(self):
        process = FakeProcess()
        process.stdin.flush.side_effect = KeyboardInterrupt()
        with self.assertRaises(KeyboardInterrupt):
            self.invoke(process)
        self.assertIn('reap', process.events)

    def test_timeout_kills_and_reaps_while_native_harness_is_hung(self):
        class Blocked:
            def __init__(self):
                self.closed = threading.Event()
            def read(self, count):
                self.closed.wait(5)
                return b''
            def close(self):
                self.closed.set()
        process = FakeProcess()
        process.stdout = Blocked()
        with patch.object(runner.time, 'monotonic', side_effect=[0, 66]), self.assertRaises(ValueError):
            self.invoke(process)
        self.assertIn('reap', process.events)


class NativeExitTests(unittest.TestCase):
    def test_windows_waits_before_job_teardown(self):
        process = FakeProcess()
        runner.await_natural_exit(process, True)
        self.assertEqual(['reap'], process.events)

    def test_darwin_zombie_group_permission_error_is_verified_before_reaping(self):
        for only_zombies in (True, False):
            process = FakeProcess()
            with patch.object(runner.os, 'killpg', side_effect=PermissionError(), create=True), \
                 patch.object(runner.signal, 'SIGKILL', 9, create=True), \
                 patch.object(runner, 'zombie_only_group', return_value=only_zombies) as verify:
                if only_zombies:
                    runner._stop_owned(process, None, False)
                    self.assertEqual(['reap'], process.events)
                else:
                    with self.assertRaises(PermissionError):
                        runner._stop_owned(process, None, False)
                    self.assertEqual([], process.events)
                verify.assert_called_once_with(process.pid)

    def test_zombie_group_query_rejects_live_foreign_missing_and_malformed_rows(self):
        for output, expected in (('42 42 Zs\n', True), ('42 42 Zs\n43 42 Z\n', True),
                                 ('42 42 Ss\n', False), ('42 42 Z\n43 42 S\n', False),
                                 ('42 99 Z\n', False), ('43 42 Z\n', False),
                                 ('42\n', False), ('', False)):
            with patch.object(runner.subprocess, 'run', return_value=Mock(returncode=0, stderr='', stdout=output)):
                self.assertEqual(expected, runner.zombie_only_group(42))

    def test_mac_observes_exit_without_reaping_or_polling(self):
        from types import SimpleNamespace
        process = FakeProcess()
        queue = Mock()
        queue.__enter__ = Mock(return_value=queue)
        queue.__exit__ = Mock(return_value=False)
        event = SimpleNamespace(ident=process.pid, flags=0, fflags=4)
        queue.control.return_value = [event]
        native = SimpleNamespace(kqueue=lambda: queue, kevent=Mock(),
                                 KQ_FILTER_PROC=-5, KQ_EV_ADD=1, KQ_EV_ONESHOT=16,
                                 KQ_NOTE_EXIT=4, KQ_EV_ERROR=0x4000)
        with patch.dict(sys.modules, {'select': native}):
            runner.await_natural_exit(process, False)
            self.assertEqual([], process.events)
            queue.control.assert_called_once_with([native.kevent.return_value], 1, 5)
            for result in ([], [SimpleNamespace(ident=process.pid, flags=0x4000, fflags=4, data=1)],
                           [SimpleNamespace(ident=process.pid + 1, flags=0, fflags=4)],
                           [SimpleNamespace(ident=process.pid, flags=0, fflags=0)]):
                queue.control.return_value = result
                with self.assertRaises(ValueError):
                    runner.await_natural_exit(process, False)
                self.assertEqual([], process.events)
            queue.control.return_value = [SimpleNamespace(ident=process.pid, flags=0x4000, fflags=4, data=runner.errno.ESRCH)]
            with patch.object(runner, 'zombie_only_group', return_value=True):
                runner.await_natural_exit(process, False)
                self.assertEqual([], process.events)
            with patch.object(runner, 'zombie_only_group', return_value=False):
                with self.assertRaises(ValueError):
                    runner.await_natural_exit(process, False)


if __name__ == '__main__':
    unittest.main()
