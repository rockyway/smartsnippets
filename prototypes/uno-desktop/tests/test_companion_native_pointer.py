"""Portable receipts plus fake Win32 boundary; never launches native UI/input."""
import sys
import json
import unittest
from pathlib import Path
from unittest.mock import patch

BASE = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(BASE))
import run_ui_smoke as smoke
import owned_pointer_hover as pointer
import test_owned_mouse_focus as mouse_tests


def receipts():
    return '\n'.join([
        'ui_companion_pointer_started', 'ui_companion_pointer_ready hwnd=77',
        'ui_companion_pointer_row_entered', 'ui_companion_pointer_opened hwnd=99',
        'ui_companion_pointer_row_exited', 'ui_companion_pointer_surface_entered',
        'ui_companion_pointer_held geometry=EXTERNAL foreground=UNCHANGED',
        'ui_companion_pointer_surface_exited', 'ui_companion_pointer_hidden',
        'ui_companion_pointer_native_closed',
        'ui_companion_pointer_complete managed_closed=2 native_absence=1 clipboard=UNTOUCHED dispatch=0'])


class CompanionNativePointerTests(unittest.TestCase):
    def test_exact_receipts_reject_disconnected_or_programmatic_hops(self):
        text = receipts()
        self.assertTrue(smoke.companion_pointer_completed(text, '', 0))
        for index in range(11):
            lines = text.splitlines()
            del lines[index]
            self.assertFalse(smoke.companion_pointer_completed('\n'.join(lines), '', 0))
        for bad in (text + '\n' + text.splitlines()[2], text.replace('hwnd=99', 'hwnd=77'),
                    text.replace('hwnd=77', 'hwnd=0'), text.replace('hwnd=99', 'hwnd=01'),
                    '\n'.join(smoke.COMPANION_RECEIPTS)):
            self.assertFalse(smoke.companion_pointer_completed(bad, '', 0))
        self.assertFalse(smoke.companion_pointer_completed(text, 'error', 0))
        self.assertFalse(smoke.companion_pointer_completed(text, '', 1))

    def test_native_composition_has_no_programmatic_entry_or_handoff(self):
        source = (BASE / 'CompanionSurfaceProbe.Pointer.cs').read_text()
        self.assertNotIn('OpenPinned', source)
        self.assertNotIn('_policy.Enter', source)
        self.assertIn('_pointer!.RowEntered()', source)
        self.assertIn('_pointer!.SurfaceEntered()', source)
        self.assertIn('_surface is { IsVisible: false }', source)


@unittest.skipUnless(sys.platform == 'win32', 'Fake Win32 ABI only')
class PointerBoundaryTests(unittest.TestCase):
    def setUp(self):
        self.fake = mouse_tests.OwnedMouseFocusTests()
        self.fake.setUp()
        self.addCleanup(self.fake.doCleanups)
        self.fake.user.GetAncestor.side_effect = [77, 77, 77, 77, 99, 99, 77, 77]
        self.fake.user.WindowFromPoint.side_effect = lambda point: 0 if (point.x, point.y) == (10, 20) else 88
        def pid(hwnd, output):
            if not hwnd:
                output._obj.value = 0
                return 0
            return self.fake.pid(hwnd, output)
        self.fake.user.GetWindowThreadProcessId.side_effect = pid
        self.log = self.fake.journal.parent / 'stdout.log'
        self.log.write_text(receipts())
        self.log.with_name("stderr.log").write_text("")

    def invoke(self, control=None):
        import os
        entry = Path('fixture.exe').resolve()
        with patch.object(pointer, 'validate_image', return_value=entry), \
             patch.object(pointer, 'query_windows_process', return_value=(os.getppid(), entry)), \
             patch.object(pointer, 'control_point', side_effect=control if control is not None else [[.5, .5], [.5, .2], [.5, .5]]), \
             patch.object(pointer.ctypes, 'WinDLL', side_effect=[self.fake.user, self.fake.kernel]):
            pointer.run(123, entry, 'unused', self.fake.journal, self.log, 77)

    def test_pointer_only_waits_for_hidden_then_restores_without_keys_buttons_or_zorder(self):
        self.invoke()
        self.fake.user.SendInput.assert_not_called()
        self.fake.user.SetWindowPos.assert_not_called()
        self.assertEqual([10, 20], self.fake.cursor)
        self.assertEqual(5, self.fake.user.SetCursorPos.call_count)

    def test_wrong_leaf_never_moves(self):
        self.fake.user.GetAncestor.side_effect = None
        self.fake.user.GetAncestor.return_value = 999
        with self.assertRaisesRegex(ValueError, 'leaf hit-test'):
            self.invoke()
        self.fake.user.SetCursorPos.assert_not_called()

    def test_held_key_refuses_without_mutation(self):
        self.fake.user.GetAsyncKeyState.return_value = 0x8000
        with self.assertRaisesRegex(ValueError, 'Exclusive idle'):
            self.invoke()
        self.fake.user.SetCursorPos.assert_not_called()
        self.assertFalse(self.fake.journal.exists())

    def test_missing_hidden_receipt_retains_cursor_and_recovery_journal(self):
        original = pointer.wait_line
        def wait(log, prefix, deadline):
            if prefix == 'ui_companion_pointer_hidden':
                raise ValueError('native hidden missing')
            return original(log, prefix, deadline)
        with patch.object(pointer, 'wait_line', side_effect=wait), self.assertRaisesRegex(ValueError, 'native hidden missing'):
            self.invoke()
        self.assertNotEqual([10, 20], self.fake.cursor)
        self.assertTrue(self.fake.journal.exists())
        self.fake.user.SendInput.assert_not_called()

