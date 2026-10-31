"""Fake API and file-only falsification of native pointer safety. No native input."""
import base64
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import time
import unittest
from unittest.mock import patch
import test_companion_native_pointer as scene
import owned_pointer_hover as pointer
import recover_owned_mouse as recovery


class ReceiptSafetyTests(unittest.TestCase):
    def test_stderr_and_malformed_receipts_fail_immediately(self):
        with tempfile.TemporaryDirectory() as temp:
            log = Path(temp) / 'stdout.log'
            error = log.with_name('stderr.log')
            error.write_text('')
            for line in ('ui_companion_pointer_opened hwnd=0', 'ui_companion_pointer_opened hwnd=01',
                         'ui_companion_pointer_opened hwnd=99 extra', 'ui_companion_pointer_opened',
                         'ui_companion_pointer_opened hwnd=99\nui_companion_pointer_opened hwnd=99'):
                log.write_text(line)
                with self.assertRaisesRegex(ValueError, 'receipt failed'):
                    pointer.wait_line(log, 'ui_companion_pointer_opened', time.monotonic() + 1)
            error.write_text('ui_companion_failed stage=2')
            with self.assertRaisesRegex(ValueError, 'stderr failure'):
                pointer.wait_line(log, 'ui_companion_pointer_opened', time.monotonic() + 1)

    def test_uia_uses_system_sta_and_strong_named_assemblies(self):
        response = subprocess.CompletedProcess([], 0, '[0.5,0.4]', '')
        with patch.object(pointer.subprocess, 'run', return_value=response) as worker, \
             patch.object(pointer.subprocess, 'CREATE_NO_WINDOW', 0x08000000, create=True):
            self.assertEqual([.5, .4], pointer.control_point(123, 99, 'OwnedCompanionSurface'))
        args, options = worker.call_args
        expected = Path(os.environ.get('SystemRoot', r'C:\Windows')) / 'System32/WindowsPowerShell/v1.0/powershell.exe'
        self.assertEqual(str(expected), args[0][0])
        self.assertIn('-STA', args[0])
        self.assertEqual(subprocess.DEVNULL, options['stdin'])
        script = base64.b64decode(args[0][-1]).decode('utf-16le')
        self.assertIn('PublicKeyToken=31bf3856ad364e35', script)
        self.assertIn("$ErrorActionPreference = 'Stop'", script)

    def test_flush_foreground_and_detach_precede_receipts(self):
        source = (scene.BASE / 'CompanionSurfaceProbe.Pointer.cs').read_text()
        self.assertLess(source.index('Require(!_surface!.IsForeground'), source.index('PointerReceipt("ui_companion_pointer_held'))
        self.assertLess(source.index('DetachPointerCallbacks();'), source.index('PointerReceipt("ui_companion_pointer_hidden'))
        receipt = source.split('private static void PointerReceipt', 1)[1].split('private static void ReceiptWindow', 1)[0]
        self.assertIn('Console.Out.Flush();', receipt)
        self.assertIn('Schedule, Open, Hide, PointerReceipt', (scene.BASE / 'CompanionSurfaceProbe.cs').read_text())

    def test_linked_parent_refused_before_resolve(self):
        with tempfile.TemporaryDirectory(prefix='ss-uno018-ui-') as temp:
            journal = Path(temp) / 'focus.json'
            original = Path.is_symlink
            with patch.object(Path, 'is_symlink', lambda path: path == journal.parent or original(path)):
                with self.assertRaisesRegex(ValueError, 'Linked recovery'):
                    recovery.private_journal_path(journal)


