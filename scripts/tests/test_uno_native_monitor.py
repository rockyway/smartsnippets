"""Execute the real monitor with filesystem receipts and inert desktop/process stubs."""
import json
from pathlib import Path
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import patch
from test_uno_candidate_native import native


class MonitorTests(unittest.TestCase):
    def run_monitor(self, scene, samples, token=None, exit_code=0, oversized=False, observation_error=False, expire=False):
        with tempfile.TemporaryDirectory() as directory:
            private = Path(directory)
            (private / 'positive-surfaces.txt').write_bytes(b'10 20 30 40')
            (private / 'positive-identities.txt').write_bytes(b'100 200 300')
            output = private / 'stdout.log'
            output.write_bytes(b'x' * (65537 if oversized else 80))
            class Process:
                returncode = None
                index = -1
                def poll(self):
                    self.index += 1
                    if self.index >= len(samples):
                        self.returncode = exit_code
                    return self.returncode
                def kill(self):
                    raise AssertionError('no termination')
            process = Process()
            class User:
                def GetForegroundWindow(self):
                    window, grant, closed, pid = samples[process.index]
                    if grant:
                        (private / 'positive-foreground-granted').write_bytes(b'granted\n')
                    if closed:
                        (private / 'positive-foreground-closed').write_bytes(b'closed\n')
                    return window
            class Desktop:
                user = User()
                def validate_input_desktop(self):
                    if observation_error:
                        raise ValueError('never retain exception payload')
            with patch.object(native, 'foreground_pid', side_effect=lambda *_: samples[process.index][3]), patch('builtins.print'):
                call = lambda: native.monitor(process, Desktop(), 1, [output], clock=lambda: 22 if expire and process.index >= 2 else 1,
                    sleep=lambda _: None, positive_private=private, evidence_private=private, scene=scene)
                if token:
                    with self.assertRaisesRegex(ValueError, '^native_scene_failed$'):
                        call()
                    record = native.validate_monitor_failure(private)
                    self.assertEqual(token, record['predicate'])
                    self.assertEqual(exit_code, record['exitCode'])
                    self.assertGreaterEqual(record['sampleOrder'], 1)
                    self.assertEqual({'predicate', 'exitCode', 'sampleOrder', 'relation'}, set(record))
                else:
                    call()
                    self.assertFalse((private / 'native-monitor-failure.json').exists())
            self.assertEqual(len(samples), process.index)

    def test_posted_transitions_all_share_monitor(self):
        for scene in native.POSTED_SCENES:
            with self.subTest(scene=scene):
                self.run_monitor(scene, [(1, False, False, 999), (10, False, False, 100),
                    (30, True, False, 200), (10, True, False, 100), (10, True, True, 100)])
        self.run_monitor('copy', [(1, False, False, 999), (10, False, False, 100)])

    def test_exact_failures_for_all_copy_and_context_scenes(self):
        for scene in ('posted-row', *native.COPY_SCENES, 'context-insert'):
            for last, token in [((99, False, False, 999), 'foreground_foreign'),
                                ((30, False, False, 200), 'foreground_unexpected_candidate'),
                                ((30, True, False, 999), 'foreground_foreign'),
                                ((40, True, False, 200), 'foreground_unexpected_candidate'),
                                ((20, False, False, 100), 'foreground_foreign')]:
                with self.subTest(scene=scene, last=last):
                    self.run_monitor(scene, [(10, False, False, 100), last], token)
            self.run_monitor(scene, [(10, False, False, 100)], 'output_oversized', oversized=True)
            self.run_monitor(scene, [(10, False, False, 100)], 'process_exit', exit_code=7)
            self.run_monitor(scene, [(10, False, False, 100)], 'observation_exception', observation_error=True)

    def test_runner_routes_all_copy_and_context_scenes_through_strict_monitor(self):
        for scene in ('posted-row', *native.COPY_SCENES, 'context-insert'):
            with self.subTest(scene=scene), tempfile.TemporaryDirectory() as directory:
                root = Path(directory)
                state = SimpleNamespace(index=-1)
                class Process:
                    returncode = None
                    def __init__(self, *args, **kwargs):
                        private = kwargs['cwd']
                        (private / 'positive-surfaces.txt').write_bytes(b'10 20 30 40')
                        (private / 'positive-identities.txt').write_bytes(b'100 200 300')
                    def poll(self):
                        state.index += 1
                        if state.index >= 2:
                            self.returncode = 0
                        return self.returncode
                class Desktop:
                    user = SimpleNamespace(GetForegroundWindow=lambda: 1 if state.index < 0 else 10 if state.index == 0 else 30,
                                           GetClipboardSequenceNumber=lambda: 1)
                    def enable_dpi(self): pass
                    def validate_input_desktop(self): pass
                    def close(self): pass
                ui = SimpleNamespace(private_directory=lambda: root, visible_windows=None)
                with patch.object(native, 'require_authorization'), patch.object(native, 'require_positive_authorization'), \
                     patch.object(native, 'validate_build', return_value=Path('fixture')), \
                     patch.object(native, 'validate_image', return_value=Path('support')), \
                     patch.object(native.helpers.startup, 'repo_module', return_value=ui), \
                     patch.object(native.helpers.startup, 'Desktop', return_value=Desktop()), \
                     patch.object(native.subprocess, 'Popen', Process), \
                     patch.object(native, 'foreground_pid', side_effect=lambda *_: 100 if state.index == 0 else 200), \
                     patch.object(native, 'verify_result'), patch.object(native, 'verify_positive_boundary', return_value={}), \
                     patch.object(native, 'source_fingerprint', return_value='a' * 64), patch('builtins.print'):
                    with self.assertRaisesRegex(ValueError, '^native_scene_failed$'):
                        native.main(['--scene', scene, '--executable', 'fixture', '--sha256', 'a' * 64,
                                     '--target', 'target', '--target-sha256', 'a' * 64,
                                     '--guardian', 'guardian', '--guardian-sha256', 'a' * 64])
                record = native.validate_monitor_failure(next(root.iterdir()))
                self.assertEqual('foreground_unexpected_candidate', record['predicate'])

    def test_closed_and_returned_windows_never_reopen(self):
        for samples in [[(10, False, False, 100), (30, True, True, 200)],
                        [(10, False, False, 100), (30, True, False, 200),
                         (10, True, False, 100), (30, True, False, 200)]]:
            self.run_monitor('posted-row', samples, 'foreground_unexpected_candidate')

    def test_deadline_missing_return_and_main_copy_cannot_take_foreground(self):
        self.run_monitor('posted-row', [(10, False, False, 100), (30, True, False, 200),
                         (30, True, False, 200)], 'foreground_unexpected_candidate', expire=True)
        self.run_monitor('posted-row', [(10, False, False, 100), (30, True, False, 200)],
                         'foreground_unexpected_candidate')
        self.run_monitor('copy', [(10, False, False, 100), (30, True, False, 200)],
                         'foreground_unexpected_candidate')
        self.run_monitor('posted-row', [], 'output_oversized', oversized=True)
        # First predicate is retained even if a later process failure also occurs.
        self.run_monitor('posted-row', [(99, False, False, 999)], 'foreground_foreign', exit_code=7)

    def test_failure_record_rejects_schema_types_duplicates_and_payloads(self):
        good = dict(predicate='foreground_foreign', exitCode=0, sampleOrder=1, relation='foreign')
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            path = root / 'native-monitor-failure.json'
            for key, value in [('predicate', 'raw text'), ('predicate', []), ('exitCode', True),
                               ('exitCode', 2**32), ('sampleOrder', False), ('sampleOrder', 0),
                               ('relation', 'window title'), ('relation', []), ('extra', 0)]:
                path.write_text(json.dumps({**good, key: value}), encoding='ascii')
                with self.subTest(key=key, value=value), self.assertRaises(ValueError):
                    native.validate_monitor_failure(root)
            path.write_text(json.dumps(good)[:-1] + ',"exitCode":0}', encoding='ascii')
            with self.assertRaises(ValueError):
                native.validate_monitor_failure(root)
