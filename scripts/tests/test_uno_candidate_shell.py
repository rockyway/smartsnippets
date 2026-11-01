"""N3 pure protocol controls. Never launches native/UI code or accesses clipboard."""
import importlib.util
import io
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import Mock, patch

SPEC = importlib.util.spec_from_file_location('shell_observer', Path(__file__).resolve().parents[1] / 'run_uno_candidate_acceptance.py')
s = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(s)


class ShellTests(unittest.TestCase):
    def test_all_closed_gates_refuse_before_artifact_access(self):
        for scene in s.SHELL_SCENES:
            with self.subTest(scene=scene), patch.object(s.sys, 'argv', [
                'observer', '--scene', scene, '--executable', 'PRIVATE.exe', '--sha256', 'a' * 64,
                '--authorized-screen-slot', '--work-area', '-1920', '1506', '0', '2586']), \
                    patch.object(s, 'validate_fixture') as validate:
                with self.assertRaisesRegex(ValueError, '^authorization$'):
                    s.main()
                validate.assert_not_called()

    def test_review_open_coexistence_refuses_before_artifact_validation(self):
        with patch.object(s, 'FULL_HOST_OBSERVER_REVIEWED', True), patch.object(s.sys, 'platform', 'win32'), \
                patch.object(s.sys, 'argv', ['observer', '--scene', 'coexistence', '--executable', 'PRIVATE.exe',
                    '--sha256', 'a' * 64, '--authorized-screen-slot', '--work-area', '-1920', '1506', '0', '2586']), \
                patch.object(s, 'validate_fixture') as validate, patch.object(s.startup, 'repo_module') as modules:
            with self.assertRaisesRegex(ValueError, '^shell_legacy_isolation_unavailable$'):
                s.main()
            validate.assert_not_called(); modules.assert_not_called()

    def test_coexistence_and_unknown_never_launch_or_open_profiles(self):
        for scene in ('coexistence', 'PRIVATE payload'):
            with patch.object(s.subprocess, 'Popen') as launch, patch.object(s.startup, 'validate_private') as validate:
                with self.assertRaises(ValueError) as error:
                    s.worker(None, None, None, None, io.BytesIO(s.startup.PERMIT), scene=scene)
                self.assertIn(str(error.exception), ('shell_legacy_isolation_unavailable', 'shell_scene'))
                launch.assert_not_called(); validate.assert_not_called()

    def test_exact_scene_phase_order_and_hidden_zero_count(self):
        expected = {
            'preview-options': ('shell-preview',),
            'hover': ('shell-hover-open', 'shell-hover-closed'),
            'tray': ('shell-tray-open', 'shell-tray-settings'),
            'close-background': ('shell-background', 'shell-reopened'),
            'message-window': ('shell-message-cycles',),
            'dpi': ('shell-dpi',),
        }
        for scene, middle in expected.items():
            phases = s.scene_phases(scene)
            self.assertEqual(('shell-ready', *middle, 'shell-verified'), tuple(p for p, _ in phases))
        self.assertEqual(0, dict(s.scene_phases('close-background'))['shell-background'])

    def test_worker_real_dispatch_permit_order_and_cleanup_for_each_scene(self):
        for scene in s.SHELL_SCENES[:-1]:
            for fail in (False, True):
                with self.subTest(scene=scene, fail=fail), tempfile.TemporaryDirectory() as tmp:
                    root = Path(tmp); events = []
                    desktop = Mock(); desktop.snapshot.return_value = (11, 22)
                    desktop.visible_windows.return_value = [201]
                    desktop.user.IsWindowVisible.return_value = True
                    desktop.user.GetDpiForWindow.return_value = 144
                    process = Mock(pid=123); process.poll.return_value = None if fail else 0
                    def launch(*args, **kwargs):
                        self.assertEqual((scene + '\n').encode(), (root / 'fixture-shell-scene-permit').read_bytes())
                        events.append('launch'); return process
                    def observe(*args, **kwargs):
                        events.append(args[2])
                        kwargs['frame_check'](args[0].pid)
                        if args[2] == 'shell-background': self.assertEqual(0, kwargs['maximum_windows'])
                        if fail: raise ValueError('geometry')
                    with patch.object(s.startup, 'require_job_membership', side_effect=lambda: events.append('job')), \
                            patch.object(s.startup, 'validate_private'), patch.object(s, 'validate_fixture', return_value=Path('fixed.exe')), \
                            patch.object(s.startup, 'repo_module'), patch.object(s.startup, 'Desktop', return_value=desktop), \
                            patch.object(s, 'require_nonprimary_monitor', return_value=144), \
                            patch.object(s.subprocess, 'Popen', side_effect=launch), \
                            patch.object(s, 'observe_phase', side_effect=observe), \
                            patch.object(s, 'require_graceful', side_effect=lambda *a: events.append('graceful')) as graceful, \
                            patch.object(s.startup, 'stop_app') as stop, patch('builtins.print'):
                        if fail:
                            with self.assertRaisesRegex(ValueError, '^geometry$'):
                                s.worker(Path('fixed.exe'), 'a' * 64, root, (-1920, 1506, 0, 2586), io.BytesIO(s.startup.PERMIT), scene)
                            stop.assert_called_once_with(process); graceful.assert_not_called()
                        else:
                            s.worker(Path('fixed.exe'), 'a' * 64, root, (-1920, 1506, 0, 2586), io.BytesIO(s.startup.PERMIT), scene)
                            self.assertEqual(['job', 'launch', *(p for p, _ in s.scene_phases(scene)), 'graceful'], events)
                            stop.assert_not_called()
                        desktop.close.assert_called_once()

    def test_input_refusal_precedes_shell_launch_and_closes_desktop(self):
        for scene in s.SHELL_SCENES[:-1]:
            with tempfile.TemporaryDirectory() as tmp:
                desktop = Mock(); helper = Mock()
                helper.quiet_input.side_effect = ValueError('input')
                with patch.object(s.startup, 'require_job_membership'), patch.object(s.startup, 'validate_private'), \
                        patch.object(s, 'validate_fixture'), patch.object(s.startup, 'repo_module', return_value=helper), \
                        patch.object(s.startup, 'Desktop', return_value=desktop), patch.object(s.subprocess, 'Popen') as launch:
                    with self.assertRaisesRegex(ValueError, '^input$'):
                        s.worker(None, None, Path(tmp), None, io.BytesIO(s.startup.PERMIT), scene)
                    launch.assert_not_called(); desktop.close.assert_called_once()

    def test_hidden_receipt_with_surviving_window_never_acknowledged(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp); (root / 'shell-background.receipt').write_text(f'123\n{root.name}\nshell-background\n')
            process = Mock(pid=123); process.poll.return_value = None
            desktop = Mock(); desktop.snapshot.return_value = (1, 2); desktop.windows.return_value = [(0, 0, 10, 10)]
            with self.assertRaisesRegex(ValueError, '^missing_shell-background$'):
                s.observe_phase(process, root, 'shell-background', desktop, (1, 2), (0, 0, 20, 20), 0,
                                maximum_windows=0, clock=Mock(side_effect=[0, 1, 13]), sleep=lambda _: None)
            self.assertFalse((root / 'shell-background.continue').exists())

    def test_dpi_refusal_precedes_acknowledgement(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp); process = Mock(pid=123); process.poll.return_value = None
            desktop = Mock(); desktop.snapshot.return_value = (1, 2); desktop.windows.return_value = []
            with self.assertRaisesRegex(ValueError, '^shell_window_dpi$'):
                s.observe_phase(process, root, 'shell-dpi', desktop, (1, 2), (0, 0, 20, 20),
                                frame_check=Mock(side_effect=ValueError('shell_window_dpi')))
            self.assertFalse((root / 'shell-dpi.continue').exists())

    def test_real_monitor_policy_rejects_primary_geometry_and_missing_dpi(self):
        for failure in (None, 'primary', 'geometry', 'dpi'):
            desktop = Mock(); desktop.user.MonitorFromRect.return_value = 9
            def info(handle, target):
                value = target._obj
                value.flags = 1 if failure == 'primary' else 0
                value.work.left, value.work.top = -1920, 1506
                value.work.right, value.work.bottom = (1 if failure == 'geometry' else 0), 2586
                return True
            desktop.user.GetMonitorInfoW.side_effect = info
            shcore = Mock()
            def dpi(handle, kind, x, y):
                x._obj.value = y._obj.value = 0 if failure == 'dpi' else 144
                return 0
            shcore.GetDpiForMonitor.side_effect = dpi
            with patch.object(s.startup.C, 'WinDLL', return_value=shcore, create=True):
                if failure:
                    with self.assertRaises(ValueError):
                        s.require_nonprimary_monitor(desktop, (-1920, 1506, 0, 2586))
                else:
                    self.assertEqual(144, s.require_nonprimary_monitor(desktop, (-1920, 1506, 0, 2586)))
            desktop.validate_work_area.assert_called_once()

    def test_private_failure_tokens_for_every_scene(self):
        for scene in s.SHELL_SCENES:
            with tempfile.TemporaryDirectory() as tmp:
                root = Path(tmp)
                s.record_failure(root, ValueError('PRIVATE content title query path ' + scene + str(root)))
                self.assertEqual({'reason': 'observer_failure'}, json.loads(next(root.glob('observer-*-failure.json')).read_text()))

    def test_planted_content_leak_refuses_every_scene(self):
        for scene in s.SHELL_SCENES:
            with tempfile.TemporaryDirectory() as tmp:
                root = Path(tmp); (root / 'graceful.receipt').write_text(f'123\n{root.name}\ngraceful\n')
                log = root / 'fixture-profile/logs/app.log'; log.parent.mkdir(parents=True)
                log.write_text(' INFO candidate_started\nacceptance synthetic')
                (root / 'stdout.log').write_bytes(b''); (root / 'stderr.log').write_bytes(b'')
                process = Mock(pid=123); process.wait.return_value = 0
                with self.assertRaisesRegex(ValueError, '^privacy$'):
                    s.require_graceful(process, root)


