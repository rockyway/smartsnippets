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
                    process = Mock(pid=123); process.poll.return_value = None if fail else 0
                    def launch(*args, **kwargs):
                        self.assertEqual((scene + '\n').encode(), (root / 'fixture-shell-scene-permit').read_bytes())
                        events.append('launch'); return process
                    def observe(*args, **kwargs):
                        events.append(args[2])
                        self.assertIsNotNone(kwargs['frame_check'])
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


if __name__ == '__main__':
    unittest.main()
