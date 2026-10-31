"""Pure tests: no desktop, native calls, or child processes."""
import importlib.util
from contextlib import redirect_stdout
import io
from pathlib import Path
import tempfile
import unittest
from unittest.mock import Mock, patch

SPEC = importlib.util.spec_from_file_location('startup', Path(__file__).resolve().parents[1] / 'run_uno_candidate_startup.py')
s = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(s)


class StartupTests(unittest.TestCase):
    def test_review_gate_closed(self):
        self.assertFalse(s.STARTUP_OBSERVER_REVIEWED)

    def test_worker_cli_never_claims_outer_job_receipt(self):
        for private_flag in ('--worker-private', '--worker-private=private'):
            output = io.StringIO()
            with patch.object(s, 'main', return_value=None), patch.object(s.sys, 'argv', ['observer', private_flag]), redirect_stdout(output):
                self.assertEqual(0, s.cli())
            self.assertEqual('', output.getvalue())

    def test_geometry_full_rect(self):
        self.assertTrue(s.inside((-90, 10, -10, 90), (-100, 0, 0, 100)))
        for rect in [(-101, 0, -10, 90), (-90, -1, -10, 90), (-90, 0, 1, 90), (-90, 0, -10, 101), (0, 0, 0, 1)]:
            self.assertFalse(s.inside(rect, (-100, 0, 0, 100)))

    def test_bottom_left_means_leftmost_column_then_bottom(self):
        monitors = [((0, 0, 100, 100), (0, 0, 100, 95)),
                    ((-100, 0, 0, 100), (-95, 0, 0, 100)),
                    ((-100, 100, 0, 200), (-100, 100, 0, 190)),
                    ((0, 200, 100, 300), (0, 200, 100, 290))]
        self.assertEqual((-100, 100, 0, 190), s.bottom_left_work_area(monitors))
        self.assertEqual((-100, 100, 0, 190), s.bottom_left_work_area(list(reversed(monitors))))

    def test_bottom_left_refuses_invalid_monitor_records(self):
        for monitors in ([], [((0, 0, 0, 1), (0, 0, 0, 1))], [((0, 0, 10, 10), (-1, 0, 10, 10))]):
            with self.assertRaises(ValueError):
                s.bottom_left_work_area(monitors)

    def test_profile_existing_refused_without_changes(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp) / 'SmartSnippets.UnoCandidate019'
            self.assertEqual(root, s.profile_absent(tmp))
            root.mkdir()
            marker = root / 'keep'
            marker.write_bytes(b'private')
            with self.assertRaises(ValueError):
                s.profile_absent(tmp)
            self.assertEqual(b'private', marker.read_bytes())

    def test_wrong_provenance_denied_before_read(self):
        with self.assertRaises(ValueError):
            s.validate_candidate(Path('elsewhere.exe'), 'a' * 64)

    def test_candidate_digest_and_native_inspection(self):
        with tempfile.TemporaryDirectory() as tmp, patch.object(s, 'REPO', Path(tmp)):
            entry = Path(tmp) / 'src/SmartSnippets.Uno/bin/publish-uno-candidate/token/SmartSnippets.UnoCandidate.exe'
            entry.parent.mkdir(parents=True)
            entry.write_bytes(b'x' * 64)
            inspector = Mock()
            inspector.inspect_executable.return_value = []
            digest = s.hashlib.sha256(entry.read_bytes()).hexdigest()
            with patch.object(s, 'repo_module', return_value=inspector):
                self.assertEqual(entry, s.validate_candidate(entry, digest))
                with self.assertRaises(ValueError):
                    s.validate_candidate(entry, '0' * 64)
                inspector.inspect_executable.return_value = ['managed']
                with self.assertRaises(ValueError):
                    s.validate_candidate(entry, digest)

    def test_bootstrap_eof_and_wrong_permit_never_launch(self):
        for text in [b'', b'wrong\n', b'owned_startup_ready']:
            with patch.object(s.subprocess, 'Popen') as launch:
                with self.assertRaises(ValueError):
                    s.worker(None, None, None, None, io.BytesIO(text))
                launch.assert_not_called()

    def test_valid_permit_without_job_never_launches(self):
        with patch.object(s, 'require_job_membership', side_effect=ValueError('not_owned')), \
             patch.object(s.subprocess, 'Popen') as launch:
            with self.assertRaises(ValueError):
                s.worker(None, None, None, None, io.BytesIO(s.PERMIT))
            launch.assert_not_called()

    def test_observe_owned_pid(self):
        process = Mock(pid=123)
        process.poll.return_value = None
        windows = Mock(return_value=[(0, 0, 10, 10)])
        s.observe(process, lambda: (1, 2), windows, (1, 2), (0, 0, 20, 20))
        windows.assert_called_once_with(123)

    def test_foreground_and_epoch_changes_fail_before_enumeration(self):
        for snapshot in [(2, 2), (1, 3)]:
            windows = Mock()
            with self.assertRaises(ValueError):
                s.observe(Mock(), lambda: snapshot, windows, (1, 2), (0, 0, 20, 20))
            windows.assert_not_called()

    def test_foreground_change_after_visible_enumeration_is_not_masked_by_stop(self):
        process = Mock(pid=123); process.poll.return_value = None
        snapshots = iter([(1, 2), (9, 2)])
        with self.assertRaisesRegex(ValueError, 'desktop_changed'):
            s.observe(process, lambda: next(snapshots), lambda pid: [(0, 0, 10, 10)], (1, 2), (0, 0, 20, 20))

    def desktop_fixture(self):
        desktop = s.Desktop.__new__(s.Desktop)
        desktop.visible_windows = Mock(return_value=[42])
        desktop.user, desktop.dwm = Mock(), Mock()
        def owner(hwnd, pointer):
            pointer._obj.value = 123
            return 1
        def rect(hwnd, pointer):
            value = pointer._obj
            value.left, value.top, value.right, value.bottom = -32000, -32000, -31000, -31000
            return True
        desktop.user.GetWindowThreadProcessId.side_effect = owner
        desktop.user.IsWindowVisible.return_value = True
        desktop.user.GetWindowRect.side_effect = rect
        desktop.dwm.DwmGetWindowAttribute.return_value = 0
        return desktop

    def test_warmup_hide_race_skips_stale_visible_entry(self):
        for fail in ('cloak', 'rect', 'after_rect'):
            desktop = self.desktop_fixture()
            desktop.user.IsWindowVisible.side_effect = [True, False]
            if fail == 'cloak': desktop.dwm.DwmGetWindowAttribute.return_value = 1
            if fail == 'rect': desktop.user.GetWindowRect.side_effect = lambda *a: False
            self.assertEqual([], desktop.windows(123))

    def test_visible_native_errors_are_not_silently_ignored(self):
        desktop = self.desktop_fixture()
        desktop.dwm.DwmGetWindowAttribute.return_value = 1
        with self.assertRaisesRegex(ValueError, 'cloak'): desktop.windows(123)
        desktop = self.desktop_fixture()
        desktop.user.GetWindowRect.side_effect = lambda *a: False
        with self.assertRaisesRegex(ValueError, 'rect'): desktop.windows(123)

    def test_reused_window_owner_is_rechecked_before_receipt(self):
        desktop = self.desktop_fixture()
        owners = iter([123, 999])
        def owner(hwnd, pointer):
            pointer._obj.value = next(owners)
            return 1
        desktop.user.GetWindowThreadProcessId.side_effect = owner
        self.assertEqual([], desktop.windows(123))

    def test_missing_window_bounded(self):
        process = Mock()
        process.poll.return_value = None
        clock = iter([0, 0, 11])
        with self.assertRaises(ValueError):
            s.observe(process, lambda: (1, 2), lambda pid: [], (1, 2), (0, 0, 20, 20), clock=lambda: next(clock), sleep=lambda _: None)

    def test_bad_geometry_and_early_exit(self):
        process = Mock()
        process.poll.return_value = None
        with self.assertRaises(ValueError):
            s.observe(process, lambda: (1, 2), lambda pid: [(0, 0, 50, 50)], (1, 2), (0, 0, 20, 20))
        process.poll.return_value = 0
        with self.assertRaises(ValueError):
            s.observe(process, lambda: (1, 2), Mock(), (1, 2), (0, 0, 20, 20))

    def test_assign_before_permit_and_cleanup(self):
        events = []
        process = Mock(returncode=0)
        process.communicate.side_effect = lambda *a, **kw: (events.append('permit') or (b'startup_observed\n', b''))
        def job_factory(child):
            self.assertIs(process, child)
            events.append('assign')
            return 'job'
        with patch.object(s.subprocess, 'CREATE_NO_WINDOW', 0, create=True), patch.object(s, 'finish_job') as finish:
            s.supervise([], job_factory, popen=Mock(return_value=process))
            self.assertEqual(['assign', 'permit'], events)
            finish.assert_called_once_with('job', process)

    def test_protocol_and_timeout_cleanup(self):
        for outcome in [(b'', b''), (b'startup_observed\n', b'private'), (b'arbitrary', b''), s.subprocess.TimeoutExpired('worker', 20)]:
            process = Mock(returncode=0)
            if isinstance(outcome, Exception):
                process.communicate.side_effect = outcome
            else:
                process.communicate.return_value = outcome
            with patch.object(s.subprocess, 'CREATE_NO_WINDOW', 0, create=True), patch.object(s, 'finish_job') as finish:
                with self.assertRaises((ValueError, s.subprocess.TimeoutExpired)):
                    s.supervise([], lambda p: 'job', popen=Mock(return_value=process))
                finish.assert_called_once_with('job', process)

    def test_assignment_failure_no_permit(self):
        process = Mock()
        with patch.object(s.subprocess, 'CREATE_NO_WINDOW', 0, create=True), patch.object(s, 'stop_app') as stop:
            with self.assertRaises(ValueError):
                s.supervise([], Mock(side_effect=ValueError()), popen=Mock(return_value=process))
            process.communicate.assert_not_called()
            stop.assert_called_once_with(process)

    def test_job_zero_verified_before_close(self):
        events = []
        kernel = Mock()
        kernel.TerminateJobObject.side_effect = lambda *a: events.append('terminate') or True
        def query(handle, kind, pointer, size, returned):
            pointer._obj.active = 0
            events.append('zero')
            return True
        kernel.QueryInformationJobObject.side_effect = query
        job = Mock(kernel=kernel, handle=99)
        job.close.side_effect = lambda: events.append('close')
        process = Mock()
        s.finish_job(job, process)
        self.assertEqual(['terminate', 'zero', 'close'], events)
        process.wait.assert_called_once_with(timeout=5)

    def test_job_query_failure_closes_and_fails(self):
        job = Mock(handle=99)
        job.kernel.TerminateJobObject.return_value = True
        job.kernel.QueryInformationJobObject.return_value = False
        with self.assertRaises(ValueError):
            s.finish_job(job, Mock())
        job.close.assert_called_once()

    def test_worker_observer_error_stops_retained_app(self):
        with tempfile.TemporaryDirectory() as tmp:
            entry = Path(tmp) / 'candidate.exe'
            private = Path(tmp)
            ui = Mock()
            desktop = Mock()
            desktop.snapshot.return_value = (1, 2)
            check = Mock(returncode=0, stderr=b'', stdout=b'none')
            process = Mock()
            with patch.object(s, 'validate_candidate', return_value=entry), patch.object(s, 'profile_absent'), \
                 patch.object(s, 'require_job_membership'), patch.object(s, 'validate_private'), \
                 patch.object(s, 'local_app_data', return_value=Path(tmp)), patch.object(s, 'system_tool', return_value='owned-system-tasklist'), \
                 patch.dict(s.os.environ, {'LOCALAPPDATA': tmp}), patch.object(s, 'repo_module', return_value=ui), \
                 patch.object(s, 'Desktop', return_value=desktop), patch.object(s.subprocess, 'run', return_value=check), \
                 patch.object(s.subprocess, 'Popen', return_value=process), \
                 patch.object(s.subprocess, 'CREATE_NO_WINDOW', 0, create=True), \
                 patch.object(s, 'observe', side_effect=ValueError('observer')), patch.object(s, 'stop_app') as stop:
                with self.assertRaises(ValueError):
                    s.worker(entry, 'digest', private, (0, 0, 20, 20), io.BytesIO(s.PERMIT))
                stop.assert_called_once_with(process)
                self.assertEqual([str(entry), '--page', 'snippets'], s.subprocess.Popen.call_args.args[0])
                desktop.enable_dpi.assert_called_once()
                desktop.close.assert_called_once()
                self.assertTrue((private / 'stdout.log').exists())
                self.assertTrue((private / 'stderr.log').exists())

    def test_stop_handle_kill_fallback(self):
        process = Mock()
        process.poll.return_value = None
        process.wait.side_effect = [s.subprocess.TimeoutExpired('app', 2), 1]
        s.stop_app(process)
        process.terminate.assert_called_once()
        process.kill.assert_called_once()
        self.assertEqual(2, process.wait.call_count)


if __name__ == '__main__':
    unittest.main()