class ShellFrameTests(unittest.TestCase):
    def run_frame_case(self, failure=None, exit_loop=False):
        # Load the actual quiet_input implementation; all Win32 entrypoints below are stubs.
        guard = s.startup.repo_module('shell_frame_input_tests', 'scripts/run_uno_candidate_ui.py')
        guard.quiet_input = Mock(wraps=guard.quiet_input)
        armed = False
        polls = 0
        desktop = Mock(); desktop.snapshot.return_value = (11, 22)
        desktop.visible_windows.return_value = [201, 202]
        desktop.user.IsWindowVisible.side_effect = lambda hwnd: hwnd == 201
        desktop.user.GetDpiForWindow.side_effect = lambda hwnd: 192 if hwnd == 202 or (armed and failure == 'window_dpi') else 144
        desktop.user.MonitorFromRect.return_value = 9
        def monitor_info(handle, target):
            info = target._obj
            info.flags = 1 if armed and failure == 'primary' else 0
            info.work.left, info.work.top, info.work.right, info.work.bottom = -1920, 1506, 0, 2586
            return True
        desktop.user.GetMonitorInfoW.side_effect = monitor_info
        def last_input(target):
            target._obj.time = 200 if armed and failure == 'epoch' else 100
            return True
        desktop.user.GetLastInputInfo.side_effect = last_input
        desktop.user.GetAsyncKeyState.side_effect = lambda key: 0x8000 if armed and failure == 'held' and key == 17 else 0
        shcore, kernel = Mock(), Mock()
        kernel.GetTickCount.return_value = 10000
        def monitor_dpi(handle, kind, x, y):
            x._obj.value = y._obj.value = 192 if armed and failure == 'monitor_dpi' else 144
            return 0
        shcore.GetDpiForMonitor.side_effect = monitor_dpi
        def win_dll(name):
            return {'shcore': shcore, 'kernel32': kernel}[name]
        process = Mock(pid=123)
        def poll():
            nonlocal polls, armed
            polls += 1
            if exit_loop:
                armed = True
            return None if failure or polls == 1 else 0
        process.poll.side_effect = poll
        phases = []
        def observe(*args, **kwargs):
            nonlocal armed
            phases.append(args[2])
            if not exit_loop:
                armed = True
            kwargs['frame_check'](process.pid)  # Execute the real worker closure, never a substitute.
        with tempfile.TemporaryDirectory() as tmp, \
                patch.object(s.startup, 'require_job_membership'), patch.object(s.startup, 'validate_private'), \
                patch.object(s, 'validate_fixture', return_value=Path('fixed.exe')), \
                patch.object(s.startup, 'repo_module', side_effect=[Mock(), guard]), \
                patch.object(s.startup, 'Desktop', return_value=desktop), \
                patch.object(s.startup.C, 'WinDLL', side_effect=win_dll, create=True), \
                patch.object(s.subprocess, 'Popen', return_value=process), \
                patch.object(s, 'observe_phase', side_effect=observe), \
                patch.object(s, 'require_graceful') as graceful, patch.object(s.startup, 'stop_app') as stop, \
                patch.object(s.time, 'sleep'), patch('builtins.print'):
            reasons = {'epoch': 'input', 'held': 'input', 'window_dpi': 'shell_window_dpi',
                       'monitor_dpi': 'shell_monitor_dpi', 'primary': 'shell_primary_monitor'}
            if failure:
                with self.assertRaisesRegex(ValueError, '^' + reasons[failure] + '$') as error:
                    s.worker(None, None, Path(tmp), (-1920, 1506, 0, 2586), io.BytesIO(s.startup.PERMIT), 'dpi')
                if failure in ('epoch', 'held'):
                    self.assertEqual('epoch_changed' if failure == 'epoch' else 'held', error.exception.input_subreason)
                graceful.assert_not_called(); stop.assert_called_once_with(process)
            else:
                s.worker(None, None, Path(tmp), (-1920, 1506, 0, 2586), io.BytesIO(s.startup.PERMIT), 'dpi')
                graceful.assert_called_once(); stop.assert_not_called()
                # Three phase frames PLUS a frame while the process is still exiting.
                self.assertEqual(len(s.scene_phases('dpi')) + 1, desktop.visible_windows.call_count)
                desktop.user.GetDpiForWindow.assert_any_call(201)
                self.assertNotIn(unittest.mock.call(202), desktop.user.GetDpiForWindow.call_args_list)
            self.assertEqual(unittest.mock.call(desktop), guard.quiet_input.call_args_list[0])
            self.assertTrue(all(call == unittest.mock.call(desktop, 100) for call in guard.quiet_input.call_args_list[1:]))
            desktop.close.assert_called_once()
            if exit_loop:
                self.assertEqual([phase for phase, _ in s.scene_phases('dpi')], phases)
                self.assertEqual(len(phases) + 3, guard.quiet_input.call_count)
            elif failure:
                self.assertEqual(['shell-ready'], phases)

    def test_real_phase_frame_refuses_epoch_held_window_monitor_dpi_and_primary_changes(self):
        for failure in ('epoch', 'held', 'window_dpi', 'monitor_dpi', 'primary'):
            with self.subTest(failure=failure):
                self.run_frame_case(failure)

    def test_real_exit_loop_frame_refuses_changes_after_all_phase_frames(self):
        for failure in ('epoch', 'held', 'window_dpi', 'monitor_dpi', 'primary'):
            with self.subTest(failure=failure):
                self.run_frame_case(failure, exit_loop=True)

    def test_success_checks_visible_window_and_preserves_epoch_during_exit_loop(self):
        self.run_frame_case(exit_loop=True)


if __name__ == '__main__':
    unittest.main()