@unittest.skipUnless(sys.platform == 'win32', 'Fake Win32 ABI only')
class PointerRecoveryTests(unittest.TestCase):
    def setUp(self):
        self.boundary = scene.PointerBoundaryTests()
        self.boundary.setUp()
        self.fake = self.boundary.fake
        self.log = self.boundary.log
        self.addCleanup(self.boundary.doCleanups)

    def invoke(self):
        self.boundary.invoke()

    def recover(self, live=False):
        self.fake.user.GetWindowThreadProcessId.side_effect = self.fake.pid if live else lambda hwnd, output: 0
        entry = Path('fixture.exe').resolve()
        with patch.object(recovery, 'validate_image', return_value=entry), \
             patch.object(recovery, 'query_windows_process', return_value=(os.getppid(), entry)), \
             patch.object(recovery.ctypes, 'WinDLL', side_effect=[self.fake.user, self.fake.kernel]):
            recovery.recover(self.fake.journal, 123, entry, 'unused')

    def abort_after_first_move(self):
        with patch.object(pointer, 'wait_line', side_effect=ValueError('abort after first move')):
            with self.assertRaises(ValueError):
                self.invoke()

    def test_every_callback_awaited_and_intra_main_leave_precedes_handoff(self):
        order = []
        original_wait = pointer.wait_line
        def wait(log, prefix, deadline):
            order.append(prefix)
            return original_wait(log, prefix, deadline)
        def move(x, y):
            order.append(('move', x, y))
            return self.fake.set_cursor(x, y)
        self.fake.user.SetCursorPos.side_effect = move
        with patch.object(pointer, 'wait_line', side_effect=wait):
            self.invoke()
        self.assertEqual(['ui_companion_pointer_row_entered', 'ui_companion_pointer_opened',
            'ui_companion_pointer_row_exited', 'ui_companion_pointer_surface_entered',
            'ui_companion_pointer_held geometry=EXTERNAL foreground=UNCHANGED',
            'ui_companion_pointer_surface_exited', 'ui_companion_pointer_hidden'],
            [value for value in order if isinstance(value, str)])
        self.assertEqual(('move', -1500, 1660), order[3])
        self.assertEqual('ui_companion_pointer_row_exited', order[4])

    def test_surface_uia_is_precomputed_while_still_over_row(self):
        queried = []
        def control(pid, hwnd, automation_id):
            queried.append(automation_id)
            if automation_id == 'OwnedCompanionSurface':
                self.assertEqual(1, self.fake.user.SetCursorPos.call_count)
                self.assertEqual([-1500, 1900], self.fake.cursor)
            return [.5, .2] if automation_id == 'OwnedCompanionLeave' else [.5, .5]
        self.boundary.invoke(control=control)
        self.assertEqual(['OwnedCompanionRow', 'OwnedCompanionLeave', 'OwnedCompanionSurface'], queried)

    def test_unowned_companion_is_rejected_before_uia(self):
        previous = self.fake.user.GetWindowThreadProcessId.side_effect
        def pid(hwnd, output):
            if hwnd == 99:
                output._obj.value = 999
                return 1
            return previous(hwnd, output)
        self.fake.user.GetWindowThreadProcessId.side_effect = pid
        queries = []
        def control(pid, hwnd, automation_id):
            queries.append(hwnd)
            return [.5, .5]
        with self.assertRaisesRegex(ValueError, 'Companion ownership'):
            self.boundary.invoke(control=control)
        self.assertEqual([77, 77], queries)

    def test_original_cursor_on_owned_probe_refused_before_journal(self):
        self.fake.user.WindowFromPoint.side_effect = None
        self.fake.user.WindowFromPoint.return_value = 88
        with self.assertRaisesRegex(ValueError, 'Original cursor'):
            self.invoke()
        self.fake.user.SetCursorPos.assert_not_called()
        self.assertFalse(self.fake.journal.exists())

    def test_companion_receipt_requires_distinct_positive_owned_hwnd(self):
        for token in ('77', '0', '-1', '01', str(2**64)):
            with self.subTest(token=token):
                self.setUp()
                self.log.write_text(scene.receipts().replace('opened hwnd=99', 'opened hwnd=' + token))
                with self.assertRaises(ValueError):
                    self.invoke()
                self.assertEqual(1, self.fake.user.SetCursorPos.call_count)

    def test_failed_second_setcursor_recovers_previous_without_zorder(self):
        def move(x, y):
            if self.fake.user.SetCursorPos.call_count == 2:
                return 0
            return self.fake.set_cursor(x, y)
        self.fake.user.SetCursorPos.side_effect = move
        with self.assertRaisesRegex(ValueError, 'move failed'):
            self.invoke()
        data = json.loads(self.fake.journal.read_text())
        self.assertEqual(self.fake.cursor, data['previous'])
        self.assertNotEqual(data['previous'], data['point'])
        self.assertFalse(data['wasTopmost'])
        self.assertFalse(data['zorderChanged'])
        self.recover()
        self.assertEqual([10, 20], self.fake.cursor)
        self.fake.user.SetWindowPos.assert_not_called()

    def test_midtraversal_final_guard_failure_preserves_recoverable_previous(self):
        original = recovery.read_journal
        reads = 0
        def read(*args):
            nonlocal reads
            data = original(*args)
            reads += 1
            if reads == 2:
                self.fake.user.GetAncestor.side_effect = None
                self.fake.user.GetAncestor.return_value = 999
            return data
        with patch.object(recovery, 'read_journal', side_effect=read), self.assertRaisesRegex(ValueError, 'Final pointer guard'):
            self.invoke()
        self.recover()
        self.assertEqual([10, 20], self.fake.cursor)
        self.fake.user.SetWindowPos.assert_not_called()

    def test_crash_after_os_move_recovers_planned_candidate(self):
        def move(x, y):
            self.fake.set_cursor(x, y)
            if self.fake.user.SetCursorPos.call_count == 2:
                raise RuntimeError('simulated worker loss after OS move')
            return 1
        self.fake.user.SetCursorPos.side_effect = move
        with self.assertRaisesRegex(RuntimeError, 'worker loss'):
            self.invoke()
        self.assertEqual(self.fake.cursor, json.loads(self.fake.journal.read_text())['point'])
        self.recover()
        self.assertEqual([10, 20], self.fake.cursor)
        self.fake.user.SetWindowPos.assert_not_called()

    def test_recovery_preserves_takeover_and_reports_pending(self):
        self.abort_after_first_move()
        self.fake.cursor[:] = [111, 222]
        with self.assertRaisesRegex(ValueError, 'pending'):
            self.recover()
        self.assertEqual([111, 222], self.fake.cursor)
        self.fake.user.SetWindowPos.assert_not_called()

    def test_recovery_refuses_changed_physical_input_epoch(self):
        self.abort_after_first_move()
        self.fake.user.GetLastInputInfo.side_effect = lambda out: (setattr(out._obj, 'tick', 1) or 1)
        count = self.fake.user.SetCursorPos.call_count
        with self.assertRaisesRegex(ValueError, 'pending'):
            self.recover()
        self.assertEqual(count, self.fake.user.SetCursorPos.call_count)
        self.fake.user.SetWindowPos.assert_not_called()

    def test_pointer_no_zorder_recovery_even_when_owned_window_still_exists(self):
        self.abort_after_first_move()
        self.recover(live=True)
        self.assertEqual([10, 20], self.fake.cursor)
        self.fake.user.SetWindowPos.assert_not_called()
        self.fake.user.SendInput.assert_not_called()

    def test_worker_loss_after_durable_journal_before_setcursor_restores_previous(self):
        original = recovery.read_journal
        reads = 0
        def read(*args):
            nonlocal reads
            data = original(*args)
            reads += 1
            if reads == 2:
                raise RuntimeError('worker killed before SetCursorPos')
            return data
        with patch.object(recovery, 'read_journal', side_effect=read), self.assertRaisesRegex(RuntimeError, 'worker killed'):
            self.invoke()
        self.assertEqual(1, self.fake.user.SetCursorPos.call_count)
        self.recover()
        self.assertEqual([10, 20], self.fake.cursor)
        self.fake.user.SetWindowPos.assert_not_called()

    def test_changed_epoch_after_setcursor_never_silently_rebases_journal(self):
        def move(x, y):
            self.fake.set_cursor(x, y)
            self.fake.user.GetLastInputInfo.side_effect = lambda out: (setattr(out._obj, 'tick', 1) or 1)
            return 1
        self.fake.user.SetCursorPos.side_effect = move
        with self.assertRaisesRegex(ValueError, 'epoch changed'):
            self.invoke()
        self.assertEqual(0, json.loads(self.fake.journal.read_text())['inputTick'])
        with self.assertRaises(recovery.PointerRecoveryPending):
            self.recover()
        self.assertEqual(1, self.fake.user.SetCursorPos.call_count)
        self.fake.user.SetWindowPos.assert_not_called()
