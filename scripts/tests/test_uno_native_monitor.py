"""Real monitor/runner, real private receipts/SQLite, inert desktop/process stubs."""
from contextlib import closing
import json
from pathlib import Path
import sqlite3
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import patch
from test_uno_candidate_native import native

A = (10, False, False, 100)
POPUP = (30, True, False, 200)
BACK = (10, True, False, 100)
CLOSED_A = (10, True, True, 100)
INITIAL = (1, True, True, 999)
ZERO_END = (0, True, True, 0)


def publish(private, sample):
    if sample[1]:
        (private / 'positive-foreground-granted').write_bytes(b'granted\n')
    if sample[2]:
        (private / 'positive-foreground-closed').write_bytes(b'closed\n')


class MonitorTests(unittest.TestCase):
    def run_monitor(self, scene, samples, token=None, expected=None, exit_code=0,
                    oversized=False, observation_error=False, expire=False):
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
                    sample = samples[process.index]
                    publish(private, sample)
                    return sample[0]
            class Desktop:
                user = User()
                def validate_input_desktop(self):
                    if observation_error:
                        raise ValueError('never retain exception payload')
            with patch.object(native, 'foreground_pid', side_effect=lambda *_: samples[process.index][3]), patch('builtins.print'):
                call = lambda: native.monitor(process, Desktop(), 1, [output],
                    clock=lambda: 22 if expire and process.index >= 2 else 1, sleep=lambda _: None,
                    positive_private=private, evidence_private=private, scene=scene)
                if token:
                    with self.assertRaisesRegex(ValueError, '^native_scene_failed$'):
                        call()
                    self.assertIsNotNone(expected, 'every refusal must pin order, relation and phase')
                    order, relation, closed = expected
                    self.assertEqual(dict(predicate=token, exitCode=exit_code, sampleOrder=order,
                                          relation=relation, closed=closed, windowFacts=native.empty_window_facts()), native.validate_monitor_failure(private))
                else:
                    call()
                    self.assertFalse((private / 'native-monitor-failure.json').exists())
            self.assertEqual(len(samples), process.index)

    def test_posted_transitions_and_teardown_all_share_monitor(self):
        for scene in native.POSTED_SCENES:
            with self.subTest(scene=scene):
                self.run_monitor(scene, [(1, False, False, 999), A, POPUP,
                    (0, True, False, 0), BACK, CLOSED_A, INITIAL, ZERO_END, INITIAL])
                # Copy popup destruction may not expose a sampled A before closure.
                self.run_monitor(scene, [A, POPUP, (0, True, False, 0), ZERO_END, INITIAL])
        self.run_monitor('copy', [A, CLOSED_A, INITIAL, ZERO_END])

    def test_exact_identity_failures_for_all_copy_and_context_scenes(self):
        for scene in ('posted-row', *native.COPY_SCENES, 'context-insert'):
            for last, token, relation in [((99, False, False, 999), 'foreground_foreign', 'foreign'),
                    ((30, False, False, 200), 'foreground_unexpected_candidate', 'candidate'),
                    ((30, True, False, 999), 'foreground_foreign', 'foreign'),
                    ((30, True, False, 100), 'foreground_foreign', 'same_pid_other'),
                    ((30, True, False, 300), 'foreground_foreign', 'guardian'),
                    ((10, True, False, 200), 'foreground_unexpected_candidate', 'candidate'),
                    ((10, True, False, 300), 'foreground_foreign', 'guardian'),
                    ((40, True, False, 200), 'foreground_unexpected_candidate', 'candidate'),
                    ((20, True, False, 100), 'foreground_foreign', 'owned_b')]:
                with self.subTest(scene=scene, last=last):
                    self.run_monitor(scene, [A, last], token, (2, relation, False))
            self.run_monitor(scene, [A], 'output_oversized', (1, 'owned_a', False), oversized=True)
            self.run_monitor(scene, [A], 'process_exit', (2, 'owned_a', False), exit_code=7)
            self.run_monitor(scene, [A], 'observation_exception', (1, 'none', False), observation_error=True)

    def test_closed_end_state_has_no_foreign_or_owned_surface_exception(self):
        for window, pid, token, relation in [(99, 999, 'foreground_foreign', 'foreign'),
                (20, 100, 'foreground_foreign', 'owned_b'),
                (40, 200, 'foreground_unexpected_candidate', 'candidate'),
                (30, 200, 'foreground_unexpected_candidate', 'candidate')]:
            for prefix in ([A, POPUP, BACK, CLOSED_A], [A, POPUP, BACK, CLOSED_A, INITIAL, ZERO_END]):
                self.run_monitor('posted-row', [*prefix, (window, True, True, pid)],
                                 token, (len(prefix) + 1, relation, True))
        # A is allowed to drain before end-state entry, but cannot re-enter afterwards.
        self.run_monitor('posted-row', [A, CLOSED_A, INITIAL, CLOSED_A],
                         'foreground_foreign', (4, 'owned_a', True))
        # Closed metadata alone never admits zero before observed A.
        self.run_monitor('posted-row', [ZERO_END], 'foreground_foreign', (1, 'none', True))
        # Preserve the first failure's phase, even if closure arrives later.
        self.run_monitor('posted-row', [A, (99, False, False, 999), INITIAL],
                         'foreground_foreign', (2, 'foreign', False))

    def test_zero_is_only_a_bounded_popup_handoff_or_closed_end_state(self):
        for samples, order in [([A, (0, True, False, 0)], 2),
                               ([A, POPUP, BACK, (0, True, False, 0)], 4)]:
            self.run_monitor('posted-row', samples, 'foreground_foreign', (order, 'none', False))
        self.run_monitor('posted-row', [A, POPUP, (0, True, False, 0)],
                         'foreground_foreign', (3, 'none', False), expire=True)
        self.run_monitor('posted-row', [A, POPUP, (99, True, False, 999)],
                         'foreground_foreign', (3, 'foreign', False))

    def test_deadline_reentry_missing_return_and_immediate_exits(self):
        self.run_monitor('posted-row', [A, POPUP, POPUP], 'foreground_unexpected_candidate',
                         (3, 'candidate', False), expire=True)
        self.run_monitor('posted-row', [A, POPUP, BACK, POPUP], 'foreground_unexpected_candidate',
                         (4, 'candidate', False))
        self.run_monitor('posted-row', [A, POPUP], 'foreground_unexpected_candidate', (3, 'candidate', False))
        self.run_monitor('copy', [A, POPUP], 'foreground_unexpected_candidate', (2, 'candidate', False))
        self.run_monitor('posted-row', [], 'output_oversized', (1, 'none', False), oversized=True)
        self.run_monitor('posted-row', [], 'process_exit', (1, 'none', False), exit_code=7)
        self.run_monitor('posted-row', [(99, False, False, 999)], 'foreground_foreign',
                         (1, 'foreign', False), exit_code=7)

    def run_main(self, scene, success):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            state = SimpleNamespace(index=-1)
            samples = ([A, CLOSED_A, INITIAL, ZERO_END] if scene == 'copy' else
                       [A, POPUP, (0, True, False, 0), BACK, CLOSED_A, INITIAL, ZERO_END]) if success else [A, (30, False, False, 200)]
            class Process:
                returncode = None
                def __init__(self, *args, **kwargs):
                    state.private = kwargs['cwd']
                    (state.private / 'positive-surfaces.txt').write_bytes(b'10 20 30 40')
                    (state.private / 'positive-identities.txt').write_bytes(b'100 200 300')
                    profile = state.private / 'native-fixture-profile'
                    (profile / 'logs').mkdir(parents=True)
                    log = 'fixed INFO candidate_started\nfixed INFO candidate_shutdown_complete\n'
                    if scene == 'editor-copy':
                        log += ('2026-11-01T00:00:00.000Z ERROR store_write_failed '
                                'op=update_snippet error=SqliteException sqlite=19 hresult=0x80004005\n')
                    (profile / 'logs/app.log').write_text(log, encoding='ascii')
                    with closing(sqlite3.connect(profile / 'smartsnippets.db')) as db:
                        db.execute('CREATE TABLE snippets (usage_count INTEGER)')
                        db.execute('INSERT INTO snippets VALUES (?)', (int(scene in native.INSERT_SCENES),))
                        db.commit()
                    kwargs['stdout'].write(('native_phase=guardian_ready\nnative_phase=awaiting_owned_a\n'
                                            f'native_result={scene}_settled\n').encode())
                    kwargs['stdout'].flush()
                def poll(self):
                    state.index += 1
                    if state.index >= len(samples):
                        self.returncode = 0
                    return self.returncode
            def foreground():
                if state.index < 0:
                    return 1
                publish(state.private, samples[state.index])
                return samples[state.index][0]
            class Desktop:
                user = SimpleNamespace(GetForegroundWindow=foreground,
                                       GetClipboardSequenceNumber=lambda: 1 if state.index < 0 else 2)
                def enable_dpi(self): pass
                def validate_input_desktop(self): pass
                def close(self): pass
            ui = SimpleNamespace(private_directory=lambda: root, visible_windows=None)
            repo_module = native.helpers.startup.repo_module
            monitor = native.monitor
            with patch.object(native, 'require_authorization'), patch.object(native, 'require_positive_authorization'), \
                 patch.object(native, 'validate_build', return_value=Path('fixture')), \
                 patch.object(native, 'validate_image', return_value=Path('support')), \
                 patch.object(native.helpers.startup, 'repo_module', side_effect=lambda name, path:
                              ui if name == 'native_private_directory' else repo_module(name, path)), \
                 patch.object(native.helpers.startup, 'Desktop', return_value=Desktop()), \
                 patch.object(native.subprocess, 'Popen', Process), \
                 patch.object(native, 'foreground_pid', side_effect=lambda *_: samples[state.index][3]), \
                 patch.object(native, 'monitor', side_effect=lambda *a, **kw: monitor(*a, **kw, sleep=lambda _: None)), \
                 patch.object(native, 'verify_result', wraps=native.verify_result) as verify, \
                 patch.object(native, 'verify_positive_boundary', wraps=native.verify_positive_boundary) as boundary, \
                 patch.object(native, 'source_fingerprint', return_value='a' * 64), patch('builtins.print') as output:
                call = lambda: native.main(['--scene', scene, '--executable', 'fixture', '--sha256', 'a' * 64,
                                           '--target', 'target', '--target-sha256', 'a' * 64,
                                           '--guardian', 'guardian', '--guardian-sha256', 'a' * 64])
                if success:
                    call()
                    verify.assert_called_once()
                    boundary.assert_called_once_with(state.private, scene, 1, 2)
                    receipt = json.loads(output.call_args.args[0])
                    self.assertEqual('positive_scene_settled', receipt['result'])
                    self.assertIs(True, receipt['usageVerified'])
                    self.assertIs(True, receipt['clipboardEpochChanged'])
                    self.assertFalse((state.private / 'native-monitor-failure.json').exists())
                else:
                    with self.assertRaisesRegex(ValueError, '^native_scene_failed$'):
                        call()
                    verify.assert_not_called()
                    boundary.assert_not_called()
                    self.assertEqual(dict(predicate='foreground_unexpected_candidate', exitCode=0,
                                          sampleOrder=2, relation='candidate', closed=False, windowFacts=native.empty_window_facts()),
                                     native.validate_monitor_failure(state.private))

    def test_real_main_granted_success_reaches_real_receipt_and_boundary_verifiers_per_scene(self):
        for scene in (*native.POSTED_SCENES, 'copy'):
            with self.subTest(scene=scene):
                self.run_main(scene, True)

    def test_real_main_no_grant_failure_short_circuits_verifiers_per_scene(self):
        for scene in (*native.POSTED_SCENES, 'copy'):
            with self.subTest(scene=scene):
                self.run_main(scene, False)

    def test_scene_is_required_and_unknown_or_none_cannot_select_legacy(self):
        with self.assertRaises(TypeError):
            native.monitor(None, None, 1, [])
        for scene in (None, 'legacy', 'typo'):
            with self.assertRaisesRegex(ValueError, '^native_monitor_scene$'):
                native.monitor(None, None, 1, [], scene=scene)

    def test_failure_record_rejects_schema_types_duplicates_and_payloads(self):
        good = dict(predicate='foreground_foreign', exitCode=0, sampleOrder=1, relation='foreign', closed=False,
                    windowFacts=native.empty_window_facts())
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            path = root / 'native-monitor-failure.json'
            invalid = [{key: value for key, value in good.items() if key != 'closed'}]
            for key, value in [('predicate', 'raw text'), ('predicate', []), ('exitCode', True),
                               ('exitCode', 2**32), ('sampleOrder', False), ('sampleOrder', 0),
                               ('relation', 'window title'), ('relation', []), ('closed', 1),
                               ('closed', 'true'), ('closed', None), ('extra', 0)]:
                invalid.append({**good, key: value})
            for record in invalid:
                path.write_text(json.dumps(record), encoding='ascii')
                with self.subTest(record=record), self.assertRaises(ValueError):
                    native.validate_monitor_failure(root)
            path.write_text(json.dumps(good)[:-1] + ',"exitCode":0}', encoding='ascii')
            with self.assertRaises(ValueError):
                native.validate_monitor_failure(root)
