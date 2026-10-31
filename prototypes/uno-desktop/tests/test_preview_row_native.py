"""Pure receipt/source contracts; never starts native processes or input APIs."""
import contextlib
import hashlib
import io
import subprocess
import tempfile
from unittest.mock import Mock, patch
import sys
import unittest
from pathlib import Path

BASE = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(BASE))
import run_ui_smoke as smoke


class NativeRowTests(unittest.TestCase):
    def test_receipt_requires_unique_owned_popup_ready_and_natural_success(self):
        lines = smoke.PREVIEW_ROW_NATIVE_RECEIPTS.copy()
        lines.insert(3, 'ui_preview_row_native_ready hwnd=123')
        text = '\n'.join(lines)
        self.assertTrue(smoke.preview_row_native_completed(text, '', 0))
        for output, error, code in [(text, '', 1), (text, 'error', 0),
                                    ('\n'.join(smoke.PREVIEW_ROW_NATIVE_RECEIPTS), '', 0),
                                    (text + '\nui_preview_row_native_ready hwnd=124', '', 0)]:
            self.assertFalse(smoke.preview_row_native_completed(output, error, code))

    def test_native_scene_waits_for_itemclick_not_programmatic_activation(self):
        source = (BASE / 'PreviewProbe.cs').read_text()
        block = source.split('if (_nativeRow)', 1)[1].split('else _ = _page.ActivateSelectedRowAsync();', 1)[0]
        self.assertNotIn('ActivateSelectedRowAsync', block)
        self.assertIn('OwnedPreviewRow', block)
        self.assertIn('_page!.ItemClickEvents == (_nativeRow ? 1 : 0)', source)
        self.assertIn('_shown = true;', source)
        self.assertIn('_rowMode && _shown && !_shows.IsWarming && _surface!.IsVisible && !_surface.IsWarmupCloaked && _page!.IsReady', source)

    def test_row_locator_is_allowlisted_read_only(self):
        source = (BASE / 'prepare_owned_focus.py').read_text()
        self.assertIn("'OwnedPreviewRow'", source)
        self.assertIn('AutomationIdProperty', source)
        self.assertNotIn('InvokePattern', source)
        self.assertIn('require_foreground=row_hwnd is None', source)

    def test_ready_receipt_exact_position_count_and_protocol(self):
        base = smoke.PREVIEW_ROW_NATIVE_RECEIPTS.copy()
        ready = 'ui_preview_row_native_ready hwnd=123'
        for position in range(9):
            candidate = base.copy()
            candidate.insert(position, ready)
            self.assertEqual(position == 3, smoke.preview_row_native_completed('\n'.join(candidate), '', 0))
        for bad in ('0', 'bad', '-1', '01', '123 extra'):
            candidate = base.copy()
            candidate.insert(3, 'ui_preview_row_native_ready hwnd=' + bad)
            self.assertFalse(smoke.preview_row_native_completed('\n'.join(candidate), '', 0))
            candidate.insert(3, ready)
            self.assertFalse(smoke.preview_row_native_completed('\n'.join(candidate), '', 0))