class CompanionPointerRunnerTests(unittest.TestCase):
    def invoke(self, *, timeout=False, alive_after_prepared=False):
        import contextlib
        import hashlib
        import io
        import subprocess
        import tempfile
        from unittest.mock import Mock
        with tempfile.TemporaryDirectory() as temp:
            base = Path(temp).resolve()
            entry = base / 'bin' / 'probe' / 'SmartSnippets.UnoProbe.exe'
            entry.parent.mkdir(parents=True)
            entry.write_bytes(b'synthetic non executable')
            private = base / 'private'
            private.mkdir()
            digest = hashlib.sha256(entry.read_bytes()).hexdigest()
            process = Mock(pid=123)
            process.wait.return_value = 0
            if alive_after_prepared:
                process.wait.side_effect = [subprocess.TimeoutExpired("child", 1), 0, 0]
            finished = False
            def start(args, **kwargs):
                self.launch_args = args
                self.assertEqual(subprocess.DEVNULL, kwargs['stdin'])
                kwargs['stdout'].write(receipts().encode())
                kwargs['stdout'].flush()
                return process
            def worker(args, **kwargs):
                nonlocal finished
                if '--window-count' in args:
                    return subprocess.CompletedProcess(args, 0, '1\n', '')
                if Path(args[1]).name == 'owned_pointer_hover.py':
                    self.assertEqual(['--hwnd', '77'], args[-2:])
                    self.assertIn(digest, args)
                    (private / 'focus.json').write_text('{}')
                    if timeout:
                        raise subprocess.TimeoutExpired('pointer', 20)
                    finished = not alive_after_prepared
                    return subprocess.CompletedProcess(args, 0, 'owned_pointer_prepared\n', '')
                if Path(args[1]).name == 'recover_owned_mouse.py':
                    self.assertEqual(timeout or alive_after_prepared, process.terminate.called)
                    if timeout:
                        process.wait.assert_called()
                    return subprocess.CompletedProcess(args, 0, 'owned_mouse_recovery_complete\n', '')
                raise AssertionError(args)
            process.poll.side_effect = lambda: 0 if finished else None
            args = ['run_ui_smoke.py', '--executable', str(entry), '--sha256', digest,
                    '--authorized-screen-slot', '--companion-native-pointer']
            output = io.StringIO()
            with patch.object(smoke.sys, 'platform', 'win32'), patch.object(sys, 'argv', args), \
                 patch.object(smoke, 'BASE', base), patch.object(smoke, 'inspect_executable', return_value=[]), \
                 patch.object(smoke, 'private_directory', return_value=private), \
                 patch.object(subprocess, 'CREATE_NO_WINDOW', 0x08000000, create=True), \
                 patch.object(subprocess, 'Popen', side_effect=start), \
                 patch.object(subprocess, 'run', side_effect=worker), contextlib.redirect_stdout(output):
                result = smoke.main()
            process.stdin.write.assert_not_called()
            self.assertIn('mouse_recovery=COMPLETE', output.getvalue())
            return result, output.getvalue()

    def test_actual_runner_pointer_scene_uses_guarded_child_and_receipt_validation(self):
        result, output = self.invoke()
        self.assertEqual(0, result)
        self.assertEqual('--companion-native-pointer', self.launch_args[-1])
        self.assertIn('ui_companion_native_pointer_result=PASS', output)

    def test_pointer_timeout_stops_owned_surfaces_before_cursor_recovery_and_fails(self):
        result, output = self.invoke(timeout=True)
        self.assertEqual(1, result)
        self.assertIn('ui_companion_native_pointer_result=FAIL', output)

    def test_prepared_still_alive_child_stops_before_recovery(self):
        result, output = self.invoke(alive_after_prepared=True)
        self.assertEqual(1, result)
        self.assertIn('ui_companion_native_pointer_result=FAIL', output)