class NativeRowRunnerTests(unittest.TestCase):
    """Actual CLI/main branch; disposable files and fake OS workers only."""
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.base = Path(self.temp.name).resolve()
        self.entry = self.base / 'bin' / 'probe' / 'SmartSnippets.UnoProbe.exe'
        self.entry.parent.mkdir(parents=True)
        self.entry.write_bytes(b'non executable test fixture')
        self.private = self.base / 'private'
        self.private.mkdir()
        self.digest = hashlib.sha256(self.entry.read_bytes()).hexdigest()
        self.args = ['run_ui_smoke.py', '--executable', str(self.entry), '--sha256', self.digest,
                     '--authorized-screen-slot', '--preview-row-native']
        self.process = Mock(pid=123)
        self.process.wait.return_value = 0
        self.finished = False

    def receipts(self, hwnd=77):
        lines = smoke.PREVIEW_ROW_NATIVE_RECEIPTS.copy()
        lines.insert(3, f'ui_preview_row_native_ready hwnd={hwnd}')
        return '\n'.join(lines) + '\n'

    def invoke(self, *, timeout=False, recovery=True, prepared=True, final_hwnd=77, observer_timeout=False):
        self.calls = []
        def start(args, **kwargs):
            self.launch_args = args
            self.launch_options = kwargs
            kwargs['stdout'].write(self.receipts().encode())
            kwargs['stdout'].flush()
            return self.process
        def worker(args, **kwargs):
            self.calls.append((args, kwargs))
            if '--window-count' in args:
                if observer_timeout:
                    raise subprocess.TimeoutExpired('observer', 3)
                return subprocess.CompletedProcess(args, 0, '1\n', '')
            if Path(args[1]).name == 'prepare_owned_focus.py':
                (self.private / 'focus.json').write_text('{"synthetic":true}')
                if timeout:
                    raise subprocess.TimeoutExpired('prepare', kwargs['timeout'], output='private secret')
                (self.private / 'stdout.log').write_text(self.receipts(final_hwnd))
                self.finished = True
                return subprocess.CompletedProcess(args, 0 if prepared else 1,
                                                   'owned_focus_prepared\n' if prepared else '', '')
            if Path(args[1]).name == 'recover_owned_mouse.py':
                self.assertTrue((self.private / 'focus.json').exists())
                self.assertFalse(self.process.terminate.called)
                return subprocess.CompletedProcess(args, 0 if recovery else 1,
                                                   'owned_mouse_recovery_complete\n' if recovery else '', '')
            raise AssertionError(args)
        self.process.poll.side_effect = lambda: 0 if self.finished else None
        output = io.StringIO()
        with patch.object(smoke.sys, 'platform', 'win32'), patch.object(sys, 'argv', self.args), \
             patch.object(smoke, 'BASE', self.base), \
             patch.object(smoke, 'inspect_executable', return_value=[]), \
             patch.object(smoke, 'private_directory', return_value=self.private), \
             patch.object(subprocess, 'CREATE_NO_WINDOW', 0x08000000, create=True), \
             patch.object(subprocess, 'Popen', side_effect=start) as launch, \
             patch.object(subprocess, 'run', side_effect=worker), \
             contextlib.redirect_stdout(output), contextlib.redirect_stderr(io.StringIO()):
            self.launch = launch
            result = smoke.main()
        return result, output.getvalue()

    def test_actual_native_row_branch_passes_pinned_hwnd_without_enter(self):
        result, output = self.invoke()
        self.assertEqual(0, result)
        self.assertEqual([str(self.entry), '--authorized-probe-ui', '--preview-row-native'], self.launch_args)
        self.assertEqual(subprocess.DEVNULL, self.launch_options['stdin'])
        self.process.stdin.write.assert_not_called()
        self.process.terminate.assert_not_called()
        prepare, options = self.calls[1]
        self.assertEqual(['--row-hwnd', '77'], prepare[-2:])
        self.assertIn(self.digest, prepare)
        self.assertLessEqual(options['timeout'], 20)
        self.assertEqual(5, self.calls[2][1]['timeout'])
        self.assertIn('mouse_recovery=COMPLETE', output)
        self.assertIn('ui_preview_row_native_result=PASS graceful_exit=true', output)

    def test_actual_prepare_timeout_replays_journal_and_emits_structured_failure(self):
        result, output = self.invoke(timeout=True)
        self.assertEqual(1, result)
        self.assertEqual('owned_focus_prepare_timeout\n', (self.private / 'prepare.log').read_text())
        self.assertTrue((self.private / 'recovery.log').exists())
        self.assertEqual(3, len(self.calls))
        self.process.terminate.assert_called_once_with()
        for receipt in ('ui_smoke_window=OBSERVED', 'private_evidence_token=',
                        'mouse_recovery=COMPLETE', 'ui_preview_row_native_result=FAIL graceful_exit=false'):
            self.assertIn(receipt, output)
        self.assertNotIn('private secret', output)

    def test_actual_timeout_failed_recovery_reports_incomplete_and_stops_child(self):
        result, output = self.invoke(timeout=True, recovery=False)
        self.assertEqual(1, result)
        self.assertIn('mouse_recovery=INCOMPLETE', output)
        self.assertIn('ui_preview_row_native_result=FAIL graceful_exit=false', output)
        self.process.terminate.assert_called_once_with()

    def test_actual_complete_receipts_cannot_pass_changed_hwnd(self):
        result, output = self.invoke(final_hwnd=78)
        self.assertEqual(1, result)
        self.assertIn('ui_preview_row_native_result=FAIL', output)

    def test_actual_complete_receipts_cannot_pass_failed_preparation(self):
        result, output = self.invoke(prepared=False)
        self.assertEqual(1, result)
        self.assertIn('ui_preview_row_native_result=FAIL', output)

    def test_actual_complete_receipts_cannot_pass_failed_recovery(self):
        result, output = self.invoke(recovery=False)
        self.assertEqual(1, result)
        self.assertIn('mouse_recovery=INCOMPLETE', output)
        self.assertIn('ui_preview_row_native_result=FAIL', output)

    def test_actual_observer_timeout_does_not_prepare_or_replay_without_journal(self):
        result, output = self.invoke(observer_timeout=True)
        self.assertEqual(1, result)
        self.assertEqual(1, len(self.calls))
        self.assertIn('ui_smoke_window=NOT_OBSERVED', output)
        self.assertNotIn('mouse_recovery=', output)
        self.process.terminate.assert_called_once_with()

    def test_actual_cli_exclusion_and_missing_authorization_refuse_before_launch(self):
        self.args.append('--preview')
        with self.assertRaises(SystemExit) as error:
            self.invoke()
        self.assertEqual(2, error.exception.code)
        self.launch.assert_not_called()
        self.args.remove('--preview')
        self.args.remove('--authorized-screen-slot')
        with self.assertRaisesRegex(ValueError, 'authorized'):
            self.invoke()
        self.launch.assert_not_called()
